using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontLate
{
    /// <summary>
    /// Home 가구 배치 (S-019 ④ → S-030 ③ → S-031 개편). 세션 데이터(GameState.placedFurniture)로
    /// 배치분을 재생성하고, 폰 가구앱의 배치 대기(PendingPlacementId)를 처리한다.
    /// 블루프린트: 클릭=확정 · R=45° 회전 · ESC=취소 · **0.5u 그리드 스냅**(S-031 ②).
    /// 배치된 가구 클릭 = 집어 들어 재배치(S-031 ①). TV는 벽에도 붙는다(S-031 ⑤).
    /// 침대는 세션당 1회 시드되는 기본 가구다(S-031 ③ — 무대 고정물에서 강등).
    /// </summary>
    public class HomeFurniturePlacer : MonoBehaviour
    {
        private const float GRID = 0.25f; // S-031 ② 스냅 간격 · S-122 ③ 1/2로 촘촘하게 (기존 좌표는 전부 0.5의 배수 = 0.25 격자에도 그대로 얹힌다)

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private FurnitureSO[] _catalog;

        private static readonly Color GhostColor = new Color(0.208f, 0.878f, 0.784f, 0.45f); // 시안 반투명

        // 이 배치기가 만든 배치물 대장 (S-122 ④ 낙하 판정용) — FindObjectsOfType 금지 규약.
        private readonly List<PlacedFurnitureVisual> _visuals = new List<PlacedFurnitureVisual>();

        private GameObject _ghost;
        private string _ghostId;
        private float _ghostYaw;
        private bool _ghostOnWall;

        private void Start()
        {
            // S-031 ③: 침대 시드 — 세션 최초 1회만 (이후엔 플레이어가 옮긴 위치가 정본).
            if (!_gameState.bedSeeded)
            {
                _gameState.bedSeeded = true;
                _gameState.placedFurniture.Add(new PlacedFurniture
                {
                    furnitureId = "fur_bed",
                    position = new Vector3(-2.5f, 0f, 2f),
                    rotationY = 0f,
                });
            }

            foreach (PlacedFurniture placed in _gameState.placedFurniture)
                SpawnVisual(placed.furnitureId, placed.position, placed.rotationY);
        }

        private void OnDisable() => ClearGhost(); // 씬 이탈 시 블루프린트 잔재 방지

        private void Update()
        {
            Mouse mouse = Mouse.current;
            Camera camera = Camera.main;
            if (mouse == null || camera == null) return;

            if (string.IsNullOrEmpty(PhoneView.PendingPlacementId))
            {
                ClearGhost();
                HandleRepick(mouse, camera); // S-031 ① — 배치된 가구 클릭 = 집기
                return;
            }

            Keyboard keyboard = Keyboard.current;

            // ESC = 취소 — 블루프린트 삭제 + 배치 대기 해제 (가구는 인벤토리에 남는다).
            // 폰이 열려 있으면 ESC는 폰 닫기 몫 (S-032 ③ 충돌 회피).
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !PhoneView.IsOpen)
            {
                PhoneView.PendingPlacementId = null;
                ClearGhost();
                WorldAudioManager.Instance?.PlayUiTickSfx(); // AU-010
                Debug.Log("[하우징] 배치 취소 (ESC)");
                return;
            }

            // R = 45° 회전 (벽 부착 중엔 벽 법선이 방향을 소유).
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame && !_ghostOnWall)
            {
                _ghostYaw = Mathf.Repeat(_ghostYaw + 45f, 360f);
                WorldAudioManager.Instance?.PlayUiTickSfx(); // AU-010
            }

            FurnitureSO so = Find(PhoneView.PendingPlacementId);
            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());

            // S-031 ⑤: 벽 설치 가능 가구는 벽 히트를 우선 시도.
            Vector3 pos;
            _ghostOnWall = false;
            if (so != null && so.wallMountable && TryWallPoint(ray, so, out Vector3 wallPos, out float wallYaw))
            {
                pos = wallPos;
                _ghostYaw = wallYaw;
                _ghostOnWall = true;
            }
            else if (TrySupportPoint(ray, so, out Vector3 supportPos)) // S-122 ② — 기존 가구 위에 올리기
            {
                pos = supportPos;
            }
            else
            {
                Plane floor = new Plane(Vector3.up, Vector3.zero);
                if (!floor.Raycast(ray, out float enter)) return;
                pos = ray.GetPoint(enter);
                pos.x = Mathf.Clamp(pos.x, -3.5f, 3.5f);
                pos.z = Mathf.Clamp(pos.z, -2.4f, 2.4f);
                pos.y = 0f;
                pos.x = Mathf.Round(pos.x / GRID) * GRID; // S-031 ② 그리드 스냅
                pos.z = Mathf.Round(pos.z / GRID) * GRID;
            }

            // 블루프린트 갱신 (대기 id가 바뀌면 재생성).
            if (_ghost == null || _ghostId != PhoneView.PendingPlacementId)
            {
                ClearGhost();
                _ghostId = PhoneView.PendingPlacementId;
                _ghost = BuildGhost(_ghostId);
            }
            _ghost.transform.SetPositionAndRotation(
                pos + Vector3.up * 0.001f, Quaternion.Euler(0f, _ghostYaw, 0f));

            // 클릭 = 확정 (폰이 열려 있으면 폰 조작에 양보).
            if (!mouse.leftButton.wasPressedThisFrame || PhoneView.IsOpen) return;

            string id = PhoneView.PendingPlacementId;
            PhoneView.PendingPlacementId = null;
            _gameState.ownedFurnitureIds.Remove(id);
            _gameState.placedFurniture.Add(new PlacedFurniture { furnitureId = id, position = pos, rotationY = _ghostYaw });
            SpawnVisual(id, pos, _ghostYaw);
            ClearGhost();
            WorldAudioManager.Instance?.PlayFurniturePlaceSfx(); // AU-010
            Debug.Log("[하우징] " + id + " 배치 — " + pos + " yaw " + _ghostYaw);
        }

        // ── 집기(좌클릭 · S-031 ①) / 철거(우클릭 · S-122 ④) ──
        // 둘 다 배치물을 인벤토리로 회수한다. 차이는 집기만 배치 모드로 재진입한다는 것뿐.
        private void HandleRepick(Mouse mouse, Camera camera)
        {
            bool pick = mouse.leftButton.wasPressedThisFrame;
            bool demolish = mouse.rightButton.wasPressedThisFrame;
            // 철거는 파괴적 조작이라 UI 위 클릭이 뒤 가구를 치우지 않게 막는다 (PlayerStatusManager 선례).
            bool overUI = UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if ((!pick && !demolish) || PhoneView.IsOpen || overUI) return;

            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;
            PlacedFurnitureVisual visual = hit.collider.GetComponentInParent<PlacedFurnitureVisual>();
            if (visual == null) return;

            int index = FindPlacedIndex(visual);
            if (index < 0) return;

            PlacedFurniture placed = _gameState.placedFurniture[index];
            bool hasFootprint = TryWorldBounds(visual.transform, out Bounds footprint); // 파괴 전 실측

            _gameState.placedFurniture.RemoveAt(index);
            _gameState.ownedFurnitureIds.Add(placed.furnitureId); // 집기·철거 모두 인벤토리로 회수
            _visuals.Remove(visual);
            Destroy(visual.gameObject);

            // 지지대가 사라졌으니 그 상단면에 얹혀 있던 가구는 지지대가 서 있던 높이로 내려앉는다.
            if (hasFootprint) DropStacked(footprint, placed.position.y);

            if (pick)
            {
                PhoneView.PendingPlacementId = placed.furnitureId;
                _ghostYaw = placed.rotationY; // 집을 때 각도 유지
            }
            WorldAudioManager.Instance?.PlayUiTickSfx(); // AU-010
            Debug.Log("[하우징] " + placed.furnitureId + (pick ? " 집음 — 재배치 모드" : " 철거 — 인벤토리 회수"));
        }

        private int FindPlacedIndex(PlacedFurnitureVisual visual)
        {
            for (int i = 0; i < _gameState.placedFurniture.Count; i++)
            {
                PlacedFurniture placed = _gameState.placedFurniture[i];
                if (placed.furnitureId != visual.FurnitureId) continue;
                if ((placed.position - visual.PlacedPosition).sqrMagnitude > 0.0001f) continue; // S-122 ② 스택 겹침 구분
                return i;
            }
            return -1;
        }

        // ── 낙하 (S-122 ④) ─────────────────────────────────
        // 배치물은 Rigidbody가 없는 정적 오브젝트라(프리팹·SpawnVisual 모두 미부착) 물리 낙하가 아니라
        // 좌표 재계산으로 내려앉힌다. 착지면 = 치운 지지대가 서 있던 높이.
        private void DropStacked(Bounds supportFootprint, float landingY)
        {
            float supportTopY = supportFootprint.max.y;
            for (int i = _visuals.Count - 1; i >= 0; i--)
            {
                PlacedFurnitureVisual visual = _visuals[i];
                if (visual == null) { _visuals.RemoveAt(i); continue; }
                // 치운 물건의 상단면에 실제로 얹혀 있던 것만 내려앉는다 — "y가 더 높다"로 잡으면
                // 벽걸이 TV·시계(y 1.0~1.6)까지 바닥으로 떨어진다.
                if (Mathf.Abs(visual.PlacedPosition.y - supportTopY) > 0.02f) continue;
                if (!TryWorldBounds(visual.transform, out Bounds bounds)) continue;
                if (bounds.max.x <= supportFootprint.min.x || bounds.min.x >= supportFootprint.max.x) continue;
                if (bounds.max.z <= supportFootprint.min.z || bounds.min.z >= supportFootprint.max.z) continue;

                int index = FindPlacedIndex(visual);
                if (index < 0) continue;
                PlacedFurniture entry = _gameState.placedFurniture[index];
                Vector3 landed = new Vector3(entry.position.x, landingY, entry.position.z);
                _gameState.placedFurniture[index] = new PlacedFurniture
                {
                    furnitureId = entry.furnitureId,
                    position = landed,
                    rotationY = entry.rotationY,
                };
                _visuals.RemoveAt(i);
                Destroy(visual.gameObject);
                SpawnVisual(entry.furnitureId, landed, entry.rotationY); // 새 높이로 재생성 (대장 자동 재등록)
                Debug.Log("[하우징] " + entry.furnitureId + " 낙하 — y "
                    + entry.position.y.ToString("0.00") + " → " + landingY.ToString("0.00"));
            }
        }

        // ── 벽 부착 (S-031 ⑤) — 벽 콜라이더 히트 → 벽면 중심 배치 + 법선 방향 ──
        private bool TryWallPoint(Ray ray, FurnitureSO so, out Vector3 position, out float yaw)
        {
            position = default;
            yaw = 0f;
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return false;
            if (!hit.collider.name.Contains("Wall")) return false;
            if (Mathf.Abs(hit.normal.y) > 0.3f) return false; // 천장·바닥면 제외

            // 부착점: 벽면에서 법선 방향으로 두께 절반 — position 규약은 "바닥 기준"이라 y를 절반 낮춰 저장.
            Vector3 center = hit.point + hit.normal * (so.size.z * 0.5f + 0.01f);
            center.y = Mathf.Max(so.size.y * 0.5f + 0.4f, Mathf.Round(hit.point.y / GRID) * GRID); // 그리드 스냅(높이)
            position = center - Vector3.up * (so.size.y * 0.5f);
            position.x = Mathf.Round(position.x / GRID) * GRID;
            yaw = Quaternion.LookRotation(hit.normal).eulerAngles.y;
            return true;
        }

        // ── 가구 상단 얹기 (S-122 ②) — 배치물 콜라이더를 맞으면 그 결합 바운즈 상단면이 새 바닥이 된다.
        // 카메라가 거의 수평(하향 8°)이라 상판만 허용하면 사실상 못 올린다 → 법선을 가리지 않고
        // "가구를 맞추면 위에 올린다"로 판정해 조준 난이도를 낮춘다.
        private bool TrySupportPoint(Ray ray, FurnitureSO so, out Vector3 position)
        {
            position = default;
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return false;
            PlacedFurnitureVisual support = hit.collider.GetComponentInParent<PlacedFurnitureVisual>();
            if (support == null) return false;
            if (!TryWorldBounds(support.transform, out Bounds top)) return false;

            Vector3 half = (so != null ? so.size : Vector3.one * 0.8f) * 0.5f;
            position = new Vector3(
                Mathf.Round(hit.point.x / GRID) * GRID,
                top.max.y,                                  // 지지대 상단면 = 새 가구의 바닥
                Mathf.Round(hit.point.z / GRID) * GRID);
            // 상판을 벗어나 공중에 뜨지 않게 지지면 안으로 당긴다 (지지면보다 큰 가구는 중앙 정렬).
            position.x = half.x * 2f >= top.size.x ? top.center.x
                : Mathf.Clamp(position.x, top.min.x + half.x, top.max.x - half.x);
            position.z = half.z * 2f >= top.size.z ? top.center.z
                : Mathf.Clamp(position.z, top.min.z + half.z, top.max.z - half.z);
            return true;
        }

        /// <summary>배치물의 월드 결합 바운즈 — 콜라이더 우선(회전 반영 AABB), 없으면 렌더러.</summary>
        private static bool TryWorldBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool has = false;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled) continue;
                if (has) bounds.Encapsulate(collider.bounds); else { bounds = collider.bounds; has = true; }
            }
            if (has) return true;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (has) bounds.Encapsulate(renderer.bounds); else { bounds = renderer.bounds; has = true; }
            }
            return has;
        }

        private FurnitureSO Find(string furnitureId)
        {
            if (_catalog != null)
                foreach (FurnitureSO item in _catalog)
                    if (item != null && item.furnitureId == furnitureId) return item;
            return null;
        }

        private void SpawnVisual(string furnitureId, Vector3 position, float rotationY)
        {
            FurnitureSO so = Find(furnitureId);
            Quaternion rotation = Quaternion.Euler(0f, rotationY, 0f);
            GameObject visual;

            if (so != null && so.prefab != null)
            {
                visual = Instantiate(so.prefab, position, rotation);
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube); // 콜라이더 보존 — 클릭 재배치 판정용 (S-031 ①)
                Vector3 size = so != null ? so.size : Vector3.one * 0.8f;
                visual.transform.SetPositionAndRotation(position + Vector3.up * (size.y * 0.5f), rotation);
                visual.transform.localScale = size;
                visual.GetComponent<Renderer>().material.color = so != null ? so.color : Color.gray;
            }

            visual.name = "Furniture_" + furnitureId;
            if (visual.GetComponentInChildren<Collider>() == null)
                visual.AddComponent<BoxCollider>(); // 프리팹에 콜라이더가 없으면 클릭 판정용 부여
            PlacedFurnitureVisual marker = visual.AddComponent<PlacedFurnitureVisual>();
            marker.Bind(furnitureId, position, rotationY);
            _visuals.Add(marker); // S-122 ④ — 낙하 판정 대장
        }

        // ── 블루프린트 (반투명 시안 고스트) ──────────────────
        private GameObject BuildGhost(string furnitureId)
        {
            FurnitureSO so = Find(furnitureId);
            GameObject ghost;

            if (so != null && so.prefab != null)
            {
                ghost = Instantiate(so.prefab);
                foreach (Collider collider in ghost.GetComponentsInChildren<Collider>())
                    collider.enabled = false;
            }
            else
            {
                ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(ghost.GetComponent<Collider>());
                Vector3 size = so != null ? so.size : Vector3.one * 0.8f;
                ghost.transform.localScale = size;
                // 큐브 폴백은 피벗이 중심이라 바닥 기준으로 올린다.
                GameObject root = new GameObject("Ghost_" + furnitureId);
                ghost.transform.SetParent(root.transform, false);
                ghost.transform.localPosition = Vector3.up * (size.y * 0.5f);
                ghost = root;
            }

            ghost.name = "Ghost_" + furnitureId;
            foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>())
            {
                Material material = renderer.material; // 인스턴스 — 공유 머티리얼 무오염
                material.color = GhostColor;
                // URP Lit 투명 전환 (고스트 반투명).
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            return ghost;
        }

        private void ClearGhost()
        {
            if (_ghost == null) return;
            Destroy(_ghost);
            _ghost = null;
            _ghostId = null;
            _ghostOnWall = false;
        }
    }
}
