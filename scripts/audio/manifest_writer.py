#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""manifest_writer.py — planning/assets_manifest.md 에 오디오 반입 행을 append 한다.

HARNESS §9 "② 기록이 입장권": 착지와 동시에 기록해야 검역으로 넘어간다.
hooks/pre-commit §2 가 Assets/Audio/* 신규 바이너리를 이 파일에서 grep -F 로 찾으므로,
dest 경로(파일명 포함)가 표에 있어야 커밋이 통과한다.
"""
import datetime
import os
import re
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")  # Windows cp949 콘솔 대응

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
MANIFEST = os.path.join(ROOT, "planning", "assets_manifest.md")

SECTION = "## ElevenLabs 오디오 INTAKE"
HEADER = (
    "\n" + SECTION + "\n\n"
    "> 출처 = ElevenLabs 공식 API(MCP `compose_music` / `text_to_sound_effects`).\n"
    "> 라이선스 = 유료 플랜 상업 이용 허용(라이선스 데이터 학습). **게임 배포용은 추가 라이선스 확인 대상** —\n"
    "> 확정 전에는 그 취지를 라이선스 열에 남긴다. 프롬프트 원본 = `scripts/audio/prompts/<bom_id>.md` (재현성).\n\n"
    "| bom_id | 원파일명 | dest | gen | 출처 | 라이선스 | 반입일 |\n"
    "|---|---|---|---|---|---|---|\n"
)


def _read():
    with open(MANIFEST, encoding="utf-8") as f:
        return f.read()


def next_gen(bom_id, text=None):
    """같은 bom_id의 기존 행 수 + 1 = 이번 세대. HARNESS §9 ④ gen 카운트."""
    text = _read() if text is None else text
    return len(re.findall(r"^\|\s*" + re.escape(bom_id) + r"\s*\|", text, re.M)) + 1


def append(bom_id, src_name, dest, license_note, source="ElevenLabs (MCP compose_music)", gen=None):
    """매니페스트에 행 1개를 추가하고 gen을 돌려준다."""
    text = _read()
    gen = next_gen(bom_id, text) if gen is None else gen
    today = datetime.date.today().isoformat()
    row = (
        f"| {bom_id} | {src_name} | `{dest}` | {gen} | {source} "
        f"(프롬프트 `scripts/audio/prompts/{bom_id}.md`) | {license_note} | {today} |\n"
    )

    if SECTION not in text:
        text = text.rstrip("\n") + "\n" + HEADER + row
    else:
        # 섹션의 표 마지막 행 뒤에 삽입 (다음 '## ' 헤딩 직전까지가 이 섹션)
        start = text.index(SECTION)
        nxt = re.search(r"^## ", text[start + len(SECTION):], re.M)
        end = start + len(SECTION) + nxt.start() if nxt else len(text)
        block = text[start:end].rstrip("\n")
        text = text[:start] + block + "\n" + row + text[end:]

    with open(MANIFEST, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    return gen


if __name__ == "__main__":
    print(f"매니페스트: {MANIFEST}")
    print("이 모듈은 audio_pipeline.py 가 호출한다 (직접 실행은 경로 확인용).")
