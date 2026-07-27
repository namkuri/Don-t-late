# BALANCE.md — 밸런스 수치 단일 대장

> D-070 도입 (2026-07-28). 코드·에셋에 산개된 밸런스 파라미터를 한 표로 모은다.
> **수치를 바꾸면 이 표도 같이 바꾼다** (출처 열이 코드 좌표). 버전은 표 끝 이력에.

## 이동·조작

| 파라미터 | 값 | 출처 |
|---|---|---|
| 걷기 속도 | 4 u/s | Tuning.asset `moveSpeed` |
| 달리기 속도 | 6 u/s | Tuning.asset `runSpeed` |
| 깊이(Z) 속도 비율 | 0.7 | Tuning.asset `depthSpeedRatio` |
| 캐리 속도 페널티 | ×0.75 | Tuning.asset `carrySpeedPenalty` |
| 점프 높이 | 1.1 u | Tuning.asset `jumpHeight` |
| 비 미끄럼 가속(일반) | 7.5 u/s² | PlayerLocomotionManager `SLIPPERY_ACCEL_RAIN` |
| 비 미끄럼 가속(언덕 — 더 미끄러움) | 4.5 u/s² | PlayerLocomotionManager `SLIPPERY_ACCEL_HILL` |
| 던지기 속도 | 7 u/s | Tuning.asset `throwSpeed` |

## 스태미나

| 파라미터 | 값 | 출처 |
|---|---|---|
| 최대치 | 100 | Tuning.asset `staminaMax` |
| 걷기 드레인 | 2.5/s | Tuning.asset `staminaDrainPerSecond` |
| 달리기 드레인 | 6/s | Tuning.asset `staminaDrainRunPerSecond` |
| 상자 무게 가중 | +0.35/kg·s | Tuning.asset `staminaDrainPerKg` |
| 언덕(Hillside) 가중 | ×1.4 | PlayerStatusManager (S-049) |
| 폭염·한파 가중 | ×1.35 | PlayerStatusManager (S-060) |
| 정지 회복 | 6/s | Tuning.asset `staminaRecoverPerSecond` |
| 드링크 회복 | 40 (+날씨 맞으면 ×1.5) | Tuning.asset `energyDrinkRecover` · S-060 |

## 경제

| 파라미터 | 값 | 출처 |
|---|---|---|
| 시작 자금 / 시작 빚 | ₩0 / ₩10,000 | GameState.asset `startMoney`/`startDebt` |
| 배송 보상 | ₩900 + (serial%4)×400 = 900~2,100 | CampOrderBoard `GenerateOrder` |
| 지각·미배달 벌금 | ₩300/건 | Tuning.asset `latePenalty` |
| 병원비 (차 사고) | ₩3,000 | WorldDeliveryManager `HOSPITAL_FEE` (S-057) |
| 심부름 보상 | 빌라촌 ₩1,500 · 아파트 ₩1,200 · 언덕 ₩2,500 | 각 씬 빌더 (S-052) |
| 상점 — 구루마 | ₩8,000 (1회 보유) | PhoneView `ShopItems` (S-056) |
| 상점 — 에너지드링크 | ₩1,500 | 〃 |
| 상점 — 고양이 사료/장난감/캣타워 | ₩2,000 / ₩3,000 / ₩10,000 | 〃 |

## 진행·숙련도

| 파라미터 | 값 | 출처 |
|---|---|---|
| 숙련도: 배송 성공 | +12 | MasteryProgress `SUCCESS_GAIN` (S-063) |
| 숙련도: 배송 실패 | −6 | MasteryProgress `FAIL_LOSS` |
| 숙련도: 주행 | 50m당 +1 | MasteryProgress `RUN_METERS_PER_POINT` |
| 레벨 상한 공식 | 100 + 25×(Lv−1) | MasteryProgress `MaxFor` |
| 두 개 들기 습득 | 누적 배송 성공 5건 | PlayerStatusManager `CanDoubleCarry` (S-055) |
| 구역 개척 조건 | 최전선 구역 배송 성공 1건(정산 시) | WorldDeliveryManager `AdvanceProgression` (S-054) |
| 트럭 수령 | 언덕주택가(마지막)까지 개척 후 성공 정산 | 〃 |

## 시간

| 파라미터 | 값 | 출처 |
|---|---|---|
| 도보 엣지 이동 | 40 게임분 | DistrictEdgeGate `_walkMinutes` (S-054b) |
| 트럭 지도 이동 | 근거리 30분 · 원거리 90분 | Tuning.asset `travelNearMinutes/FarMinutes` |
| 엘베 호출 대기 / 층당 이동 | 8분 / 3분 | Tuning.asset `elevatorWait/RideMinutesPerFloor` |
| 캠프 주문 최소 여유 | 마감 120분 전 소멸 | CampOrderBoard `MIN_SLACK_MINUTES` |
| 먹자골목 특칙 | 19시 마감 | CampOrderBoard (S-035) |

## 고양이 (S-059)

| 파라미터 | 값 | 출처 |
|---|---|---|
| 도망 조건 | 마지막 급여 후 1일 초과 | HomeCat |

---
### 이력
- v1 (2026-07-28) — 최초 집계 (S-065까지 반영). 심사 전 밸런스 패스의 기준표.
