using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 아파트 엘리베이터 — 실물리 캐빈 (S-038 → S-048 ③ 재설계). 각 층 맨 오른쪽 샤프트를
    /// 캐빈(바닥+벽)이 실제로 오르내린다. 층 호출 패널(ApartmentElevatorPanel)이 CallToFloor를,
    /// 캐빈 내부 패널이 RequestFloorSelect→FloorChosen을 부른다. 이동 중 캐빈 범위의
    /// 플레이어·대차·상자를 캐빈에 임시 부모화해 함께 운반한다. 대기·이동은 게임 시계를 소모.
    /// </summary>
    public class ApartmentElevator : MonoBehaviour
    {
        private const float RIDE_SECONDS_PER_FLOOR = 0.9f; // 실시간 연출
        private static readonly Vector3 CABIN_HALF = new Vector3(1.5f, 1.8f, 1.5f);

        [SerializeField] private TuningConfigSO _tuning;
        [Tooltip("캐빈 루트 — 바닥·벽 포함, 이 트랜스폼이 실제로 이동한다.")]
        [SerializeField] private Transform _cabin;
        [Tooltip("층별 캐빈 정지 y (인덱스 0=1층).")]
        [SerializeField] private float[] _floorYs;

        private int _currentFloor = 1;
        private bool _busy;
        private bool _awaitingChoice;

        private void OnEnable() => WorldEvents.FloorChosen += OnFloorChosen;
        private void OnDisable()
        {
            WorldEvents.FloorChosen -= OnFloorChosen;
            _awaitingChoice = false;
        }

        /// <summary>층 호출 패널 — 캐빈을 이 층으로 부른다 (대기 게임분 소모).</summary>
        public void CallToFloor(int floor)
        {
            if (_busy || floor == _currentFloor)
            {
                if (floor == _currentFloor) Debug.Log("[엘베] 이미 이 층에 있다 — 타라.");
                return;
            }
            WorldDayNightManager.Instance?.AdvanceMinutes(_tuning.elevatorWaitMinutes);
            StartCoroutine(MoveCabin(floor, carryRiders: false));
            Debug.Log("[엘베] " + floor + "층 호출 — 대기 " + _tuning.elevatorWaitMinutes + "게임분.");
        }

        /// <summary>캐빈 내부 패널 — 층 선택 UI 요청.</summary>
        public void RequestFloorSelect()
        {
            if (_busy) return;
            var floors = new int[_floorYs.Length];
            for (int i = 0; i < floors.Length; i++) floors[i] = i + 1;
            _awaitingChoice = true;
            WorldEvents.RaiseFloorSelectRequested(floors);
        }

        private void OnFloorChosen(int floor)
        {
            if (!_awaitingChoice) return;
            _awaitingChoice = false;
            if (_busy || floor == _currentFloor || floor < 1 || floor > _floorYs.Length) return;

            float rideMinutes = Mathf.Abs(floor - _currentFloor) * _tuning.elevatorRideMinutesPerFloor;
            WorldDayNightManager.Instance?.AdvanceMinutes(rideMinutes);
            StartCoroutine(MoveCabin(floor, carryRiders: true));
        }

        private IEnumerator MoveCabin(int targetFloor, bool carryRiders)
        {
            _busy = true;
            float fromY = _cabin.position.y;
            float toY = _floorYs[targetFloor - 1];
            float seconds = Mathf.Max(0.4f, Mathf.Abs(targetFloor - _currentFloor) * RIDE_SECONDS_PER_FLOOR);

            List<(Transform t, Transform parent, Rigidbody body, bool wasKinematic)> riders = null;
            if (carryRiders) riders = AttachRiders();

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                float eased = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
                Vector3 position = _cabin.position;
                position.y = Mathf.Lerp(fromY, toY, eased);
                _cabin.position = position;
                yield return null;
            }
            Vector3 final = _cabin.position;
            final.y = toY;
            _cabin.position = final;

            if (riders != null) DetachRiders(riders);
            _currentFloor = targetFloor;
            _busy = false;
            Debug.Log("[엘베] " + targetFloor + "층 도착.");
        }

        // 캐빈 범위의 플레이어·대차·상자를 임시 부모화 — CC·리지드바디 안전 처리.
        private List<(Transform, Transform, Rigidbody, bool)> AttachRiders()
        {
            var riders = new List<(Transform, Transform, Rigidbody, bool)>();
            Vector3 center = _cabin.position + Vector3.up * CABIN_HALF.y;

            void TryAttach(Transform target)
            {
                Vector3 local = target.position - center;
                if (Mathf.Abs(local.x) > CABIN_HALF.x || Mathf.Abs(local.y) > CABIN_HALF.y
                    || Mathf.Abs(local.z) > CABIN_HALF.z) return;
                Rigidbody body = target.GetComponent<Rigidbody>();
                bool wasKinematic = body != null && body.isKinematic;
                if (body != null) body.isKinematic = true;
                riders.Add((target, target.parent, body, wasKinematic));
                target.SetParent(_cabin, worldPositionStays: true);
            }

            // Find 금지 규칙 — 캐빈 부피 물리 쿼리로 탑승자를 실측한다 (안에 있는 것만 태운다).
            var seen = new HashSet<Transform>();
            foreach (Collider hit in Physics.OverlapBox(center, CABIN_HALF, Quaternion.identity))
            {
                Transform candidate = null;
                if (hit.GetComponentInParent<PlayerManager>() is PlayerManager player) candidate = player.transform;
                else if (hit.GetComponentInParent<DeliveryCart>() is DeliveryCart cart) candidate = cart.transform;
                else if (hit.GetComponentInParent<PickupBox>() is PickupBox box && box.transform.parent == null)
                    candidate = box.transform;
                if (candidate != null && seen.Add(candidate)) TryAttach(candidate);
            }
            return riders;
        }

        private void DetachRiders(List<(Transform t, Transform parent, Rigidbody body, bool wasKinematic)> riders)
        {
            foreach (var rider in riders)
            {
                if (rider.t == null) continue;
                rider.t.SetParent(rider.parent, worldPositionStays: true);
                if (rider.body != null) rider.body.isKinematic = rider.wasKinematic;
            }
        }
    }
}
