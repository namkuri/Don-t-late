using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontLate
{
    /// <summary>
    /// 택배 상자 호버 툴팁 (S-073 ③ — 남규님 발주, 매니페스트 직교 추가). 월드의 PickupBox에
    /// 마우스를 올리면 배송지(구역+건물이름)·스캔 여부·남은 시간을 풀해상 오버레이로 보여준다.
    /// 표시만 한다 — 데이터는 GameStateSO 읽기 전용.
    /// </summary>
    public class BoxTooltipView : MonoBehaviour
    {
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private TMP_Text _label;

        private void Update()
        {
            if (_label == null) return;

            Camera camera = Camera.main;
            Mouse mouse = Mouse.current;
            if (camera == null || mouse == null) { _label.gameObject.SetActive(false); return; }

            PickupBox box = null;
            float nearest = float.MaxValue;
            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = camera.ScreenPointToRay(mousePos);
            foreach (RaycastHit hit in Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Collide))
            {
                if (!hit.collider.TryGetComponent(out PickupBox candidate)) continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                box = candidate;
            }

            if (box == null || box.Order == null)
            {
                _label.gameObject.SetActive(false);
                return;
            }

            DeliveryOrderSO order = box.Order;
            string text = "<color=#8a93a8>" + order.district + "</color>  " + order.address;
            if (_gameState != null && _gameState.scannedOrderIds.Contains(order.orderId))
                text += "  <color=#7d8698>스캔완료</color>";
            if (_gameState != null)
            {
                int remaining = Mathf.FloorToInt(order.deadlineMinuteOfDay - _gameState.minuteOfDay);
                text += remaining > 0
                    ? "  <color=#ff9f45>남은 " + remaining + "분</color>"
                    : "  <color=#ff7359>마감 지남</color>";
            }
            _label.text = text;
            _label.gameObject.SetActive(true);

            // 마우스 우상단에 따라붙는다 (커서에 안 가리게 오프셋).
            _label.rectTransform.position = new Vector3(mousePos.x + 18f, mousePos.y + 26f, 0f);
        }
    }
}
