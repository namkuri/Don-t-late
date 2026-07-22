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

        private void Start()
        {
            string district = _gameState != null ? _gameState.currentDistrict : null;
            var matching = new List<DeliveryOrderSO>();
            foreach (DeliveryOrderSO order in _gameState.cargo)
            {
                if (order == null) continue;
                if (!string.IsNullOrEmpty(district) && order.district != district) continue;
                matching.Add(order);
            }

            for (int i = 0; i < matching.Count; i++)
            {
                SpawnBox(matching[i], new Vector3(-16f + i * 1.2f, 0f, -1.2f));
                SpawnBeacon(matching[i], new Vector3(-8f + i * 8f, 0f, 0f));
            }
            Debug.Log("[CargoSpawner] 구역 '" + district + "' — 짐 " + matching.Count + "건 · 비콘 " + matching.Count + "개 스폰.");
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
