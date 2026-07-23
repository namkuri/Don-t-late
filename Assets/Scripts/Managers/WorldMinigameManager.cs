using System.Collections;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 진상 전화 → 방향키 리듬 미니게임 구동 — S-007. 씬이 아니라 UI 오버레이 모듈(ARCHITECTURE §5).
    /// District 도착 후 일정 시간(phoneCallDelaySeconds) 뒤 전화를 울리고 오버레이를 요청한다.
    /// 방문당 1회. 판정 결과의 게임 반영은 하지 않는다 — Debt 등이 MinigameEnded를 구독해 처리.
    /// </summary>
    public class WorldMinigameManager : MonoBehaviour
    {
        public static WorldMinigameManager Instance { get; private set; }

        [SerializeField] private TuningConfigSO _tuning;

        private Coroutine _pending;
        private Coroutine _ringTimeout; // S-037 — 수신 방치 타이머

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => WorldEvents.SceneTransitionCompleted += OnSceneArrived;

        private void OnDisable()
        {
            WorldEvents.SceneTransitionCompleted -= OnSceneArrived;
            if (_pending != null) { StopCoroutine(_pending); _pending = null; }
            CancelRingTimeout();
        }

        private void OnSceneArrived(GameScene scene)
        {
            if (_pending != null) { StopCoroutine(_pending); _pending = null; }
            CancelRingTimeout(); // 수신 방치 중 씬 이탈 — 타이머도 소멸
            if (scene != GameScene.District) return;
            _pending = StartCoroutine(RingAfterDelay());
        }

        private IEnumerator RingAfterDelay()
        {
            yield return new WaitForSeconds(_tuning.phoneCallDelaySeconds);
            _pending = null;

            // S-031 ⑧: 울리기만 한다 — 미니게임은 폰 수신 화면(받기)이 AcceptCall로 개시.
            WorldEvents.RaisePhoneRang(new PhoneCall { CallerName = "박말순", ScenarioId = "phone_grumpy" });
            _ringTimeout = StartCoroutine(TimeoutAfterRing()); // S-037 — 방치 시 부재중 처리
        }

        // S-037: 수신 후 timeout 실초 내 받기/거절이 없으면 부재중 — 거절과 동일 실패 처리
        // (전화를 무시하는 것도 진상 응대 거부다). 폰 접힘은 PhoneView가 MinigameEnded 구독으로 처리.
        private IEnumerator TimeoutAfterRing()
        {
            yield return new WaitForSeconds(_tuning.phoneCallTimeoutSeconds);
            _ringTimeout = null;
            Debug.Log("[전화] 부재중 — " + _tuning.phoneCallTimeoutSeconds + "초 무응답, 거절과 동일 실패 처리.");
            WorldEvents.RaiseMinigameEnded(new MinigameResult { Success = false, HitCount = 0, TotalCount = 0, Accuracy = 0f });
        }

        private void CancelRingTimeout()
        {
            if (_ringTimeout != null) { StopCoroutine(_ringTimeout); _ringTimeout = null; }
        }

        /// <summary>폰 수신 화면 "받기" (S-031 ⑧) — 리듬 오버레이 개시.</summary>
        public void AcceptCall()
        {
            CancelRingTimeout(); // S-037 — 응답 = 타이머 해제
            WorldEvents.RaiseMinigameRequested();
        }

        /// <summary>폰 수신 화면 "거절" — 진상 응대 거부 = 실패 판정과 동일(벌금은 Debt가 MinigameEnded로 처리).</summary>
        public void DeclineCall()
        {
            CancelRingTimeout(); // S-037 — 응답 = 타이머 해제
            Debug.Log("[전화] 거절 — 진상 응대 거부, 실패 처리.");
            WorldEvents.RaiseMinigameEnded(new MinigameResult { Success = false, HitCount = 0, TotalCount = 0, Accuracy = 0f });
        }

        /// <summary>오버레이(MinigameRhythmView)가 판정을 마치면 호출한다.</summary>
        public void SubmitResult(MinigameResult result)
        {
            WorldEvents.RaiseMinigameEnded(result);
        }
    }
}
