using UnityEngine;

namespace DontLate
{
    /// <summary>배송 건 1개의 정적 데이터.</summary>
    [CreateAssetMenu(menuName = "DontLate/DeliveryOrder")]
    public class DeliveryOrderSO : ScriptableObject
    {
        public int orderId;
        public string address;
        /// <summary>배송 구역 라벨 (S-015) — 폰 안내·구역 도착 시 스폰 매칭 키. 이동맵 노드 라벨과 일치해야 한다.</summary>
        public string district;
        public int floor;
        /// <summary>마감 시각 (하루 기준 분, 0~1440).</summary>
        public float deadlineMinuteOfDay;
        public int reward;
        [TextArea] public string memo;
    }
}
