using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// Camp 트럭 적재존 — S-005, S-009 개조. 박스를 "손에 든 채" 트럭 앞에서 E를 누르면
    /// 그 주문이 적재(OrderAccepted)되고, 상자가 짐칸에 실제로 쌓인다(시각 피드백).
    /// 패드+E 무피드백 방식은 폐지 — 픽업은 PickupBox, 적재는 여기.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LoadingZone : MonoBehaviour, IInteractable
    {
        [Tooltip("실린 상자가 쌓이는 짐칸 내부 앵커.")]
        [SerializeField] private Transform _stackRoot;
        [Tooltip("짐칸에 쌓이는 상자 비주얼(prop_box_parcel). 비우면 큐브 폴백.")]
        [SerializeField] private GameObject _boxVisualPrefab;
        [SerializeField] private Material _boxMaterial;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        private int _stacked;

        public void Interact(PlayerContext ctx)
        {
            PlayerStatusManager status = ctx.Player.Status;
            if (!status.IsCarrying)
            {
                Debug.Log("[LoadingZone] 빈손 — 박스를 먼저 들어라 (E로 픽업).");
                return;
            }

            GameStateSO state = ctx.Player.GameState;
            if (state.cargo.Count >= ctx.Player.Tuning.maxCargo)
            {
                Debug.Log("[LoadingZone] 적재 상한(" + ctx.Player.Tuning.maxCargo + ") — 더 실을 수 없다.");
                return;
            }

            DeliveryOrderSO order = status.CarriedOrder;
            status.ReleaseCarry(); // 손의 상자 비주얼 제거
            WorldDeliveryManager.Instance.AcceptOrder(order);
            StackBoxVisual();
        }

        /// <summary>짐칸에 상자를 한 칸씩 쌓는다 — "실렸다"가 눈에 보이게. 수제 박스 프리팹 우선 (S-012).</summary>
        private void StackBoxVisual()
        {
            if (_stackRoot == null) return;

            GameObject box;
            if (_boxVisualPrefab != null)
            {
                box = Instantiate(_boxVisualPrefab);
                float height = ComputeHeight(box);
                if (height > 0.001f) box.transform.localScale = Vector3.one * (0.7f / height);
            }
            else
            {
                box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(box.GetComponent<Collider>());
                box.transform.localScale = Vector3.one * 0.8f;
                if (_boxMaterial != null) box.GetComponent<Renderer>().sharedMaterial = _boxMaterial;
            }

            box.name = "LoadedBox_" + (_stacked + 1);
            box.transform.SetParent(_stackRoot, true);
            box.transform.localPosition = new Vector3((_stacked % 2) * 0.85f - 0.4f, (_stacked / 2) * 0.75f, 0f);
            _stacked++;
        }

        private static float ComputeHeight(GameObject go)
        {
            Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
            bool initialized = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (!initialized) { bounds = r.bounds; initialized = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return initialized ? bounds.size.y : 0f;
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            Material material = on ? _highlightMaterial : _normalMaterial;
            if (material != null) _renderer.sharedMaterial = material;
        }
    }
}
