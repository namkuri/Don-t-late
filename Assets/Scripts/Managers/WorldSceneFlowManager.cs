using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLate
{
    /// <summary>
    /// 콘텐츠 씬 전이 상태기계. Core 씬은 상주하고 콘텐츠 씬만 Additive로 교체한다.
    /// </summary>
    public class WorldSceneFlowManager : MonoBehaviour
    {
        public static WorldSceneFlowManager Instance { get; private set; }

        private static readonly Dictionary<GameScene, GameScene[]> Transitions =
            new Dictionary<GameScene, GameScene[]>
            {
                { GameScene.Main, new[] { GameScene.Home } },
                { GameScene.Home, new[] { GameScene.Camp } },
                { GameScene.Camp, new[] { GameScene.Travel, GameScene.Home } },
                { GameScene.Travel, new[] { GameScene.District, GameScene.Camp, GameScene.Apartment } },
                { GameScene.District, new[] { GameScene.Travel, GameScene.Home } },
                { GameScene.Apartment, new[] { GameScene.Travel, GameScene.Home } }, // S-038
            };

        [SerializeField] private GameStateSO _gameState;
        [Tooltip("페이드 아웃을 기다리는 시간. FadeScreen의 길이와 맞춘다.")]
        [SerializeField] private float _transitionDelay = 0.35f;

        private GameScene _current;
        private bool _hasCurrent;
        private bool _busy;

        public bool IsTransitioning => _busy;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>씬 단독 Play(S-015): 이미 떠 있는 콘텐츠 씬을 현재 상태로 인계 — 다음 전이에서 정상 언로드되게.</summary>
        public void AdoptCurrent(GameScene scene)
        {
            _current = scene;
            _hasCurrent = true;
        }

        public bool CanGoTo(GameScene next)
        {
            if (!_hasCurrent) return true;
            return Transitions.TryGetValue(_current, out GameScene[] allowed)
                && System.Array.IndexOf(allowed, next) >= 0;
        }

        public void Request(GameScene next)
        {
            if (_busy)
            {
                Debug.LogWarning($"[SceneFlow] 전이 중이라 {next} 요청을 무시했다.");
                return;
            }

            if (!CanGoTo(next))
            {
                Debug.LogWarning($"[SceneFlow] {_current} → {next} 는 허용되지 않은 전이다.");
                return;
            }

            StartCoroutine(TransitionRoutine(next));
        }

        private IEnumerator TransitionRoutine(GameScene next)
        {
            _busy = true;
            WorldEvents.RaiseSceneTransitionStarted(next);
            yield return new WaitForSeconds(_transitionDelay);

            if (_hasCurrent)
            {
                Scene previous = SceneManager.GetSceneByName(_current.ToString());
                if (previous.isLoaded) yield return SceneManager.UnloadSceneAsync(previous);
            }

            string sceneName = next.ToString();
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            }
            else
            {
                Debug.LogWarning($"[SceneFlow] '{sceneName}' 씬이 빌드 세팅에 없다. 상태만 전이한다.");
            }

            _current = next;
            _hasCurrent = true;
            _gameState.currentScene = next;

            _busy = false;
            WorldEvents.RaiseSceneTransitionCompleted(next);
        }
    }
}
