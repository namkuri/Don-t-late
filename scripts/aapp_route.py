# -*- coding: utf-8 -*-
"""aapp_route.py — AAPP C4 라우팅 시트 자동 생성기 (S-095 실가동).

사용:
    python scripts/aapp_route.py <bom_id> <type> [--source X] [--fabrication X]
        [--socket "대상 소켓"] [--status queued|running|done] [--note "비고"]

동작: planning/aapp/process-map.yaml 규칙을 위에서부터 매칭 → templates.yaml에서
공정·표준시간을 가져와 planning/routing/RT-<날짜>-<seq>.md 시트를 발행한다.
(외부 의존 없음 — 단순 YAML은 자체 파서로 읽는다.)
"""
import io
import os
import re
import sys
import datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AAPP = os.path.join(ROOT, "planning", "aapp")
ROUTING = os.path.join(ROOT, "planning", "routing")


def parse_rules(path):
    rules = []
    for line in io.open(path, encoding="utf-8"):
        m = re.match(r"\s*-\s*when:\s*\{(.+?)\}", line)
        if m:
            cond = {}
            for pair in m.group(1).split(","):
                k, v = pair.split(":")
                cond[k.strip()] = v.strip()
            rules.append({"when": cond, "template": None})
        m2 = re.match(r"\s*template:\s*(\S+)", line)
        if m2 and rules and rules[-1]["template"] is None:
            rules[-1]["template"] = m2.group(1)
    return rules


def parse_template(path, name):
    text = io.open(path, encoding="utf-8").read()
    block = re.search(r"^" + re.escape(name) + r":[^\n]*\n(.*?)(?=^\S|\Z)", text, re.M | re.S)
    if not block:
        return [], None
    body = block.group(1)
    steps = re.findall(r"\{op:\s*([\w+]+),.*?std_min:\s*(\d+)\}", body)
    est = re.search(r"est_total_min:\s*(\d+)", body)
    return steps, int(est.group(1)) if est else sum(int(s[1]) for s in steps)


def match_template(rules, tags):
    for rule in rules:
        if all(tags.get(k) == v for k, v in rule["when"].items()):
            return rule["template"]
    return "STD-CODE-FEATURE"  # 기본 레인


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    bom_id, asset_type = sys.argv[1], sys.argv[2]
    tags = {"type": asset_type}
    opts = {"socket": "-", "status": "queued", "note": ""}
    args = sys.argv[3:]
    i = 0
    while i < len(args):
        key = args[i].lstrip("-")
        if key in ("source", "fabrication"):
            tags[key] = args[i + 1]
        elif key in opts:
            opts[key] = args[i + 1]
        i += 2

    rules = parse_rules(os.path.join(AAPP, "process-map.yaml"))
    template = match_template(rules, tags)
    steps, est = parse_template(os.path.join(AAPP, "templates.yaml"), template)

    os.makedirs(ROUTING, exist_ok=True)
    today = datetime.date.today().strftime("%Y%m%d")
    # 결번이 있어도 충돌하지 않도록 최대 번호+1 (개수+1 방식은 2026-07-29 실제 덮어쓰기 사고)
    used = [int(m.group(1)) for f in os.listdir(ROUTING)
            for m in [re.match(r"RT-" + today + r"-(\d+)\.md$", f)] if m]
    seq = max(used) + 1 if used else 1
    sheet_id = "RT-%s-%02d" % (today, seq)
    path = os.path.join(ROUTING, sheet_id + ".md")

    step_names = [s[0] for s in steps] if steps else ["(템플릿 참조)"]
    lines = [
        "# 라우팅 시트 %s — %s" % (sheet_id, bom_id),
        "",
        "> aapp_route.py 자동 발행 (S-095 실가동) · 규칙 매칭: %s" % template,
        "",
        "```yaml",
        "routing:",
        "  bom_id: %s" % bom_id,
        "  asset_type: %s" % asset_type,
        "  template: %s" % template,
        "  variations: %s" % ([("%s=%s" % (k, v)) for k, v in tags.items() if k != "type"] or "[]"),
        "  steps: [%s]" % ", ".join(step_names),
        "  target_socket: %s" % opts["socket"],
        "  est_total_min: %s" % est,
        "  status: %s" % opts["status"],
        "```",
        "",
    ]
    if opts["note"]:
        lines.append("- 비고: %s" % opts["note"])
    if steps:
        lines += ["", "## 공정 (표준시간)", "", "| op | std_min |", "|---|---|"]
        lines += ["| %s | %s |" % (op, mins) for op, mins in steps]
    lines += ["", "## 실행 기록", "", "| step | 결과 | 시각 |", "|---|---|---|", "| (미실행) | — | — |", ""]

    io.open(path, "w", encoding="utf-8", newline="\n").write("\n".join(lines))
    print("[aapp_route] %s issued: template=%s est=%smin status=%s" % (sheet_id, template, est, opts["status"]))


if __name__ == "__main__":
    main()
