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
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        public DeliveryOrderSO Order => _order;

        public void Interact(PlayerContext ctx)
        {
            if (_order == null) return;
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

            // 손에 든 동안은 센서에 다시 잡히면 안 된다.
            GetComponent<Collider>().enabled = false;
            ctx.Player.Status.AttachCarried(transform);
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            Material material = on ? _highlightMaterial : _normalMaterial;
            if (material != null) _renderer.sharedMaterial = material;
        }
    }
}
