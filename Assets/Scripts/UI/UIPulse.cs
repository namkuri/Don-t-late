using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// UI 알파 펄스(View 연출) — S-026 아트팀 발주. 진행 표시 상자 "깜박"과 서브 로고 "반짝"에 쓴다.
    /// 활성화되어 있는 동안 Graphic 알파를 사인파로 왕복. 일시정지 무관(unscaled).
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class UIPulse : MonoBehaviour
    {
        [SerializeField] private float _minAlpha = 0.35f;
        [SerializeField] private float _maxAlpha = 1f;
        [SerializeField] private float _speed = 3f;

        private Graphic _graphic;
        private float _phase;

        private void Awake() => _graphic = GetComponent<Graphic>();

        private void OnEnable() => _phase = 0f;

        private void OnDisable()
        {
            if (_graphic == null) return;
            Color color = _graphic.color;
            color.a = _maxAlpha;
            _graphic.color = color;
        }

        private void Update()
        {
            _phase += Time.unscaledDeltaTime * _speed;
            float alpha = Mathf.Lerp(_minAlpha, _maxAlpha, (Mathf.Sin(_phase) + 1f) * 0.5f);
            Color color = _graphic.color;
            color.a = alpha;
            _graphic.color = color;
        }

        /// <summary>빌더 배선용 파라미터 주입.</summary>
        public void Configure(float minAlpha, float maxAlpha, float speed)
        {
            _minAlpha = minAlpha;
            _maxAlpha = maxAlpha;
            _speed = speed;
        }
    }
}
