#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""bom_audio.py — planning/BOM.md §8(오디오)을 파싱해 bom_id 목록·스펙을 제공한다.

BOM이 오디오의 단일 진실이다. 파이프라인은 여기 없는 bom_id를 만들지 않는다
(CODE_RULES §7 YAGNI · pipelines/audio.md "JUICE 이벤트 밖 SFX는 만들지 않는다").

JUICE 근거 열이 ❌ 인 항목은 J-1 개정안 미승인 상태 → 발주 불가로 표시된다.
"""
import os
import re
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")  # Windows cp949 콘솔 대응

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BOM_PATH = os.path.join(ROOT, "planning", "BOM.md")

DEST = {"bgm": "Assets/Audio/BGM", "sfx": "Assets/Audio/SFX"}


def _cells(line):
    """마크다운 표 한 줄 → 셀 리스트. 구분선(---)이면 None."""
    if not line.lstrip().startswith("|"):
        return None
    parts = [c.strip() for c in line.strip().strip("|").split("|")]
    if all(set(p) <= set("-: ") for p in parts):
        return None
    return parts


def _split_ids(cell):
    """`sfx_rhythm_hit / _miss` 같은 축약 표기를 개별 bom_id로 편다."""
    cell = cell.replace("`", "").strip()
    tokens = [t.strip() for t in cell.split("/") if t.strip()]
    if not tokens:
        return []
    ids = [tokens[0]]
    for t in tokens[1:]:
        if t.startswith("_"):
            prefix = tokens[0].rsplit("_", 1)[0]  # sfx_rhythm_hit → sfx_rhythm
            ids.append(prefix + t)
        else:
            ids.append(t)
    return ids


def _section(text):
    """`## 8. 오디오` ~ 다음 `## ` 직전 구간만 잘라낸다."""
    start = re.search(r"^##\s*8\.\s*오디오", text, re.M)
    if not start:
        raise RuntimeError("BOM.md에서 '## 8. 오디오' 절을 찾지 못했다.")
    rest = text[start.end():]
    end = re.search(r"^##\s", rest, re.M)
    return rest[: end.start()] if end else rest


def load(bom_path=BOM_PATH):
    """bom_id → dict(kind, desc, spec, juice_ok, dest_dir, note) 를 돌려준다."""
    with open(bom_path, encoding="utf-8") as f:
        body = _section(f.read())

    items, kind = {}, None
    for line in body.splitlines():
        if line.startswith("### BGM"):
            kind = "bgm"
            continue
        if line.startswith("### SFX"):
            kind = "sfx"
            continue
        if kind is None:
            continue
        cells = _cells(line)
        if not cells or len(cells) < 4:
            continue
        if cells[0] in ("bom_id", ""):
            continue
        if not cells[0].replace("`", "").startswith(("bgm_", "sfx_", "amb_")):
            continue

        for bid in _split_ids(cells[0]):
            items[bid] = {
                "kind": kind,
                "desc": cells[1],
                "spec": cells[2],
                # BGM 표엔 JUICE 열이 없다 — 이미 승인된 항목으로 본다.
                "juice_ok": True if kind == "bgm" else ("✓" in cells[3]),
                "dest_dir": DEST[kind],
                "note": cells[4] if len(cells) > 4 else "",
            }
    return items


def fallback(bom_id):
    """BOM에 없는 id를 위한 임시 항목.

    지침 위계(2026-07-21): **생성 단계의 제1지침은 GAME-BGM-RULES.md**이고 BOM은 참고다.
    따라서 미등재 id로도 프롬프트 조립·생성은 가능해야 한다(경고만).
    다만 `Assets/`로 들어가는 반입·승격은 여전히 BOM이 지배한다 — audio_pipeline 쪽 게이트는 유지.
    """
    kind = "sfx" if bom_id.startswith("sfx_") else "bgm"
    return {
        "kind": kind,
        "desc": "(BOM 미등재)",
        "spec": "(BOM 미등재 — 생성은 규격만으로 진행)",
        "juice_ok": True,
        "dest_dir": DEST[kind],
        "note": "BOM §8 등재 후에만 반입·승격 가능",
    }


def resolve(bom_id, items=None):
    """생성 단계용 조회 — 없거나 JUICE 미승인이면 **경고만** 하고 진행한다."""
    items = load() if items is None else items
    if bom_id not in items:
        print(f"  ⚠ '{bom_id}' 는 BOM §8에 없다 — 생성은 진행하되 반입·승격은 등재 후에만 가능하다.")
        return fallback(bom_id)
    item = items[bom_id]
    if not item["juice_ok"]:
        print(f"  ⚠ '{bom_id}' 는 JUICE 근거가 없다(J-1 미승인) — 생성은 진행하되 반입은 차단된다.")
    return item


def main():
    items = load()
    print(f"BOM §8 오디오 항목 {len(items)}건 ({BOM_PATH})\n")
    for bid, it in items.items():
        gate = "발주가능" if it["juice_ok"] else "JUICE 미승인(J-1 대기) — 발주 불가"
        print(f"  [{it['kind']}] {bid:<20} {gate}")
        print(f"        {it['desc']} · {it['spec']}")
    ok = sum(1 for i in items.values() if i["juice_ok"])
    print(f"\n발주 가능 {ok}건 / 전체 {len(items)}건")


if __name__ == "__main__":
    main()
