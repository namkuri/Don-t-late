using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 주변 독백 스팟 (S-123 ① — 남규님 아이디어). 쓰레기통·자판기·횡단보도·포장마차 곁을 지나면
    /// 주인공이 혼잣말을 한다. 말풍선은 <see cref="SpeechBubble"/>이 주인공 머리 위에 띄운다.
    ///
    /// 콜라이더를 새로 붙이지 않는 이유: 배경 프랍에 트리거를 달면 보행·상자 물리에 간섭한다
    /// (S-119 벨트 콜라이더 사고 계열). 스팟이 주기적으로 주변을 질의하는 방식만 쓴다.
    /// </summary>
    public class AmbientRemarkSpot : MonoBehaviour
    {
        [Tooltip("이 스팟에서 나올 혼잣말 (랜덤 1줄).")]
        [SerializeField] private string[] _lines;
        [Tooltip("반응 반경(u).")]
        [SerializeField] private float _radius = 2.6f;

        private const float SPOT_COOLDOWN = 25f;   // 같은 스팟 재발화 간격
        private const float GLOBAL_COOLDOWN = 8f;  // 전 스팟 공통 — 왕복 데모에서 도배 방지
        private const float TICK = 0.3f;

        /// <summary>런타임 생성물(DistrictLayoutGenerator)용 주입 — 빌더는 SerializedObject로 넣는다.</summary>
        public void SetLines(string[] lines) => _lines = lines;

        private static float _globalReadyAt;
        private float _spotReadyAt;
        private float _tick;
        private readonly Collider[] _hits = new Collider[8];

        private void Update()
        {
            _tick -= Time.deltaTime;
            if (_tick > 0f) return;
            _tick = TICK;

            if (_lines == null || _lines.Length == 0) return;
            // S-125 ③ — 촬영 씬은 잡 기능 전면 정지 (남규님 반려: District 1에서 독백이 떴다).
            // 이 컴포넌트는 런타임 생성물이라 빌더의 StripInteractions로는 못 지운다.
            if (DistrictCaptureDemo.CaptureMode) return;
            if (Time.time < _spotReadyAt || Time.time < _globalReadyAt) return;

            int count = Physics.OverlapSphereNonAlloc(transform.position, _radius, _hits,
                ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                PlayerManager player = _hits[i].GetComponentInParent<PlayerManager>();
                if (player == null) continue;
                _spotReadyAt = Time.time + SPOT_COOLDOWN;
                _globalReadyAt = Time.time + GLOBAL_COOLDOWN;
                SpeechBubble.ShowOn(player.gameObject, _lines[Random.Range(0, _lines.Length)], 2.2f);
                return;
            }
        }
    }
}
