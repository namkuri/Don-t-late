using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 배치된 가구 비주얼의 마커 (S-031 ①) — 클릭 재배치 판정용.
    /// HomeFurniturePlacer가 스폰 시 부착하고, 클릭 시 이 데이터로 placedFurniture 항목을 되찾는다.
    ///
    /// S-273 — 값을 **직렬화 필드**로 들고 있는다. 종전엔 자동 구현 프로퍼티라 런타임에만 살아 있었고,
    /// 빌더가 씬 고정 가구(침대)에 `Bind`를 걸어도 **씬 저장에서 통째로 날아갔다**(실측: FurnitureId 빈 값).
    /// </summary>
    public class PlacedFurnitureVisual : MonoBehaviour
    {
        [SerializeField] private string _furnitureId;
        [SerializeField] private Vector3 _placedPosition;
        [SerializeField] private float _rotationY;

        public string FurnitureId => _furnitureId;
        public Vector3 PlacedPosition => _placedPosition;
        public float RotationY => _rotationY;

        public void Bind(string furnitureId, Vector3 placedPosition, float rotationY)
        {
            _furnitureId = furnitureId;
            _placedPosition = placedPosition;
            _rotationY = rotationY;
        }
    }
}
