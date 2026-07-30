using UnityEngine;
using UnityEngine.InputSystem;

namespace DontLate
{
    /// <summary>
    /// Input System 읽기 전담. 액션은 에셋 없이 코드로 정의한다.
    /// 프로퍼티는 호출 시점에 직접 읽으므로 스크립트 실행 순서에 영향받지 않는다.
    /// ⚠ 클래스명 주의 — Input System에 PlayerInputManager가 실존한다.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        private InputAction _move;
        private InputAction _run;
        private InputAction _jump;
        private InputAction _interact;

        // S-116 ⑤ — 촬영 데모 오버라이드(DistrictCaptureDemo): 실입력 대신 주입값을 흘린다.
        private bool _demoActive;
        private Vector2 _demoMove;
        private bool _demoRun;

        /// <summary>데모 주행 속도 배수 — 로코모션이 곱한다 (평시 1).</summary>
        public float DemoSpeedMultiplier { get; private set; } = 1f;
        /// <summary>촬영 데모 입력이 실입력을 대체 중인가 (스태미나 페널티 면제 판정용).</summary>
        public bool DemoActive => _demoActive;

        /// <summary>x = 좌우(진행 방향), y = 깊이(Z).</summary>
        public Vector2 MoveVector => _demoActive ? _demoMove : _move.ReadValue<Vector2>();
        /// <summary>누르고 있는 동안 달린다.</summary>
        public bool RunHeld => _demoActive ? _demoRun : _run.IsPressed();

        public void SetDemoInput(Vector2 move, bool run, float speedMultiplier)
        {
            _demoActive = true;
            _demoMove = move;
            _demoRun = run;
            DemoSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
        }

        public void ClearDemoInput()
        {
            _demoActive = false;
            _demoMove = Vector2.zero;
            _demoRun = false;
            DemoSpeedMultiplier = 1f;
        }
        public bool JumpPressed => _jump.WasPressedThisFrame();
        public bool InteractPressed => _interact.WasPressedThisFrame();

        private void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");

            _run = new InputAction("Run", InputActionType.Button);
            _run.AddBinding("<Keyboard>/leftShift");
            _run.AddBinding("<Keyboard>/rightShift");
            _run.AddBinding("<Gamepad>/leftStickPress");

            _jump = new InputAction("Jump", InputActionType.Button);
            _jump.AddBinding("<Keyboard>/space");
            _jump.AddBinding("<Gamepad>/buttonSouth");

            _interact = new InputAction("Interact", InputActionType.Button);
            _interact.AddBinding("<Keyboard>/e");
            _interact.AddBinding("<Gamepad>/buttonWest");
        }

        private void OnEnable()
        {
            _move.Enable();
            _run.Enable();
            _jump.Enable();
            _interact.Enable();
            WorldEvents.MinigameRequested += OnMinigameRequested;
            WorldEvents.MinigameEnded += OnMinigameEnded;
        }

        private void OnDisable()
        {
            _move.Disable();
            _run.Disable();
            _jump.Disable();
            _interact.Disable();
            WorldEvents.MinigameRequested -= OnMinigameRequested;
            WorldEvents.MinigameEnded -= OnMinigameEnded;
        }

        // 미니게임(방향키 리듬) 동안 이동·상호작용이 오버레이 입력과 겹치지 않게 잠근다 — S-007.
        private void OnMinigameRequested()
        {
            _move.Disable();
            _jump.Disable();
            _interact.Disable();
        }

        private void OnMinigameEnded(MinigameResult _)
        {
            _move.Enable();
            _jump.Enable();
            _interact.Enable();
        }

        private void OnDestroy()
        {
            _move.Dispose();
            _run.Dispose();
            _jump.Dispose();
            _interact.Dispose();
        }
    }
}
