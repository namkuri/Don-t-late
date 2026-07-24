using UnityEngine;

namespace DontLate
{
    /// <summary>배송 건 1개의 정적 데이터.</summary>
    [CreateAssetMenu(menuName = "DontLate/DeliveryOrder")]
    public class DeliveryOrderSO : ScriptableObject
    {
        // 구역 라벨 정본 (S-035 · D-064) — district 문자열이 스폰 계약이라 리터럴 산개 금지.
        // 잔여 2구역(아파트단지·언덕주택가)은 S-036 지도 "준비 중" 잠금 전용 — 활성화 시 여기 승격.
        public const string DISTRICT_VILLATOWN = "빌라촌";
        public const string DISTRICT_FOODALLEY = "먹자골목";
        public const string DISTRICT_APARTMENT = "아파트단지"; // S-038 (D-067)
        public const string DISTRICT_HILLSIDE = "언덕주택가"; // S-049 (D-064)

        public int orderId;
        public string address;
        /// <summary>배송 구역 라벨 (S-015) — 폰 안내·구역 도착 시 스폰 매칭 키. 이동맵 노드 라벨과 일치해야 한다.</summary>
        public string district;
        public int floor;
        /// <summary>상자 무게(kg) — 스태미나 가중(S-019 ③)·연출용.</summary>
        public float weight = 3f;
        /// <summary>마감 시각 (하루 기준 분, 0~1440).</summary>
        public float deadlineMinuteOfDay;
        public int reward;
        [TextArea] public string memo;
    }
}
