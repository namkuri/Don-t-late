using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 수익 정산·빚 게이지 — S-005. 배송 결과·미니게임 벌금을 하루 동안 집계해 두었다가
    /// Camp 복귀 시 정산한다: 벌금 차감 → 잔액으로 빚 상환 → DebtSettled 발행.
    /// money/debt 원본은 GameStateSO 소유, 갱신은 여기서만 한다 (보상 가산은 Delivery 몫).
    /// </summary>
    public class WorldDebtManager : MonoBehaviour
    {
        public static WorldDebtManager Instance { get; private set; }

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private TuningConfigSO _tuning;

        private int _pendingPenalty;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            WorldEvents.DeliveryFailed += OnDeliveryFailed;
            WorldEvents.MinigameEnded += OnMinigameEnded;
            WorldEvents.SceneTransitionCompleted += OnSceneArrived;
        }

        private void OnDisable()
        {
            WorldEvents.DeliveryFailed -= OnDeliveryFailed;
            WorldEvents.MinigameEnded -= OnMinigameEnded;
            WorldEvents.SceneTransitionCompleted -= OnSceneArrived;
        }

        private void OnDeliveryFailed(DeliveryData data)
        {
            if (data.IsLate) _pendingPenalty += _tuning.latePenalty;
        }

        private void OnMinigameEnded(MinigameResult result)
        {
            if (!result.Success) _pendingPenalty += _tuning.minigamePenalty;
        }

        private void OnSceneArrived(GameScene scene)
        {
            if (scene != GameScene.Camp) return;
            Settle();
        }

        /// <summary>벌금 차감 → 잔액으로 빚 상환. 정산할 것이 없으면 조용히 지나간다.</summary>
        private void Settle()
        {
            int penalty = Mathf.Min(_pendingPenalty, _gameState.money);
            _gameState.money -= penalty;
            _pendingPenalty = 0;

            int repaid = Mathf.Min(_gameState.money, _gameState.debt);
            _gameState.money -= repaid;
            _gameState.debt -= repaid;

            if (penalty == 0 && repaid == 0) return;

            WorldEvents.RaiseDebtSettled(new DebtSettlement
            {
                Repaid = repaid,
                Penalty = penalty,
                Money = _gameState.money,
                Debt = _gameState.debt
            });
        }
    }
}
