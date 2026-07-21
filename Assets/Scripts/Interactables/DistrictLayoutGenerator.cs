using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// District 씬의 빈 슬롯(건물 12·소품 10)을 <b>결정론적 시드 랜덤</b>으로 채운다.
    /// 같은 구역(districtId)은 재방문·재로드해도 항상 같은 배치가 나온다(결정 D-036).
    /// 슬롯은 런타임 이름 검색 없이 <see cref="DistrictSceneBuilder"/>가 직렬화 참조로 주입한다.
    /// 프리팹 풀이 비면 그레이박스(큐브 조합)로 폴백한다 — Prefabs/Auto 카탈로그 도착 시 소켓 교체.
    /// </summary>
    public class DistrictLayoutGenerator : MonoBehaviour
    {
        // 지금은 빌더가 "HappyVilla"를 직렬화로 주입한다. 구역별 주문 연동(구역 id→배치)은 P3 몫.
        [SerializeField] private string _districtId = "HappyVilla";
        [SerializeField] private Transform[] _buildingSlots;
        [SerializeField] private Transform[] _propSlots;
        [Tooltip("비면 그레이박스 건물로 폴백. 카탈로그 프리팹이 들어오면 슬롯당 결정론 선택.")]
        [SerializeField] private GameObject[] _buildingPrefabPool;
        [Tooltip("비면 상자더미 큐브로 폴백.")]
        [SerializeField] private GameObject[] _propPrefabPool;

        private const string GENERATED_ROOT = "GeneratedLayout";

        // 그레이박스 건물 규약: 층 1~3 × 3.0u · 폭 6~7u · 깊이 5u · 색 3톤.
        private const int MIN_FLOORS = 1;
        private const int MAX_FLOORS = 3;
        private const float FLOOR_HEIGHT = 3.0f;
        private const float MIN_WIDTH = 6f;
        private const float MAX_WIDTH = 7f;
        private const float BUILDING_DEPTH = 5f;
        private const int TONE_COUNT = 3;

        // 공간 정합(S-003): 건물 전면(길 쪽 = −Z 면)을 보도 경계 뒤 Z=+3.0에 정렬하고
        // 깊이는 +Z(길 안쪽)로만 확장한다 — 보도(Z −3~+3)·뒷줄 가로등(Z=+2.4) 침범 0.
        private const float BUILDING_FRONT_Z = 3.0f;

        // 소품: 배치 확률·종류(현재 1종 폴백).
        private const float PROP_PLACE_CHANCE = 0.6f;
        private const int PROP_KINDS = 1;

        // 건물 3톤(그레이 계열 명도 변형) · 소품 상자색.
        private static readonly Color[] ToneColors =
        {
            new Color(0.30f, 0.30f, 0.34f),
            new Color(0.38f, 0.36f, 0.40f),
            new Color(0.24f, 0.26f, 0.30f),
        };
        private static readonly Color PropColor = new Color(0.55f, 0.40f, 0.25f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // Additive 재진입·중복 OnEnable 대비: 생성 전 항상 이전 생성물을 지운다(멱등).
        private void OnEnable() => Generate();

        /// <summary>
        /// 이전 생성물을 정리하고 시드 고정 배치를 다시 만든다. 같은 districtId면 결과가 동일하다.
        /// </summary>
        public void Generate()
        {
            ClearGenerated();

            Transform root = new GameObject(GENERATED_ROOT).transform;
            root.SetParent(transform, false);

            var rng = new System.Random(Fnv1a(_districtId));

            if (_buildingSlots != null)
            {
                for (int i = 0; i < _buildingSlots.Length; i++)
                {
                    Transform slot = _buildingSlots[i];

                    // 슬롯당 결정론 추첨(추첨 순서 고정 = 스트림 안정) — slot이 null이어도 소비한다.
                    int floors = rng.Next(MIN_FLOORS, MAX_FLOORS + 1);
                    int tone = rng.Next(0, TONE_COUNT);
                    float width = MIN_WIDTH + (float)rng.NextDouble() * (MAX_WIDTH - MIN_WIDTH);

                    if (slot == null) continue;
                    BuildBuilding(root, i + 1, slot, floors, tone, width);
                }
            }

            if (_propSlots != null)
            {
                for (int i = 0; i < _propSlots.Length; i++)
                {
                    Transform slot = _propSlots[i];

                    bool place = rng.NextDouble() < PROP_PLACE_CHANCE;
                    int kind = rng.Next(0, PROP_KINDS);

                    if (slot == null || !place) continue;
                    BuildProp(root, i + 1, slot, kind);
                }
            }
        }

        // ── 그레이박스 조립 ──────────────────────────────────

        private void BuildBuilding(Transform root, int slotNo, Transform slot, int floors, int tone, float width)
        {
            // 이름에 결정론 값(floors·tone)을 새겨 스냅샷 비교의 지문으로 쓴다.
            GameObject building = new GameObject($"Building_{slotNo:00}_f{floors}_t{tone}");
            building.transform.SetParent(root, false);
            building.transform.SetPositionAndRotation(slot.position, slot.rotation);

            if (_buildingPrefabPool != null && _buildingPrefabPool.Length > 0)
            {
                GameObject prefab = _buildingPrefabPool[tone % _buildingPrefabPool.Length];
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, building.transform);
                    NormalizeBuilding(instance, floors, slot.position.z);
                    return;
                }
            }

            // 전면 면을 세계 Z=+3.0에 맞추기 위한 로컬 Z 중심 (슬롯 회전=identity 전제 · 건물 라인은 직선).
            float centerZLocal = (BUILDING_FRONT_Z + BUILDING_DEPTH * 0.5f) - slot.position.z;

            for (int f = 0; f < floors; f++)
            {
                GameObject cube = MakeCube(building.transform, $"Floor_{f}",
                    new Vector3(0f, f * FLOOR_HEIGHT + FLOOR_HEIGHT * 0.5f, centerZLocal),
                    new Vector3(width, FLOOR_HEIGHT, BUILDING_DEPTH));
                Tint(cube, ToneColors[tone]);
            }
        }

        /// <summary>임포트 건물을 층수 높이에 맞춰 스케일하고 발 y=0·전면 Z=3.0 정렬 (S-011 — 반입물이 1u 정규화라 필수).</summary>
        private void NormalizeBuilding(GameObject instance, int floors, float slotZ)
        {
            Bounds bounds = new Bounds(instance.transform.position, Vector3.zero);
            bool initialized = false;
            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
            {
                if (!initialized) { bounds = r.bounds; initialized = true; }
                else bounds.Encapsulate(r.bounds);
            }
            if (!initialized || bounds.size.y < 0.01f) return;

            float targetHeight = floors * FLOOR_HEIGHT;
            instance.transform.localScale *= targetHeight / bounds.size.y;

            initialized = false;
            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
            {
                if (!initialized) { bounds = r.bounds; initialized = true; }
                else bounds.Encapsulate(r.bounds);
            }
            instance.transform.position += new Vector3(
                0f, -bounds.min.y, BUILDING_FRONT_Z - bounds.min.z);
        }

        private void BuildProp(Transform root, int slotNo, Transform slot, int kind)
        {
            GameObject prop = new GameObject($"Prop_{slotNo:00}_k{kind}");
            prop.transform.SetParent(root, false);
            prop.transform.SetPositionAndRotation(slot.position, slot.rotation);

            if (_propPrefabPool != null && _propPrefabPool.Length > 0)
            {
                GameObject prefab = _propPrefabPool[kind % _propPrefabPool.Length];
                if (prefab != null)
                {
                    Instantiate(prefab, prop.transform);
                    return;
                }
            }

            // 상자더미 폴백: 큐브 3개를 쌓아 더미 실루엣.
            Tint(MakeCube(prop.transform, "Box_0", new Vector3(0f, 0.4f, 0f), Vector3.one * 0.8f), PropColor);
            Tint(MakeCube(prop.transform, "Box_1", new Vector3(0.5f, 0.35f, 0.2f), Vector3.one * 0.7f), PropColor);
            Tint(MakeCube(prop.transform, "Box_2", new Vector3(0.15f, 1.0f, -0.1f), Vector3.one * 0.6f), PropColor);
        }

        private static GameObject MakeCube(Transform parent, string name, Vector3 localPos, Vector3 localScale)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            Destroy(cube.GetComponent<BoxCollider>()); // 배경 시각물 — 배송 루프 물리 간섭 방지
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = localScale;
            return cube;
        }

        // 머티리얼을 새로 만들지 않고 MaterialPropertyBlock으로 색만 밀어넣는다(할당·릭 없음).
        private static void Tint(GameObject go, Color color)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            var mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, color);
            mpb.SetColor(ColorId, color);
            renderer.SetPropertyBlock(mpb);
        }

        private void ClearGenerated()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name != GENERATED_ROOT) continue;
                // 동기 파괴로 같은 프레임 중복 OnEnable에도 사본이 절대 남지 않게 한다(멱등 프레임 정확).
                DestroyImmediate(child.gameObject);
            }
        }

        // string.GetHashCode는 플랫폼·런타임 보증이 없다 → 자체 FNV-1a 32비트로 결정론 시드 산출.
        private static int Fnv1a(string s)
        {
            uint hash = 2166136261u;
            foreach (char c in s)
            {
                hash ^= c;
                hash *= 16777619u;
            }
            return unchecked((int)hash);
        }
    }
}
