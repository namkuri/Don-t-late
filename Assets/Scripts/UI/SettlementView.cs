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

            // S-084 ① — 개척 해금·트럭 수령은 맨 아래에서 하이라이트.
            if (!string.IsNullOrEmpty(d.UnlockedDistrict))
            {
                _lines.Add("");
                _lines.Add("<color=#35e0c8><b>새 구역 개척 — " + d.UnlockedDistrict + " 해금!</b></color>");
            }
            if (d.TruckAwarded)
            {
                _lines.Add("");
                _lines.Add("<color=#35e0c8><b>전 구역 완주 — 회사 트럭 지급!</b></color>");
            }
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
                // S-086 — 해금·트럭 라인은 팡파레+콘페티, 나머지는 줄 틱.
                if (line.Contains("해금!") || line.Contains("트럭 지급!"))
                {
                    WorldAudioManager.Instance?.PlayFanfareSfx();
                    BurstConfetti();
                }
                else if (!string.IsNullOrEmpty(line)) WorldAudioManager.Instance?.PlayUiTickSfx(); // 줄 틱
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

        // ── S-086 — 개척 해금 콘페티: 화면 중간쯤에서 색조각이 터져 날린다 ──
        // UI 캔버스 위 오버레이(정산 패널 위에도 보임) · unscaled(정산 정지 중에도 흐름) ·
        // 1회성 50조각이라 풀링 없이 생성-자멸.

        private static readonly Color[] CONFETTI_COLORS =
        {
            new Color(0.208f, 0.878f, 0.784f), new Color(1f, 0.624f, 0.271f),
            new Color(1f, 0.55f, 0.65f), new Color(0.55f, 0.75f, 1f), new Color(1f, 0.9f, 0.4f),
        };

        private void BurstConfetti()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform root = canvas != null ? canvas.transform : transform;
            Vector2 center = new Vector2(UnityEngine.Screen.width * 0.5f, UnityEngine.Screen.height * 0.55f);
            for (int i = 0; i < 50; i++)
            {
                Image piece = new GameObject("Confetti", typeof(RectTransform)).AddComponent<Image>();
                piece.transform.SetParent(root, false);
                piece.color = CONFETTI_COLORS[Random.Range(0, CONFETTI_COLORS.Length)];
                piece.raycastTarget = false;
                RectTransform rect = piece.rectTransform;
                rect.sizeDelta = new Vector2(Random.Range(8f, 16f), Random.Range(5f, 9f));
                rect.position = new Vector3(center.x + Random.Range(-30f, 30f), center.y + Random.Range(-20f, 20f), 0f);
                float angle = Random.Range(35f, 145f) * Mathf.Deg2Rad; // 위쪽 부채꼴로 분출
                float power = Random.Range(280f, 720f);
                StartCoroutine(ConfettiFly(rect, piece,
                    new Vector2(Mathf.Cos(angle) * power, Mathf.Sin(angle) * power),
                    Random.Range(-540f, 540f)));
            }
        }

        private System.Collections.IEnumerator ConfettiFly(RectTransform rect, Image piece, Vector2 velocity, float spin)
        {
            const float LIFE = 2.4f;
            const float GRAVITY = -880f;
            float t = 0f;
            while (t < LIFE && rect != null)
            {
                // 프레임 스파이크(로드·탭 전환)에 조각이 순간이동 소멸하지 않게 상한.
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                t += dt;
                velocity += new Vector2(0f, GRAVITY * dt);
                velocity.x *= 1f - 0.9f * dt; // 공기 저항 — 팔랑임
                rect.position += (Vector3)(velocity * dt);
                rect.localRotation = Quaternion.Euler(0f, 0f, spin * t);
                if (t > LIFE - 0.6f)
                {
                    Color color = piece.color;
                    color.a = (LIFE - t) / 0.6f;
                    piece.color = color;
                }
                yield return null;
            }
            if (rect != null) Destroy(rect.gameObject);
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
