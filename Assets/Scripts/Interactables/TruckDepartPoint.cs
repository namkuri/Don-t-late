using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 트럭 출발 인터랙트 (S-072 ⑦ — 남규님 발주, 매니페스트 직교 추가). 트럭 해금(hasTruck) 후
    /// 트럭 앞쪽에서 E → Travel 씬(지도 이동). 통짜 모델 교체를 감안해 Cab 오브젝트에 의존하지
    /// 않는다 — 빌더가 트럭 루트 기준 오프셋 위치에 이 트리거를 깐다.
    /// </summary>
    public class TruckDepartPoint : MonoBehaviour, IInteractable, IFocusGate
    {
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        // 해금 전에는 포커스 자체가 안 잡힌다 — 하이라이트·[E] 안내 모두 침묵.
        public bool AllowsFocus(Vector3 playerPosition) => _gameState != null && _gameState.hasTruck;

        public void Interact(PlayerContext ctx)
        {
            if (_gameState == null || !_gameState.hasTruck) return;
            if (WorldSceneFlowManager.Instance == null) return;
            Debug.Log("[트럭] 운전석 탑승 — 지도 이동");
            WorldSceneFlowManager.Instance.Request(GameScene.Travel);
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            _renderer.sharedMaterial = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
        }
    }
}
