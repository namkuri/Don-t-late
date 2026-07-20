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
            if (carried == null) return;
            if (_expectedOrder != null && carried != _expectedOrder) return;

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
            ApplyHighlight();
            if (_riseEffect != null) _riseEffect.SetActive(false);
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
