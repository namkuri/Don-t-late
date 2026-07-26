using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 아파트 공동현관 자동 슬라이드문 (S-048 ②). 비번 성공(Unlock) 시 깊이(+Z — 카메라에서 먼 쪽)로 슬라이드 개방(코드 트윈) →
    /// 일정 시간 후 자동 닫힘. 해제된 뒤에는 문 앞 모션 센서(트리거)가 움직임을 감지하면 다시 열린다.
    /// 문 패널 자체가 실콜라이더 — 닫혀 있으면 물리적으로 막힌다.
    /// </summary>
    public class ApartmentSlidingDoor : MonoBehaviour
    {
        [Tooltip("슬라이드하는 문 패널 (실콜라이더 포함).")]
        [SerializeField] private Transform _panel;
        [SerializeField] private float _slideDistance = 1.7f; // 깊이(+Z — 카메라 반대쪽)로 (S-050 ②)
        [SerializeField] private float _slideSeconds = 0.45f;
        [SerializeField] private float _autoCloseSeconds = 4f;

        public bool Unlocked { get; private set; }

        private Vector3 _closedPos;
        private float _openAmount;   // 0=닫힘, 1=열림
        private float _target;
        private float _closeTimer;

        private void Start()
        {
            if (_panel != null) _closedPos = _panel.localPosition;
        }

        /// <summary>비번 성공 — 게이트가 호출. 이후엔 센서 모드.</summary>
        public void Unlock()
        {
            Unlocked = true;
            Open();
            Debug.Log("[자동문] 해제 — 개방 (이후 모션 센서 모드).");
        }

        private void Open()
        {
            _target = 1f;
            _closeTimer = _autoCloseSeconds;
        }

        private void Update()
        {
            if (_panel == null) return;

            if (_target > 0.5f)
            {
                _closeTimer -= Time.deltaTime;
                if (_closeTimer <= 0f) _target = 0f; // 자동 닫힘
            }

            _openAmount = Mathf.MoveTowards(_openAmount, _target, Time.deltaTime / _slideSeconds);
            _panel.localPosition = _closedPos + Vector3.forward * (_slideDistance * _openAmount);
        }

        // 모션 센서 — 이 오브젝트의 트리거 콜라이더(문 앞뒤 구역). 해제 후에만 반응.
        private void OnTriggerStay(Collider other)
        {
            if (!Unlocked || _target > 0.5f) return;
            if (other.GetComponentInParent<PlayerManager>() == null
                && other.GetComponentInParent<DeliveryCart>() == null) return;
            Open();
        }
    }
}
