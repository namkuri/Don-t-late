# -*- coding: utf-8 -*-
"""art_swap.py — 플레이스홀더 ↔ 실아트 스왑 도구 (S-109 · 넣고 빼기 1커맨드 구조화).

사용:
    python scripts/art_swap.py swap <bom_id> <_intake 상대경로>   # 반입 (복사 — 원본 보존)
    python scripts/art_swap.py unswap <bom_id>                    # 원복 (Art쪽 삭제 → 코드 폴백 부활)
    python scripts/art_swap.py list                               # 현재 스왑 상태

동작: bom_id 접두로 Art/ 분류 자동 결정(fur_|prop_→Props · fx_|bg_→Backgrounds · ui_→UI ·
chr_→Characters · bld_→Buildings · por_→Portraits) → 확장자 유지 복사 → planning/swap-ledger.md
append(누가 어디서 언제 — unswap·재스왑 이력 포함). _intake 원본은 절대 건드리지 않는다.
"""
import io
import os
import shutil
import sys
import datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LEDGER = os.path.join(ROOT, "planning", "swap-ledger.md")

PREFIX_DIR = [
    ("fur_", "Props"), ("prop_", "Props"), ("fx_", "Backgrounds"), ("bg_", "Backgrounds"),
    ("ui_", "UI"), ("chr_", "Characters"), ("bld_", "Buildings"), ("por_", "Portraits"),
]


def art_dir(bom_id):
    for prefix, folder in PREFIX_DIR:
        if bom_id.startswith(prefix):
            return os.path.join(ROOT, "Assets", "Art", folder)
    sys.exit("[art_swap] bom_id 접두를 분류로 매핑 못함: " + bom_id + " (지원: " +
             ", ".join(p for p, _ in PREFIX_DIR) + ")")


def ledger_append(line):
    now = datetime.datetime.now().strftime("%Y-%m-%d %H:%M")
    header = not os.path.exists(LEDGER)
    with io.open(LEDGER, "a", encoding="utf-8", newline="\n") as f:
        if header:
            f.write(u"# swap-ledger.md — 실아트 스왑 이력 (art_swap.py 생성 · append-only)\n\n"
                    u"| 시각 | 동작 | bom_id | 소스(_intake) → 목적지(Art) |\n|---|---|---|---|\n")
        f.write(u"| %s | %s\n" % (now, line))


def find_art_file(bom_id):
    folder = art_dir(bom_id)
    if not os.path.isdir(folder):
        return None
    for name in os.listdir(folder):
        if os.path.splitext(name)[0] == bom_id and not name.endswith(".meta"):
            return os.path.join(folder, name)
    return None


def cmd_swap(bom_id, intake_rel):
    src = os.path.join(ROOT, intake_rel)
    if not os.path.isfile(src):
        sys.exit("[art_swap] 소스 없음: " + intake_rel)
    if "_intake" not in intake_rel.replace("\\", "/"):
        sys.exit("[art_swap] 소스는 _intake 경로만 허용 (원본 보존 계약)")

    folder = art_dir(bom_id)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    ext = os.path.splitext(src)[1].lower()
    dst = os.path.join(folder, bom_id + ext)

    existing = find_art_file(bom_id)
    replaced = existing is not None
    if existing and existing != dst:  # 확장자 다른 구버전 정리 (meta 포함)
        os.remove(existing)
        if os.path.exists(existing + ".meta"):
            os.remove(existing + ".meta")

    shutil.copy2(src, dst)
    rel_dst = os.path.relpath(dst, ROOT).replace("\\", "/")
    ledger_append(u"%s | %s | %s -> %s |" % ("reswap" if replaced else "swap", bom_id,
                                             intake_rel.replace("\\", "/"), rel_dst))
    print("[art_swap] %s: %s -> %s" % ("RESWAP" if replaced else "SWAP", intake_rel, rel_dst))


def cmd_unswap(bom_id):
    target = find_art_file(bom_id)
    if target is None:
        sys.exit("[art_swap] Art에 스왑된 파일 없음: " + bom_id)
    os.remove(target)
    if os.path.exists(target + ".meta"):
        os.remove(target + ".meta")
    rel = os.path.relpath(target, ROOT).replace("\\", "/")
    ledger_append(u"unswap | %s | %s 제거 -> 코드 폴백 |" % (bom_id, rel))
    print("[art_swap] UNSWAP: %s removed -> code fallback (rebuild may be needed)" % rel)


def cmd_list():
    for _, folder in dict(PREFIX_DIR).items():
        pass
    seen = set()
    for prefix, folder in PREFIX_DIR:
        if folder in seen:
            continue
        seen.add(folder)
        path = os.path.join(ROOT, "Assets", "Art", folder)
        if not os.path.isdir(path):
            continue
        files = [n for n in os.listdir(path) if not n.endswith(".meta") and os.path.isfile(os.path.join(path, n))]
        if files:
            print("Art/%s: %s" % (folder, ", ".join(sorted(files))))


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    command = sys.argv[1]
    if command == "swap" and len(sys.argv) == 4:
        cmd_swap(sys.argv[2], sys.argv[3])
    elif command == "unswap" and len(sys.argv) == 3:
        cmd_unswap(sys.argv[2])
    elif command == "list":
        cmd_list()
    else:
        print(__doc__)
        sys.exit(1)
