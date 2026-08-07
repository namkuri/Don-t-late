using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 교통사고 팝업 (S-066 ③ → S-119 ② 영수증 개편). CarAccident 이벤트 수신 →
    /// 화면 전체 붉은 깜빡임 2회 + <b>병원비 영수증</b>(S-087 정산 영수증 포맷 — 좌/우 정렬·
    /// 절취선·Don't Late Inc.)으로 차감 내역을 보여준다. "치료 후 집으로" 버튼으로만 닫힌다(집 전이).
    /// </summary>
    public class AccidentView : MonoBehaviour
    {
        [SerializeField] private GameStateSO _gameState; // S-119 ② — 차감 후 잔액·빚 표시용
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _bodyLabel;
        [SerializeField] private Button _homeButton;
        [SerializeField] private Image _redFlash;

        // S-087 영수증 포맷 재사용 — 좌(이름)/우(금액) 정렬은 TMP line-height 0 트릭.
        // S-202 — 구분선을 **글자 폭에 의존하지 않게** 만든다. 종전엔 고정 개수의 `*`·`-`라
        // 폰트를 바꾸면 글리프 advance가 달라져 줄이 짧아지거나 종이를 넘쳤다(남규님 보고).
        // `<mspace>`는 글자마다 **폭을 em으로 못박아** 폰트와 무관하게 만든다.
        // 본문 실측 600px / fontSize 34 = 17.6em → 0.44em × 40칸 = 17.6em (종이 폭에 정확히 맞다).
        private const string RULE_STARS = "<align=center><color=#4a5568><mspace=0.44em>****************************************</mspace></color></align>";
        private const string RULE_DASH = "<align=center><color=#8a93a8><mspace=0.44em>----------------------------------------</mspace></color></align>";

        private static string Row(string left, string right)
            => "<align=left>" + left + "<line-height=0>\n<align=right>" + right + "<line-height=1em>";

        private void Awake()
        {
            if (_homeButton != null) _homeButton.onClick.AddListener(OnHomePressed);
        }

        private void OnEnable() => WorldEvents.CarAccident += OnCarAccident;
        private void OnDisable() => WorldEvents.CarAccident -= OnCarAccident;

        private void OnCarAccident(int hospitalFee, bool hospitalized)
        {
            // S-165 ③ — **후송일 때만 영수증을 띄운다**(남규님 난이도 조절).
            // 종전엔 hospitalized와 무관하게 패널을 열어, 체력이 남아 있어도 정산창이 떠
            // 한 번 치일 때마다 하루가 끊기는 느낌이었다. 체력이 남으면 붉은 깜빡임만 주고
            // 넉백·짐 낙하(TrafficCar 몫)로 끝낸다 — 아프지만 계속 뛸 수 있다.
            if (!hospitalized)
            {
                StartCoroutine(RedFlashRoutine());
                return;
            }

            if (_bodyLabel != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("<align=center><b>병 원 비   영 수 증</b></align>");
                sb.AppendLine(RULE_STARS);
                sb.AppendLine(Row("Date:", "Day " + (_gameState != null ? _gameState.day.ToString() : "?")));
                sb.AppendLine(Row("환자:", _gameState != null && !string.IsNullOrEmpty(_gameState.nickname) ? _gameState.nickname : "늦지마맨"));
                sb.AppendLine(Row("사유:", "교통사고 (차도 횡단)"));
                sb.AppendLine(RULE_DASH);
                sb.AppendLine(Row("치료비", "<color=#e05a48>−" + hospitalFee.ToString("N0") + "</color>"));
                if (hospitalized)
                    sb.AppendLine(Row("소견", "<color=#e05a48>당일 후송 — 업무 중단</color>"));
                sb.AppendLine(RULE_DASH);
                sb.AppendLine(Row("<b>잔액</b>", "<b>" + (_gameState != null ? _gameState.money.ToString("N0") : "?") + "</b>"));
                sb.AppendLine(Row("남은 빚", _gameState != null ? _gameState.debt.ToString("N0") : "?"));
                sb.AppendLine(RULE_STARS);
                sb.AppendLine("<align=center><color=#4a5568>늦지마 종합병원</color></align>");
                _bodyLabel.text = sb.ToString();
            }
            if (_panel != null) _panel.SetActive(true);
            StartCoroutine(RedFlashRoutine());
            _hospitalized = hospitalized; // S-134 ④ — 확인 버튼이 정산까지 밟을지 결정
        }

        // 붉은 깜빡임 2회 — 알파 0.55 → 0.
        private IEnumerator RedFlashRoutine()
        {
            if (_redFlash == null) yield break;
            _redFlash.gameObject.SetActive(true);
            for (int i = 0; i < 2; i++)
            {
                for (float t = 0f; t < 1f; t += Time.deltaTime * 4f)
                {
                    _redFlash.color = new Color(0.85f, 0.1f, 0.08f, Mathf.Lerp(0.55f, 0f, t));
                    yield return null;
                }
            }
            _redFlash.gameObject.SetActive(false);
        }

        // S-134 ④ — 체력 0칸이면 그날은 끝. 하루를 정산하고 집으로(남규님 결정 '강제 귀가 + 정산').
        // 매니저 두 개를 부르는 오케스트레이션은 View 층 몫 — 매니저끼리 직접 부르지 않는다(§3).
        private bool _hospitalized;

        private void OnHomePressed()
        {
            if (WorldSceneFlowManager.Instance == null || WorldSceneFlowManager.Instance.IsTransitioning) return;
            if (_panel != null) _panel.SetActive(false);
            if (_hospitalized)
            {
                _hospitalized = false;
                if (_gameState != null) _gameState.health = GameStateSO.HEALTH_MAX; // 치료 완료
                WorldEvents.RaiseGoHomeRequested(); // 정산 UI가 하루를 마감하고 귀가시킨다
                return;
            }
            WorldSceneFlowManager.Instance.Request(GameScene.Home);
        }
    }
}
