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

        /// <summary>정산창이 떠 있는가 (S-134 ⑤ 동반수리) — ESC 설정 토글이 이 위로 열려
        /// 복구 불가 프리즈를 만들던 것을 막는 가드. InvoiceView.IsOpen 선례와 동형.</summary>
        public static bool IsOpen { get; private set; }

        private void Awake()
        {
            if (_openButton != null) _openButton.onClick.AddListener(Open);
            if (_confirmButton != null) _confirmButton.onClick.AddListener(Confirm);
            if (_panel != null) _panel.SetActive(false);
        }

        // S-134 ⑤ — 도보 귀가(엣지워크)도 버튼과 같은 마감을 타게 한다.
        private void OnEnable() => WorldEvents.GoHomeRequested += Open;
        private void OnDisable()
        {
            WorldEvents.GoHomeRequested -= Open;
            IsOpen = false; // 씬 언로드 중 잔류 방지
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
            IsOpen = true;

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
        private const float LINE_INTERVAL_SECONDS = 0.3f; // S-094 ② — 500ms는 느리다 (남규님 판정)

        // ── S-087 — 영수증 포맷: 좌(이름)/우(금액) 정렬은 TMP line-height 0 트릭 ──
        // S-202 — 구분선을 **글자 폭에 의존하지 않게** 만든다. 종전엔 고정 개수의 `*`·`-`라
        // 폰트를 바꾸면 글리프 advance가 달라져 줄이 짧아지거나 종이를 넘쳤다(남규님 보고).
        // `<mspace>`는 글자마다 **폭을 em으로 못박아** 폰트와 무관하게 만든다.
        // 본문 실측 600px / fontSize 34 = 17.6em → 0.44em × 40칸 = 17.6em (종이 폭에 정확히 맞다).
        private const string RULE_STARS = "<align=center><color=#4a5568><mspace=0.44em>****************************************</mspace></color></align>";
        private const string RULE_DASH = "<align=center><color=#8a93a8><mspace=0.44em>----------------------------------------</mspace></color></align>";

        private static string Row(string left, string right)
            => "<align=left>" + left + "<line-height=0>\n<align=right>" + right + "<line-height=1em>";

        [SerializeField] private GameStateSO _gameState; // S-087 — 영수증 머리(Day·배송원) 표시용

        private void BuildLines(DeliveryDaySummary d, DebtSettlement s)
        {
            _lines.Clear();
            _lines.Add("<align=center><b>정 산   영 수 증</b></align>");
            _lines.Add(RULE_STARS);
            _lines.Add(Row("Date:", "Day " + (_gameState != null ? _gameState.day.ToString() : "?")));
            _lines.Add(Row("배송원:", _gameState != null && !string.IsNullOrEmpty(_gameState.nickname) ? _gameState.nickname : "늦지마맨"));
            _lines.Add(RULE_DASH);
            if (d.Lines != null)
                foreach (SettleLine line in d.Lines)
                    _lines.Add(line.Success
                        ? Row(line.Address, "<color=#2b9e8e>+" + line.Amount.ToString("N0") + "</color>")
                        : Row(line.Address + " <color=#e05a48>(" + line.Note + ")</color>",
                              "<color=#e05a48>−" + (-line.Amount).ToString("N0") + "</color>"));
            if (d.Lines == null || d.Lines.Count == 0)
                _lines.Add("<align=center><color=#8a93a8>(배송 내역 없음)</color></align>");
            _lines.Add(RULE_DASH);
            _lines.Add(Row("성공 " + d.SuccessCount + "건", "<color=#2b9e8e>+" + d.RewardTotal.ToString("N0") + "</color>"));
            _lines.Add(Row("실패 " + d.FailCount + "건", "<color=#e05a48>−" + d.PenaltyTotal.ToString("N0") + "</color>"));
            _lines.Add(Row("빚 상환", s.Repaid.ToString("N0")));
            _lines.Add(Row("<b>잔액</b>", "<b>" + s.Money.ToString("N0") + "</b>"));
            _lines.Add(Row("남은 빚", s.Debt.ToString("N0")));

            // S-084 ① — 개척 해금·트럭 수령 하이라이트 (콘페티 트리거 문구 유지).
            if (!string.IsNullOrEmpty(d.UnlockedDistrict))
                _lines.Add("<align=center><color=#2b9e8e><b>새 구역 개척 — " + d.UnlockedDistrict + " 해금!</b></color></align>");
            if (d.TruckAwarded)
                _lines.Add("<align=center><color=#2b9e8e><b>전 구역 완주 — 회사 트럭 지급!</b></color></align>");
            // S-165 ④ — 이번 라운드에 레벨업으로 능력을 얻었으면 알린다(남규님 지시).
            // "해금!"이 들어가면 아래 PrintLines가 팡파레+콘페티를 자동으로 태운다 — 같은 규칙을 재사용한다.
            if (!string.IsNullOrEmpty(d.UnlockedPerk))
                _lines.Add("<align=center><color=#2b9e8e><b>Lv." + (_gameState != null ? _gameState.playerLevel : 0)
                         + " 달성 — " + d.UnlockedPerk + " 해금!</b></color></align>");

            _lines.Add(RULE_STARS);
            _lines.Add("<align=center><color=#4a5568>Don't Late Inc.</color></align>");
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
            IsOpen = false;
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
