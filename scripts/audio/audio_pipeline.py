#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""audio_pipeline.py — ElevenLabs 생성물의 반입·후공정·승격 CLI.

역할 경계: **생성은 MCP**(compose_music / text_to_sound_effects)가, **후공정은 이 스크립트**가 한다.
HARNESS §9 반입 5단계 중 ②기록 ③검역 ④이동 ⑤폐기를 집행한다.

    python scripts/audio/audio_pipeline.py list
    python scripts/audio/audio_pipeline.py status
    python scripts/audio/audio_pipeline.py intake   --bom-id bgm_day_loop [--file NAME]
    python scripts/audio/audio_pipeline.py normalize --bom-id bgm_day_loop [--loop-crossfade-ms 500]
    python scripts/audio/audio_pipeline.py promote  --bom-id bgm_day_loop --yes

의존성: 표준 라이브러리만 (wave + array). 16bit PCM WAV만 처리한다 —
mp3는 인코더 패딩 때문에 심리스 루프가 원리적으로 불가하므로 생성 단계에서 WAV/PCM으로 받는다.
"""
import argparse
import array
import json
import os
import shutil
import sys
import wave

for _s in (sys.stdout, sys.stderr):        # 차단 메시지는 stderr로 나간다 — 둘 다 필요
    if hasattr(_s, "reconfigure"):
        _s.reconfigure(encoding="utf-8")   # Windows cp949 콘솔 대응

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bom_audio          # noqa: E402
import manifest_writer    # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# 착지 디렉토리 — Assets 밖 + .gitignore 대상이라 Unity·팀에 영향이 없다.
# MCP가 쓰는 ELEVENLABS_MCP_BASE_PATH와 같은 값을 보게 해서 진실을 하나로 유지한다.
INTAKE_DIR = (
    os.environ.get("AUDIO_INTAKE_DIR")
    or os.environ.get("ELEVENLABS_MCP_BASE_PATH")
    or os.path.join(ROOT, "_audio_intake", "elevenlabs")
)

ALLOWED_EXT = (".wav", ".ogg", ".mp3")   # HARNESS §9 ③ 확장자 화이트리스트
AUDIO_BUDGET_BYTES = 10 * 1024 * 1024    # BOM §8 총 오디오 예산 ≤ 10MB
DEFAULT_LICENSE = "ElevenLabs 유료플랜 상업이용 — ⚠ 게임 배포용 추가 라이선스 확인 대상"


# ---------- WAV 입출력 (16bit PCM 전용) ----------

def _read_wav(path):
    with wave.open(path, "rb") as w:
        if w.getsampwidth() != 2:
            raise SystemExit(
                f"[검역] {os.path.basename(path)}: {w.getsampwidth() * 8}bit — 16bit PCM만 처리한다. "
                "생성 단계에서 PCM 16bit로 받아라."
            )
        nch, rate, nframes = w.getnchannels(), w.getframerate(), w.getnframes()
        samples = array.array("h")
        samples.frombytes(w.readframes(nframes))
    return samples, nch, rate


def _write_wav(path, samples, nch, rate):
    with wave.open(path, "wb") as w:
        w.setnchannels(nch)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(samples.tobytes())


def _peak(samples):
    return max(max(samples), -min(samples)) if samples else 0


# ---------- 공용 ----------

def _work_path(bom_id):
    return os.path.join(INTAKE_DIR, bom_id + ".wav")


def _require_bom(bom_id):
    items = bom_audio.load()
    if bom_id not in items:
        raise SystemExit(f"[차단] '{bom_id}' 는 BOM §8에 없다. 발주서 밖 항목은 만들지 않는다(CODE_RULES §7).")
    item = items[bom_id]
    if not item["juice_ok"]:
        raise SystemExit(
            f"[차단] '{bom_id}' 는 JUICE 근거가 없다(J-1 개정안 미승인). "
            "임의 추가 금지 — JUICE.md 개정 승인 후 진행하라."
        )
    return item


def _audio_budget():
    total = 0
    for kind_dir in ("BGM", "SFX"):
        d = os.path.join(ROOT, "Assets", "Audio", kind_dir)
        for base, _, files in os.walk(d):
            for f in files:
                if f.endswith(".meta"):
                    continue
                total += os.path.getsize(os.path.join(base, f))
    return total


# ---------- 명령 ----------

def cmd_list(args):
    bom_audio.main()


def cmd_status(args):
    print(f"착지 디렉토리: {INTAKE_DIR}")
    if not os.path.isdir(INTAKE_DIR):
        print("  (아직 없음 — HARNESS §9 '첫 반입 시 생성' 원칙대로 intake 시 만들어진다)")
    else:
        entries = sorted(os.listdir(INTAKE_DIR))
        if not entries:
            print("  (비어 있음)")
        for e in entries:
            size = os.path.getsize(os.path.join(INTAKE_DIR, e))
            mark = "" if e.lower().endswith(ALLOWED_EXT) else "  ⚠ 화이트리스트 밖"
            print(f"  {e:<40} {size / 1024:8.1f} KB{mark}")

    used = _audio_budget()
    pct = used / AUDIO_BUDGET_BYTES * 100
    print(f"\nAssets/Audio 예산: {used / 1024:.1f} KB / {AUDIO_BUDGET_BYTES / 1024 / 1024:.0f} MB ({pct:.1f}%)")


def cmd_intake(args):
    item = _require_bom(args.bom_id)

    # 재현성 게이트 — 프롬프트 원본이 없으면 착지 원본 폐기 후 재생산이 불가능해진다(HARNESS §9 ②).
    prompt_md = os.path.join(os.path.dirname(os.path.abspath(__file__)), "prompts", args.bom_id + ".md")
    if not os.path.isfile(prompt_md):
        raise SystemExit(
            f"[차단] scripts/audio/prompts/{args.bom_id}.md 없음 — 프롬프트 원본 없이는 반입하지 않는다.\n"
            f"  먼저: python scripts/audio/prompt_builder.py build --bom-id {args.bom_id}"
        )

    if not os.path.isdir(INTAKE_DIR):
        raise SystemExit(f"[중단] 착지 디렉토리가 없다: {INTAKE_DIR}\n  MCP 생성이 선행돼야 한다.")

    if args.file:
        src = os.path.join(INTAKE_DIR, args.file)
        if not os.path.isfile(src):
            raise SystemExit(f"[중단] 파일 없음: {src}")
    else:
        cands = [
            os.path.join(INTAKE_DIR, f)
            for f in os.listdir(INTAKE_DIR)
            if f.lower().endswith(ALLOWED_EXT) and not f.startswith(args.bom_id)
        ]
        if not cands:
            raise SystemExit(f"[중단] {INTAKE_DIR} 에 반입할 신규 오디오가 없다.")
        src = max(cands, key=os.path.getmtime)   # 가장 최근 생성물

    if not src.lower().endswith(ALLOWED_EXT):
        raise SystemExit(f"[검역] 확장자 화이트리스트 위반: {os.path.basename(src)}")

    src_name = os.path.basename(src)
    dest = f"{item['dest_dir']}/{args.bom_id}.wav"

    # 생성 근거(seed·프롬프트 해시)를 매니페스트로 옮긴다 — seed가 있으면 재현이 근사가 아니라 복원이 된다.
    source = "ElevenLabs REST"
    sidecar = os.path.join(INTAKE_DIR, args.bom_id + ".gen.json")
    if os.path.isfile(sidecar):
        with open(sidecar, encoding="utf-8") as f:
            meta = json.load(f)
        bits = [f"{meta.get('endpoint', '')} {meta.get('output_format', '')}",
                f"prompt#{meta.get('prompt_sha1', '?')}"]
        if meta.get("seed") is not None:
            bits.append(f"seed={meta['seed']}")
        if meta.get("composition_plan"):
            bits.append("plan=" + meta["composition_plan"])
        source = "ElevenLabs REST (" + " · ".join(bits) + ")"

    gen = manifest_writer.append(args.bom_id, src_name, dest, args.license, source=source)

    work = _work_path(args.bom_id)
    if os.path.abspath(src) != os.path.abspath(work):
        shutil.move(src, work)

    print(f"[반입] {src_name} → {os.path.basename(work)}  (gen={gen})")
    print(f"[기록] planning/assets_manifest.md 에 행 추가 · dest={dest}")
    print("  다음: normalize → (승인 후) promote")


def cmd_normalize(args):
    _require_bom(args.bom_id)
    path = _work_path(args.bom_id)
    if not os.path.isfile(path):
        raise SystemExit(f"[중단] {path} 없음 — intake 를 먼저 실행하라.")

    samples, nch, rate = _read_wav(path)
    total_frames = len(samples) // nch
    print(f"[입력] {total_frames / rate:.2f}s · {rate}Hz · {nch}ch · peak {_peak(samples)}")

    # ① 루프 크로스페이드 — 꼬리를 머리에 겹쳐 이음매를 없앤다 (BOM: 심리스 루프포인트)
    n = int(args.loop_crossfade_ms * rate / 1000)
    if n > 0:
        if n * 2 >= total_frames:
            raise SystemExit("[중단] 크로스페이드 길이가 원본의 절반 이상이다.")
        out = array.array("h", samples[: n * nch])
        for i in range(n):
            t = i / n
            for c in range(nch):
                head = samples[i * nch + c]
                tail = samples[(total_frames - n + i) * nch + c]
                out[i * nch + c] = int(tail * (1 - t) + head * t)
        out.extend(samples[n * nch: (total_frames - n) * nch])
        samples = out
        total_frames -= n
        print(f"[루프] 크로스페이드 {args.loop_crossfade_ms}ms 적용 → {total_frames / rate:.2f}s")

    # ② 피크 정규화
    peak = _peak(samples)
    if peak == 0:
        raise SystemExit("[중단] 무음 파일이다.")
    target = int(32767 * (10 ** (args.peak_db / 20.0)))
    gain = target / peak
    samples = array.array("h", (max(-32768, min(32767, int(v * gain))) for v in samples))
    print(f"[정규화] peak {peak} → {_peak(samples)} (목표 {args.peak_db:+.1f} dBFS · gain ×{gain:.3f})")

    _write_wav(path, samples, nch, rate)
    print(f"[저장] {path}")


def cmd_promote(args):
    item = _require_bom(args.bom_id)
    src = _work_path(args.bom_id)
    if not os.path.isfile(src):
        raise SystemExit(f"[중단] {src} 없음 — intake/normalize 를 먼저 실행하라.")

    dest_dir = os.path.join(ROOT, *item["dest_dir"].split("/"))
    dest = os.path.join(dest_dir, args.bom_id + ".wav")
    size = os.path.getsize(src)
    projected = _audio_budget() - (os.path.getsize(dest) if os.path.isfile(dest) else 0) + size

    if not args.yes:
        print("⚠ 승격은 유일한 Unity 접점이다 — 에셋DB가 바뀌고 팀원 pull 시 임포트가 발생한다.")
        print(f"   {src}\n → {dest}  ({size / 1024:.1f} KB)")
        print(f"   승격 후 예산: {projected / 1024:.1f} KB / {AUDIO_BUDGET_BYTES / 1024 / 1024:.0f} MB")
        raise SystemExit("실행하려면 --yes 를 붙여라.")

    if projected > AUDIO_BUDGET_BYTES:
        raise SystemExit(
            f"[차단] BOM §8 오디오 예산 초과: {projected / 1024 / 1024:.2f} MB > 10 MB. "
            "길이·채널·샘플레이트를 줄여라."
        )

    os.makedirs(dest_dir, exist_ok=True)
    shutil.copy2(src, dest)
    os.remove(src)   # HARNESS §9 ⑤ 원본 폐기 — 재현은 매니페스트+프롬프트+seed가 보장
    sidecar = os.path.join(INTAKE_DIR, args.bom_id + ".gen.json")
    if os.path.isfile(sidecar):
        os.remove(sidecar)   # 내용은 이미 매니페스트로 옮겨졌다
    print(f"[승격] {item['dest_dir']}/{args.bom_id}.wav  ({size / 1024:.1f} KB)")
    print(f"[폐기] 착지 원본 삭제 (HARNESS §9 ⑤)")
    print(f"[예산] {projected / 1024:.1f} KB / {AUDIO_BUDGET_BYTES / 1024 / 1024:.0f} MB")
    print("\n남규 통합 안내:")
    kind = "BGM=Vorbis·Streaming" if item["kind"] == "bgm" else "SFX=Vorbis q70·Decompress On Load·2D"
    print(f"  · 임포트 설정: {kind} (BOM §8)")
    print(f"  · 소비자: WorldAudioManager (P4) — 이벤트 훅 연결은 별건")


def main():
    p = argparse.ArgumentParser(description="ElevenLabs 오디오 반입·후공정·승격 파이프라인")
    sub = p.add_subparsers(dest="cmd", required=True)

    sub.add_parser("list", help="BOM §8 오디오 항목·발주 가능 여부").set_defaults(func=cmd_list)
    sub.add_parser(
        "prompt", help="프롬프트 조립 → prompt_builder.py 로 (build/check 는 그쪽 CLI)"
    ).set_defaults(func=lambda a: print(
        "프롬프트 조립은 전용 CLI 를 쓴다:\n"
        "  python scripts/audio/prompt_builder.py build --bom-id <id> [--note '...'] [--length N]\n"
        "  python scripts/audio/prompt_builder.py check"
    ))
    sub.add_parser("status", help="착지 디렉토리 내용 + 오디오 예산").set_defaults(func=cmd_status)

    s = sub.add_parser("intake", help="착지물 검역 + 매니페스트 기록 + bom_id 리네임")
    s.add_argument("--bom-id", required=True)
    s.add_argument("--file", help="착지 디렉토리 내 파일명 (생략 시 가장 최근 생성물)")
    s.add_argument("--license", default=DEFAULT_LICENSE)
    s.set_defaults(func=cmd_intake)

    s = sub.add_parser("normalize", help="루프 크로스페이드 + 피크 정규화")
    s.add_argument("--bom-id", required=True)
    s.add_argument("--peak-db", type=float, default=-1.0, help="목표 피크 (기본 -1.0 dBFS)")
    s.add_argument("--loop-crossfade-ms", type=int, default=0, help="BGM 루프용 (권장 500)")
    s.set_defaults(func=cmd_normalize)

    s = sub.add_parser("promote", help="Assets/Audio 로 승격 (⚠ Unity 접점)")
    s.add_argument("--bom-id", required=True)
    s.add_argument("--yes", action="store_true", help="실제 승격 실행")
    s.set_defaults(func=cmd_promote)

    args = p.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
