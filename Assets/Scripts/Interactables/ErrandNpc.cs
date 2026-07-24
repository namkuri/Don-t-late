using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 심부름 노인 NPC (S-052 ③). 길가에 서 있다(간혹 부재 추첨). 말 걸면 옆의 짐을 지정 위치로
    /// 옮겨달라 부탁 → 짐을 목표 마커에 가져가면 배달 완료 → 돌아와 말 걸면 보상(₩).
    /// 심부름 짐은 런타임 주문(SO 인스턴스)이라 배송 정산(placedDeliveries)과 섞이지 않는다.
    /// </summary>
    public class ErrandNpc : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private DialogueScenarioSO _askScenario;
        [SerializeField] private DialogueScenarioSO _progressScenario;
        [SerializeField] private DialogueScenarioSO _thanksScenario;
        [Tooltip("월드 좌표 목표 지점 — 여기까지 짐을 옮기면 완료.")]
        [SerializeField] private Vector3 _targetPosition;
        [SerializeField] private int _reward = 1500;
        [Tooltip("오늘 자리에 없을 확률.")]
        [SerializeField] private float _absentChance = 0.35f;
        [SerializeField] private Material _boxHighlight;
        [SerializeField] private Material _boxNormal;
        [SerializeField] private Renderer _highlightRenderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        private enum Phase { Idle, Asked, Delivered, Done }
        private Phase _phase = Phase.Idle;
        private DeliveryOrderSO _errandOrder;
        private PlayerManager _player;
        private GameObject _marker;

        private void Start()
        {
            if (Random.value < _absentChance) gameObject.SetActive(false); // 오늘은 안 나옴
        }

        public void Interact(PlayerContext ctx)
        {
            if (WorldDialogueManager.Instance == null || WorldDialogueManager.Instance.IsPlaying) return;

            switch (_phase)
            {
                case Phase.Idle:
                    _player = ctx.Player;
                    SpawnErrandBox();
                    SpawnTargetMarker();
                    _phase = Phase.Asked;
                    if (_askScenario != null) WorldDialogueManager.Instance.PlayScenario(_askScenario);
                    Debug.Log("[심부름] 의뢰 수락 — 짐을 마커까지.");
                    break;

                case Phase.Asked:
                    if (_progressScenario != null) WorldDialogueManager.Instance.PlayScenario(_progressScenario);
                    break;

                case Phase.Delivered:
                    _phase = Phase.Done;
                    if (_gameState != null)
                    {
                        _gameState.money += _reward;
                        _gameState.totalEarned += _reward;
                    }
                    if (_thanksScenario != null) WorldDialogueManager.Instance.PlayScenario(_thanksScenario);
                    Debug.Log("[심부름] 보상 지급 +₩" + _reward);
                    break;

                case Phase.Done:
                    if (_thanksScenario != null) WorldDialogueManager.Instance.PlayScenario(_thanksScenario);
                    break;
            }
        }

        private void Update()
        {
            if (_phase != Phase.Asked || _player == null) return;
            if (_player.Status.CarriedOrder != _errandOrder || _errandOrder == null) return;

            Vector3 playerXZ = _player.transform.position;
            Vector3 targetXZ = _targetPosition;
            playerXZ.y = 0f; targetXZ.y = 0f;
            float heightGap = Mathf.Abs(_player.transform.position.y - _targetPosition.y);
            if (Vector3.Distance(playerXZ, targetXZ) > 1.4f || heightGap > 1.5f) return;

            // 배달 완료 — 손의 짐을 목표 지점에 내려놓는다.
            _player.Status.ReleaseCarry();
            PlaceDeliveredBox();
            if (_marker != null) _marker.SetActive(false);
            _phase = Phase.Delivered;
            Debug.Log("[심부름] 짐 도착 — 어르신께 돌아가자.");
        }

        private void SpawnErrandBox()
        {
            _errandOrder = ScriptableObject.CreateInstance<DeliveryOrderSO>();
            _errandOrder.address = "심부름 짐";
            _errandOrder.reward = 0;

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "ErrandBox";
            box.transform.position = transform.position + transform.right * 0.9f + Vector3.up * 0.25f;
            box.transform.localScale = Vector3.one * 0.5f;
            if (_boxNormal != null) box.GetComponent<Renderer>().sharedMaterial = _boxNormal;

            PickupBox pickup = box.AddComponent<PickupBox>();
            pickup.Initialize(_errandOrder, _boxHighlight, requireInCargo: false, requireScanned: false);
        }

        private void SpawnTargetMarker()
        {
            _marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _marker.name = "ErrandTarget";
            Object.Destroy(_marker.GetComponent<Collider>());
            _marker.transform.position = _targetPosition + Vector3.up * 0.06f;
            _marker.transform.localScale = new Vector3(1.6f, 0.05f, 1.6f);
            if (_boxHighlight != null) _marker.GetComponent<Renderer>().sharedMaterial = _boxHighlight;
        }

        private void PlaceDeliveredBox()
        {
            GameObject placed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placed.name = "ErrandBoxDelivered";
            placed.transform.position = _targetPosition + Vector3.up * 0.25f;
            placed.transform.localScale = Vector3.one * 0.5f;
            if (_boxNormal != null) placed.GetComponent<Renderer>().sharedMaterial = _boxNormal;
        }

        public void SetHighlight(bool on)
        {
            if (_highlightRenderer == null) return;
            _highlightRenderer.sharedMaterial = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
        }
    }
}
