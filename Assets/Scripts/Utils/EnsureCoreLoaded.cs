using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate
{
    /// <summary>
    /// 콘텐츠 씬 단독 Play 지원 — S-013. 콘텐츠 씬을 직접 열고 Play 했을 때 Core(매니저·폰·HUD)가
    /// 없으면 Additive로 불러온다. CoreBootstrap이 사후 로드를 감지해 Main으로 끌고 가지 않는다.
    /// 정상 흐름(Core에서 Play)에서는 아무것도 하지 않는다.
    /// </summary>
    public class EnsureCoreLoaded : MonoBehaviour
    {
        private void Awake()
        {
            if (WorldSceneFlowManager.Instance != null) return;
            if (SceneManager.GetSceneByName("Core").isLoaded) return;
            Debug.Log("[EnsureCoreLoaded] 씬 단독 Play 감지 — Core를 Additive 로드한다.");
            SceneManager.LoadScene("Core", LoadSceneMode.Additive);
        }
    }
}
