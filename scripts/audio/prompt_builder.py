#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""prompt_builder.py — bom_id → ElevenLabs 전송 프롬프트를 **조립**한다.

이 파이프라인의 앞단 게이트. 후공정(정규화·크로스페이드)은 못 만든 음원을 좋게 만들지 못하므로,
품질은 여기서 갈린다.

조립 방식 = 하이브리드:
  · 규격(길이·루프·톤·금칙어)은 **코드가 결정론적으로** 박는다 — 사람이 기억할 필요가 없다.
  · 악기·장르·무드 같은 창작 부분만 **감독 노트**로 받는다.
금칙어(`no ending fade` 등)가 빠지면 전송을 차단한다 — 페이드가 붙으면 후공정 루프 처리가 통째로 무의미해진다.

    python scripts/audio/prompt_builder.py build --bom-id bgm_day_loop --note "..."
    python scripts/audio/prompt_builder.py build --bom-id sfx_pickup --length 1.2
    python scripts/audio/prompt_builder.py check                 # 전 프롬프트 금칙어 검사
"""
import argparse
import datetime
import os
import re
import sys

for _s in (sys.stdout, sys.stderr):
    if hasattr(_s, "reconfigure"):
        _s.reconfigure(encoding="utf-8")  # Windows cp949 콘솔 대응

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bom_audio  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PROMPT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "prompts")
INTENT_PATH = os.path.join(ROOT, "docs", "INTENT.md")

# 규격 문서(INTENT·BOM)의 한국어 값 → 전송용 영문구.
# ElevenLabs에 한국어를 섞어 보내면 해석이 흔들린다 — 규격이 바뀌면 여기 매핑을 추가한다.
PHRASE_EN = {
    # INTENT
    "다크코미디": "dark comedy — wry, never heroic",
    "쫓기는 밑바닥 노동자": "a bottom-rung worker perpetually chased by the clock",
    "늦지마!! — 쫓기며 웃픈 하루": "a hectic day that is funny and bitter at once",
    "3D 픽셀아트 사이드뷰 배달 아케이드 (2.5D 앵글)": "3D pixel-art side-view delivery arcade game",
    # BOM §8 BGM 용도
    "낮 거리 BGM": "a daytime city street",
    "밤 변주": "the same city street at night",
}
REFERENCE_EN = (
    "3D pixel-art look (lighting and shadows quantized into pixels), "
    "Korean city street with shop signs, convenience stores and churches; "
    "grounded slice-of-life dark comedy."
)
SFX_STYLE_EN = "Style: retro pixel-art game sound design, dark comedy tone, clean and readable in a busy mix."

# 금칙어 = 규격상 반드시 프롬프트에 살아 있어야 하는 지시.
REQUIRED = {
    "bgm": ["loopable", "no intro", "no ending fade", "no vocals"],
    "sfx": ["no background music", "no vocals"],
}
CONSTRAINTS = {
    "bgm": "Clean loopable structure, no intro sweep, no ending fade, no vocals.",
    "sfx": "Single isolated sound effect, dry and close, no background music, no vocals, no long reverb tail.",
}
DEFAULT_LENGTH = {"bgm": 75.0, "sfx": 2.0}
SFX_RANGE = (0.5, 5.0)   # text_to_sound_effects 제약

NOTE_BEGIN, NOTE_END = "<!-- NOTE:BEGIN -->", "<!-- NOTE:END -->"
PROMPT_BEGIN, PROMPT_END = "<!-- PROMPT:BEGIN -->", "<!-- PROMPT:END -->"

SEED_NOTES = {
    "bgm_day_loop": (
        "Upbeat but slightly weary city-pop groove, mid-tempo 108 BPM, light shuffle drums, "
        "muted funk guitar, warm electric bass, soft Rhodes chords, subtle retro synth lead. "
        "Melody core in the mid range so a low-pass night variant still holds up."
    ),
    "bgm_night_var": (
        "Night variant of the day theme: same chord motion, slower feel, muted drums, "
        "soft pad wash, distant reverb, sparse melody."
    ),
    "sfx_delivery_ok": "A bright two-tone doorbell ding-dong followed by a small coin chime.",
    "sfx_late_buzzer": "A short low dull buzzer, deflating and unpleasant, like a failed game show answer.",
    "sfx_pickup": "A cardboard box being grabbed and lifted, light paper scuff and a soft thud.",
    "sfx_footstep": "A single footstep on concrete pavement, worn sneaker, slight grit.",
}


# ---------- 규격 로드 ----------

def load_intent():
    """INTENT.md(동결)에서 톤 근거를 읽는다. 값이 바뀌면 프롬프트도 따라 바뀐다."""
    with open(INTENT_PATH, encoding="utf-8") as f:
        text = f.read()

    def key(name):
        m = re.search(r"^%s:\s*(.+?)\s*(?:#.*)?$" % name, text, re.M)
        return m.group(1).strip().strip('"') if m else ""

    ref = ""
    m = re.search(r"^reference:\s*>\s*\n((?:\s{2,}.*\n)+)", text, re.M)
    if m:
        ref = " ".join(l.strip() for l in m.group(1).splitlines())
    return {
        "tone": key("tone"),
        "one_emotion": key("one_emotion"),
        "player_fantasy": key("player_fantasy"),
        "genre": key("genre"),
        "reference": ref,
    }


def _en(value):
    """한국어 규격값 → 영문구. 매핑이 없으면 원문을 흘리되 경고한다(조용한 한글 유출 방지)."""
    if value in PHRASE_EN:
        return PHRASE_EN[value]
    if value:
        print(f"  ⚠ PHRASE_EN 매핑 없음: '{value}' — 한국어가 그대로 전송된다. 매핑 추가를 권한다.")
    return value


# ---------- 조립 ----------

def compose(bom_id, item, intent, note, length):
    """감독 노트 + 규격 + 금칙어 → 전송 프롬프트 한 덩어리. 전송문은 전부 영문으로 맞춘다."""
    kind = item["kind"]
    parts = [note.strip().rstrip(".") + "."]

    if kind == "bgm":
        # BGM 표: desc=용도, spec=길이·루프 규격
        parts.append(f"Background music loop for {_en(item['desc'])} in a {_en(intent['genre'])}.")
        parts.append(f"Target length {length:.0f} seconds.")
        parts.append(
            f"Overall tone: {_en(intent['tone'])}; the feeling is {_en(intent['one_emotion'])}; "
            f"the player is {_en(intent['player_fantasy'])}."
        )
        parts.append(f"Setting reference: {REFERENCE_EN}")
    else:
        # SFX 표: desc=트리거 이벤트, spec=소리 묘사(한국어) — 소리 묘사는 감독 노트가 영문으로 담당한다.
        event = re.search(r"`([A-Za-z]\w+)`", item["desc"])
        if event:
            parts.append(f"It plays when the game event {event.group(1)} fires.")
        parts.append(f"Duration about {length:.1f} seconds.")
        parts.append(SFX_STYLE_EN)

    parts.append(CONSTRAINTS[kind])
    return " ".join(p for p in parts if p.strip())


def verify(prompt, kind):
    """금칙어 검사 — 빠진 지시를 돌려준다(빈 리스트 = 통과)."""
    low = prompt.lower()
    return [t for t in REQUIRED[kind] if t not in low]


# ---------- 프롬프트 문서 ----------

def _extract(path, begin, end):
    if not os.path.isfile(path):
        return ""
    with open(path, encoding="utf-8") as f:
        text = f.read()
    m = re.search(re.escape(begin) + r"(.*?)" + re.escape(end), text, re.S)
    return m.group(1).strip().strip("`").strip() if m else ""


def render(bom_id, item, intent, note, prompt, length, gen, history_rows, prev_prompt):
    kind = item["kind"]
    plan = ""
    if kind == "bgm":
        plan = (
            "\n## 구성 계획 게이트 (BGM 전용)\n\n"
            "`compose_music` 직행 전에 **`create_composition_plan`으로 구조를 먼저 받는다.**\n"
            "섹션 구성·길이 배분을 눈으로 확인하고 고친 뒤 작곡에 들어가면, 마음에 안 들 때\n"
            "곡 전체를 재생성하지 않아도 되어 크레딧과 시간을 아낀다. SFX는 5초 이하라 이 단계를 건너뛴다.\n"
        )

    lines = [
        f"# 프롬프트 원본 — `{bom_id}`",
        "",
        "> ⚙ **자동 생성 문서** — `scripts/audio/prompt_builder.py` 가 조립한다. 규격 부분을 손으로 고치지 말 것",
        "> (다음 build 때 덮어쓰인다). 바꾸려면 **감독 노트**를 고치거나 BOM·INTENT를 고쳐라.",
        ">",
        "> HARNESS §9 ②는 \"프롬프트 기록 = 재현성\"을 요구한다. 착지 원본은 승격 후 폐기되므로",
        "> **이 파일과 `planning/assets_manifest.md`가 유일한 재생산 근거**다.",
        "",
        "## 대상 스펙 (출처: BOM §8 — 여기서 고치지 말 것)",
        "",
        "| 항목 | 값 |",
        "|---|---|",
        f"| bom_id | `{bom_id}` |",
        f"| 종류 | {kind.upper()} |",
        f"| {'용도' if kind == 'bgm' else '트리거'} | {item['desc']} |",
        f"| {'스펙' if kind == 'bgm' else '소리'} | {item['spec']} |",
        f"| 길이 | {length:.1f}s |",
        f"| dest | `{item['dest_dir']}/{bom_id}.wav` |",
        f"| 임포트 | {'Vorbis · Streaming' if kind == 'bgm' else 'Vorbis q70 · Decompress On Load · 2D'} |",
        "",
        "## 톤 근거 (출처: INTENT.md — 동결 · 자동 주입)",
        "",
        f"`tone: {intent['tone']}` · `one_emotion: {intent['one_emotion']}` · "
        f"`player_fantasy: {intent['player_fantasy']}`",
        "",
        "## 감독 노트 (사람이 고치는 유일한 칸 — 창작 지시)",
        "",
        NOTE_BEGIN,
        note.strip(),
        NOTE_END,
        "",
        "## 전송 프롬프트 (조립 결과 — 그대로 MCP에 투입)",
        "",
        PROMPT_BEGIN,
        "```",
        prompt,
        "```",
        PROMPT_END,
        "",
        f"금칙어 검사: **통과** (`{'` · `'.join(REQUIRED[kind])}`)",
        "",
        "## 생성 파라미터",
        "",
        "| 파라미터 | 값 |",
        "|---|---|",
        f"| MCP 툴 | `{'compose_music' if kind == 'bgm' else 'text_to_sound_effects'}` |",
        f"| 모델 | `{'music_v2' if kind == 'bgm' else '기본'}` |",
        f"| 길이 | {length:.1f}s |",
        "| 출력 포맷 | **PCM 16bit WAV** (mp3 패딩 = 심리스 루프 불가 · 후공정도 WAV 전용) |",
        plan,
        "## 재생산 절차",
        "",
        "```bash",
        f"python scripts/audio/prompt_builder.py build --bom-id {bom_id}",
        "# ↑ 전송 프롬프트로 MCP 생성 → _audio_intake/elevenlabs/ 착지",
        f"python scripts/audio/audio_pipeline.py intake    --bom-id {bom_id}",
        f"python scripts/audio/audio_pipeline.py normalize --bom-id {bom_id}"
        + (" --loop-crossfade-ms 500" if kind == "bgm" else ""),
        f"python scripts/audio/audio_pipeline.py promote   --bom-id {bom_id} --yes   # ⚠ 별도 승인",
        "```",
        "",
        "## 세대 이력 (append-only)",
        "",
        "| gen | 일자 | 변경 |",
        "|---|---|---|",
    ]
    lines += history_rows
    change = "최초 조립" if not history_rows else ("감독 노트/규격 변경" if prompt != prev_prompt else "재조립(내용 동일)")
    lines.append(f"| {gen} | {datetime.date.today().isoformat()} | {change} |")
    lines.append("")
    return "\n".join(lines)


# ---------- 명령 ----------

def cmd_build(args):
    items = bom_audio.load()
    if args.bom_id not in items:
        raise SystemExit(f"[차단] '{args.bom_id}' 는 BOM §8에 없다.")
    item = items[args.bom_id]
    if not item["juice_ok"]:
        raise SystemExit(f"[차단] '{args.bom_id}' 는 JUICE 근거가 없다(J-1 미승인). 프롬프트도 만들지 않는다.")

    path = os.path.join(PROMPT_DIR, args.bom_id + ".md")
    note = args.note or _extract(path, NOTE_BEGIN, NOTE_END) or SEED_NOTES.get(args.bom_id, "")
    if not note:
        raise SystemExit(f"[중단] 감독 노트가 없다. --note 로 창작 지시를 달라 (규격은 코드가 채운다).")

    length = args.length or DEFAULT_LENGTH[item["kind"]]
    if item["kind"] == "sfx" and not (SFX_RANGE[0] <= length <= SFX_RANGE[1]):
        raise SystemExit(f"[차단] SFX 길이 {length}s — text_to_sound_effects 는 {SFX_RANGE[0]}~{SFX_RANGE[1]}초만 지원한다.")

    intent = load_intent()
    prompt = compose(args.bom_id, item, intent, note, length)

    missing = verify(prompt, item["kind"])
    if missing:
        raise SystemExit(f"[차단] 금칙어 누락: {missing} — 전송 불가.")

    prev = _extract(path, PROMPT_BEGIN, PROMPT_END).strip("`\n ")
    prev = prev[3:].strip() if prev.startswith("```") else prev
    old_rows = []
    if os.path.isfile(path):
        with open(path, encoding="utf-8") as f:
            old_rows = re.findall(r"^\| \d+ \| \d{4}-\d{2}-\d{2} \| .*\|$", f.read(), re.M)
    gen = len(old_rows) + 1

    os.makedirs(PROMPT_DIR, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(render(args.bom_id, item, intent, note, prompt, length, gen, old_rows, prev))

    print(f"[조립] {args.bom_id}  gen={gen}  ({item['kind']} · {length:.1f}s)")
    print(f"[검사] 금칙어 통과: {' · '.join(REQUIRED[item['kind']])}")
    print(f"[저장] scripts/audio/prompts/{args.bom_id}.md")
    print("\n--- 전송 프롬프트 ---")
    print(prompt)
    if item["kind"] == "bgm":
        print("\n※ BGM은 create_composition_plan 으로 구조를 먼저 받고 확인한 뒤 compose_music 에 넘긴다.")


def cmd_check(args):
    items = bom_audio.load()
    bad = 0
    for bid, item in items.items():
        path = os.path.join(PROMPT_DIR, bid + ".md")
        if not os.path.isfile(path):
            if item["juice_ok"]:
                print(f"  ○ {bid:<20} 프롬프트 없음 (발주 가능 항목)")
            continue
        body = _extract(path, PROMPT_BEGIN, PROMPT_END)
        body = body[3:].strip("`\n ") if body.startswith("```") else body
        missing = verify(body, item["kind"])
        if missing:
            print(f"  ✗ {bid:<20} 금칙어 누락: {missing}")
            bad += 1
        else:
            print(f"  ✓ {bid:<20} 통과")
    if bad:
        raise SystemExit(f"\n[차단] {bad}건이 금칙어 검사에 실패했다 — 손으로 고친 흔적일 수 있다. build 로 재조립하라.")
    print("\n금칙어 검사 전건 통과.")


def main():
    p = argparse.ArgumentParser(description="ElevenLabs 전송 프롬프트 조립기 (규격=코드 · 창작=감독노트)")
    sub = p.add_subparsers(dest="cmd", required=True)

    s = sub.add_parser("build", help="프롬프트 조립·저장")
    s.add_argument("--bom-id", required=True)
    s.add_argument("--note", help="감독 노트(창작 지시). 생략 시 기존 노트 → 시드 노트 순으로 재사용")
    s.add_argument("--length", type=float, help="초 단위 (BGM 기본 75 · SFX 기본 2.0)")
    s.set_defaults(func=cmd_build)

    sub.add_parser("check", help="전 프롬프트 금칙어 검사").set_defaults(func=cmd_check)

    args = p.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
