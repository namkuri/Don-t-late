using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-144 — 타이틀 진입 연출. 카메라가 **하늘에서 수직으로** 제자리까지 천천히 내려앉는다.
    ///
    /// 착지점은 씬에 저장된 카메라 위치 그 자체다 — 인스펙터에 목표를 따로 적지 않는다.
    /// 빌더가 카메라 위치를 바꾸면 착지점도 따라 바뀌므로 두 값이 어긋날 일이 없다.
    ///
    /// 수평 성분은 건드리지 않는다(요구: "수직으로"). X·Z는 착지점 값을 그대로 유지하고
    /// Y만 위에서 아래로 움직인다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class TitleCameraDrop : MonoBehaviour
    {
        [Tooltip("착지점 기준 시작 고도(u). 이 높이만큼 위에서 출발한다.")]
        [SerializeField] private float _dropHeight = 42f;

        [Tooltip("내려앉는 데 걸리는 시간(초).")]
        [SerializeField] private float _duration = 4.5f;

        [Tooltip("착지 후 다음 연출까지의 여유(초) — 지금은 로그용 표식.")]
        [SerializeField] private float _settlePause = 0.4f;

        private Vector3 _landing;
        private float _elapsed;
        private bool _done;

        private void Awake()
        {
            // 착지점 = 씬에 저장된 위치. Awake에서 잡아둔다(다른 컴포넌트가 옮기기 전에).
            _landing = transform.position;
            transform.position = _landing + Vector3.up * _dropHeight;
        }

        private void Update()
        {
            if (_done) return;

            _elapsed += Time.deltaTime;
            float t = _duration > 0.01f ? Mathf.Clamp01(_elapsed / _duration) : 1f;

            // 감속 착지 — 처음엔 빠르게 떨어지고 끝에서 부드럽게 멈춘다.
            // 등속으로 두면 착지 순간이 뚝 끊겨 "떨어졌다"가 아니라 "순간이동"으로 읽힌다.
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            Vector3 position = transform.position;
            position.y = Mathf.Lerp(_landing.y + _dropHeight, _landing.y, eased);
            transform.position = position;

            if (t < 1f) return;

            transform.position = _landing; // 부동소수 오차 제거 — 정확히 착지점에 앉힌다.
            _done = true;
        }

        /// <summary>강하가 끝났는지. 다른 연출이 착지 뒤에 붙고 싶을 때 본다.</summary>
        public bool Landed => _done;

        /// <summary>착지 후 여유 시간(초).</summary>
        public float SettlePause => _settlePause;
    }
}
