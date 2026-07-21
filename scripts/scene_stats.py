#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""scene_stats.py — unity-cli exec로 활성 씬 통계 집계 → TECH_SPEC 예산 대비 표.

집계: 총 tri(활성 씬 렌더러의 sharedMesh) · 렌더러 수(DC 근사 상한) ·
텍스처 런타임 메모리(씬 머티리얼이 참조하는 텍스처 중복 제거 합).
예산(TECH_SPEC): tris < 200,000 · DC < 150 · tex < 96MB.

전제: Unity 에디터 가동 중(unity-cli 커넥터). exit 0=예산 내 · 1=초과 항목 존재 · 2=실행 실패.
사용: python scripts/scene_stats.py
"""
import json
import subprocess
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")  # Windows cp949 콘솔 대응

BUDGET_TRIS = 200_000
BUDGET_DC = 150
BUDGET_TEX_MB = 96.0

CSHARP = r"""
var scene = EditorSceneManager.GetActiveScene();
long tris = 0; int rend = 0; long texBytes = 0;
var seen = new HashSet<Texture>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) {
    if (r.gameObject.scene != scene) continue;
    rend++;
    Mesh m = null;
    if (r is SkinnedMeshRenderer smr) m = smr.sharedMesh;
    else { var mf = r.GetComponent<MeshFilter>(); if (mf != null) m = mf.sharedMesh; }
    if (m != null) for (int s = 0; s < m.subMeshCount; s++) tris += (long)m.GetIndexCount(s) / 3;
    foreach (var mat in r.sharedMaterials) {
        if (mat == null) continue;
        foreach (var id in mat.GetTexturePropertyNameIDs()) {
            var t = mat.GetTexture(id);
            if (t != null && seen.Add(t)) texBytes += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
        }
    }
}
return scene.name + "|" + tris + "|" + rend + "|" + texBytes;
"""


def main() -> int:
    try:
        p = subprocess.run(
            ["unity-cli", "exec", CSHARP],
            capture_output=True, text=True, encoding="utf-8", timeout=120, shell=False,
        )
    except FileNotFoundError:
        print("[scene_stats] unity-cli 없음 — PATH 확인.")
        return 2
    except subprocess.TimeoutExpired:
        print("[scene_stats] unity-cli exec 타임아웃 — 에디터 상태 확인.")
        return 2
    out = (p.stdout or "").strip()
    if p.returncode != 0 or "|" not in out:
        print("[scene_stats] exec 실패 — Unity 에디터가 켜져 있는지 확인하라.")
        print(out or p.stderr)
        return 2

    # exec 출력은 JSON 문자열("...")일 수도, 생 문자열일 수도 있다 — 둘 다 수용
    line = next(l for l in out.splitlines() if "|" in l).strip()
    try:
        line = json.loads(line)
    except (ValueError, TypeError):
        pass
    name, tris_s, rend_s, texb_s = line.strip('"').split("|")
    tris, rend, tex_mb = int(tris_s), int(rend_s), int(texb_s) / 1048576.0

    rows = [
        ("총 tris", f"{tris:,}", f"< {BUDGET_TRIS:,}", tris < BUDGET_TRIS),
        ("렌더러 수 (DC 근사 상한)", f"{rend}", f"< {BUDGET_DC}", rend < BUDGET_DC),
        ("텍스처 메모리", f"{tex_mb:.1f} MB", f"< {BUDGET_TEX_MB:.0f} MB", tex_mb < BUDGET_TEX_MB),
    ]
    print(f"# scene_stats — 활성 씬: {name} (TECH_SPEC 예산 대비)")
    print()
    print("| 항목 | 측정값 | 예산 | 판정 |")
    print("|---|---|---|---|")
    over = False
    for label, val, budget, ok in rows:
        print(f"| {label} | {val} | {budget} | {'OK' if ok else '**초과**'} |")
        over = over or not ok
    print()
    print("주: 렌더러 수는 배칭 전 상한 근사 — 실제 DC는 Frame Debugger로 확정.")
    return 1 if over else 0


if __name__ == "__main__":
    sys.exit(main())
