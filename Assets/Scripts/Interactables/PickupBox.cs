using UnityEngine;

namespace DontLate
{
    /// <summary>택배 상자. 상호작용하면 플레이어가 손에 들고 월드에서 사라진다.</summary>
    [RequireComponent(typeof(Collider))]
    public class PickupBox : MonoBehaviour, IInteractable
    {
        [SerializeField] private DeliveryOrderSO _order;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        public DeliveryOrderSO Order => _order;

        public void Interact(PlayerContext ctx)
        {
            if (_order == null) return;
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
