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
