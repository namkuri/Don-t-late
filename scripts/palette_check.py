#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""palette_check.py <png> — 스크린샷 색분포 vs STYLE 팔레트 4색 근접도 리포트.

차단이 아닌 신호기: 각 픽셀을 팔레트 최근접 색에 귀속시키고
점유율·평균거리·거리 히스토그램을 출력한다. exit code는 항상 0.

사용: python scripts/palette_check.py Screenshots/night_v3.png
"""
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")  # Windows cp949 콘솔 대응

PALETTE = {
    "#0a0d16 (네이비 바탕)":   (0x0A, 0x0D, 0x16),
    "#ff9f45 (앰버 가로등)":   (0xFF, 0x9F, 0x45),
    "#35e0c8 (시안 상호작용)": (0x35, 0xE0, 0xC8),
    "#ff4658 (경고 레드)":     (0xFF, 0x46, 0x58),
}
# 거리 히스토그램 버킷 (RGB 유클리드, 최대 √(3·255²)≈441.7)
BUCKETS = [0, 32, 64, 96, 128, 192, 442]


def main() -> int:
    if len(sys.argv) != 2:
        print("사용법: python scripts/palette_check.py <png>")
        return 2
    try:
        from PIL import Image
    except ImportError:
        print("[palette_check] PIL(Pillow) 없음 — `pip install pillow` 후 재실행.")
        return 2

    path = sys.argv[1]
    img = Image.open(path).convert("RGB")
    w, h = img.size
    # 대형 스크린샷은 다운샘플(정보 손실 무시 가능 — 분포 신호기 용도)
    if w * h > 512 * 512:
        img.thumbnail((512, 512), Image.NEAREST)
        w, h = img.size
    data = img.tobytes()  # RGB 연속 바이트
    n = w * h

    names = list(PALETTE.keys())
    cols = list(PALETTE.values())
    counts = [0] * len(cols)
    dist_sum = [0.0] * len(cols)
    hist = [0] * (len(BUCKETS) - 1)

    for off in range(0, n * 3, 3):
        r, g, b = data[off], data[off + 1], data[off + 2]
        best_i, best_d2 = 0, 1 << 30
        for i, (pr, pg, pb) in enumerate(cols):
            d2 = (r - pr) ** 2 + (g - pg) ** 2 + (b - pb) ** 2
            if d2 < best_d2:
                best_i, best_d2 = i, d2
        d = best_d2 ** 0.5
        counts[best_i] += 1
        dist_sum[best_i] += d
        for k in range(len(BUCKETS) - 1):
            if BUCKETS[k] <= d < BUCKETS[k + 1]:
                hist[k] += 1
                break

    print(f"# palette_check — {path} ({w}x{h} 샘플 {n}px)")
    print()
    print("| 팔레트 색 | 최근접 점유율 | 평균거리 |")
    print("|---|---|---|")
    for i, name in enumerate(names):
        share = counts[i] / n * 100
        mean = dist_sum[i] / counts[i] if counts[i] else 0.0
        print(f"| {name} | {share:5.1f}% | {mean:6.1f} |")
    print()
    print("거리 히스토그램 (픽셀→최근접 팔레트색 RGB 유클리드 거리):")
    for k in range(len(hist)):
        share = hist[k] / n * 100
        bar = "#" * int(share / 2)
        print(f"  [{BUCKETS[k]:3d}~{BUCKETS[k+1]:3d}) {share:5.1f}% {bar}")
    near = sum(hist[:2]) / n * 100
    print()
    print(f"신호: 거리<64 픽셀 비율 = {near:.1f}% (높을수록 팔레트 밀착 — 차단 아님, 참고용)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
