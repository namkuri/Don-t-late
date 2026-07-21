#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""elevenlabs_client.py — ElevenLabs REST 직호출로 **PCM을 받아 WAV로 착지**시킨다.

왜 MCP가 아니라 REST인가 (2026-07-21 실사):
공식 MCP의 `compose_music`은 **출력 포맷 파라미터가 없고 mp3로 하드코딩**돼 있다.
반면 REST `/v1/music`은 `output_format=pcm_44100`을 받는다. mp3로 받으면 인코더 패딩 때문에
심리스 루프가 불가하고, 표준 라이브러리엔 mp3 디코더도 없다 — 그래서 생성 경로를 REST로 일원화했다.

의존성: 표준 라이브러리만 (urllib · json · wave).

    python scripts/audio/elevenlabs_client.py plan --bom-id bgm_day_loop
    python scripts/audio/elevenlabs_client.py gen  --bom-id bgm_day_loop [--seed 12345]
    python scripts/audio/elevenlabs_client.py gen  --bom-id sfx_pickup
"""
import argparse
import array
import hashlib
import json
import os
import sys
import urllib.error
import urllib.request
import wave

for _s in (sys.stdout, sys.stderr):
    if hasattr(_s, "reconfigure"):
        _s.reconfigure(encoding="utf-8")  # Windows cp949 콘솔 대응

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bom_audio        # noqa: E402
import prompt_builder   # noqa: E402

API_ROOT = "https://api.elevenlabs.io"
EP_MUSIC = "/v1/music"
EP_SFX = "/v1/sound-generation"
# 문서마다 표기가 갈린다 — 크레딧이 들지 않는 호출이라 순회하며 실측으로 확정한다.
EP_PLAN_CANDIDATES = ["/v1/music/plan"]   # 2026-07-21 실호출로 확정 (다른 후보 2종은 404)

PCM_FORMAT = "pcm_44100"
PCM_RATE = 44100
SAMPLE_WIDTH = 2          # s16le
TIMEOUT = 300             # 75초 곡 생성은 느리다

INTAKE_DIR = (
    os.environ.get("AUDIO_INTAKE_DIR")
    or os.environ.get("ELEVENLABS_MCP_BASE_PATH")
    or os.path.join(
        os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))),
        "_audio_intake", "elevenlabs",
    )
)


# ---------- HTTP ----------

def _key():
    k = os.environ.get("ELEVENLABS_API_KEY")
    if not k:
        raise SystemExit(
            "[중단] ELEVENLABS_API_KEY 환경변수가 없다.\n"
            "  setx ELEVENLABS_API_KEY \"<키>\"  후 셸을 새로 열어라."
        )
    return k


def _get(path):
    req = urllib.request.Request(
        API_ROOT + path, headers={"xi-api-key": _key()}, method="GET"
    )
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            return json.loads(r.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        detail = e.read().decode("utf-8", "replace")[:500]
        raise SystemExit(f"[API {e.code}] {path}\n  {detail}")
    except urllib.error.URLError as e:
        raise SystemExit(f"[네트워크] {path} — {e.reason}")


def _post(path, body, query=None, expect_json=False):
    url = API_ROOT + path + (f"?{query}" if query else "")
    req = urllib.request.Request(
        url,
        data=json.dumps(body).encode("utf-8"),
        headers={"xi-api-key": _key(), "Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=TIMEOUT) as r:
            raw = r.read()
    except urllib.error.HTTPError as e:
        detail = e.read().decode("utf-8", "replace")[:500]
        raise SystemExit(f"[API {e.code}] {path}\n  {detail}")
    except urllib.error.URLError as e:
        raise SystemExit(f"[네트워크] {path} — {e.reason}")
    return json.loads(raw.decode("utf-8")) if expect_json else raw


# ---------- PCM → WAV ----------

def pcm_to_wav(pcm, path, expected_seconds):
    """헤더 없는 raw PCM을 WAV로 감싼다. 채널 수는 문서에 없으므로 **길이로 실측 판정**한다."""
    total_samples = len(pcm) // SAMPLE_WIDTH
    expected_frames = PCM_RATE * expected_seconds
    ratio = total_samples / expected_frames if expected_frames else 0
    channels = int(round(ratio))

    if channels not in (1, 2):
        raise SystemExit(
            f"[중단] 채널 수 판정 실패 — 샘플 {total_samples}, 기대 프레임 {expected_frames:.0f}, 비 {ratio:.3f}.\n"
            "  응답 길이가 요청 길이와 다르다. 포맷·길이 파라미터를 확인하라."
        )
    actual = total_samples / channels / PCM_RATE
    with wave.open(path, "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(SAMPLE_WIDTH)
        w.setframerate(PCM_RATE)
        w.writeframes(pcm)

    peak = 0
    if total_samples:
        a = array.array("h")
        a.frombytes(pcm[: min(len(pcm), PCM_RATE * SAMPLE_WIDTH * channels * 5)])  # 앞 5초만 검사
        peak = max(max(a), -min(a)) if a else 0
    print(f"[WAV] {channels}ch · {PCM_RATE}Hz · {actual:.2f}s · 선두 피크 {peak}")
    if peak == 0:
        print("  ⚠ 선두 5초가 무음이다 — 포맷 해석이 틀렸을 수 있다. 반드시 들어보라.")
    return channels, actual


# ---------- 프롬프트 ----------

def _prompt_of(bom_id):
    """prompt_builder가 조립해 저장한 전송 프롬프트를 읽는다(손편집 방지 검사 포함)."""
    items = bom_audio.load()
    if bom_id not in items:
        raise SystemExit(f"[차단] '{bom_id}' 는 BOM §8에 없다.")
    item = items[bom_id]
    if not item["juice_ok"]:
        raise SystemExit(f"[차단] '{bom_id}' 는 JUICE 근거가 없다(J-1 미승인).")

    path = os.path.join(prompt_builder.PROMPT_DIR, bom_id + ".md")
    if not os.path.isfile(path):
        raise SystemExit(
            f"[차단] 프롬프트가 없다 — 먼저:\n"
            f"  python scripts/audio/prompt_builder.py build --bom-id {bom_id}"
        )
    body = prompt_builder._extract(path, prompt_builder.PROMPT_BEGIN, prompt_builder.PROMPT_END)
    if body.startswith("```"):
        body = body[3:].strip("`\n ")
    missing = prompt_builder.verify(body, item["kind"])
    if missing:
        raise SystemExit(f"[차단] 금칙어 누락: {missing} — build 로 재조립하라.")

    length = _length_of(path, item["kind"])
    return item, body.strip(), length


def _length_of(path, kind):
    import re
    with open(path, encoding="utf-8") as f:
        m = re.search(r"^\| 요청 길이 \| ([\d.]+)s \|", f.read(), re.M)
    if not m:
        raise SystemExit(
            "[중단] 프롬프트 문서에서 '요청 길이'를 못 읽었다 — 형식이 낡았다. "
            "prompt_builder.py build 로 재조립하라."
        )
    return float(m.group(1))


def _sidecar(bom_id, data):
    """생성 근거(프롬프트 해시·seed·엔드포인트)를 착지물 옆에 남긴다 — intake가 매니페스트로 옮긴다."""
    p = os.path.join(INTAKE_DIR, bom_id + ".gen.json")
    with open(p, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    return p


# ---------- 명령 ----------

def cmd_quota(args):
    """인증·플랜·크레딧 잔량 확인. 조회는 크레딧을 쓰지 않는다 — 첫 호출은 반드시 이것."""
    sub = _get("/v1/user/subscription")
    used = sub.get("character_count")
    limit = sub.get("character_limit")
    print(f"[인증] 성공 — 키가 유효하다")
    print(f"[플랜] {sub.get('tier', '?')}  (status={sub.get('status', '?')})")
    if isinstance(used, int) and isinstance(limit, int) and limit:
        print(f"[크레딧] {used:,} / {limit:,} 사용  (잔량 {limit - used:,} · {100 * used / limit:.1f}%)")
    nxt = sub.get("next_character_count_reset_unix")
    if nxt:
        print(f"[리셋] unix {nxt}")
    print("\n다음: plan (크레딧 0) → gen --length 30 (소액 검증) → 본 생성")


def cmd_plan(args):
    """BGM 구성 계획 — 크레딧이 들지 않는다. 구조를 먼저 보고 고친 뒤 작곡에 들어간다."""
    item, prompt, length = _prompt_of(args.bom_id)
    if item["kind"] != "bgm":
        raise SystemExit("[중단] 구성 계획은 BGM 전용이다 (SFX는 ≤5초라 불필요).")

    body = {"prompt": prompt, "music_length_ms": int(length * 1000), "model_id": args.model}
    last = None
    for ep in EP_PLAN_CANDIDATES:
        try:
            plan = _post(ep, body, expect_json=True)
            print(f"[구성계획] {ep} 성공")
            break
        except SystemExit as e:
            last = e
            print(f"  · {ep} 실패 — 다음 후보 시도")
    else:
        raise last

    out = os.path.join(prompt_builder.PROMPT_DIR, args.bom_id + ".plan.json")
    with open(out, "w", encoding="utf-8") as f:
        json.dump(plan, f, ensure_ascii=False, indent=2)
    print(f"[저장] {os.path.relpath(out)}")
    for i, c in enumerate(plan.get("chunks", []), 1):
        print(f"  {i}. {c.get('duration_ms', '?')}ms · {str(c.get('text', ''))[:70]}")
    print("\n계획을 확인·수정한 뒤:  gen --bom-id %s --use-plan" % args.bom_id)


def cmd_gen(args):
    item, prompt, length = _prompt_of(args.bom_id)
    if args.length:
        print(f"[소액검증] 길이를 {length:.0f}s → {args.length:.0f}s 로 낮춰 포맷·채널만 확인한다. "
              "결과물은 규격 미달이므로 반입하지 마라.")
        length = args.length
    os.makedirs(INTAKE_DIR, exist_ok=True)
    out = os.path.join(INTAKE_DIR, args.bom_id + ".wav")

    meta = {
        "bom_id": args.bom_id,
        "prompt": prompt,
        "prompt_sha1": hashlib.sha1(prompt.encode("utf-8")).hexdigest()[:12],
        "output_format": PCM_FORMAT,
        "length_s": length,
    }

    if item["kind"] == "bgm":
        body = {"model_id": args.model, "force_instrumental": True}
        plan_path = os.path.join(prompt_builder.PROMPT_DIR, args.bom_id + ".plan.json")
        if args.use_plan:
            if not os.path.isfile(plan_path):
                raise SystemExit(f"[중단] {plan_path} 없음 — 먼저 plan 을 실행하라.")
            with open(plan_path, encoding="utf-8") as f:
                body["composition_plan"] = json.load(f)   # prompt와 병용 불가
            meta["composition_plan"] = os.path.basename(plan_path)
        else:
            body["prompt"] = prompt
            body["music_length_ms"] = int(length * 1000)
        if args.seed is not None:
            body["seed"] = args.seed
            meta["seed"] = args.seed
        endpoint = EP_MUSIC
    else:
        body = {"text": prompt, "duration_seconds": length}
        endpoint = EP_SFX

    meta["endpoint"] = endpoint
    if args.dry_run:
        print(json.dumps({"endpoint": endpoint, "query": f"output_format={PCM_FORMAT}", "body": body},
                         ensure_ascii=False, indent=2))
        raise SystemExit(0)

    print(f"[요청] POST {endpoint}?output_format={PCM_FORMAT}  ({length:.1f}s · {item['kind'].upper()})")
    pcm = _post(endpoint, body, query=f"output_format={PCM_FORMAT}")
    channels, actual = pcm_to_wav(pcm, out, length)
    meta.update({"channels": channels, "actual_s": round(actual, 2)})
    _sidecar(args.bom_id, meta)

    print(f"[착지] {out}")
    print(f"다음:  python scripts/audio/audio_pipeline.py intake --bom-id {args.bom_id}")


def main():
    p = argparse.ArgumentParser(description="ElevenLabs REST 직호출 — PCM 수신 후 WAV 착지")
    sub = p.add_subparsers(dest="cmd", required=True)

    sub.add_parser("quota", help="인증·플랜·크레딧 잔량 (크레딧 0 — 첫 호출용)").set_defaults(func=cmd_quota)

    s = sub.add_parser("plan", help="BGM 구성 계획 생성 (크레딧 0)")
    s.add_argument("--bom-id", required=True)
    s.add_argument("--model", default="music_v2")
    s.set_defaults(func=cmd_plan)

    s = sub.add_parser("gen", help="생성 → _audio_intake 착지")
    s.add_argument("--bom-id", required=True)
    s.add_argument("--model", default="music_v2")
    s.add_argument("--seed", type=int, help="같은 프롬프트+같은 seed = 같은 곡 (재현)")
    s.add_argument("--use-plan", action="store_true", help="저장된 구성 계획으로 작곡(프롬프트와 병용 불가)")
    s.add_argument("--length", type=float, help="소액 검증용 길이 축소 (예: 30). 생략 시 프롬프트 문서의 규격 길이")
    s.add_argument("--dry-run", action="store_true", help="전송 없이 요청 본문만 출력")
    s.set_defaults(func=cmd_gen)

    args = p.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
