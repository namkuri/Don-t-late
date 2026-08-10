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
                // S-278 — 문구는 남규님 지정 그대로.
                ["Village"] = "여기가 빌라촌이구나.",
                ["FoodStreet"] = "먹자골목이네 맛있겠다...",
                ["Hillside"] = "언덕이 너무 높은데..",
                ["Apartment"] = "공동현관문 비밀번호는 배달앱에 있다고했어.",
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
            _timer -= Time.unscaledDeltaTime; // 페이드·대화로 시간이 멈춰도 흐른다
            if (_timer > 0f) return;
            _pending = false;

            // S-278 — **대화를 기다리지 않는다.** 종전엔 대화 중이면 미뤘는데, 구역 첫 진입에는
            // 배송 안내가 딸려 와서 그 대기가 사실상 영구가 됐다(남규님: "씬 독백 문구 안 나왔었어" —
            // 실측 시점에도 `대화중=True`로 계속 밀리고 있었다).
            // 대화창은 화면 하단, 말풍선은 머리 위라 서로 가리지 않는다.

            string scene = SceneManager.GetActiveScene().name;
            if (REMARKS.TryGetValue(scene, out string line)) SpeechBubble.ShowOn(gameObject, line, 2.6f);
        }
    }
}
