using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 에너지드링크 — S-005 → S-031 ⑩ 개편. E = 손에 잡는다(즉시 섭취 아님) →
    /// 좌클릭 = 마신다(PlayerStatusManager가 처리). 플레이어 도메인 내 처리라 이벤트 불요(YAGNI).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnergyDrinkPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        public void Interact(PlayerContext ctx)
        {
            // S-064 — 가방 수납 우선. 가득이면 기존처럼 손에 든다.
            if (BagStorage.TryAdd(ctx.Player.GameState, "drink", "에너지드링크", stackable: true, holdable: true))
            {
                if (BagView.Instance != null) BagView.Instance.Refresh();
                Destroy(gameObject);
                return;
            }

            if (!ctx.Player.Status.TryHoldDrink(transform)) return; // 이미 다른 드링크를 들고 있음

            // 손에 들어간 뒤엔 픽업체가 아니다 — 물리·콜라이더·본 컴포넌트 은퇴.
            if (TryGetComponent(out Rigidbody body)) Destroy(body);
            if (TryGetComponent(out Collider col)) col.enabled = false;
            Destroy(this);
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            Material material = on ? _highlightMaterial : _normalMaterial;
            if (material != null) _renderer.sharedMaterial = material;
        }
    }
}
