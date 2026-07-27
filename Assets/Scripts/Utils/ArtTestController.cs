using UnityEngine;
using UnityEngine.InputSystem;

namespace DontLate
{
    /// <summary>
    /// 아트 테스트 씬 조작 (A-006). ←→/A·D = 카메라 이동, T = 낮밤 사이클 토글,
    /// 스페이스 = 시간 정지/재생. 민지님이 반입 아트를 게임 룩(픽셀화·조명)에서 확인하는 진열대.
    /// </summary>
    public class ArtTestController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Light _sun;
        [SerializeField] private float _panSpeed = 8f;
        [SerializeField] private float _dayCycleSeconds = 24f; // T 사이클 1바퀴 실초

        private bool _cycling = true;
        private float _time01 = 0.35f; // 아침 시작

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _camera == null) return;

            float pan = 0f;
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) pan -= 1f;
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) pan += 1f;
            _camera.transform.position += Vector3.right * (pan * _panSpeed * Time.deltaTime);

            if (keyboard.tKey.wasPressedThisFrame) _cycling = !_cycling;
            if (_cycling) _time01 = Mathf.Repeat(_time01 + Time.deltaTime / _dayCycleSeconds, 1f);

            ApplySun();
        }

        // 간이 낮밤 — 태양 각도·색·강도 램프 (Core의 DayNight 근사).
        private void ApplySun()
        {
            if (_sun == null) return;
            float angle = _time01 * 360f - 90f; // 0.25=정오
            _sun.transform.rotation = Quaternion.Euler(angle, -30f, 0f);

            float daylight = Mathf.Clamp01(Mathf.Sin(_time01 * Mathf.PI * 2f - Mathf.PI * 0.5f) * 1.4f + 0.5f);
            _sun.intensity = Mathf.Lerp(0.06f, 1.15f, daylight);
            _sun.color = Color.Lerp(new Color(0.55f, 0.62f, 0.85f), new Color(1f, 0.96f, 0.88f), daylight);
            RenderSettings.ambientLight = Color.Lerp(new Color(0.10f, 0.12f, 0.20f), new Color(0.55f, 0.57f, 0.62f), daylight);
        }
    }
}
