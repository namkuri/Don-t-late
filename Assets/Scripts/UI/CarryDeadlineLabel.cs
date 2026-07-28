using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 든 상자 위 마감 카운트다운 (S-073 ④ — 남규님 발주, 매니페스트 직교 추가). 캐리 중인
    /// 상자 비주얼에 붙어 "마감 N분"을 풀해상 오버레이로 띄운다 (BoxDurability HP바 패턴).
    /// 상자가 손을 떠나면(드롭·파괴) 컴포넌트째 제거된다 — 라벨 캔버스도 함께 정리.
    /// </summary>
    public class CarryDeadlineLabel : MonoBehaviour
    {
        private DeliveryOrderSO _order;
        private GameStateSO _gameState;
        private TMP_FontAsset _font;
        private GameObject _canvasGo;
        private TMP_Text _label;

        public void Init(DeliveryOrderSO order, GameStateSO gameState, TMP_FontAsset font)
        {
            _order = order;
            _gameState = gameState;
            _font = font;
        }

        private void OnDestroy()
        {
            if (_canvasGo != null) Destroy(_canvasGo);
        }

        private void LateUpdate()
        {
            if (_order == null || _gameState == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            if (_label == null) BuildLabel();

            Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 0.85f);
            if (screen.z < 0f) { _label.gameObject.SetActive(false); return; }
            _label.gameObject.SetActive(true);
            _label.rectTransform.position = new Vector3(screen.x, screen.y, 0f);

            int remaining = Mathf.FloorToInt(_order.deadlineMinuteOfDay - _gameState.minuteOfDay);
            if (remaining > 0)
            {
                _label.text = "마감 " + remaining + "분";
                _label.color = remaining < 60 ? new Color(1f, 0.45f, 0.35f) : new Color(1f, 0.624f, 0.271f);
            }
            else
            {
                _label.text = "지각!";
                _label.color = new Color(1f, 0.45f, 0.35f);
            }
        }

        private void BuildLabel()
        {
            _canvasGo = new GameObject("CarryDeadlineCanvas");
            Canvas canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6; // HP바(5) 위·HUD(10) 아래

            _label = new GameObject("Deadline", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _label.transform.SetParent(_canvasGo.transform, false);
            if (_font != null) _label.font = _font;
            _label.fontSize = 22f;
            _label.fontStyle = FontStyles.Bold;
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.raycastTarget = false;
            _label.rectTransform.sizeDelta = new Vector2(180f, 30f);
        }
    }
}
