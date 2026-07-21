using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 하늘 천체 구동(S-010) — 시간에 따라 해·달은 지평선 아래 피벗을 중심으로 포물선(반타원)을
    /// 그리고, 별밭은 천천히 회전한다. ClockTicked(분 단위) 구독 — 프레임 데이터 아님.
    /// 해(정점 13시)와 달(정점 1시)은 위상이 반대라 아침·저녁에 교차한다.
    /// </summary>
    public class SkyBodyOrbit : MonoBehaviour
    {
        private enum Mode { Arc, Spin }

        [SerializeField] private Mode _mode = Mode.Arc;

        [Header("Arc — 해·달")]
        [Tooltip("궤도 피벗. 지평선 아래에 둬야 뜨고 지는 그림이 된다.")]
        [SerializeField] private Vector3 _center = new Vector3(0f, -6f, 69.5f);
        [SerializeField] private float _radiusX = 55f;
        [SerializeField] private float _radiusY = 34f;
        [Tooltip("남중(정점) 시각 (0~24시). 해=13, 달=1.")]
        [SerializeField] private float _peakHour = 13f;

        [Header("Spin — 별밭")]
        [Tooltip("하루당 회전 각도. 쿼드 모서리 노출을 피해 완만하게.")]
        [SerializeField] private float _spinDegreesPerDay = 30f;

        private void OnEnable() => WorldEvents.ClockTicked += OnClockTicked;
        private void OnDisable() => WorldEvents.ClockTicked -= OnClockTicked;

        private void OnClockTicked(GameClock clock)
        {
            float t = clock.MinuteOfDay / 1440f;

            if (_mode == Mode.Arc)
            {
                // 정점 시각에 각도 90°(꼭대기). 시간이 지나며 서쪽(-X)으로 내려간다.
                float angle = (90f + (t - _peakHour / 24f) * 360f) * Mathf.Deg2Rad;
                transform.position = _center + new Vector3(
                    Mathf.Cos(angle) * _radiusX, Mathf.Sin(angle) * _radiusY, 0f);
            }
            else
            {
                transform.localRotation = Quaternion.Euler(0f, 0f, t * _spinDegreesPerDay);
            }
        }
    }
}
