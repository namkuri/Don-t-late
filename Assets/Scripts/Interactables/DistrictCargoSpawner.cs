using System.Collections.Generic;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 구역 도착 시 짐·비콘 스폰 — S-015. GameState.cargo 중 현재 구역(currentDistrict) 소속 건만큼
    /// 내린 박스(PickupBox)와 집앞 비콘 패드(DeliveryPoint)를 깐다. 구역 미지정(씬 단독 Play)이면 전량.
    /// 정적 배치였던 __gb_Box·__gb_Beacon을 대체한다 — 실개수·실주소가 데이터에서 나온다.
    /// </summary>
    public class DistrictCargoSpawner : MonoBehaviour
    {
        [SerializeField] private GameStateSO _gameState;
        [Tooltip("prop_box_parcel 자동 프리팹. 비면 큐브 폴백.")]
        [SerializeField] private GameObject _boxVisualPrefab;
        [SerializeField] private GameObject _beaconPrefab;
        [SerializeField] private Material _boxHighlight;
        [SerializeField] private Material _boxFallback;
        [SerializeField] private TuningConfigSO _tuning;

        [Header("아파트 배치 (S-038 — 비면 거리 기본 배치)")]
        [Tooltip("짐 스폰 원점 — 아파트는 외부 마당.")]
        [SerializeField] private Transform _boxOrigin;
        [Tooltip("층별 비콘 앵커 — 인덱스 0=2층, 1=3층… 설정 시 order.floor로 층을 찾는다.")]
        [SerializeField] private Transform[] _floorBeaconAnchors;
        [Tooltip("S-129 — 비콘을 Ground 레이어 지면에 스냅한다. 경사 지형(Hillside) 전용 — 평지 씬은 끈 채로 둔다.")]
        [SerializeField] private bool _snapBeaconsToGround;
        [Tooltip("S-255 — 트럭으로 도착했을 때 짐 왼쪽에 세울 플레이어. 빌더 주입, 비면 배치 생략.")]
        [SerializeField] private Transform _player;

        // S-075 ① — 패드 기본 슬롯: 교차 도로(x -2.1~2.1)와 그 여백을 피해 건물 앞에만 (x=0 스폰 금지).
        private static readonly float[] BEACON_SLOTS_X = { -8f, 8f, 16f, -16f, 24f, -24f, 32f, -32f };

        private void OnEnable() => WorldEvents.SceneTransitionStarted += OnSceneLeaving;
        private void OnDisable() => WorldEvents.SceneTransitionStarted -= OnSceneLeaving;

        // S-068 ③ — 씬을 떠날 때 바닥에 남은 짐(활성 PickupBox·cargo 소속)의 위치를 기록한다.
        private void OnSceneLeaving(GameScene _)
        {
            if (_gameState == null) return;
            string district = _gameState.currentDistrict;
            _gameState.droppedCargo.RemoveAll(d => d.district == district);
            foreach (PickupBox box in Object.FindObjectsByType<PickupBox>())
            {
                if (box == null || box.Order == null || !box.gameObject.activeInHierarchy) continue;
                if (!_gameState.cargo.Contains(box.Order)) continue;
                _gameState.droppedCargo.Add(new DroppedCargo
                {
                    orderId = box.Order.orderId,
                    district = district,
                    position = box.transform.position
                });
            }
        }

        private void Start()
        {
            string district = _gameState != null ? _gameState.currentDistrict : null;

            // S-074 ② — 패드는 하루 물량(dayOrders) 기준으로 첫 입장부터 전부 선다. 위치는 물량
            // 확정 순서(dayOrders 인덱스)로 결정적 — 재입장에도 같은 자리다. (구 cargo 기준은
            // 픽업분만·방문마다 인덱스가 달라 "패드가 옮겨 다니는" 원흉이었다.)
            // dayOrders가 비면(씬 단독 Play) cargo 폴백.
            var pool = _gameState.dayOrders.Count > 0 ? _gameState.dayOrders : _gameState.cargo;
            var matching = new List<DeliveryOrderSO>();
            foreach (DeliveryOrderSO order in pool)
            {
                if (order == null) continue;
                if (!string.IsNullOrEmpty(district) && order.district != district) continue;
                matching.Add(order);
            }

            var beaconPositions = new Dictionary<string, Vector3>(); // 주소→패드 위치 (배치 상자 복원용)
            var perFloorCount = new Dictionary<int, int>();
            int boxSlot = 0;
            for (int i = 0; i < matching.Count; i++)
            {
                DeliveryOrderSO order = matching[i];

                Vector3 beaconPos;
                if (_floorBeaconAnchors != null && _floorBeaconAnchors.Length > 0)
                {
                    // S-038: 층별 앵커 배치 — order.floor(2층=[0])별로 옆으로 나열.
                    int floorIndex = Mathf.Clamp(order.floor - 2, 0, _floorBeaconAnchors.Length - 1);
                    perFloorCount.TryGetValue(floorIndex, out int slot);
                    perFloorCount[floorIndex] = slot + 1;
                    beaconPos = _floorBeaconAnchors[floorIndex].position + new Vector3(slot * 5f, 0f, 0f);
                    if (_snapBeaconsToGround) beaconPos = SnapToGround(beaconPos);
                }
                else beaconPos = new Vector3(BEACON_SLOTS_X[i % BEACON_SLOTS_X.Length], 0f, 0f);

                SpawnBeacon(order, beaconPos);
                beaconPositions[order.address] = beaconPos;
            }

            // 상자: cargo(픽업 등록) 소속 건만 실물이 있다. 파손 건은 그날 재스폰 금지 (S-074 ③).
            int boxCount = 0;
            Vector3 firstBoxPos = Vector3.zero; // S-255 — 도착 지점 기준
            foreach (DeliveryOrderSO order in matching)
            {
                if (!_gameState.cargo.Contains(order)) continue;
                if (_gameState.destroyedOrderIds.Contains(order.orderId)) continue;
                // S-264 — 트럭이 있으면 **짐칸에 실제로 실은 것만** 내린다. `cargo`는 손에 드는
                // 순간 등록되므로(S-066 ①) 그것만 보면 캠프에서 들었다 놓은 짐까지 따라온다
                // (남규님 관찰). 트럭이 없는 도보 시대는 종전대로 — 그때는 손에 든 것이 곧 적재다.
                if (_gameState.hasTruck && !_gameState.truckCargoIds.Contains(order.orderId)) continue;
                // S-066 ② — 도보로 손에 들고 온 짐은 상자 재스폰 없음(중복 방지).
                if (_gameState.carriedOrders.Exists(c => c != null && c.orderId == order.orderId)) continue;

                int placedIndex = _gameState.placedDeliveries.FindIndex(p => p.orderId == order.orderId);
                Vector3 boxPos;
                if (placedIndex >= 0)
                {
                    // S-074 ① — 배치 완료 건은 그 패드 위에 놓인 모습으로 유지 (정산 전까지).
                    string beaconAddress = _gameState.placedDeliveries[placedIndex].beaconAddress;
                    if (!beaconPositions.TryGetValue(beaconAddress, out boxPos)) continue; // 딴 구역 패드에 배치 — 여긴 없음
                    boxPos += new Vector3(0f, 0.02f, 0f); // S-098 ① — kinematic 고정 스폰이라 낙하 여유 불요
                }
                else
                {
                    // S-068 ③ — 지난번 버려둔 자리가 기록돼 있으면 그 자리에 되살린다.
                    int droppedIndex = _gameState.droppedCargo.FindIndex(
                        d => d.orderId == order.orderId && d.district == district);
                    boxPos = droppedIndex >= 0
                        ? _gameState.droppedCargo[droppedIndex].position
                        : (_boxOrigin != null
                            ? _boxOrigin.position + new Vector3(boxSlot * 1.2f, 0f, 0f)
                            : new Vector3(-16f + boxSlot * 1.2f, 0f, -1.2f));
                    boxSlot++;
                }
                if (boxCount == 0) firstBoxPos = boxPos;
                SpawnBox(order, boxPos, frozen: placedIndex >= 0); // S-097 ① — 배치 상자는 물리 낙하 없이 고정
                boxCount++;
            }
            Debug.Log("[CargoSpawner] 구역 '" + district + "' — 비콘 " + matching.Count + "개 · 짐 " + boxCount + "건 스폰.");
            PlaceArrivingPlayer(firstBoxPos, boxCount);
        }

        /// <summary>
        /// S-255 — **트럭으로 도착했을 때만** 플레이어를 짐 왼쪽에 세운다(남규님 지시).
        /// 도보(엣지 워크)로 넘어온 경우는 `DistrictEdgeGate`가 이미 진입 지점에 세워 뒀으므로
        /// 건드리면 안 된다 — 넘어오자마자 반대편으로 순간이동하는 꼴이 된다.
        /// 짐이 하나도 없으면(전량 배치 완료 등) 손대지 않는다.
        /// </summary>
        private void PlaceArrivingPlayer(Vector3 firstBoxPos, int boxCount)
        {
            if (_player == null) return;
            if (WorldSceneFlowManager.Instance == null
                || WorldSceneFlowManager.Instance.PreviousScene != GameScene.Travel) return;

            // S-260 — 짐이 하나도 없어도 **짐이 놓일 자리**를 기준으로 세운다. 종전엔 여기서
            // 돌아서서, 짐을 안 싣고 이동하면 배치가 통째로 건너뛰어졌다(남규님 관찰: 맵 중간에 스폰).
            // 기준 좌표는 상자 스폰과 같은 식이어야 한다 — 다르면 짐이 있을 때와 없을 때 자리가 달라진다.
            if (boxCount <= 0)
                firstBoxPos = _boxOrigin != null ? _boxOrigin.position : new Vector3(-16f, 0f, -1.2f);

            // 짐보다 왼쪽 — 다가가면 바로 집을 수 있는 거리.
            Vector3 spawn = new Vector3(firstBoxPos.x - ARRIVAL_LEFT_OFFSET, _player.position.y, firstBoxPos.z);

            // 엣지 게이트와 붙이지 않는다(남규님 단서). 빌라촌 실측: 짐 −16 · 엣지 −19라 오프셋만
            // 쓰면 −18.2로 게이트에 0.8까지 붙는다 — 도착하자마자 옆 구역으로 넘어간다.
            foreach (DistrictEdgeGate gate in Object.FindObjectsByType<DistrictEdgeGate>(FindObjectsInactive.Include))
            {
                if (gate == null) continue;
                float gateX = gate.transform.position.x;
                if (gateX < firstBoxPos.x) spawn.x = Mathf.Max(spawn.x, gateX + EDGE_CLEARANCE);
            }

            CharacterController controller = _player.GetComponentInChildren<CharacterController>();
            if (controller != null) controller.enabled = false; // CC가 켜져 있으면 위치 대입이 씹힌다
            _player.position = spawn;
            if (controller != null) controller.enabled = true;
            Debug.Log("[CargoSpawner] 트럭 도착 — 플레이어를 짐 왼쪽 " + spawn.ToString("F1") + "에 세움 (S-255).");
        }

        private const float ARRIVAL_LEFT_OFFSET = 2.2f;
        private const float EDGE_CLEARANCE = 2.0f;

        // 트럭에서 내린 짐 — 보도 앞줄에 일렬.
        private void SpawnBox(DeliveryOrderSO order, Vector3 groundPosition, bool frozen = false)
        {
            GameObject root = new GameObject("SpawnedBox_" + order.orderId);
            root.transform.position = groundPosition;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = false; // 실물 — 던지기·취급주의 파손 (S-019 ①)
            collider.size = Vector3.one * 0.7f;
            collider.center = new Vector3(0f, 0.35f, 0f);
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 2f;
            body.isKinematic = frozen; // S-097 ① — 재방문 배치 상자: 그 자리 고정 (픽업 시 어차피 kinematic 전환)
            root.AddComponent<BoxDurability>().Initialize(_tuning);

            if (_boxVisualPrefab != null)
            {
                GameObject visual = Instantiate(_boxVisualPrefab, root.transform);
                Bounds bounds = ComputeBounds(visual);
                if (bounds.size.y > 0.001f)
                    visual.transform.localScale = Vector3.one * (0.7f / bounds.size.y);
                bounds = ComputeBounds(visual);
                visual.transform.position += root.transform.position
                    - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
            else
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(cube.GetComponent<Collider>());
                cube.transform.SetParent(root.transform, false);
                cube.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                cube.transform.localScale = Vector3.one * 0.7f;
                if (_boxFallback != null) cube.GetComponent<Renderer>().sharedMaterial = _boxFallback;
            }

            PickupBox pickup = root.AddComponent<PickupBox>();
            pickup.Initialize(order, _boxHighlight, requireInCargo: true, requireScanned: false);
        }

        // 집(건물 슬롯 라인) 앞 인증 패드.
        // S-129 — 같은 층에 물량이 둘 이상이면 앵커에서 +5u씩 옆으로 나열한다(위 슬롯 로직).
        // 평지에서는 맞지만 27° 비탈에서는 5u 옆이 곧 2.5u 높이 차라 패드가 파묻히거나 뜬다.
        // Ground 레이어(S-128 ③)가 곧 지면이므로 거기에만 스냅한다.
        private static Vector3 SnapToGround(Vector3 position)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0) return position;
            if (Physics.Raycast(new Vector3(position.x, position.y + 30f, position.z), Vector3.down,
                    out RaycastHit hit, 60f, 1 << groundLayer, QueryTriggerInteraction.Ignore))
                return new Vector3(position.x, hit.point.y, position.z);
            return position;
        }

        private void SpawnBeacon(DeliveryOrderSO order, Vector3 position)
        {
            if (_beaconPrefab == null) return;
            GameObject beacon = Instantiate(_beaconPrefab, position, Quaternion.identity);
            beacon.name = "SpawnedBeacon_" + order.orderId;
            beacon.GetComponent<DeliveryPoint>().SetOrder(order);
        }

        private static Bounds ComputeBounds(GameObject go)
        {
            Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
            bool initialized = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (!initialized) { bounds = r.bounds; initialized = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }
    }
}
