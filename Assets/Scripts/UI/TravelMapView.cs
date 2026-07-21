using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 이동(미니맵) 씬 노드 버튼(View) — S-006. 클릭하면 노드별 소모 시간을 시계에 가산하고
    /// District 전이를 요청만 한다 — 로직 없음(시간 가산=DayNight, 전이=SceneFlow 소유).
    /// 버튼 하나당 하나씩 붙이고, 원거리 여부만 인스펙터에서 지정한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TravelMapView : MonoBehaviour
    {
        [SerializeField] private TuningConfigSO _tuning;
        [Tooltip("켜면 원거리 노드(travelFarMinutes), 끄면 근거리(travelNearMinutes).")]
        [SerializeField] private bool _isFarNode;
        [Tooltip("이 노드의 구역 라벨 — 주문 SO의 district와 일치해야 스폰이 맞물린다 (S-015).")]
        [SerializeField] private string _district;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Depart);
        }

        private void Depart()
        {
            if (WorldSceneFlowManager.Instance == null || WorldDayNightManager.Instance == null)
            {
                Debug.LogWarning("[TravelMapView] World 매니저 없음 — 씬 단독 Play인가?");
                return;
            }
            if (WorldSceneFlowManager.Instance.IsTransitioning) return;

            float cost = _isFarNode ? _tuning.travelFarMinutes : _tuning.travelNearMinutes;
            WorldDayNightManager.Instance.AdvanceMinutes(cost);
            WorldDeliveryManager.Instance.SetDestination(_district);
            WorldSceneFlowManager.Instance.Request(GameScene.District);
        }
    }
}
