using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 에너지드링크 — S-005. E로 마시면 스태미나를 회복(TuningConfigSO.energyDrinkRecover)하고 사라진다.
    /// 플레이어 도메인 내 처리로 충분해 이벤트를 신설하지 않는다(YAGNI).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnergyDrinkPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        public void Interact(PlayerContext ctx)
        {
            ctx.Player.Status.RecoverStamina(ctx.Player.Tuning.energyDrinkRecover);
            WorldAudioManager.Instance?.PlayDrinkSfx(); // AU-009 — Instance 명령 (이벤트 없는 지점)
            Destroy(gameObject);
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            Material material = on ? _highlightMaterial : _normalMaterial;
            if (material != null) _renderer.sharedMaterial = material;
        }
    }
}
