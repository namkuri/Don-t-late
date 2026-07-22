using UnityEngine;

namespace DontLate
{
    /// <summary>택배 상자. 상호작용하면 플레이어가 손에 들고 월드에서 사라진다.</summary>
    [RequireComponent(typeof(Collider))]
    public class PickupBox : MonoBehaviour, IInteractable
    {
        [SerializeField] private DeliveryOrderSO _order;
        [Tooltip("켜면 오늘 적재 목록(cargo)에 있는 건만 픽업 가능 — 배송지(District) 상자용. Camp 상자는 끔.")]
        [SerializeField] private bool _requireInCargo;
        [Tooltip("켜면 폰으로 바코드 스캔한 건만 픽업 가능 — Camp 상차용 (S-011).")]
        [SerializeField] private bool _requireScanned;
        [SerializeField] private Material _highlightMaterial;

        // 수제 박스는 렌더러·머티리얼 슬롯이 여럿(본체+테이프) — 전 슬롯을 통째로 바꿨다 되돌린다 (S-013).
        private Renderer[] _renderers;
        private Material[][] _originalMaterials;

        public DeliveryOrderSO Order => _order;

        private void Awake() => CacheRenderers();

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _originalMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
                _originalMaterials[i] = _renderers[i].sharedMaterials;
        }

        /// <summary>주문 교체 (S-021 ③ — 캠프 주문 갱신. 소진된 건을 새 목적지로).</summary>
        public void SetOrder(DeliveryOrderSO order)
        {
            _order = order;
            GetComponent<Collider>().enabled = true; // 소진으로 꺼졌던 픽업도 재활성
        }

        /// <summary>런타임 스폰 초기화 (S-015 — DistrictCargoSpawner). AddComponent 직후 호출.</summary>
        public void Initialize(DeliveryOrderSO order, Material highlight, bool requireInCargo, bool requireScanned)
        {
            _order = order;
            _highlightMaterial = highlight;
            _requireInCargo = requireInCargo;
            _requireScanned = requireScanned;
            CacheRenderers(); // 비주얼이 Initialize 전에 붙었을 수 있어 재캐시
        }

        public void Interact(PlayerContext ctx)
        {
            if (_order == null) return;
            // 씬 단독 Play 직후 프레임 등 매니저 부재 시 조용히 무시 (S-013 — EnsureCoreLoaded가 로드 중).
            if (WorldDeliveryManager.Instance == null)
            {
                Debug.LogWarning("[PickupBox] WorldDeliveryManager 없음 — Core 로드 대기 중이거나 씬 구성 오류.");
                return;
            }
            // S-010: 캠프에서 싣지 않은(또는 지각 실패한) 건은 배송 불가 — 침묵 무반응 대신 사유를 남긴다.
            if (_requireInCargo && !WorldDeliveryManager.Instance.IsInCargo(_order))
            {
                Debug.Log("[PickupBox] #" + _order.orderId + " 은 오늘 적재 목록에 없다 — 캠프에서 싣지 않았거나 지각 실패한 건.");
                return;
            }
            if (_requireScanned && !WorldDeliveryManager.Instance.IsScanned(_order))
            {
                Debug.Log("[PickupBox] #" + _order.orderId + " 은 바코드 미스캔 — Tab으로 폰을 열고 박스를 클릭해 송장을 찍어라.");
                return;
            }
            if (!ctx.Player.Status.TryCarry(_order)) return;

            WorldDeliveryManager.Instance.NotifyPickedUp(_order);
            SetHighlight(false);

            // 손에 든 동안은 센서에 다시 잡히면 안 된다. 물리 상자면 손 안에서 잠근다 (S-016 ⑥).
            GetComponent<Collider>().enabled = false;
            if (TryGetComponent(out Rigidbody body)) body.isKinematic = true;
            ctx.Player.Status.AttachCarried(transform);
        }

        public void SetHighlight(bool on)
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                if (on && _highlightMaterial != null)
                {
                    var materials = new Material[_originalMaterials[i].Length];
                    for (int s = 0; s < materials.Length; s++) materials[s] = _highlightMaterial;
                    _renderers[i].sharedMaterials = materials;
                }
                else
                {
                    _renderers[i].sharedMaterials = _originalMaterials[i];
                }
            }
        }
    }
}
