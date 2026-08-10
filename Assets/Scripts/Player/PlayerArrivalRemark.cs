using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate
{
    /// <summary>
    /// S-268 — 구역에 **처음 들어선 순간** 주인공이 한마디 한다(남규님 지시).
    ///
    /// 이벤트를 구독하지 않는다: 플레이어는 씬마다 새로 태어나므로 `Start`가 곧 "도착"이다.
    /// 같은 구역을 다시 와도 반복하지 않게 방문 기록은 <see cref="GameStateSO.greetedScenes"/>에
    /// 남긴다(하루 단위가 아니라 세션 단위 — 첫인상은 한 번뿐이다).
    ///
    /// 말풍선은 <see cref="SpeechBubble"/>이 머리 위에 띄운다 — `AmbientRemarkSpot`(S-123 ①)과 같은 창구.
    /// </summary>
    public class PlayerArrivalRemark : MonoBehaviour
    {
        [SerializeField] private GameStateSO _gameState;
        [Tooltip("도착 직후 연출·대사와 겹치지 않게 잠깐 두고 띄운다.")]
        [SerializeField] private float _delay = 1.4f;

        private static readonly System.Collections.Generic.Dictionary<string, string> REMARKS =
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["Village"] = "여기가 빌라촌이구나.",
                ["FoodStreet"] = "먹자골목이네. 맛있겠다...",
                ["Apartment"] = "아파트단지라 층수까지 봐야겠네.",
                ["Hillside"] = "언덕주택가... 다리 좀 쓰겠는데.",
                ["Camp"] = "물류캠프. 오늘도 시작이다.",
            };

        private float _timer;
        private bool _pending;

        // S-268 — 밖에서 요청하는 독백(트럭 앞 망설임 등). 월드 오브젝트가 플레이어를 직접
        // 찾지 않도록 이벤트로 받는다.
        private void OnEnable() => WorldEvents.PlayerRemarked += OnRemarkRequested;
        private void OnDisable() => WorldEvents.PlayerRemarked -= OnRemarkRequested;

        private void OnRemarkRequested(string line)
        {
            if (!string.IsNullOrEmpty(line)) SpeechBubble.ShowOn(gameObject, line, 3f);
        }

        private void Start()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (!REMARKS.ContainsKey(scene)) return;
            if (_gameState == null || _gameState.greetedScenes.Contains(scene)) return;

            _gameState.greetedScenes.Add(scene);
            _timer = _delay;
            _pending = true;
        }

        private void Update()
        {
            if (!_pending) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _pending = false;

            // 대화가 돌고 있으면 말풍선이 그 위에 겹친다 — 끝날 때까지 미룬다.
            if (WorldDialogueManager.Instance != null && WorldDialogueManager.Instance.IsPlaying)
            {
                _timer = 0.5f;
                _pending = true;
                return;
            }

            string scene = SceneManager.GetActiveScene().name;
            if (REMARKS.TryGetValue(scene, out string line)) SpeechBubble.ShowOn(gameObject, line, 2.6f);
        }
    }
}
