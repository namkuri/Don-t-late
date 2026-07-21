using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 배송지 문 앞. 들고 온 건이 이 주소와 맞으면 인증되고 완료 처리된다.
    /// 하이라이트는 두 갈래 — 근접 포커스(센서)와 목적지 표시(픽업 이후) 중 하나라도 켜지면 켠다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DeliveryPoint : MonoBehaviour, IInteractable, IFocusGate
    {
        [SerializeField] private DeliveryOrderSO _expectedOrder;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;
        [SerializeField] private Vector2 _padSize = new Vector2(1f, 1f);
        [SerializeField] private GameObject _riseEffect;
        [SerializeField] private float _idleAlpha = 1f;
        [SerializeField] private float _focusedAlpha = 0.3f;

        public Vector2 PadSize => _padSize;

        private bool _focused;
        private bool _isDestination;
        private MaterialPropertyBlock _riseMpb;

        private void OnEnable()
        {
            WorldEvents.PackagePickedUp += OnPackagePickedUp;
            WorldEvents.DeliveryCompleted += OnDeliverySettled;
            WorldEvents.DeliveryFailed += OnDeliverySettled;
        }

        private void OnDisable()
        {
            WorldEvents.PackagePickedUp -= OnPackagePickedUp;
            WorldEvents.DeliveryCompleted -= OnDeliverySettled;
            WorldEvents.DeliveryFailed -= OnDeliverySettled;
        }

        public void Interact(PlayerContext ctx)
        {
            DeliveryOrderSO carried = ctx.Player.Status.CarriedOrder;
            if (carried == null) { Debug.Log("[DeliveryPoint] 빈손 — 상자를 들고 와야 인증된다."); return; }
            if (_expectedOrder != null && carried != _expectedOrder)
            {
                Debug.Log("[DeliveryPoint] 주소 불일치 — 든 건 #" + carried.orderId + ", 이 문은 #" + _expectedOrder.orderId + ".");
                return;
            }
            // 이미 실패(지각)로 적재에서 빠진 건이면 인증 불가 — 상자를 떨어뜨리지 않는다 (S-009 엣지).
            if (!WorldDeliveryManager.Instance.IsInCargo(carried))
            {
                Debug.Log("[DeliveryPoint] #" + carried.orderId + " 은 적재 목록에 없다(지각 실패?) — 인증 불가.");
                return;
            }

            ctx.Player.Status.ReleaseCarry(dropAsPhysics: true);
            WorldDeliveryManager.Instance.CompleteDelivery(carried);
        }

        /// <summary>플레이어 XZ가 패드 사각형(_padSize) 안에 있을 때만 포커스 후보로 인정한다.</summary>
        public bool AllowsFocus(Vector3 playerPosition)
        {
            Vector3 center = transform.position;
            float dx = Mathf.Abs(playerPosition.x - center.x);
            float dz = Mathf.Abs(playerPosition.z - center.z);
            return dx <= _padSize.x * 0.5f && dz <= _padSize.y * 0.5f;
        }

        public void SetHighlight(bool on)
        {
            _focused = on;
            ApplyHighlight();
            ApplyRiseAlpha(on);
        }

        private void OnPackagePickedUp(DeliveryData data)
        {
            if (_expectedOrder == null || data.OrderId != _expectedOrder.orderId) return;
            _isDestination = true;
            ApplyHighlight();
        }

        private void OnDeliverySettled(DeliveryData data)
        {
            if (_expectedOrder == null || data.OrderId != _expectedOrder.orderId) return;
            _isDestination = false;
            // 처리된 배송지는 패드째 완전 소멸 (S-009) — 서 있어도 다시 빛나지 않는다.
            gameObject.SetActive(false);
        }

        private void ApplyHighlight()
        {
            if (_renderer == null) return;
            Material material = (_focused || _isDestination) ? _highlightMaterial : _normalMaterial;
            if (material != null) _renderer.sharedMaterial = material;
        }

        /// <summary>빛기둥 알파를 MaterialPropertyBlock으로 전환한다 — 공유 머티리얼을 오염시키지 않는다.</summary>
        private void ApplyRiseAlpha(bool focused)
        {
            if (_riseEffect == null) return;
            float alpha = focused ? _focusedAlpha : _idleAlpha;
            _riseMpb ??= new MaterialPropertyBlock();
            foreach (Renderer r in _riseEffect.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(_riseMpb);
                _riseMpb.SetFloat("_Alpha", alpha);
                r.SetPropertyBlock(_riseMpb);
            }
            Debug.Log($"[Beacon] rise _Alpha = {alpha} (focused={focused})");
        }
    }
}
