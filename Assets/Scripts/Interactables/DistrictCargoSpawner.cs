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
                }
                else beaconPos = new Vector3(BEACON_SLOTS_X[i % BEACON_SLOTS_X.Length], 0f, 0f);

                SpawnBeacon(order, beaconPos);
                beaconPositions[order.address] = beaconPos;
            }

            // 상자: cargo(픽업 등록) 소속 건만 실물이 있다. 파손 건은 그날 재스폰 금지 (S-074 ③).
            int boxCount = 0;
            foreach (DeliveryOrderSO order in matching)
            {
                if (!_gameState.cargo.Contains(order)) continue;
                if (_gameState.destroyedOrderIds.Contains(order.orderId)) continue;
                // S-066 ② — 도보로 손에 들고 온 짐은 상자 재스폰 없음(중복 방지).
                if (_gameState.carriedOrders.Exists(c => c != null && c.orderId == order.orderId)) continue;

                int placedIndex = _gameState.placedDeliveries.FindIndex(p => p.orderId == order.orderId);
                Vector3 boxPos;
                if (placedIndex >= 0)
                {
                    // S-074 ① — 배치 완료 건은 그 패드 위에 놓인 모습으로 유지 (정산 전까지).
                    string beaconAddress = _gameState.placedDeliveries[placedIndex].beaconAddress;
                    if (!beaconPositions.TryGetValue(beaconAddress, out boxPos)) continue; // 딴 구역 패드에 배치 — 여긴 없음
                    boxPos += new Vector3(0f, 0.1f, 0f);
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
                SpawnBox(order, boxPos);
                boxCount++;
            }
            Debug.Log("[CargoSpawner] 구역 '" + district + "' — 비콘 " + matching.Count + "개 · 짐 " + boxCount + "건 스폰.");
        }

        // 트럭에서 내린 짐 — 보도 앞줄에 일렬.
        private void SpawnBox(DeliveryOrderSO order, Vector3 groundPosition)
        {
            GameObject root = new GameObject("SpawnedBox_" + order.orderId);
            root.transform.position = groundPosition;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = false; // 실물 — 던지기·취급주의 파손 (S-019 ①)
            collider.size = Vector3.one * 0.7f;
            collider.center = new Vector3(0f, 0.35f, 0f);
            root.AddComponent<Rigidbody>().mass = 2f;
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
