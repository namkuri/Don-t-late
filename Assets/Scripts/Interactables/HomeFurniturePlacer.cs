using UnityEngine;
using UnityEngine.InputSystem;

namespace DontLate
{
    /// <summary>
    /// Home 가구 배치 (S-019 ④). 세션 데이터(GameState.placedFurniture)로 배치분을 재생성하고,
    /// 폰 가구앱이 걸어둔 배치 대기(PendingPlacementId)를 바닥 클릭으로 소비한다.
    /// </summary>
    public class HomeFurniturePlacer : MonoBehaviour
    {
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private FurnitureSO[] _catalog;

        private void Start()
        {
            foreach (PlacedFurniture placed in _gameState.placedFurniture)
                SpawnVisual(placed.furnitureId, placed.position);
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(PhoneView.PendingPlacementId)) return;
            Mouse mouse = Mouse.current;
            Camera camera = Camera.main;
            if (mouse == null || camera == null || !mouse.leftButton.wasPressedThisFrame) return;

            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            Plane floor = new Plane(Vector3.up, Vector3.zero);
            if (!floor.Raycast(ray, out float enter)) return;

            Vector3 pos = ray.GetPoint(enter);
            pos.x = Mathf.Clamp(pos.x, -3.5f, 3.5f); // 방 안으로
            pos.z = Mathf.Clamp(pos.z, -2.4f, 2.4f);
            pos.y = 0f;

            string id = PhoneView.PendingPlacementId;
            PhoneView.PendingPlacementId = null;
            _gameState.ownedFurnitureIds.Remove(id);
            _gameState.placedFurniture.Add(new PlacedFurniture { furnitureId = id, position = pos });
            SpawnVisual(id, pos);
            Debug.Log("[하우징] " + id + " 배치 — " + pos);
        }

        private void SpawnVisual(string furnitureId, Vector3 position)
        {
            FurnitureSO so = null;
            if (_catalog != null)
                foreach (FurnitureSO item in _catalog)
                    if (item != null && item.furnitureId == furnitureId) { so = item; break; }

            if (so != null && so.prefab != null)
            {
                Instantiate(so.prefab, position, Quaternion.identity);
                return;
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Furniture_" + furnitureId;
            Vector3 size = so != null ? so.size : Vector3.one * 0.8f;
            cube.transform.position = position + Vector3.up * (size.y * 0.5f);
            cube.transform.localScale = size;
            Object.Destroy(cube.GetComponent<Collider>());
            cube.GetComponent<Renderer>().material.color = so != null ? so.color : Color.gray;
        }
    }
}
