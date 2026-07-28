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

            // S-034 ④: 배송 일괄 판정(보상·벌금 반영)이 먼저, 그 잔액으로 빚 상환.
            DeliveryDaySummary d = WorldDeliveryManager.Instance != null
                ? WorldDeliveryManager.Instance.SettleDeliveries()
                : default;
            DebtSettlement s = WorldDebtManager.Instance.SettleNow();

            // AU-010 — 정산 요약음: 실패가 하나라도 있으면 하행, 전건 성공이면 상행.
            if (d.FailCount > 0) WorldAudioManager.Instance?.PlaySettleBadSfx();
            else WorldAudioManager.Instance?.PlaySettleOkSfx();

            // S-075 ⑥ — 영수증 연출: 줄 배열을 만들어 500ms 간격 순차 출현(클릭=한 줄 스킵),
            // "집으로" 버튼은 전 줄이 찍힌 뒤 마지막에 나타난다.
            BuildLines(d, s);
            if (_confirmButton != null) _confirmButton.gameObject.SetActive(false);
            _panel.SetActive(true);
            _printRoutine = StartCoroutine(PrintLines());
        }

        private readonly System.Collections.Generic.List<string> _lines = new System.Collections.Generic.List<string>();
        private Coroutine _printRoutine;
        private bool _skipOnce;
        private const float LINE_INTERVAL_SECONDS = 0.5f;

        private void BuildLines(DeliveryDaySummary d, DebtSettlement s)
        {
            _lines.Clear();
            _lines.Add("<b>오늘 정산</b>");
            _lines.Add("");
            _lines.Add("배송 성공  <color=#35e0c8>" + d.SuccessCount + "건  +₩" + d.RewardTotal.ToString("N0") + "</color>");
            if (d.Lines != null)
                foreach (SettleLine line in d.Lines)
                    if (line.Success)
                        _lines.Add("<size=72%>  · " + line.Address + "  <color=#35e0c8>+₩" + line.Amount.ToString("N0") + "</color></size>");
            _lines.Add("배송 실패  <color=#ff7359>" + d.FailCount + "건  −₩" + d.PenaltyTotal.ToString("N0") + "</color>");
            if (d.Lines != null)
                foreach (SettleLine line in d.Lines)
                    if (!line.Success)
                        _lines.Add("<size=72%>  · " + line.Address + "  <color=#ff7359>" + line.Note + " −₩" + (-line.Amount).ToString("N0") + "</color></size>");
            _lines.Add("");
            _lines.Add("빚 상환   <color=#35e0c8>₩" + s.Repaid.ToString("N0") + "</color>");
            _lines.Add("잔액       ₩" + s.Money.ToString("N0"));
            _lines.Add("남은 빚   ₩" + s.Debt.ToString("N0"));
        }

        private System.Collections.IEnumerator PrintLines()
        {
            _bodyLabel.text = string.Empty;
            _skipOnce = false;
            var sb = new System.Text.StringBuilder();
            foreach (string line in _lines)
            {
                float waited = 0f;
                while (waited < LINE_INTERVAL_SECONDS && !_skipOnce)
                {
                    waited += Time.unscaledDeltaTime; // 정산 중 timeScale=0 — unscaled로 흐른다
                    yield return null;
                }
                _skipOnce = false;

                sb.Append(line).Append('\n');
                _bodyLabel.text = sb.ToString();
                if (!string.IsNullOrEmpty(line)) WorldAudioManager.Instance?.PlayUiTickSfx(); // 줄 틱
            }
            if (_confirmButton != null) _confirmButton.gameObject.SetActive(true); // 맨 마지막에 맨 아래
            _printRoutine = null;
        }

        private void Update()
        {
            // 클릭 = 다음 줄 즉시 (연출 스킵 한 줄씩).
            if (_printRoutine == null || _panel == null || !_panel.activeSelf) return;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) _skipOnce = true;
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
