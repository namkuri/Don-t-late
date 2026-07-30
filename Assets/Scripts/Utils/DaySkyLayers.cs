using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-116 ⑤ — 촬영용 낮 하늘·구름·타이틀 로고 레이어 (District 1, 가이드 §5).
    /// 카메라 자식으로 배치된 스프라이트들을 구동한다:
    /// 하늘은 오전·낮 시간대에만 보이고, 구름은 서로 다른 속도로 좌우 이동 + 상하 사인 운동,
    /// 카메라 하강 완료 시점부터 구름 1.5초 페이드아웃 · 로고 2초 페이드인.
    /// </summary>
    public class DaySkyLayers : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sky;
        [SerializeField] private SpriteRenderer[] _clouds;
        [SerializeField] private SpriteRenderer _logo;
        [Tooltip("연출 기준시(재생 후 초) — 카메라 하강 완료 시점과 맞춘다 (시작 대기 2s + 하강 10s).")]
        [SerializeField] private float _revealAt = 12f;
        [SerializeField] private float _cloudFadeSeconds = 1.5f;
        [SerializeField] private float _logoFadeSeconds = 2f;
        [Tooltip("구름 좌우 왕복 반경(u)·상하 진폭(u).")]
        [SerializeField] private float _cloudDriftX = 1.2f;
        [SerializeField] private float _cloudBobY = 0.15f;

        private float _elapsed;
        private Vector3[] _cloudHomes;

        private void Awake()
        {
            if (_clouds == null) return;
            _cloudHomes = new Vector3[_clouds.Length];
            for (int i = 0; i < _clouds.Length; i++)
                if (_clouds[i] != null) _cloudHomes[i] = _clouds[i].transform.localPosition;
            if (_logo != null) SetAlpha(_logo, 0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            // 하늘은 오전·낮에만 (밤·저녁 변주에선 조명·LUT가 세계를 물들이게 비켜준다).
            if (_sky != null && WorldDayNightManager.Instance != null)
            {
                DayPhase phase = WorldDayNightManager.Instance.Phase;
                _sky.enabled = phase == DayPhase.Morning || phase == DayPhase.Day;
            }

            float cloudAlpha = 1f - Mathf.Clamp01((_elapsed - _revealAt) / Mathf.Max(0.01f, _cloudFadeSeconds));
            if (_clouds != null)
            {
                for (int i = 0; i < _clouds.Length; i++)
                {
                    SpriteRenderer cloud = _clouds[i];
                    if (cloud == null) continue;
                    // 레이어마다 속도·위상이 다르게 — 인덱스로 결정론 변주.
                    float speed = 0.25f + 0.12f * i;
                    float phase = i * 1.7f;
                    cloud.transform.localPosition = _cloudHomes[i] + new Vector3(
                        Mathf.Sin(_elapsed * speed + phase) * _cloudDriftX,
                        Mathf.Sin(_elapsed * (speed * 1.6f) + phase) * _cloudBobY, 0f);
                    SetAlpha(cloud, cloudAlpha);
                }
            }

            if (_logo != null)
                SetAlpha(_logo, Mathf.Clamp01((_elapsed - _revealAt) / Mathf.Max(0.01f, _logoFadeSeconds)));
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
