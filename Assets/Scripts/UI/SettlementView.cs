using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 하루 정산 패널(View) — S-009. District "집으로" 버튼이 Open()을 부르면
    /// WorldDebtManager.SettleNow() 결과를 표시하고, 확인을 누르면 Home으로 전이 요청한다.
    /// 계산은 전부 매니저 몫 — 여기는 표시·위임뿐.
    /// </summary>
    public class SettlementView : MonoBehaviour
    {
        [Tooltip("정산을 여는 버튼 (District '집으로').")]
        [SerializeField] private Button _openButton;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _bodyLabel;
        [SerializeField] private Button _confirmButton;

        private void Awake()
        {
            if (_openButton != null) _openButton.onClick.AddListener(Open);
            if (_confirmButton != null) _confirmButton.onClick.AddListener(Confirm);
            if (_panel != null) _panel.SetActive(false);
        }

        /// <summary>"집으로" 버튼이 호출한다.</summary>
        public void Open()
        {
            if (WorldDebtManager.Instance == null || WorldSceneFlowManager.Instance == null)
            {
                Debug.LogWarning("[SettlementView] World 매니저 없음 — 씬 단독 Play인가?");
                return;
            }
            if (_panel.activeSelf) return; // 중복 클릭 방지 (S-010)

            // 정산은 하루의 마침표 — 패널이 떠 있는 동안 세계를 멈춰 표시·상태 불일치를 차단 (S-010).
            Time.timeScale = 0f;

            DebtSettlement s = WorldDebtManager.Instance.SettleNow();
            if (_bodyLabel != null)
                _bodyLabel.text =
                    "오늘 정산\n\n" +
                    "빚 상환   <color=#35e0c8>₩" + s.Repaid.ToString("N0") + "</color>\n" +
                    "벌금       <color=#ff9f45>-₩" + s.Penalty.ToString("N0") + "</color>\n" +
                    "잔액       ₩" + s.Money.ToString("N0") + "\n" +
                    "남은 빚   ₩" + s.Debt.ToString("N0");
            _panel.SetActive(true);
        }

        private void Confirm()
        {
            Time.timeScale = 1f;
            _panel.SetActive(false);
            WorldSceneFlowManager.Instance.Request(GameScene.Home);
        }

        private void OnDestroy()
        {
            // 패널이 뜬 채 씬이 언로드되는 예외 경로에서도 시간은 반드시 복구.
            if (_panel != null && _panel.activeSelf) Time.timeScale = 1f;
        }
    }
}
