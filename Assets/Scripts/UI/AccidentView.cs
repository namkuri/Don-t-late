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
        private const string RULE_STARS = "<align=center><color=#4a5568>******************************************</color></align>";
        private const string RULE_DASH = "<align=center><color=#8a93a8>----------------------------------------------------</color></align>";

        private static string Row(string left, string right)
            => "<align=left>" + left + "<line-height=0>\n<align=right>" + right + "<line-height=1em>";

        private void Awake()
        {
            if (_homeButton != null) _homeButton.onClick.AddListener(OnHomePressed);
        }

        private void OnEnable() => WorldEvents.CarAccident += OnCarAccident;
        private void OnDisable() => WorldEvents.CarAccident -= OnCarAccident;

        private void OnCarAccident(int hospitalFee, int failedCount)
        {
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
                if (failedCount > 0)
                    sb.AppendLine(Row("미배송 실패", "<color=#e05a48>" + failedCount + "건</color>"));
                sb.AppendLine(RULE_DASH);
                sb.AppendLine(Row("<b>잔액</b>", "<b>" + (_gameState != null ? _gameState.money.ToString("N0") : "?") + "</b>"));
                sb.AppendLine(Row("남은 빚", _gameState != null ? _gameState.debt.ToString("N0") : "?"));
                sb.AppendLine(RULE_STARS);
                sb.AppendLine("<align=center><color=#4a5568>늦지마 종합병원</color></align>");
                _bodyLabel.text = sb.ToString();
            }
            if (_panel != null) _panel.SetActive(true);
            StartCoroutine(RedFlashRoutine());
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

        private void OnHomePressed()
        {
            if (WorldSceneFlowManager.Instance == null || WorldSceneFlowManager.Instance.IsTransitioning) return;
            if (_panel != null) _panel.SetActive(false);
            WorldSceneFlowManager.Instance.Request(GameScene.Home);
        }
    }
}
