using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 배송지 문 앞. 들고 온 건이 이 주소와 맞으면 인증되고 완료 처리된다.
    /// 하이라이트는 두 갈래 — 근접 포커스(센서)와 목적지 표시(픽업 이후) 중 하나라도 켜지면 켠다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DeliveryPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private DeliveryOrderSO _expectedOrder;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        private bool _focused;
        private bool _isDestination;

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
            if (carried == null) return;
            if (_expectedOrder != null && carried != _expectedOrder) return;

            ctx.Player.Status.ReleaseCarry();
            WorldDeliveryManager.Instance.CompleteDelivery(carried);
        }

        public void SetHighlight(bool on)
        {
            _focused = on;
            ApplyHighlight();
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
            ApplyHighlight();
        }

        private void ApplyHighlight()
        {
            if (_renderer == null) return;
            Material material = (_focused || _isDestination) ? _highlightMaterial : _normalMaterial;
            if (material != null) _renderer.sharedMaterial = material;
        }
    }
}
