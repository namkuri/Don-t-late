using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 트럭 구매 인터랙트 (S-241 — 남규님 발주). 트럭이 없을 때만 트럭 앞에서 [E] →
    /// **외상으로 산다**: 빚 +1,000, 즉시 보유. 구매 직후 독백으로 다음 할 일을 알려 준다.
    ///
    /// 현금을 받지 않고 빚을 얹는 이유는 발주 그대로다 — 이 게임의 문법은 "빚을 지고 일한다"이고,
    /// 종잣돈(<see cref="GameStateSO.PROTECTED_CASH"/>)은 소모품을 사라고 준 돈이다.
    ///
    /// 짝: 구매 후에는 <see cref="TruckDepartPoint"/>가 같은 자리에서 포커스를 가져간다
    /// (이쪽은 hasTruck=false, 저쪽은 true일 때만 잡힌다 — 둘이 겹치지 않는다).
    /// </summary>
    public class TruckPurchasePoint : MonoBehaviour, IInteractable, IFocusGate, IInteractPrompt
    {
        public const int PRICE = 1000;

        // S-249 — 값이 붙은 선택지는 화면에 값이 보여야 선택이 된다(남규님 지시 문구 그대로).
        public string PromptText => "[E] 트럭구매  빚 " + PRICE.ToString("N0") + "원 추가";

        [SerializeField] private GameStateSO _gameState;
        [Tooltip("S-248 — 하이라이트를 걸 범위(트럭 루트). 하위 렌더러 전부를 갈아끼운다.")]
        [SerializeField] private Transform _highlightRoot;
        [SerializeField] private Material _highlightMaterial;

        private HighlightSwapper _swapper;

        private void Awake()
        {
            _swapper = new HighlightSwapper(_highlightRoot != null ? _highlightRoot : transform);
        }

        // 이미 트럭이 있으면 포커스 자체가 안 잡힌다 — 하이라이트·[E] 안내 모두 침묵.
        public bool AllowsFocus(Vector3 playerPosition) => _gameState != null && !_gameState.hasTruck;

        public void Interact(PlayerContext ctx)
        {
            if (_gameState == null || _gameState.hasTruck) return;

            _gameState.hasTruck = true;
            _gameState.debt += PRICE;
            Debug.Log("[트럭] 외상 구매 — 빚 +" + PRICE + " (총 " + _gameState.debt + ")");

            WorldEvents.RaiseItemAcquired("트럭을 외상으로 샀습니다 (빚 +" + PRICE + "원)");
            WorldDialogueManager.Instance?.PlayScenario(MakeTutorialMonologue());
        }

        public void SetHighlight(bool on) => _swapper?.Set(on, _highlightMaterial);

        /// <summary>
        /// 구매 튜토리얼 독백 (S-241). 산 직후가 "이걸로 뭘 하지?"가 제일 큰 순간이라
        /// 여기서 조작을 알려 준다 — 짐을 싣고, 트럭 앞에서 다시 [E]로 출발한다.
        /// </summary>
        private static DialogueScenarioSO MakeTutorialMonologue()
        {
            var scenario = ScriptableObject.CreateInstance<DialogueScenarioSO>();
            scenario.name = "Truck_Purchase_Tutorial"; // 런타임 인스턴스 — 에셋 저장 없음
            (string speaker, string text)[] lines =
            {
                ("주인공", "(…샀다. 빚이 " + PRICE + "원 더 늘었네.)"),
                ("주인공", "(이제 걸어서 안 다녀도 된다. 적재존에서 짐부터 싣자.)"),
                ("주인공", "(짐을 다 실으면 트럭 앞에서 한 번 더 눌러서 출발하면 된다.)"),
            };
            scenario.lines = new DialogueScenarioSO.Line[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                scenario.lines[i] = new DialogueScenarioSO.Line { speaker = lines[i].speaker, text = lines[i].text };
            return scenario;
        }
    }
}
