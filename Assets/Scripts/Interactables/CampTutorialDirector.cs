using System;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-146 — 캠프 튜토리얼 진행부. 김사장님이 한 항목씩 알려주고, **플레이어가 실제로 해내면**
    /// 다음으로 넘어간다(읽고 넘기기 아님 — 남규님 확정 방식).
    ///
    /// 왜 별도 컴포넌트인가: `CampBossNpc`는 접근·복귀·잔소리를 맡는 **배우**다. 여기에 7단계
    /// 상태기계까지 넣으면 한 클래스가 두 일을 하게 된다. 이 클래스는 **진행만** 맡고,
    /// 대사 재생은 WorldDialogueManager에, 연기는 보스에게 맡긴다.
    ///
    /// 판정은 전부 **경계 이벤트 구독**이다(폴링·Find 금지 규약). 이동만 예외로 플레이어
    /// Transform의 이동 거리를 본다 — 이동은 프레임 데이터라 이벤트로 흘리지 않는 규칙이라서다.
    /// </summary>
    public class CampTutorialDirector : MonoBehaviour
    {
        /// <summary>한 단계가 "해냈다"고 볼 조건.</summary>
        public enum Gate
        {
            Move,        // 일정 거리 이동
            BagOpen,     // 가방 열기
            PhoneOpen,   // 휴대폰 열기
            BoxPickup,   // 상자 집기
            Barcode,     // 송장 바코드 스캔 (S-151 — 남규님 "바코드 어떻게 찍는지 설명 안 함")
            DrinkUse,    // 에너지드링크 마시기 (S-155 — 시작 지급분을 써 보게 한다)
            ReadOnly,    // 설명만 — 대사가 끝나면 통과 (지역 설명)
            NpcTalk,     // NPC와 대화
            KioskOpen,   // 자판기·편의점·포장마차 구매창 열기
        }

        [Serializable]
        public struct Step
        {
            public DialogueScenarioSO scenario;
            public Gate gate;
            [Tooltip("이 단계에서 화면에 띄울 한 줄 안내(대사가 끝난 뒤 남는다).")]
            public string hint;
            [Tooltip("S-151 — 해냈을 때 사장님이 건네는 칭찬. 비면 건너뛴다.")]
            public DialogueScenarioSO praise;
            [Tooltip("S-162 — 미션 카드 제목. 비면 힌트 첫 줄을 쓴다.")]
            public string title;
        }

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private Step[] _steps;
        [Tooltip("이동 판정 거리(u). 이만큼 걸으면 통과.")]
        [SerializeField] private float _moveDistance = 3f;

        // S-151 — 통과 직후 숨 돌리는 시간. 종전엔 게이트가 열리자마자 다음 대사가 나가서
        // "I키 눌렀는데 가방이 열리기도 전에 사장이 말한다"가 됐다(남규님 지적).
        // 플레이어가 자기가 한 일의 결과(열린 가방·집은 상자)를 보고 나서 칭찬이 오게 한다.
        [Tooltip("게이트 통과 후 칭찬까지의 여유(초).")]
        [SerializeField] private float _beatBeforePraise = 1.1f;

        private int _index = -1;
        private bool _gateCleared;
        private bool _waitingDialogue;
        private bool _praising;
        private float _beatLeft;
        private Gate _pendingGate;   // S-157 — 대사 중에 해낸 행동(대사 종료 시 인정)
        private bool _hasPending;
        private Transform _player;
        private Vector3 _moveAnchor;
        private float _moved;

        /// <summary>진행 중인지. 보스가 복귀 타이밍을 잡을 때 본다.</summary>
        public bool Running => _index >= 0 && _index < (_steps?.Length ?? 0);

        /// <summary>현재 단계 안내 문구(없으면 빈 문자열). HUD가 표시한다.</summary>
        public string CurrentHint =>
            Running && _steps[_index].hint != null ? _steps[_index].hint : string.Empty;

        private void OnEnable()
        {
            WorldEvents.BagOpened += OnBagOpened;
            WorldEvents.PhoneOpened += OnPhoneOpened;
            WorldEvents.PackagePickedUp += OnPackagePickedUp;
            WorldEvents.BarcodeScanned += OnBarcodeScanned;
            WorldEvents.NpcMet += OnNpcMet;
            WorldEvents.KioskRequested += OnKioskRequested;
            WorldEvents.BagItemConsumed += OnBagItemConsumed;
        }

        private void OnDisable()
        {
            WorldEvents.BagOpened -= OnBagOpened;
            WorldEvents.PhoneOpened -= OnPhoneOpened;
            WorldEvents.PackagePickedUp -= OnPackagePickedUp;
            WorldEvents.BarcodeScanned -= OnBarcodeScanned;
            WorldEvents.NpcMet -= OnNpcMet;
            WorldEvents.KioskRequested -= OnKioskRequested;
            WorldEvents.BagItemConsumed -= OnBagItemConsumed;
        }

        private void Start()
        {
            // S-160 — **소유자 없는 잠금은 즉시 푼다**(이중 안전).
            // 튜토리얼을 이미 본 세션이면 이 진행부는 시작되지 않는다. 그런데 잠금은 SO에 남아
            // 있을 수 있어(씬 이동으로 진행부가 파괴된 경우) 아무도 풀지 못하는 상태가 된다.
            if (_gameState != null && _gameState.bossIntroPlayed && !Running)
                _gameState.tutorialExitLocked = false;
        }

        /// <summary>보스가 플레이어 앞에 도착하면 부른다.</summary>
        public void Begin(Transform player)
        {
            if (_steps == null || _steps.Length == 0) return;
            _player = player;
            _index = -1;
            // S-158 — 상자 픽업 단계를 넘길 때까지 귀가를 막는다(중도 이탈 시 재개 불가라서).
            if (_gameState != null) _gameState.tutorialExitLocked = true;
            Advance();
        }

        private void Advance()
        {
            _index++;
            if (_index >= _steps.Length)
            {
                if (_gameState != null)
                {
                    _gameState.tutorialDone = true;
                    _gameState.tutorialExitLocked = false; // 안전망 — 픽업에서 이미 풀렸어야 한다
                }
                Debug.Log("[튜토리얼] 전 단계 완료.");
                return;
            }

            // S-158 — 픽업 단계를 지났으면 귀가를 푼다(남규님 지정: "상자 바코드찍고 잡기 전까진").
            // 방금 끝낸 단계가 픽업이면 이 시점부터 나가도 된다.
            if (_gameState != null && _index > 0 && _steps[_index - 1].gate == Gate.BoxPickup)
                _gameState.tutorialExitLocked = false;

            _gateCleared = false;
            _waitingDialogue = true;
            _praising = false;
            _beatLeft = 0f;
            _hasPending = false;
            _moved = 0f;
            if (_player != null) _moveAnchor = _player.position;

            Step step = _steps[_index];
            if (WorldDialogueManager.Instance != null && step.scenario != null)
                WorldDialogueManager.Instance.PlayScenario(step.scenario);
            else
                _waitingDialogue = false;

            // S-162 — 미션 카드에 알린다. 힌트가 빈 단계(읽기 전용)는 카드를 띄우지 않는다 —
            // 할 일이 없는데 "미션"이 뜨면 무엇을 기다리는지 헷갈린다.
            if (!string.IsNullOrEmpty(step.hint))
            {
                string title = string.IsNullOrEmpty(step.title)
                    ? $"튜토리얼 {_index + 1}/{_steps.Length}" : step.title;
                WorldEvents.RaiseTutorialStepStarted(title, step.hint);
            }

            Debug.Log($"[튜토리얼] {_index + 1}/{_steps.Length} — {step.gate}");
        }

        private void Update()
        {
            if (!Running) return;

            // S-157 — 이동 거리는 **대사 중에도 누적**한다. 대사가 끝난 뒤 다시 재기 시작하면
            // 설명 듣는 동안 걸은 것이 버려져 "또 걸어야" 한다(discrete 게이트의 보류와 같은 취지).
            if (!_gateCleared && _steps[_index].gate == Gate.Move && _player != null)
            {
                _moved += Vector3.Distance(_player.position, _moveAnchor);
                _moveAnchor = _player.position;
            }

            // 대사가 끝나기 전엔 통과시키지 않는다 — 말하는 도중에 넘어가면 설명이 잘린다.
            // (해낸 사실은 위/`Clear`에서 기록해 두고, 대사가 끝나는 순간 인정한다.)
            if (_waitingDialogue)
            {
                if (WorldDialogueManager.Instance != null && WorldDialogueManager.Instance.IsPlaying) return;
                _waitingDialogue = false;

                if (_steps[_index].gate == Gate.ReadOnly) _gateCleared = true;

                // S-157 — 대사 중에 이미 해냈으면 여기서 인정한다(다시 시키지 않는다).
                // 이동 기준점은 다시 잡지 않는다 — 종전엔 여기서 리셋해 말 듣는 동안 걸은
                // 거리를 버렸고, 그래서 "이미 걸었는데 또 걸어야" 했다.
                if (_hasPending && _pendingGate == _steps[_index].gate) _gateCleared = true;
                _hasPending = false;
                // 이동도 이미 채웠으면 인정한다(위에서 대사 중에도 누적했다).
                if (_steps[_index].gate == Gate.Move && _moved >= _moveDistance) _gateCleared = true;

                if (_gateCleared) _beatLeft = _beatBeforePraise;
            }

            // 누적은 위(대사 중 포함)에서 하고 여기선 판정만 한다 — 두 곳에서 더하면 두 배로 센다.
            if (!_gateCleared && _steps[_index].gate == Gate.Move && _moved >= _moveDistance)
            {
                _gateCleared = true;
                _beatLeft = _beatBeforePraise;
            }

            if (!_gateCleared) return;

            // S-151 — 통과 → (숨 돌리기) → 칭찬 → 다음 단계. 곧바로 넘기지 않는 이유는 위 주석 참조.
            if (_beatLeft > 0f)
            {
                _beatLeft -= Time.deltaTime;
                if (_beatLeft > 0f) return;
                PlayPraise();
                return;
            }

            if (_praising)
            {
                if (WorldDialogueManager.Instance != null && WorldDialogueManager.Instance.IsPlaying) return;
                _praising = false;
            }

            Advance();
        }

        private void Clear(Gate gate)
        {
            if (!Running || _gateCleared) return;
            if (_steps[_index].gate != gate) return;

            // S-157 — 대사 중 행동은 **무시가 아니라 보류**다.
            // 종전엔 여기서 그냥 return 했다(설명이 잘리는 걸 막으려고). 그 바람에 설명 도중
            // 이미 해낸 행동이 통째로 버려져, 상자를 들고 있는데도 "상자를 집어 보세요"가 남고
            // 폰을 껐다 다시 켜야 했다(남규님 지적). 설명은 끝까지 보여주되 한 일은 기억한다.
            if (_waitingDialogue) { _pendingGate = gate; _hasPending = true; return; }

            _gateCleared = true;
            _beatLeft = _beatBeforePraise; // 결과를 보고 나서 칭찬이 오도록 한 박자 쉰다
        }

        private void PlayPraise()
        {
            // S-162 — 카드를 "완료"로 전환한다(칭찬 대사와 같은 타이밍).
            if (!string.IsNullOrEmpty(_steps[_index].hint)) WorldEvents.RaiseTutorialStepCleared();

            // S-153 — 폰 단계는 확인이 끝나면 폰을 닫아준다(남규님 지시). 열어둔 채로 칭찬이
            // 나오면 폰이 대화창을 덮어 다음 안내가 안 보인다. 가방은 화면 일부만 가려 그대로 둔다.
            if (_steps[_index].gate == Gate.PhoneOpen && PhoneView.Instance != null)
                PhoneView.Instance.ClosePanel();

            DialogueScenarioSO praise = _steps[_index].praise;
            if (praise == null || WorldDialogueManager.Instance == null) return;
            WorldDialogueManager.Instance.PlayScenario(praise);
            _praising = true;
        }

        private void OnBagOpened() => Clear(Gate.BagOpen);
        private void OnPhoneOpened() => Clear(Gate.PhoneOpen);
        private void OnPackagePickedUp(DeliveryData _)
        {
            // S-160 — 잠금 해제를 **단계 진행이 아니라 "상자를 들었다"는 사실 자체**에 건다.
            // S-158에선 `Advance()` 안에서만 풀었는데, 진행부는 Camp 씬과 함께 파괴된다.
            // 픽업 후 배송하러 나갔다 오면 진행부가 새로 생기고 `Begin()`은 다시 안 불리므로
            // (`bossIntroPlayed`=true) **잠금을 풀 주체가 사라져** SO에 영구 잔존했다 —
            // 집에 영영 못 간다(남규님 실관찰). 남규님 지시: "상자 하나 들기 성공하면 집으로".
            if (_gameState != null) _gameState.tutorialExitLocked = false;
            Clear(Gate.BoxPickup);
        }
        private void OnBarcodeScanned(DeliveryData _) => Clear(Gate.Barcode);
        private void OnNpcMet(string _) => Clear(Gate.NpcTalk);
        private void OnKioskRequested(KioskOffer _) => Clear(Gate.KioskOpen);
        private void OnBagItemConsumed(BagItem _) => Clear(Gate.DrinkUse);

        /// <summary>
        /// S-155 — 놓친 안내를 다시 듣는다(남규님 지시: 사장님에게 E). 지금 단계의 대사를
        /// 처음부터 재생한다. 게이트를 이미 통과했으면 칭찬을, 아직이면 안내를 다시 튼다 —
        /// 플레이어가 "지금 뭘 해야 하지?"를 물었을 때 답이 되는 쪽을 준다.
        /// 되듣기 중에는 게이트 판정을 멈춘다(설명 도중 통과 방지 규칙과 같은 이유).
        /// </summary>
        public bool TryRepeatCurrentLine()
        {
            if (!Running || WorldDialogueManager.Instance == null) return false;
            if (WorldDialogueManager.Instance.IsPlaying) return false;

            DialogueScenarioSO line = _gateCleared ? _steps[_index].praise : _steps[_index].scenario;
            if (line == null) return false;

            WorldDialogueManager.Instance.PlayScenario(line);
            if (!_gateCleared) _waitingDialogue = true; // 다시 듣는 동안엔 통과 판정을 멈춘다
            return true;
        }
    }
}
