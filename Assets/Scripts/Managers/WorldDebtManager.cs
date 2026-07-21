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
        }

        private void OnDisable()
        {
            WorldEvents.DeliveryFailed -= OnDeliveryFailed;
            WorldEvents.MinigameEnded -= OnMinigameEnded;
        }

        // 벌금은 그 자리에서 빚에 붙는다 (S-015 — "지각하면 빚이 늘어난다"가 눈에 보이게).
        private void OnDeliveryFailed(DeliveryData data)
        {
            if (!data.IsLate) return;
            _gameState.debt += _tuning.latePenalty;
            WorldEvents.RaiseDebtIncreased(_tuning.latePenalty);
        }

        private void OnMinigameEnded(MinigameResult result)
        {
            if (result.Success) return;
            _gameState.debt += _tuning.minigamePenalty;
            WorldEvents.RaiseDebtIncreased(_tuning.minigamePenalty);
        }

        /// <summary>
        /// 하루 정산 (S-009: "집으로" 시점에 SettlementView가 호출) —
        /// 잔액으로 빚 상환 → DebtSettled 발행 후 요약을 돌려준다. 벌금은 발생 즉시 빚에 붙었다(S-015).
        /// </summary>
        public DebtSettlement SettleNow()
        {
            int repaid = Mathf.Min(_gameState.money, _gameState.debt);
            _gameState.money -= repaid;
            _gameState.debt -= repaid;

            var settlement = new DebtSettlement
            {
                Repaid = repaid,
                Penalty = 0,
                Money = _gameState.money,
                Debt = _gameState.debt
            };
            WorldEvents.RaiseDebtSettled(settlement);
            return settlement;
        }
    }
}
