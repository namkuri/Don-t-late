using System.Collections;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 아파트 엘리베이터 패널 (S-038 · D-067) — 각 층에 하나씩. E = 호출 → 대기(게임분 소모) →
    /// 층 선택 UI(FloorSelectRequested) → 선택 층으로 플레이어+근처 대차·낱개 상자를 이동(층당 게임분 소모).
    /// "늦지마" 압박과 직결 — 대기·이동이 전부 시계를 먹는다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ApartmentElevator : MonoBehaviour, IInteractable
    {
        private const float CARGO_RADIUS = 5f; // 함께 이동하는 대차·상자 반경
        private const float DOOR_WAIT_REALTIME = 1.2f; // 연출용 실초 (게임분 소모는 튜닝)

        [SerializeField] private TuningConfigSO _tuning;
        [Tooltip("이 패널이 있는 층 (1=로비).")]
        [SerializeField] private int _floor = 1;
        [Tooltip("층별 내리는 지점 — 인덱스 0=1층(로비), 1=2층…")]
        [SerializeField] private Transform[] _floorExits;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        private Transform _player;
        private bool _awaitingChoice;

        private void OnEnable() => WorldEvents.FloorChosen += OnFloorChosen;

        private void OnDisable()
        {
            WorldEvents.FloorChosen -= OnFloorChosen;
            _awaitingChoice = false;
        }

        public void Interact(PlayerContext ctx)
        {
            if (_awaitingChoice) return;
            _player = ctx.Player.transform;
            StartCoroutine(CallElevator());
        }

        public void SetHighlight(bool on)
        {
            if (_renderer == null) return;
            Material material = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
            if (material != null) _renderer.sharedMaterial = material;
        }

        private IEnumerator CallElevator()
        {
            Debug.Log("[엘베] 호출 — 대기 " + _tuning.elevatorWaitMinutes + "게임분.");
            WorldDayNightManager.Instance?.AdvanceMinutes(_tuning.elevatorWaitMinutes);
            yield return new WaitForSeconds(DOOR_WAIT_REALTIME);

            var floors = new int[_floorExits.Length];
            for (int i = 0; i < floors.Length; i++) floors[i] = i + 1;
            _awaitingChoice = true;
            WorldEvents.RaiseFloorSelectRequested(floors);
        }

        private void OnFloorChosen(int floor)
        {
            if (!_awaitingChoice) return; // 다른 층 패널의 요청
            _awaitingChoice = false;

            if (floor == _floor || floor < 1 || floor > _floorExits.Length) return;
            Transform exit = _floorExits[floor - 1];
            if (exit == null || _player == null) return;

            float rideMinutes = Mathf.Abs(floor - _floor) * _tuning.elevatorRideMinutesPerFloor;
            WorldDayNightManager.Instance?.AdvanceMinutes(rideMinutes);

            // 플레이어 + 반경 내 대차·낱개 상자 동반 이동.
            var controller = _player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            _player.position = exit.position;
            if (controller != null) controller.enabled = true;

            foreach (DeliveryCart cart in Object.FindObjectsByType<DeliveryCart>())
                if ((cart.transform.position - transform.position).magnitude <= CARGO_RADIUS)
                    cart.MoveTo(exit.position + Vector3.right * 1.4f);

            int loose = 0;
            foreach (PickupBox box in Object.FindObjectsByType<PickupBox>())
            {
                if (box.transform.parent != null) continue;
                if ((box.transform.position - transform.position).magnitude > CARGO_RADIUS) continue;
                box.transform.position = exit.position + new Vector3(2.4f + loose * 0.9f, 0.4f, 0f);
                loose++;
            }
            Debug.Log("[엘베] " + _floor + "층 → " + floor + "층 (" + rideMinutes + "게임분 소모).");
        }
    }
}
