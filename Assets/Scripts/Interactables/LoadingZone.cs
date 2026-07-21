using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// Camp 적재존 — S-005. 패드 위에서 E를 누르면 배정된 주문 1건을 적재(OrderAccepted)한다.
    /// 적재 상한(TuningConfigSO.maxCargo)을 넘으면 거절. 적재 후에는 소비되어 다시 잡히지 않는다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LoadingZone : MonoBehaviour, IInteractable
    {
        [SerializeField] private DeliveryOrderSO _order;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        public void Interact(PlayerContext ctx)
        {
            if (_order == null) return;

            GameStateSO state = ctx.Player.GameState;
            if (state.cargo.Count >= ctx.Player.Tuning.maxCargo)
            {
                Debug.Log("[LoadingZone] 적재 상한(" + ctx.Player.Tuning.maxCargo + ") — 더 실을 수 없다.");
                return;
            }

            WorldDeliveryManager.Instance.AcceptOrder(_order);
            SetHighlight(false);
            GetComponent<Collider>().enabled = false; // 소비 — 패드당 1건
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            Material material = on ? _highlightMaterial : _normalMaterial;
            if (material != null) _renderer.sharedMaterial = material;
        }
    }
}
