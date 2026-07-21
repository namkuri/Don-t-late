#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""new_order.py <domain> <id> <수신자> — 발주 스켈레톤(5칸+시각 헤더) append.

planning/orders/<domain>.md 에 v3 형식 봉투 스켈레톤을 덧붙인다 (append-only).
파일이 없으면 대장 헤더와 함께 생성. leadtime_report.py가 이 헤더를 파싱한다.

사용: python scripts/new_order.py system S-002 "general-purpose 서브에이전트"
"""
import datetime
import os
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")  # Windows cp949 콘솔 대응

TEMPLATE = """
---

## {oid} · 발주 {now} → {recipient} (제목 미기입)

목표:

입력·산출 위치:
-

기대:
-

수용기준:
-

실패시:

보고:
"""

NEW_FILE_HEADER = """# orders/{domain}.md — {domain} 발주 대장 (append-only)

> 형식: [[guides/distributed-workflow]] §3 v3. 발주·결과 시각은 파일 안에 명시 — 리드타임 자기완결.
> 봉투 전문이 곧 서브에이전트 투입 프롬프트다.
"""


def main() -> int:
    if len(sys.argv) != 4:
        print("사용법: python scripts/new_order.py <domain> <id> <수신자>")
        return 2
    domain, oid, recipient = sys.argv[1], sys.argv[2], sys.argv[3]
    path = os.path.join("planning", "orders", f"{domain}.md")
    now = datetime.datetime.now().strftime("%Y-%m-%d %H:%M")

    if os.path.exists(path):
        with open(path, encoding="utf-8") as f:
            if f"## {oid} " in f.read():
                print(f"[new_order] 중단: {path} 에 {oid} 가 이미 있다 (append-only — id 중복 금지).")
                return 1
        body = TEMPLATE.format(oid=oid, now=now, recipient=recipient)
    else:
        os.makedirs(os.path.dirname(path), exist_ok=True)
        body = NEW_FILE_HEADER.format(domain=domain) + TEMPLATE.format(
            oid=oid, now=now, recipient=recipient)

    with open(path, "a", encoding="utf-8", newline="\n") as f:
        f.write(body)
    print(f"[new_order] {path} ← {oid} 스켈레톤 append (발주 {now} → {recipient})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
