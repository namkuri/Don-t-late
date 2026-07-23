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
        [Tooltip("패드 위 포커스 시 나타나는 주소 라벨(월드 텍스트) — S-016 ②.")]
        [SerializeField] private TMPro.TMP_Text _addressLabel;
        [SerializeField] private float _idleAlpha = 1f;
        [SerializeField] private float _focusedAlpha = 0.3f;

        public Vector2 PadSize => _padSize;
        /// <summary>HUD 풀해상 표시용 주소 (S-021 ② — 월드 텍스트는 픽셀레이트에 뭉개져 폐지).</summary>
        public string Address => _expectedOrder != null ? _expectedOrder.address : string.Empty;

        /// <summary>런타임 스폰 초기화 (S-015 — 비콘 프리팹 인스턴스에 주문 배정).</summary>
        public void SetOrder(DeliveryOrderSO order)
        {
            _expectedOrder = order;
            if (_addressLabel != null)
            {
                _addressLabel.text = order != null ? order.address : string.Empty;
                _addressLabel.gameObject.SetActive(false); // 포커스 시에만 (S-016 ②)
            }
        }

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

        // S-034 ④: 비콘에 놓기 = 내려놓기일 뿐 — 완료·보상 없음. 주소가 달라도 놓인다(오배치 = 정산 때 실패).
        public void Interact(PlayerContext ctx)
        {
            DeliveryOrderSO carried = ctx.Player.Status.CarriedOrder;
            if (carried == null) { Debug.Log("[DeliveryPoint] 빈손 — 상자를 들고 와야 내려놓는다."); return; }
            if (!WorldDeliveryManager.Instance.IsInCargo(carried))
            {
                Debug.Log("[DeliveryPoint] #" + carried.orderId + " 은 적재 목록에 없다(지각 실패?) — 내려놓기 불가.");
                return;
            }

            ctx.Player.Status.ReleaseCarry(dropAsPhysics: true);
            WorldDeliveryManager.Instance.PlaceDelivery(carried, Address);
        }

        /// <summary>
        /// 던져 넣기 (S-017 ② → S-034 배치화) — 물리로 굴러온 상자가 패드에 닿으면 배치 기록.
        /// 상자는 파괴하지 않는다 — 다시 들어 옮길 수 있다.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out PickupBox box) || box.Order == null) return;
            if (WorldDeliveryManager.Instance == null || !WorldDeliveryManager.Instance.IsInCargo(box.Order)) return;
            WorldDeliveryManager.Instance.PlaceDelivery(box.Order, Address);
        }

        /// <summary>패드 밖으로 굴러 나가면 배치 철회 (재픽업은 PickupBox 쪽에서 철회).</summary>
        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out PickupBox box) || box.Order == null) return;
            if (WorldDeliveryManager.Instance == null) return;
            WorldDeliveryManager.Instance.UnplaceDelivery(box.Order.orderId);
            Debug.Log("[배송] #" + box.Order.orderId + " 패드 이탈 — 배치 철회.");
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
            if (_addressLabel != null) _addressLabel.gameObject.SetActive(on); // 패드 위 = 주소 표시 (S-016 ②)
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
