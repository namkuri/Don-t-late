using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 교통사고 팝업 (S-066 ③). CarAccident 이벤트 수신 → 화면 전체 붉은 깜빡임 2회 +
    /// 병원비·실패 요약 팝업. "치료 후 집으로" 버튼으로만 닫힌다(집 전이).
    /// </summary>
    public class AccidentView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _bodyLabel;
        [SerializeField] private Button _homeButton;
        [SerializeField] private Image _redFlash;

        private void Awake()
        {
            if (_homeButton != null) _homeButton.onClick.AddListener(OnHomePressed);
        }

        private void OnEnable() => WorldEvents.CarAccident += OnCarAccident;
        private void OnDisable() => WorldEvents.CarAccident -= OnCarAccident;

        private void OnCarAccident(int hospitalFee, int failedCount)
        {
            if (_bodyLabel != null)
                _bodyLabel.text = "차에 치였다...\n\n병원비  <color=#ff7060>-₩" + hospitalFee.ToString("N0") + "</color>\n"
                    + "미배송 실패  <color=#ff7060>" + failedCount + "건</color>\n\n"
                    + "<size=70%>짐은 흩어졌고, 오늘 등록분은 실패 처리됐다.</size>";
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
