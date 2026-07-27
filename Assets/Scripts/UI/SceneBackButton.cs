using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>좌상단 뒤로가기 버튼 (S-062 ⑤) — 직전 씬으로 복귀. Travel 등 막다른 씬 탈출용.</summary>
    [RequireComponent(typeof(Button))]
    public class SceneBackButton : MonoBehaviour
    {
        private void Awake() => GetComponent<Button>().onClick.AddListener(OnPressed);

        private void OnPressed()
        {
            if (WorldSceneFlowManager.Instance == null || WorldSceneFlowManager.Instance.IsTransitioning) return;
            WorldSceneFlowManager.Instance.GoBack();
        }
    }
}
