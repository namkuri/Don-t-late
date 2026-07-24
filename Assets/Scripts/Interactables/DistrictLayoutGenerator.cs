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
        // S-035(D-064): 런타임엔 GameState.currentDistrict(이동맵 선택)가 우선 — 구역별 배치·프로필 연동.
        // _districtId는 씬 단독 Play 폴백(빌더 주입 기본값).
        [SerializeField] private string _districtId = DeliveryOrderSO.DISTRICT_VILLATOWN;
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private Transform[] _buildingSlots;
        [SerializeField] private Transform[] _propSlots;
        [Tooltip("비면 그레이박스 건물로 폴백. 카탈로그 프리팹이 들어오면 슬롯당 결정론 선택.")]
        [SerializeField] private GameObject[] _buildingPrefabPool;
        [Tooltip("비면 상자더미 큐브로 폴백.")]
        [SerializeField] private GameObject[] _propPrefabPool;

        private const string GENERATED_ROOT = "GeneratedLayout";

        // 그레이박스 건물 규약: 층 높이 3.0u · 깊이 5u. 층수·폭·색톤은 구역 프로필이 정한다(S-035).
        private const float FLOOR_HEIGHT = 3.0f;
        private const float BUILDING_DEPTH = 5f;

        // 공간 정합(S-003): 건물 전면(길 쪽 = −Z 면)을 보도 경계 뒤 Z=+3.0에 정렬하고
        // 깊이는 +Z(길 안쪽)로만 확장한다 — 보도(Z −3~+3)·뒷줄 가로등(Z=+2.4) 침범 0.
        private const float BUILDING_FRONT_Z = 3.0f;

        // 소품 종류(현재 1종 폴백) — 배치 확률은 구역 프로필 몫.
        private const int PROP_KINDS = 1;

        private static readonly Color PropColor = new Color(0.55f, 0.40f, 0.25f);

        // ── 구역 프로필 (S-035 · D-064) — 색톤·밀도 파라미터로 구역감. 그레이박스 수준, 실아트는 A-004 이후. ──

        private struct DistrictProfile
        {
            public int minFloors;
            public int maxFloors;
            public float minWidth;
            public float maxWidth;
            public float propChance;
            public Color[] tones;
            public bool signs; // 먹자골목: 건물 전면 간판 스트립
        }

        // 빌라촌: 낮은 건물(1~2층) 밀집 — 폭 넓게·소품 확률 높게, 주택 웜그레이 3톤.
        private static readonly DistrictProfile VillatownProfile = new DistrictProfile
        {
            minFloors = 1, maxFloors = 2, minWidth = 6.5f, maxWidth = 7.5f, propChance = 0.85f,
            tones = new[]
            {
                new Color(0.42f, 0.36f, 0.32f),
                new Color(0.36f, 0.34f, 0.36f),
                new Color(0.30f, 0.27f, 0.26f),
            },
            signs = false,
        };

        // 먹자골목: 상가(2~3층) + 간판 — 폭 들쭉날쭉, 어두운 상가 3톤 위에 간판이 튄다.
        private static readonly DistrictProfile FoodalleyProfile = new DistrictProfile
        {
            minFloors = 2, maxFloors = 3, minWidth = 5.5f, maxWidth = 7f, propChance = 0.6f,
            tones = new[]
            {
                new Color(0.26f, 0.27f, 0.33f),
                new Color(0.33f, 0.30f, 0.36f),
                new Color(0.22f, 0.24f, 0.28f),
            },
            signs = true,
        };

        // 미지정 구역(구 지문 호환): S-002 원 규약 — 층 1~3 · 폭 6~7 · 그레이 3톤 · 소품 0.6.
        private static readonly DistrictProfile DefaultProfile = new DistrictProfile
        {
            minFloors = 1, maxFloors = 3, minWidth = 6f, maxWidth = 7f, propChance = 0.6f,
            tones = new[]
            {
                new Color(0.30f, 0.30f, 0.34f),
                new Color(0.38f, 0.36f, 0.40f),
                new Color(0.24f, 0.26f, 0.30f),
            },
            signs = false,
        };

        // 간판 2색 교대(팔레트 시안·앰버) — 톤 인덱스에서 유도(추가 추첨 없음 = RNG 스트림 안정).
        private static readonly Color[] SignColors =
        {
            new Color(0.207f, 0.878f, 0.784f), // #35e0c8
            new Color(1.0f, 0.623f, 0.270f),   // #ff9f45
        };

        private static DistrictProfile GetProfile(string districtId)
        {
            switch (districtId)
            {
                case DeliveryOrderSO.DISTRICT_VILLATOWN: return VillatownProfile;
                case DeliveryOrderSO.DISTRICT_FOODALLEY: return FoodalleyProfile;
                default: return DefaultProfile;
            }
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // Additive 재진입·중복 OnEnable 대비: 생성 전 항상 이전 생성물을 지운다(멱등).
        private void OnEnable() => Generate();

        /// <summary>
        /// 이전 생성물을 정리하고 시드 고정 배치를 다시 만든다. 같은 구역이면 결과가 동일하다.
        /// 구역 = GameState.currentDistrict(런타임 우선) → _districtId(단독 Play 폴백).
        /// </summary>
        public void Generate()
        {
            ClearGenerated();

            Transform root = new GameObject(GENERATED_ROOT).transform;
            root.SetParent(transform, false);

            string districtId = _districtId;
            if (_gameState != null && !string.IsNullOrEmpty(_gameState.currentDistrict))
                districtId = _gameState.currentDistrict;

            DistrictProfile profile = GetProfile(districtId);
            var rng = new System.Random(Fnv1a(districtId));

            if (_buildingSlots != null)
            {
                for (int i = 0; i < _buildingSlots.Length; i++)
                {
                    Transform slot = _buildingSlots[i];

                    // 슬롯당 결정론 추첨(추첨 순서 고정 = 스트림 안정) — slot이 null이어도 소비한다.
                    int floors = rng.Next(profile.minFloors, profile.maxFloors + 1);
                    int tone = rng.Next(0, profile.tones.Length);
                    float width = profile.minWidth + (float)rng.NextDouble() * (profile.maxWidth - profile.minWidth);

                    if (slot == null) continue;
                    BuildBuilding(root, i + 1, slot, floors, tone, width, profile);
                }
            }

            if (_propSlots != null)
            {
                for (int i = 0; i < _propSlots.Length; i++)
                {
                    Transform slot = _propSlots[i];

                    bool place = rng.NextDouble() < profile.propChance;
                    int kind = rng.Next(0, PROP_KINDS);

                    if (slot == null || !place) continue;
                    BuildProp(root, i + 1, slot, kind);
                }
            }
        }

        // ── 그레이박스 조립 ──────────────────────────────────

        private void BuildBuilding(Transform root, int slotNo, Transform slot, int floors, int tone, float width,
            DistrictProfile profile)
        {
            // 이름에 결정론 값(floors·tone)을 새겨 스냅샷 비교의 지문으로 쓴다.
            GameObject building = new GameObject($"Building_{slotNo:00}_f{floors}_t{tone}");
            building.transform.SetParent(root, false);
            building.transform.SetPositionAndRotation(slot.position, slot.rotation);

            bool builtFromPrefab = false;
            if (_buildingPrefabPool != null && _buildingPrefabPool.Length > 0)
            {
                GameObject prefab = _buildingPrefabPool[tone % _buildingPrefabPool.Length];
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, building.transform);
                    NormalizeBuilding(instance, floors, slot.position.z);
                    builtFromPrefab = true;
                }
            }

            // 전면 면을 세계 Z=+3.0에 맞추기 위한 로컬 Z 중심 (슬롯 회전=identity 전제 · 건물 라인은 직선).
            float centerZLocal = (BUILDING_FRONT_Z + BUILDING_DEPTH * 0.5f) - slot.position.z;

            if (!builtFromPrefab)
            {
                for (int f = 0; f < floors; f++)
                {
                    GameObject cube = MakeCube(building.transform, $"Floor_{f}",
                        new Vector3(0f, f * FLOOR_HEIGHT + FLOOR_HEIGHT * 0.5f, centerZLocal),
                        new Vector3(width, FLOOR_HEIGHT, BUILDING_DEPTH));
                    Tint(cube, profile.tones[tone]);
                }
            }

            // 먹자골목 간판(S-035): 1층 위 전면(−Z 길 쪽)에 밝은 스트립 — 색은 톤에서 유도(추가 추첨 없음).
            // 프리팹 건물에도 얹는다 — 전면 평면은 두 경로 모두 세계 Z=+3.0 정렬(NormalizeBuilding·centerZLocal 동일 규약).
            if (profile.signs)
            {
                GameObject sign = MakeCube(building.transform, "Sign",
                    new Vector3(0f, FLOOR_HEIGHT + 0.45f, centerZLocal - BUILDING_DEPTH * 0.5f - 0.06f),
                    new Vector3(width * 0.62f, 0.7f, 0.1f));
                Tint(sign, SignColors[tone % SignColors.Length]);
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
