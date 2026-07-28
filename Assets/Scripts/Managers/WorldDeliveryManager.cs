using UnityEngine;

namespace DontLate
{
    /// <summary>하루 배송 일괄 정산 요약 (S-034 ④ — SettlementView 표시용).</summary>
    public struct DeliveryDaySummary
    {
        public int SuccessCount;
        public int FailCount;
        public int RewardTotal;
        public int PenaltyTotal;
        /// <summary>정산 항목별 내역 (S-075 ⑥ — 정산 UI 리스트 연출용). null 가능 — View가 가드.</summary>
        public System.Collections.Generic.List<SettleLine> Lines;
        /// <summary>이번 정산으로 해금된 구역 (S-084 ① — 없으면 null).</summary>
        public string UnlockedDistrict;
        /// <summary>이번 정산으로 회사 트럭 수령 (S-084 ①).</summary>
        public bool TruckAwarded;
    }

    /// <summary>정산 한 줄 (S-075 ⑥) — 주소·금액·성패·사유. 표시 전용 데이터.</summary>
    public struct SettleLine
    {
        public string Address;
        public int Amount;
        public bool Success;
        public string Note; // "파손"·"미배치"·"오배치" 등
    }

    /// <summary>
    /// 배송 수명주기 (S-034 재설계): 수주 → 픽업 → 운반 → **비콘 배치(내려놓기)** → 정산 일괄 판정.
    /// 비콘에 놓는 것은 완료가 아니다 — "집으로" 정산 때 배치 주소와 목적지 일치를 한꺼번에 판정한다.
    /// 적재 목록의 원본은 GameStateSO.cargo이며 여기서만 갱신한다.
    /// </summary>
    public class WorldDeliveryManager : MonoBehaviour
    {
        public static WorldDeliveryManager Instance { get; private set; }

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private TuningConfigSO _tuning; // S-034 — 실패 벌금(latePenalty)

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private const int HOSPITAL_FEE = 3000; // S-057 — 병원비 (밸런스 추후)

        private void OnEnable()
        {
            WorldEvents.DeliveryFailed += OnDeliveryFailed;
            WorldEvents.PlayerHitByCar += OnPlayerHitByCar; // S-057
        }
        private void OnDisable()
        {
            WorldEvents.DeliveryFailed -= OnDeliveryFailed;
            WorldEvents.PlayerHitByCar -= OnPlayerHitByCar;
        }

        /// <summary>이동맵 노드 선택 시 목적 구역 기록 (S-015). District 스포너가 이 값으로 짐·비콘을 깐다.</summary>
        public void SetDestination(string district)
        {
            _gameState.currentDistrict = district;
        }

        /// <summary>폰 바코드 스캔 등록 (S-011). 이미 등록된 건이면 false — 호출자가 경고 표시.</summary>
        public bool RegisterBarcode(DeliveryOrderSO order)
        {
            if (_gameState.scannedOrderIds.Contains(order.orderId)) return false;
            _gameState.scannedOrderIds.Add(order.orderId);
            WorldEvents.RaiseBarcodeScanned(DeliveryData.From(order));
            return true;
        }

        public bool IsScanned(DeliveryOrderSO order) => _gameState.scannedOrderIds.Contains(order.orderId);

        /// <summary>Camp에서 짐을 받는다.</summary>
        public void AcceptOrder(DeliveryOrderSO order)
        {
            if (_gameState.cargo.Contains(order)) return;

            _gameState.cargo.Add(order);
            WorldEvents.RaiseOrderAccepted(DeliveryData.From(order));
        }

        /// <summary>상자를 실제로 손에 들었을 때 PickupBox가 알린다.</summary>
        public void NotifyPickedUp(DeliveryOrderSO order)
        {
            WorldEvents.RaisePackagePickedUp(DeliveryData.From(order));
        }

        // ── 비콘 배치 (S-034 ④ — 내려놓기는 완료가 아니다) ──

        /// <summary>비콘 패드에 상자를 내려놓았다 — 같은 주문의 기존 배치는 교체.</summary>
        public void PlaceDelivery(DeliveryOrderSO order, string beaconAddress)
        {
            UnplaceDelivery(order.orderId);
            _gameState.placedDeliveries.Add(new PlacedDelivery { orderId = order.orderId, beaconAddress = beaconAddress });
            Debug.Log("[배송] #" + order.orderId + " 을 '" + beaconAddress + "' 비콘에 내려놓음 — 판정은 정산 때.");
        }

        /// <summary>패드에서 상자가 이탈(재픽업·굴러 나감) — 배치 기록 철회.</summary>
        public void UnplaceDelivery(int orderId)
        {
            _gameState.placedDeliveries.RemoveAll(p => p.orderId == orderId);
        }

        /// <summary>운반·배치 자격 (S-075 ② — 지각 완화): cargo 소속이거나, 지각 실패로 cargo에서
        /// 빠졌어도 당일 물량(dayOrders)이면 허용 — 늦어도 갖다는 놓을 수 있다(보상은 이미 없음).</summary>
        public bool CanHandle(DeliveryOrderSO order)
            => order != null && (IsInCargo(order)
                || _gameState.dayOrders.Exists(o => o != null && o.orderId == order.orderId));

        /// <summary>상자 파손 통지 (S-074 ③ — BoxDurability). 정산에서 실패로 청산되고 그날 재스폰이 막힌다.</summary>
        public void ReportDestroyed(DeliveryOrderSO order)
        {
            if (order == null || _gameState.destroyedOrderIds.Contains(order.orderId)) return;
            _gameState.destroyedOrderIds.Add(order.orderId);
            UnplaceDelivery(order.orderId); // 패드 위에서 깨졌으면 배치도 무효
        }

        public bool IsPlaced(int orderId) => _gameState.placedDeliveries.Exists(p => p.orderId == orderId);

        /// <summary>
        /// "집으로" 정산 일괄 판정 (S-034 ④): 적재된 각 건 — 배치 주소가 목적지와 일치하면 성공(보상),
        /// 아니면(미배치·오배치) 실패(벌금 — 잔액에서 차감, 부족분은 빚). 판정 후 상차·스캔·배치 전부 초기화.
        /// </summary>
        public DeliveryDaySummary SettleDeliveries()
        {
            var summary = new DeliveryDaySummary();
            summary.Lines = new System.Collections.Generic.List<SettleLine>(); // S-075 ⑥
            _settledDistricts.Clear(); // S-054 — 이번 정산에서 성공한 구역
            int penalty = _tuning != null ? _tuning.latePenalty : 500;

            // S-074 ③ — 파손 건 선청산: 실패로 벌금. cargo에서 미리 빼서 아래 미배치 경로와
            // 이중 가산을 막고, 픽업 전 파손(cargo 미소속)도 같은 경로로 잡는다.
            foreach (int destroyedId in _gameState.destroyedOrderIds)
            {
                DeliveryOrderSO order = _gameState.dayOrders.Find(o => o != null && o.orderId == destroyedId);
                if (order == null) continue;
                _gameState.cargo.RemoveAll(o => o != null && o.orderId == destroyedId);
                summary.FailCount++;
                summary.PenaltyTotal += penalty;
                _gameState.money -= penalty;
                if (_gameState.money < 0) { _gameState.debt += -_gameState.money; _gameState.money = 0; }
                _gameState.lateCount++;
                MasteryProgress.Add(_gameState, -MasteryProgress.FAIL_LOSS);
                summary.Lines.Add(new SettleLine { Address = order.address, Amount = -penalty, Success = false, Note = "파손" });
                WorldEvents.RaiseDeliveryFailed(DeliveryData.From(order));
            }
            _gameState.destroyedOrderIds.Clear();

            foreach (DeliveryOrderSO order in _gameState.cargo.ToArray())
            {
                if (order == null) continue;
                // 이벤트 발행 전에 적재에서 제거 — OnDeliveryFailed 핸들러(지각 경로용)가
                // cargo 부재를 보고 무시하게 해 lateCount 이중 가산을 구조적으로 차단 (PR#12 정수 적발).
                _gameState.cargo.Remove(order);
                int placedIndex = _gameState.placedDeliveries.FindIndex(p => p.orderId == order.orderId);
                bool success = placedIndex >= 0
                    && _gameState.placedDeliveries[placedIndex].beaconAddress == order.address;

                if (success)
                {
                    summary.SuccessCount++;
                    summary.RewardTotal += order.reward;
                    _gameState.money += order.reward;
                    _gameState.completedCount++;
                    _gameState.totalEarned += order.reward;
                    _gameState.deliveryHistory.Add(new DeliveryRecord
                    {
                        orderId = order.orderId,
                        address = order.address,
                        reward = order.reward,
                        day = _gameState.day,
                        minuteOfDay = Mathf.FloorToInt(_gameState.minuteOfDay)
                    });
                    if (!string.IsNullOrEmpty(order.district)) _settledDistricts.Add(order.district); // S-054
                    MasteryProgress.Add(_gameState, MasteryProgress.SUCCESS_GAIN); // S-063
                    summary.Lines.Add(new SettleLine { Address = order.address, Amount = order.reward, Success = true }); // S-075 ⑥
                    WorldEvents.RaiseDeliveryCompleted(DeliveryData.From(order));
                }
                else
                {
                    summary.FailCount++;
                    summary.PenaltyTotal += penalty;
                    _gameState.money -= penalty;
                    if (_gameState.money < 0) { _gameState.debt += -_gameState.money; _gameState.money = 0; } // 잔액 부족분은 빚
                    _gameState.lateCount++;
                    MasteryProgress.Add(_gameState, -MasteryProgress.FAIL_LOSS); // S-063
                    summary.Lines.Add(new SettleLine // S-075 ⑥
                    {
                        Address = order.address,
                        Amount = -penalty,
                        Success = false,
                        Note = placedIndex >= 0 ? "오배치" : "미배치"
                    });
                    WorldEvents.RaiseDeliveryFailed(DeliveryData.From(order));
                }
            }

            _gameState.cargo.Clear();
            _gameState.scannedOrderIds.Clear();
            _gameState.placedDeliveries.Clear();
            AdvanceProgression(ref summary); // S-054 — 개척 판정 (성공 구역 기준·S-084 해금 기록)
            _gameState.daySettled = true;      // S-068 ③ — 캠프 주문판 리롤 신호
            _gameState.stamina = -1f;          // S-081 ① — 하루 마감: 다음날 풀 충전
            _gameState.droppedCargo.Clear();   // 정산 = 하루 마감: 버려둔 짐 기록도 청산
            Debug.Log("[배송] 일괄 정산 — 성공 " + summary.SuccessCount + " · 실패 " + summary.FailCount
                    + " · 보상 " + summary.RewardTotal + " · 벌금 " + summary.PenaltyTotal);
            return summary;
        }

        // S-057 — 교통사고: 병원비 청구 + 아직 배치 못 한 짐 전량 실패 정산 + 집으로 후송.
        private void OnPlayerHitByCar()
        {
            _gameState.money -= HOSPITAL_FEE;
            if (_gameState.money < 0) { _gameState.debt += -_gameState.money; _gameState.money = 0; }
            WorldEvents.RaiseMoneySpent(HOSPITAL_FEE);

            int failed = 0;
            foreach (DeliveryOrderSO order in _gameState.cargo.ToArray())
            {
                if (order == null) continue;
                _gameState.cargo.Remove(order);
                _gameState.lateCount++;
                MasteryProgress.Add(_gameState, -MasteryProgress.FAIL_LOSS);
                WorldEvents.RaiseDeliveryFailed(DeliveryData.From(order));
                failed++;
            }
            _gameState.cargo.Clear();
            _gameState.scannedOrderIds.Clear();

            Debug.Log("[교통사고] 병원비 -₩" + HOSPITAL_FEE.ToString("N0") + " · 미배송 " + failed + "건 실패");
            WorldEvents.RaiseCarAccident(HOSPITAL_FEE, failed); // S-066 ③ — 팝업이 "치료 후 집으로"를 안내
        }

        private readonly System.Collections.Generic.HashSet<string> _settledDistricts =
            new System.Collections.Generic.HashSet<string>();

        // S-054 진행 시스템 — 개척 최전선 구역에서 배송 성공하면 다음 구역 해금.
        // 최전선이 언덕주택가(마지막)면 회사 트럭 수령. unlockedDistricts가 비면(테스트·그레이박스) 판정 안 함.
        private void AdvanceProgression(ref DeliveryDaySummary summary)
        {
            if (summary.SuccessCount <= 0 || _gameState.unlockedDistricts.Count == 0) return;

            string[] progression = DeliveryOrderSO.DISTRICT_PROGRESSION;
            int frontier = -1;
            for (int i = 0; i < progression.Length; i++)
                if (_gameState.unlockedDistricts.Contains(progression[i])) frontier = i;
            if (frontier < 0 || !_settledDistricts.Contains(progression[frontier])) return;

            if (frontier + 1 < progression.Length)
            {
                string next = progression[frontier + 1];
                _gameState.unlockedDistricts.Add(next);
                summary.UnlockedDistrict = next; // S-084 ① — 정산 화면 표시용
                WorldEvents.RaiseDistrictUnlocked(next);
            }
            else if (!_gameState.hasTruck)
            {
                _gameState.hasTruck = true;
                summary.TruckAwarded = true; // S-084 ①
                WorldEvents.RaiseTruckAwarded();
            }
        }

        public bool IsInCargo(DeliveryOrderSO order) => _gameState.cargo.Contains(order);

        private void OnDeliveryFailed(DeliveryData data)
        {
            int index = _gameState.cargo.FindIndex(o => o != null && o.orderId == data.OrderId);
            if (index < 0) return;

            _gameState.cargo.RemoveAt(index);
            _gameState.lateCount++;
        }
    }
}
