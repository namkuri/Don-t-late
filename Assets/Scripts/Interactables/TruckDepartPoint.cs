using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 트럭 출발 인터랙트 (S-072 ⑦ — 남규님 발주, 매니페스트 직교 추가). 트럭 해금(hasTruck) 후
    /// 트럭 앞쪽에서 E → Travel 씬(지도 이동). 통짜 모델 교체를 감안해 Cab 오브젝트에 의존하지
    /// 않는다 — 빌더가 트럭 루트 기준 오프셋 위치에 이 트리거를 깐다.
    /// </summary>
    public class TruckDepartPoint : MonoBehaviour, IInteractable, IFocusGate, IInteractPrompt
    {
        public string PromptText => "[E] 트럭탑승"; // S-249

        [SerializeField] private GameStateSO _gameState;
        [Tooltip("S-248 — 하이라이트를 걸 범위(트럭 루트). 하위 렌더러 전부를 갈아끼운다.")]
        [SerializeField] private Transform _highlightRoot;
        [SerializeField] private Material _highlightMaterial;

        private HighlightSwapper _swapper;

        private void Awake()
        {
            _swapper = new HighlightSwapper(_highlightRoot != null ? _highlightRoot : transform);
        }

        // 해금 전에는 포커스 자체가 안 잡힌다 — 하이라이트·[E] 안내 모두 침묵.
        public bool AllowsFocus(Vector3 playerPosition) => _gameState != null && _gameState.hasTruck;

        public void Interact(PlayerContext ctx)
        {
            if (_gameState == null || !_gameState.hasTruck) return;
            if (WorldSceneFlowManager.Instance == null) return;
            Debug.Log("[트럭] 운전석 탑승 — 지도 앱");
            WorldSceneFlowManager.Instance.Request(GameScene.Travel);
            // S-249 — 탑승하면 **폰 지도 앱**이 뜬다(남규님 지시). Travel 도착 시에도 같은 화면을
            // 여는 경로가 있지만, 씬 전환을 기다리지 않고 여기서 확정한다 — 눌렀는데 아무 일도
            // 안 일어나는 몇 프레임이 "안 먹었다"로 읽힌다.
            PhoneView.Instance?.OpenMapApp();
        }

        public void SetHighlight(bool on) => _swapper?.Set(on, _highlightMaterial);
    }
}
