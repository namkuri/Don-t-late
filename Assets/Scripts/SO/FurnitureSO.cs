using UnityEngine;

namespace DontLate
{
    /// <summary>가구 정의 (S-019 ④ 하우징). 그레이박스 = 색 박스, 실모델은 프리팹 스왑 계약.</summary>
    [CreateAssetMenu(menuName = "DontLate/Furniture")]
    public class FurnitureSO : ScriptableObject
    {
        public string furnitureId;
        public string displayName;
        public int price;
        [Tooltip("그레이박스 박스 치수 (실프리팹이 오면 무시).")]
        public Vector3 size = new Vector3(1f, 1f, 1f);
        public Color color = Color.white;
        [Tooltip("실모델 프리팹 — 비면 색 박스 폴백 (스왑 계약).")]
        public GameObject prefab;
    }
}
