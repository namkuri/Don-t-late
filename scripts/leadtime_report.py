#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""leadtime_report.py — orders/*.md 발주/결과 헤더 파싱 → planning/calibration.md 생성/갱신.

파싱 대상 (v3 형식 — orders/system.md 상단 규격):
  발주 헤더: `## <ID> · 발주 YYYY-MM-DD HH:MM → <수신자> (...)`
  결과 블록: `### 결과 · YYYY-MM-DD HH:MM (리드 N분)`
발주 하나에 결과 블록이 여러 개면 재시도 = 블록수-1.
형식 불일치 파일(레거시 봉투)은 표 하단에 명시만 한다.

사용: python scripts/leadtime_report.py
"""
import datetime
import glob
import os
import re
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")  # Windows cp949 콘솔 대응

ORDER_RE = re.compile(r"^## (\S+) · 발주 (\d{4}-\d{2}-\d{2} \d{2}:\d{2}) → (.+)$")
# 관대한 매칭 (2026-07-22): 시각 뒤 어떤 괄호·주석이 와도 허용, 표기 리드는 있으면 추출
RESULT_RE = re.compile(r"^### 결과 · (\d{4}-\d{2}-\d{2} \d{2}:\d{2})(?:.*?리드\s*[~약]?\s*(\d+)\s*분)?")
FMT = "%Y-%m-%d %H:%M"
OUT = os.path.join("planning", "calibration.md")


def main() -> int:
    orders = []          # dict: id,file,recipient,issued,results[(time,declared_min)]
    unparsed = []
    for path in sorted(glob.glob(os.path.join("planning", "orders", "*.md"))):
        cur = None
        found = False
        with open(path, encoding="utf-8") as f:
            for line in f:
                m = ORDER_RE.match(line)
                if m:
                    cur = {"id": m.group(1), "file": os.path.basename(path),
                           "issued": m.group(2), "recipient": m.group(3).strip(),
                           "results": []}
                    orders.append(cur)
                    found = True
                    continue
                m = RESULT_RE.match(line)
                if m and cur is not None:
                    cur["results"].append((m.group(1), int(m.group(2)) if m.group(2) else None))
        if not found:
            unparsed.append(os.path.basename(path))

    now = datetime.datetime.now().strftime(FMT)
    lines = [
        "# calibration.md — 발주 리드타임 집계 (leadtime_report.py 생성물 — 수기 편집 금지)",
        "",
        f"> 갱신 {now} · 소스: planning/orders/*.md · 리드 = 마지막 결과 시각 − 발주 시각",
        "",
        "| 발주 | 파일 | 수신자 | 발주 시각 | 결과 시각 | 리드(분) | 표기 리드 | 재시도 |",
        "|---|---|---|---|---|---|---|---|",
    ]
    for o in orders:
        if o["results"]:
            done, declared = o["results"][-1]
            calc = int((datetime.datetime.strptime(done, FMT)
                        - datetime.datetime.strptime(o["issued"], FMT)).total_seconds() // 60)
            retries = len(o["results"]) - 1
            lines.append(f"| {o['id']} | {o['file']} | {o['recipient']} | {o['issued']} "
                         f"| {done} | {calc} | {declared} | {retries} |")
        else:
            lines.append(f"| {o['id']} | {o['file']} | {o['recipient']} | {o['issued']} "
                         f"| — 진행중 | — | — | — |")
    if unparsed:
        lines += ["", "형식 불일치(레거시 봉투 — 미집계): " + " · ".join(unparsed)]
    lines.append("")

    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines))
    done_n = sum(1 for o in orders if o["results"])
    print(f"[leadtime_report] {OUT} 갱신 — 발주 {len(orders)}건 (완료 {done_n} · 진행중 "
          f"{len(orders) - done_n} · 레거시 파일 {len(unparsed)})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
