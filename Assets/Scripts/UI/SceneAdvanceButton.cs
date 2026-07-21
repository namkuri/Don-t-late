using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 씬 흐름 골격 버튼(View). 클릭하면 지정한 콘텐츠 씬으로 전이를 요청만 한다 — 로직 없음.
    /// 전이 허용 여부·연출 판단은 WorldSceneFlowManager 몫이다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SceneAdvanceButton : MonoBehaviour
    {
        [SerializeField] private GameScene _target;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Advance);
        }

        private void Advance()
        {
            if (WorldSceneFlowManager.Instance == null)
            {
                Debug.LogWarning("[SceneAdvanceButton] WorldSceneFlowManager 없음 — 씬 단독 Play인가?");
                return;
            }
            WorldSceneFlowManager.Instance.Request(_target);
        }
    }
}
