using TMPro;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-229 ① — 집→캠프 진행 버튼의 라벨을 **엔딩 상황에서만** "떠나기"로 바꾼다.
    ///
    /// 평소엔 "하루 시작 > 물류캠프"가 맞다(하루가 또 시작된다). 그런데 빚을 다 갚고 독백까지 끝난
    /// 뒤엔 캠프에 가는 이유가 달라진다 — 일하러 가는 게 아니라 인사하고 떠나러 간다.
    /// 같은 버튼이 같은 말을 하면 마지막 장면의 무게가 안 산다(남규님 지시).
    ///
    /// 표시만 한다(UI 규약) — 판단 근거인 상태는 GameState가 단독 소유한다.
    /// </summary>
    public class EndingDepartureLabel : MonoBehaviour
    {
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private TMP_Text _label;
        [Tooltip("엔딩 조건이 섰을 때 보일 문구.")]
        [SerializeField] private string _endingText = "떠나기";

        private string _normalText;

        private void OnEnable()
        {
            if (_label == null || _gameState == null) return;
            if (_normalText == null) _normalText = _label.text; // 평상시 문구는 빌더가 넣은 그대로

            // S-230 ⑥ — **씬에 들어서는 순간부터** "떠나기"로 보인다(남규님 지시).
            // 종전엔 독백까지 끝나야 바뀌었는데, 독백은 Home에 도착한 **뒤에** 재생되므로
            // 버튼이 눈앞에서 글자를 갈아치우는 꼴이었다. 빚을 다 갚은 순간 이미 떠날 사람이다.
            bool leaving = _gameState.debt <= 0;
            _label.text = leaving ? _endingText : _normalText;
        }
    }
}
