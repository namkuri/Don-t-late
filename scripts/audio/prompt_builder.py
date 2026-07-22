#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""prompt_builder.py — bom_id → ElevenLabs 전송 프롬프트를 **조립**한다.

이 파이프라인의 앞단 게이트. 후공정(정규화·편집)은 못 만든 음원을 좋게 만들지 못하므로
품질은 여기서 갈린다.

지침 위계 (2026-07-21 Director 결정):
  1. `rules/GAME-BGM-RULES.md`  — **제1지침. 차단력을 가진 유일한 문서.**
  2. 스타일 md(낮/밤)            — 참고(앵커·기본 BPM·네거티브). 충돌 시 1번이 이긴다.
  3. BOM §8 · JUICE             — 참고(생성 단계는 경고만). 반입·승격에서만 차단한다.
  4. INTENT                     — 참고(톤 근거).

조립 방식 = 하이브리드:
  · 규격(필수 태그·금지 태그·BPM·조성·길이·네거티브)은 **코드가 결정론적으로** 박는다.
  · 악기·장면·무드 같은 **창작 태그만** 사람이 준다.
BGM은 규격 §5의 「편집 인계」 블록을 함께 낸다 — 곡을 받는 순간 어디를 자를지가 손에 있어야 한다.

    python scripts/audio/prompt_builder.py build --bom-id bgm_night_var \\
        --tags "city pop, retro 80s, lonely muted saxophone, pre-dawn empty streets"
    python scripts/audio/prompt_builder.py build --bom-id sfx_pickup --length 1.2
    python scripts/audio/prompt_builder.py check
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
import bgm_rules   # noqa: E402
import bom_audio   # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PROMPT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "prompts")
INTENT_PATH = os.path.join(ROOT, "docs", "INTENT.md")

# SFX는 태그 문법이 아니라 서술형이 잘 먹는다 — BGM과 규칙을 분리한다.
SFX_CONSTRAINTS = (
    "Single isolated sound effect, dry and close, no background music, no vocals, no long reverb tail."
)
SFX_REQUIRED = ["no background music", "no vocals"]
# 사람 판정 1차 (2026-07-22): 음량 낮음(→후공정 정규화)·과장됨·8bit 부족 → 앵커 개정.
# ⚠ API 상한: text ≤ 450자 — 앵커+제약+태그 합산이라 앵커는 짧게 유지한다 (2026-07-22 실측 400).
SFX_STYLE_EN = ("Style: 8-bit retro game sound design, chiptune sound chip, "
                "square wave and noise channel, subtle and understated, "
                "dark comedy tone, readable in a busy mix.")
SFX_RANGE = (0.5, 5.0)          # text_to_sound_effects 제약
DEFAULT_LENGTH = {"bgm": bgm_rules.DEFAULT_TRACK_SECONDS, "sfx": 2.0}   # BGM 기본 60초 (완화 정책)
DEFAULT_LOOP_BARS = 16

NOTE_BEGIN, NOTE_END = "<!-- NOTE:BEGIN -->", "<!-- NOTE:END -->"
PROMPT_BEGIN, PROMPT_END = "<!-- PROMPT:BEGIN -->", "<!-- PROMPT:END -->"

# 슬롯 추론 — BOM의 bom_id에서 낮/밤/타이틀을 읽는다.
def slot_of(bom_id):
    if "title" in bom_id:
        return "title"
    if "night" in bom_id:
        return "night"
    return "day"


SEED_TAGS = {
    "bgm_day_loop": "sunny afternoon, cozy neighborhood, breezy town stroll, cheerful",
    "bgm_night_var": "neon nightscape, late-night introspective, melancholic, cozy",
    "bgm_night_loop": "neon nightscape, late-night introspective, melancholic, cozy",
    "bgm_title": "nostalgic, title screen, bold opening statement",
    "sfx_delivery_ok": "A bright two-tone doorbell ding-dong followed by a small coin chime.",
    "sfx_late_buzzer": "A short low dull buzzer, deflating and unpleasant, like a failed game show answer.",
    "sfx_pickup": "A cardboard box being grabbed and lifted, light paper scuff and a soft thud.",
    "sfx_footstep": "A single footstep on concrete pavement, worn sneaker, slight grit.",
}


# ---------- SFX 조립 ----------

def compose_sfx(item, note, length):
    parts = [note.strip().rstrip(".") + "."]
    event = re.search(r"`([A-Za-z]\w+)`", item["desc"])
    if event:
        parts.append(f"It plays when the game event {event.group(1)} fires.")
    parts.append(f"Duration about {length:.1f} seconds.")
    parts.append(SFX_STYLE_EN)
    parts.append(SFX_CONSTRAINTS)
    return " ".join(parts)


def verify(prompt, kind, oneshot=False):
    """전송 직전 가벼운 관문 (elevenlabs_client 가 호출). 상세 규격 검사는 build 시점에 한다.

    oneshot(타이틀·트레일러·엔딩) 은 규격 §3 예외라 필수 태그를 요구하지 않는다 —
    조립기와 같은 판단을 해야 한다(두 곳이 어긋나면 통과한 프롬프트가 전송에서 막힌다).
    """
    low = prompt.lower()
    if kind == "sfx":
        return [t for t in SFX_REQUIRED if t not in low]
    missing = [] if oneshot else [t for t in bgm_rules.MANDATORY_TAGS if t.lower() not in low]
    if "instrumental" not in low:
        missing.append("instrumental")
    if "major key" not in low and "minor key" not in low:
        missing.append("major/minor key")
    return missing


# ---------- 프롬프트 문서 ----------

def _extract(path, begin, end):
    if not os.path.isfile(path):
        return ""
    with open(path, encoding="utf-8") as f:
        text = f.read()
    m = re.search(re.escape(begin) + r"(.*?)" + re.escape(end), text, re.S)
    return m.group(1).strip().strip("`").strip() if m else ""


def load_intent():
    with open(INTENT_PATH, encoding="utf-8") as f:
        text = f.read()

    def key(name):
        m = re.search(r"^%s:\s*(.+?)\s*(?:#.*)?$" % name, text, re.M)
        return m.group(1).strip().strip('"') if m else ""

    return {"tone": key("tone"), "one_emotion": key("one_emotion"),
            "player_fantasy": key("player_fantasy"), "genre": key("genre")}


def render(bom_id, item, intent, note, prompt, length, gen, old_rows, meta):
    kind = item["kind"]
    lines = [
        f"# 프롬프트 원본 — `{bom_id}`",
        "",
        "> ⚙ **자동 생성 문서** — `scripts/audio/prompt_builder.py` 가 조립한다. 규격 부분을 손으로 고치지 말 것",
        "> (다음 build 때 덮어쓰인다). 바꾸려면 **창작 태그**를 고치거나 BOM·규격 문서를 고쳐라.",
        ">",
        "> 규격 출처: `scripts/audio/rules/GAME-BGM-RULES.md` (충돌 시 스타일보다 우선) ·",
        "> 스타일: `rules/afternoon-bgm-02.md`(낮) · `rules/night-bgm.md`(밤)",
        "",
        "## 대상 스펙 (출처: BOM §8)",
        "",
        "| 항목 | 값 |",
        "|---|---|",
        f"| bom_id | `{bom_id}` |",
        f"| 종류 | {kind.upper()}" + (f" · 슬롯 `{meta['slot']}`" if kind == "bgm" else "") + " |",
        f"| {'용도' if kind == 'bgm' else '트리거'} | {item['desc']} |",
        f"| {'스펙' if kind == 'bgm' else '소리'} | {item['spec']} |",
        f"| 요청 길이 | {length:.1f}s |",
        f"| dest | `{item['dest_dir']}/{bom_id}.wav` |",
        "",
        "## 창작 태그 (사람이 고치는 유일한 칸)",
        "",
        NOTE_BEGIN,
        note.strip(),
        NOTE_END,
        "",
        "## 전송 프롬프트 (조립 결과 — 그대로 API에 투입)",
        "",
        PROMPT_BEGIN,
        "```",
        prompt,
        "```",
        PROMPT_END,
        "",
    ]

    if kind == "bgm":
        lines += [
            "## 편집 인계 (규격 §5)",
            "",
            "```",
            bgm_rules.handoff_block(meta["bpm"], length, meta["loop_bars"], meta["loop_risk"], meta.get("oneshot")),
            "```",
            "",
            "## 규격 검사",
            "",
        ]
        if meta.get("raw"):
            lines.append(
                "- ⚠ **raw 붙여넣기** — 조립기를 거치지 않은 사람 작성 프롬프트다. "
                "규격 태그·톤 근거가 코드로 보장되지 않는다."
                + ("  \n- 🚨 **게이트 우회(`--no-gate`)** — 규격 위반을 알고도 통과시켰다."
                   if meta.get("gate_bypassed") else "  \n- 규격 검사 자체는 통과했다.")
            )
        lines += [
            f"- 필수 태그 {len(bgm_rules.MANDATORY_TAGS)}종 · 금지 태그 {len(bgm_rules.BANNED_TAGS)}종: **통과**"
            + ("  (타이틀=단발 연출이라 §1·§3 면제)" if meta.get("oneshot") else ""),
            f"- 조성 `{bgm_rules.STYLES[meta['slot']]['key']}` · BPM `{meta['bpm']}`(정수) · instrumental 명시",
        ]
        if meta["dropped_negatives"]:
            lines.append(
                f"- ⚠ 네거티브 해제: `{'`, `'.join(meta['dropped_negatives'])}` — "
                "창작 태그가 해당 악기를 **명시 요청**해서 기본 네거티브를 뺐다(노트 > 스타일 기본값)"
            )
        if meta["loop_risk"]:
            lines.append(f"- 🔴 루프 난이도: `{'`, `'.join(meta['loop_risk'])}` → {bgm_rules.EDIT_WARNING}")
    else:
        lines += ["## 규격 검사", "", f"- 금칙어 **통과** (`{'` · `'.join(SFX_REQUIRED)}`)"]

    lines += [
        "",
        "## 생성 파라미터",
        "",
        "| 파라미터 | 값 |",
        "|---|---|",
        f"| 엔드포인트 | `POST {'/v1/music' if kind == 'bgm' else '/v1/sound-generation'}` (REST 직호출) |",
        f"| 모델 | `{'music_v2' if kind == 'bgm' else '기본'}` |",
        "| 출력 포맷 | `output_format=pcm_44100` → **PCM 16bit를 WAV로 래핑** |",
        "| mp3 금지 근거 | 규격 §7 — 인코더가 앞뒤 무음 패딩을 붙여 매 루프마다 공백이 생긴다 |",
        "",
        "## 톤 근거 (INTENT.md — 동결)",
        "",
        f"`tone: {intent['tone']}` · `one_emotion: {intent['one_emotion']}` · "
        f"`player_fantasy: {intent['player_fantasy']}`",
        "",
        "## 재생산 절차",
        "",
        "```bash",
        f"python scripts/audio/prompt_builder.py build --bom-id {bom_id}",
    ]
    if kind == "bgm":
        lines += [
            f"python scripts/audio/elevenlabs_client.py plan --bom-id {bom_id}   # 구성 확인 (크레딧 0)",
            f"python scripts/audio/elevenlabs_client.py gen  --bom-id {bom_id} --use-plan [--seed N]",
        ]
    else:
        lines += [f"python scripts/audio/elevenlabs_client.py gen  --bom-id {bom_id}"]
    lines += [
        f"python scripts/audio/audio_pipeline.py intake --bom-id {bom_id}",
        "```",
        "",
        "## 세대 이력 (append-only)",
        "",
        "| gen | 일자 | 변경 |",
        "|---|---|---|",
    ]
    lines += old_rows
    lines.append(f"| {gen} | {datetime.date.today().isoformat()} | "
                 f"{'최초 조립' if not old_rows else '재조립'} |")
    lines.append("")
    return "\n".join(lines)


# ---------- 명령 ----------

def cmd_build(args):
    # 지침 위계: 생성 단계의 제1지침은 rules/GAME-BGM-RULES.md 다.
    # BOM·JUICE 는 참고 — 여기선 경고만 하고, 차단은 반입·승격(audio_pipeline)에서 한다.
    item = bom_audio.resolve(args.bom_id)

    path = os.path.join(PROMPT_DIR, args.bom_id + ".md")
    note = args.tags or _extract(path, NOTE_BEGIN, NOTE_END) or SEED_TAGS.get(args.bom_id, "")
    if not note:
        raise SystemExit("[중단] 창작 태그가 없다. --tags 로 달라 (규격은 코드가 채운다).")

    length = args.length or DEFAULT_LENGTH[item["kind"]]

    if args.raw:
        # 붙여넣기 경로 — 조립을 건너뛰고 사람이 쓴 프롬프트를 그대로 쓴다.
        # 재현성은 유지된다(문서에 원문이 남는다). 규격 검사는 여전히 돈다.
        prompt = args.raw.strip()
        note = "(raw 붙여넣기 — 조립기 미경유)"
        if item["kind"] == "bgm":
            m = re.search(r"(\d+)\s*BPM", prompt, re.I)
            bpm = args.bpm or (int(m.group(1)) if m else None)
            if not bpm:
                raise SystemExit("[중단] raw 프롬프트에서 BPM을 못 찾았다 — --bpm 으로 지정하라 (편집 인계 계산에 필요).")
            missing = verify(prompt, "bgm")
            banned = [t for t in bgm_rules.BANNED_TAGS if t.lower() in prompt.lower()]
            if (missing or banned) and not args.no_gate:
                raise SystemExit(
                    "[차단] 규격 위반:\n"
                    + (f"  - 필수 태그 누락: {missing}\n" if missing else "")
                    + (f"  - 금지 태그 포함: {banned}\n" if banned else "")
                    + "  그대로 쓰려면 --no-gate (문서에 우회 사실이 기록된다)"
                )
            if missing or banned:
                print(f"[우회] --no-gate — 누락 {missing} 금지 {banned}")
            risk = [t for t in bgm_rules.LOOP_RISK_TAGS if t in prompt.lower()]
            meta = {"slot": args.slot or slot_of(args.bom_id), "bpm": bpm, "loop_bars": args.loop_bars,
                    "dropped_negatives": [], "loop_risk": risk, "oneshot": False,
                    "raw": True, "gate_bypassed": bool(missing or banned)}
        else:
            meta = {"raw": True, "gate_bypassed": False}

    elif item["kind"] == "bgm":
        slot = args.slot or slot_of(args.bom_id)
        bpm = args.bpm or bgm_rules.STYLES[slot]["bpm"]
        tags = [t.strip() for t in note.split(",") if t.strip()]
        key = f"{args.key} key" if args.key else None
        prompt, info = bgm_rules.compose_bgm(slot, tags, bpm, length,
                                             no_anchors=args.no_anchors, key=key)
        problems = bgm_rules.verify_bgm(prompt, bpm, length, args.loop_bars, info["oneshot"])
        if problems:
            raise SystemExit("[차단] 규격 위반:\n  - " + "\n  - ".join(problems))
        meta = {"slot": slot, "bpm": bpm, "loop_bars": args.loop_bars, **info}
    else:
        if not (SFX_RANGE[0] <= length <= SFX_RANGE[1]):
            raise SystemExit(f"[차단] SFX 길이 {length}s — {SFX_RANGE[0]}~{SFX_RANGE[1]}초만 지원한다.")
        prompt = compose_sfx(item, note, length)
        missing = verify(prompt, "sfx")
        if missing:
            raise SystemExit(f"[차단] 금칙어 누락: {missing}")
        meta = {}

    old_rows = []
    if os.path.isfile(path):
        with open(path, encoding="utf-8") as f:
            old_rows = re.findall(r"^\| \d+ \| \d{4}-\d{2}-\d{2} \| .*\|$", f.read(), re.M)
    gen = len(old_rows) + 1

    os.makedirs(PROMPT_DIR, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(render(args.bom_id, item, load_intent(), note, prompt, length, gen, old_rows, meta))

    print(f"[조립] {args.bom_id}  gen={gen}  ({item['kind']}"
          + (f" · {meta['slot']} · {meta['bpm']}BPM" if item["kind"] == "bgm" else "")
          + f" · {length:.1f}s)")
    if item["kind"] == "bgm" and not meta.get("raw"):
        print(f"[규격] 필수 {len(bgm_rules.MANDATORY_TAGS)}종 · 금지 {len(bgm_rules.BANNED_TAGS)}종 · 길이/루프 통과")
    elif item["kind"] == "bgm" and meta.get("gate_bypassed"):
        print("[규격] ⚠ 미통과 상태로 진행 — 이 프롬프트는 루프·반복내성이 보장되지 않는다")
        if meta["dropped_negatives"]:
            print(f"[해제] 네거티브 제거: {', '.join(meta['dropped_negatives'])}  ← 창작 태그가 명시 요청")
        if meta["loop_risk"]:
            print(f"[경고] 루프 난이도 🔴 {', '.join(meta['loop_risk'])}")
    print(f"[저장] scripts/audio/prompts/{args.bom_id}.md")
    print("\n--- 전송 프롬프트 ---")
    print(prompt)
    if item["kind"] == "bgm":
        print()
        print(bgm_rules.handoff_block(meta["bpm"], length, meta["loop_bars"], meta["loop_risk"], meta.get("oneshot")))


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
        banned = [t for t in bgm_rules.BANNED_TAGS if t.lower() in body.lower()] if item["kind"] == "bgm" else []
        if missing or banned:
            print(f"  ✗ {bid:<20} 누락 {missing} 금지태그 {banned}")
            bad += 1
        else:
            print(f"  ✓ {bid:<20} 통과")
    if bad:
        raise SystemExit(f"\n[차단] {bad}건 실패 — 손편집 흔적일 수 있다. build 로 재조립하라.")
    print("\n규격 검사 전건 통과.")


def main():
    p = argparse.ArgumentParser(description="전송 프롬프트 조립기 (규격=코드 · 창작=태그)")
    sub = p.add_subparsers(dest="cmd", required=True)

    s = sub.add_parser("build", help="프롬프트 조립·저장")
    s.add_argument("--bom-id", required=True)
    s.add_argument("--tags", help="창작 태그(쉼표 구분). 생략 시 기존 노트 → 시드 순으로 재사용")
    s.add_argument("--slot", choices=["day", "night", "title"], help="생략 시 bom_id에서 추론")
    s.add_argument("--bpm", type=int, help="정수 1개 (규격 §2 — 범위 금지). 생략 시 스타일 기본")
    s.add_argument("--length", type=float, help="초 (BGM 기본 180 · SFX 기본 2.0)")
    s.add_argument("--loop-bars", type=int, default=DEFAULT_LOOP_BARS, choices=[16, 32, 64])
    s.add_argument("--no-anchors", action="store_true",
                   help="스타일 팩 앵커 생략 — 요청 장르가 팩과 다를 때(칩튠 vs 신스웨이브 등)")
    s.add_argument("--key", choices=["major", "minor"], help="조성 강제 (팩 기본값 무시)")
    s.add_argument("--raw", help="완성된 프롬프트를 그대로 사용 (조립기 미경유). 규격 검사는 그대로 돈다")
    s.add_argument("--no-gate", action="store_true", help="--raw 전용: 규격 위반을 경고로 낮춘다(문서에 기록됨)")
    s.set_defaults(func=cmd_build)

    sub.add_parser("check", help="전 프롬프트 규격 검사").set_defaults(func=cmd_check)

    args = p.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
