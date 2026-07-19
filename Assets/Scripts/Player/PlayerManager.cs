using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// Player 도메인 허브. 서브매니저를 소유·연결하며 형제끼리는 이 허브로만 서로를 본다.
    /// 도메인 밖(World)과의 통신은 WorldEvents로만 한다.
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerLocomotionManager))]
    [RequireComponent(typeof(PlayerStatusManager))]
    public class PlayerManager : MonoBehaviour
    {
        [SerializeField] private TuningConfigSO _tuning;
        [SerializeField] private GameStateSO _gameState;

        public TuningConfigSO Tuning => _tuning;
        public GameStateSO GameState => _gameState;

        public PlayerInputHandler Input { get; private set; }
        public PlayerLocomotionManager Locomotion { get; private set; }
        public PlayerAnimationManager Animation { get; private set; }
        public PlayerStatusManager Status { get; private set; }
        public InteractionSensor Sensor { get; private set; }

        private void Awake()
        {
            Input = GetComponent<PlayerInputHandler>();
            Locomotion = GetComponent<PlayerLocomotionManager>();
            Animation = GetComponent<PlayerAnimationManager>();
            Status = GetComponent<PlayerStatusManager>();
            Sensor = GetComponentInChildren<InteractionSensor>();
        }
    }
}
