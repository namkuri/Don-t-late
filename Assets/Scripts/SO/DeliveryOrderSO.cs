using UnityEngine;

namespace DontLate
{
    /// <summary>배송 건 1개의 정적 데이터.</summary>
    [CreateAssetMenu(menuName = "DontLate/DeliveryOrder")]
    public class DeliveryOrderSO : ScriptableObject
    {
        public int orderId;
        public string address;
        public int floor;
        /// <summary>마감 시각 (하루 기준 분, 0~1440).</summary>
        public float deadlineMinuteOfDay;
        public int reward;
        [TextArea] public string memo;
    }
}
