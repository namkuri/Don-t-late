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
        [Tooltip("실프리팹 배율 (S-173 ② — 모델마다 제작 스케일이 달라 방 안에서 크기가 안 맞는다). 1 = 원본.")]
        public float prefabScale = 1f;
        [Tooltip("실프리팹 회전 보정(로컬 오일러). 모델이 누워서 나오는 것을 세운다 — 의자 X 90 (S-201). 0 = 원본.")]
        public Vector3 prefabRotation;
        [Tooltip("벽 설치 허용 (S-031 ⑤ — TV).")]
        public bool wallMountable;
    }
}
