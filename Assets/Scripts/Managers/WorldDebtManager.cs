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

        // ── 경제 API (S-019 — 폰 앱이 Instance 명령으로 호출) ──

        /// <summary>코인 현재가 — minuteOfDay 기반 결정론 랜덤워크 (원/개, 기준 1,000).</summary>
        public int CoinPrice() => CoinPriceAt(_gameState.day * 1440f + _gameState.minuteOfDay);

        /// <summary>절대 게임분 기준 시세 — 결정론 사인파라 과거 시점도 계산 가능 (S-032 ⑤ 차트용).</summary>
        public int CoinPriceAt(float absoluteMinute)
        {
            float t = absoluteMinute;
            float wave = Mathf.Sin(t * 0.011f) * 0.5f + Mathf.Sin(t * 0.037f + 2.1f) * 0.3f
                       + Mathf.Sin(t * 0.0041f + 4.7f) * 0.2f;
            return Mathf.Max(100, Mathf.RoundToInt(1000f * (1f + wave * _tuning.coinVolatility)));
        }

        // S-032 ⑤: 거래는 1개 단위 — 금액 매수(BuyCoin(won)) 폐지. 차익 계산용 매수원가(coinCostBasis) 동반.

        /// <summary>현재 시세로 1개 매수 — 잔액 부족이면 false.</summary>
        public bool BuyOneCoin()
        {
            int price = CoinPrice();
            if (_gameState.money < price) return false;
            _gameState.money -= price;
            _gameState.coinUnits += 1f;
            _gameState.coinCostBasis += price;
            WorldEvents.RaiseMoneySpent(price);
            return true;
        }

        /// <summary>현재 시세로 1개 매도 — 평균 원가만큼 원가를 덜어낸다. 반환 = 회수액(0=보유 없음).</summary>
        public int SellOneCoin()
        {
            if (_gameState.coinUnits < 1f) return 0;
            int price = CoinPrice();
            _gameState.coinCostBasis -= _gameState.coinCostBasis / _gameState.coinUnits; // 평균법
            _gameState.coinUnits -= 1f;
            if (_gameState.coinUnits < 0.001f) { _gameState.coinUnits = 0f; _gameState.coinCostBasis = 0f; }
            _gameState.money += price;
            return price;
        }

        /// <summary>코인 전량 매도 — 판 금액 반환(없으면 0).</summary>
        public int SellAllCoin()
        {
            if (_gameState.coinUnits <= 0f) return 0;
            int gained = Mathf.RoundToInt(_gameState.coinUnits * CoinPrice());
            _gameState.money += gained;
            _gameState.coinUnits = 0f;
            _gameState.coinCostBasis = 0f;
            return gained;
        }

        /// <summary>일반 지출(자판기·가구) — 잔액 부족이면 false. 성공 시 MoneySpent 통지 (S-030 ③ 차감 연출).</summary>
        public bool TrySpend(int won)
        {
            if (won <= 0 || _gameState.money < won) return false;
            _gameState.money -= won;
            WorldEvents.RaiseMoneySpent(won);
            return true;
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
