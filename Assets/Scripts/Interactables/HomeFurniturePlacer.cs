using UnityEngine;
using UnityEngine.InputSystem;

namespace DontLate
{
    /// <summary>
    /// Home 가구 배치 (S-019 ④ → S-030 ③ 개편). 세션 데이터(GameState.placedFurniture)로 배치분을 재생성하고,
    /// 폰 가구앱이 걸어둔 배치 대기(PendingPlacementId)를 처리한다:
    /// 마우스 위치에 반투명 블루프린트(고스트)를 띄우고 — 클릭=확정 · R=45° 회전 · ESC=취소.
    /// </summary>
    public class HomeFurniturePlacer : MonoBehaviour
    {
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private FurnitureSO[] _catalog;

        private static readonly Color GhostColor = new Color(0.208f, 0.878f, 0.784f, 0.45f); // 시안 반투명

        private GameObject _ghost;
        private string _ghostId;
        private float _ghostYaw;

        private void Start()
        {
            foreach (PlacedFurniture placed in _gameState.placedFurniture)
                SpawnVisual(placed.furnitureId, placed.position, placed.rotationY);
        }

        private void OnDisable() => ClearGhost(); // 씬 이탈 시 블루프린트 잔재 방지

        private void Update()
        {
            if (string.IsNullOrEmpty(PhoneView.PendingPlacementId)) { ClearGhost(); return; }

            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            Camera camera = Camera.main;
            if (mouse == null || camera == null) return;

            // ESC = 취소 — 블루프린트 삭제 + 배치 대기 해제 (가구는 인벤토리에 남는다).
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                PhoneView.PendingPlacementId = null;
                ClearGhost();
                Debug.Log("[하우징] 배치 취소 (ESC)");
                return;
            }

            // R = 45° 회전.
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                _ghostYaw = Mathf.Repeat(_ghostYaw + 45f, 360f);

            // 마우스 → 바닥 평면 투영 (방 안으로 클램프).
            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            Plane floor = new Plane(Vector3.up, Vector3.zero);
            if (!floor.Raycast(ray, out float enter)) return;
            Vector3 pos = ray.GetPoint(enter);
            pos.x = Mathf.Clamp(pos.x, -3.5f, 3.5f);
            pos.z = Mathf.Clamp(pos.z, -2.4f, 2.4f);
            pos.y = 0f;

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
            Debug.Log("[하우징] " + id + " 배치 — " + pos + " yaw " + _ghostYaw);
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

            if (so != null && so.prefab != null)
            {
                Instantiate(so.prefab, position, rotation);
                return;
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Furniture_" + furnitureId;
            Vector3 size = so != null ? so.size : Vector3.one * 0.8f;
            cube.transform.SetPositionAndRotation(position + Vector3.up * (size.y * 0.5f), rotation);
            cube.transform.localScale = size;
            Object.Destroy(cube.GetComponent<Collider>());
            cube.GetComponent<Renderer>().material.color = so != null ? so.color : Color.gray;
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
                // 큐브 폴백은 피벗이 중심이라 바닥 기준으로 올린다 — 자식 없이 단일 메시.
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
        }
    }
}
