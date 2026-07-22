using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 배치된 가구 비주얼의 마커 (S-031 ①) — 클릭 재배치 판정용.
    /// HomeFurniturePlacer가 스폰 시 부착하고, 클릭 시 이 데이터로 placedFurniture 항목을 되찾는다.
    /// </summary>
    public class PlacedFurnitureVisual : MonoBehaviour
    {
        public string FurnitureId { get; private set; }
        public Vector3 PlacedPosition { get; private set; }
        public float RotationY { get; private set; }

        public void Bind(string furnitureId, Vector3 placedPosition, float rotationY)
        {
            FurnitureId = furnitureId;
            PlacedPosition = placedPosition;
            RotationY = rotationY;
        }
    }
}
