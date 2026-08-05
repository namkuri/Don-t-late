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
                { GameScene.Home, new[] { GameScene.Camp, GameScene.Main } },
                { GameScene.Camp, new[] { GameScene.Travel, GameScene.Home, GameScene.District, GameScene.Main } }, // S-054b·S-065
                // S-134 ⑤ — Home 추가. 다른 씬은 전부 Home을 갖는데 Travel만 빠져 있어
                // 이동맵에서 귀가가 막혀 있었다(엣지워크 정산 누락의 한 겹).
                { GameScene.Travel, new[] { GameScene.District, GameScene.Camp, GameScene.Apartment, GameScene.Hillside, GameScene.Home, GameScene.Main } },
                { GameScene.District, new[] { GameScene.Travel, GameScene.Home, GameScene.Camp, GameScene.District, GameScene.Apartment, GameScene.Main } }, // S-053 ④·S-054b·S-065
                { GameScene.Apartment, new[] { GameScene.Travel, GameScene.Home, GameScene.Camp, GameScene.District, GameScene.Hillside, GameScene.Main } }, // S-038·S-054b·S-065
                { GameScene.Hillside, new[] { GameScene.Travel, GameScene.Home, GameScene.Camp, GameScene.Apartment, GameScene.Main } },  // S-049·S-054b·S-065
            };

        [SerializeField] private GameStateSO _gameState;
        [Tooltip("페이드 아웃을 기다리는 시간. FadeScreen의 길이와 맞춘다.")]
        [SerializeField] private float _transitionDelay = 0.35f;

        private GameScene _current;
        private bool _hasCurrent;
        private bool _busy;

        public bool IsTransitioning => _busy;
        /// <summary>직전 씬 (S-062 ⑤ — 뒤로가기).</summary>
        public GameScene PreviousScene { get; private set; } = GameScene.Main;

        /// <summary>직전 씬으로 복귀 (S-062 ⑤) — Travel 등에서 좌상단 ←·Backspace/Delete.</summary>
        public void GoBack()
        {
            if (PreviousScene == _gameState.currentScene) return;
            Request(PreviousScene);
        }

        private void Update()
        {
            // S-062 ⑤ — Travel에서 폰이 닫혀 있을 때 Backspace/Delete = 뒤로가기.
            if (_gameState.currentScene != GameScene.Travel || PhoneView.IsOpen || _busy) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.backspaceKey.wasPressedThisFrame || keyboard.deleteKey.wasPressedThisFrame) GoBack();
        }

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
            // S-159 — **상태도 함께 맞춘다.** 종전엔 `_current`만 세팅해서, 콘텐츠 씬에서 바로
            // Play하면 `_current`=Camp인데 `currentScene`=Main(부트스트랩 초기값)으로 갈라졌다.
            // `DistrictEdgeGate.FindTargetIndex()`는 `currentScene`을 보므로 int.MinValue를 돌려
            // **양쪽 엣지워크가 통째로 무반응**이 됐다 — Deny 메시지조차 없어 고장으로 읽힌다
            // (남규님 실관찰). 두 값이 갈라질 이유가 없다.
            if (_gameState != null) _gameState.currentScene = scene;
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

            PreviousScene = _gameState.currentScene; // S-062 ⑤
            StartCoroutine(TransitionRoutine(next));
        }

        private IEnumerator TransitionRoutine(GameScene next)
        {
            _busy = true;
            WorldEvents.RaiseSceneTransitionStarted(next);
            // ⚠ S-134 ⑤ 동반수리 — **Realtime이어야 한다.** 정산창이 timeScale=0으로 멈춘 상태에서
            // 전이가 걸리면 스케일 시간 대기는 영영 안 깨어나고, 페이드가 이미 입력을 막은 뒤라
            // 복구 불가 프리즈가 된다(실측 재현 경로: 정산창 → ESC 설정 → "처음 화면으로").
            yield return new WaitForSecondsRealtime(_transitionDelay);

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
