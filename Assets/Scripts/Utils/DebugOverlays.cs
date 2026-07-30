using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 화면 구석 개발 정보(버전 라벨·BGM 디버그 라인)의 공용 on/off 게이트 (S-107 ④ — 촬영용).
    /// F1로 토글. 상태만 보관 — 각 오버레이가 표시 직전에 Visible을 읽는다.
    /// </summary>
    public static class DebugOverlays
    {
        public static bool Visible { get; private set; } = true;

        /// <summary>S-122 ⑯ — 촬영 모드 강제 소거·복구 (DistrictCaptureDemo). F1 토글과 같은 상태를 쓴다.</summary>
        public static void SetVisible(bool on) => Visible = on;

        /// <summary>프레임당 1회 호출(VersionLabel이 담당 — Core 상주라 전 씬 커버). 중복 호출 무해.</summary>
        public static void Tick()
        {
            if (_tickedFrame == Time.frameCount) return;
            _tickedFrame = Time.frameCount;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                Visible = !Visible;
                Debug.Log("[오버레이] 개발 정보 표시 = " + (Visible ? "ON" : "OFF") + " (F1)");
            }
        }

        private static int _tickedFrame = -1;
    }
}
