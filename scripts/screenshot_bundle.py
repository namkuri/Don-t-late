#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""screenshot_bundle.py — unity-cli로 Greybox·District 순회 스크린샷 수집 (체크포인트용).

동작: 현재 씬 기록 → (씬이 dirty면 중단 — 에디터 작업 보호) → 각 씬을 열고
game 뷰 스크린샷을 Screenshots/bundle_<YYYY-MM-DD>/<씬>.png 로 저장 → 원래 씬 복귀.
씬을 저장하지 않는다(파일 불변).

전제: Unity 에디터 가동 중. exit 0=성공 · 1=일부 실패 · 2=중단(에디터/미저장 씬).
사용: python scripts/screenshot_bundle.py
"""
import datetime
import subprocess
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")  # Windows cp949 콘솔 대응

SCENES = [
    ("Greybox", "Assets/Scenes/Greybox.unity"),
    ("District", "Assets/Scenes/District.unity"),
]


def uexec(code: str, timeout: int = 120) -> str:
    p = subprocess.run(["unity-cli", "exec", code],
                       capture_output=True, text=True, encoding="utf-8", timeout=timeout)
    if p.returncode != 0:
        raise RuntimeError((p.stdout or "") + (p.stderr or ""))
    return (p.stdout or "").strip().strip('"')


def main() -> int:
    try:
        cur = uexec("return EditorSceneManager.GetActiveScene().path + '|' + "
                    "EditorSceneManager.GetActiveScene().isDirty;")
    except (FileNotFoundError, RuntimeError, subprocess.TimeoutExpired) as e:
        print(f"[screenshot_bundle] unity-cli exec 실패 — 에디터 가동 확인: {e}")
        return 2
    cur_path, dirty = cur.rsplit("|", 1)
    if dirty.strip().lower() == "true":
        print(f"[screenshot_bundle] 중단: 현재 씬({cur_path})에 미저장 변경 — 저장/폐기 후 재실행.")
        return 2

    day = datetime.date.today().isoformat()
    out_dir = f"Screenshots/bundle_{day}"
    fails = 0
    for name, path in SCENES:
        try:
            uexec(f"EditorSceneManager.OpenScene(\"{path}\", OpenSceneMode.Single); return \"ok\";")
            p = subprocess.run(["unity-cli", "screenshot", "--view", "game",
                                "--output_path", f"{out_dir}/{name.lower()}.png"],
                               capture_output=True, text=True, encoding="utf-8", timeout=120)
            if p.returncode != 0:
                raise RuntimeError(p.stdout + p.stderr)
            print(f"[screenshot_bundle] {name} → {out_dir}/{name.lower()}.png")
        except (RuntimeError, subprocess.TimeoutExpired) as e:
            print(f"[screenshot_bundle] {name} 실패: {e}")
            fails += 1
    # 원래 씬 복귀
    if cur_path:
        try:
            uexec(f"EditorSceneManager.OpenScene(\"{cur_path}\", OpenSceneMode.Single); return \"ok\";")
            print(f"[screenshot_bundle] 원래 씬 복귀: {cur_path}")
        except (RuntimeError, subprocess.TimeoutExpired):
            print(f"[screenshot_bundle] 경고: 원래 씬({cur_path}) 복귀 실패 — 수동 복귀 필요.")
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main())
