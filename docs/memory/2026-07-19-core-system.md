# 2026-07-19 — 코어 시스템 구축 (P1 + P2 코드계층)

커밋 `f2ff808` / 브랜치 `feature/jjs` / 85파일 3281줄

---

## 1. 무엇을 만들었나

매니페스트 P1 전량 + P2 중 **코드만으로 완결되는 것**. 씬·프리팹은 손대지 않음(남규 독점).

| 폴더 | 파일 |
|---|---|
| `Events/` | WorldEvents(정적 허브), EventPayloads |
| `SO/` | GameStateSO, DeliveryOrderSO, TuningConfigSO |
| `Managers/` | CoreBootstrap, WorldSceneFlowManager, WorldDeliveryManager, WorldDeadlineManager, WorldDayNightManager(시계 진행부만) |
| `Player/` | PlayerManager(허브), PlayerInputHandler, PlayerLocomotionManager, PlayerAnimationManager, PlayerStatusManager, InteractionSensor |
| `Interactables/` | IInteractable, PlayerContext, WalkableVolume, PickupBox, DeliveryPoint |
| `UI/` | FadeScreen |
| `Utils/` | GameTimer, SpriteTween |
| `Editor/` | GreyboxStageBuilder (개발 도구, 빌드 제외) |
| `Data/` | GameState / Tuning / Order_HappyVilla 에셋 + 그레이박스 머티리얼 7종 |

**제외한 것**: `ArtImportPostprocessor`, `CategoryPrefabFactory` — 임포트 규칙이 문서상 충돌(§8-①)이라 보류.

---

## 2. 설계 결정 (문서 무수정, 모순 회피)

### ① 시각(Clock) 단일 소유 = `GameStateSO`
문서 §5는 "DayNight와 Deadline이 동일 시계 공유"라고만 하고 메커니즘이 없었다.
둘 다 World 매니저라 통신 2층 규칙상 이벤트만 허용되는데, 시각은 매 프레임 진행 = "프레임 데이터 이벤트 금지"에 걸려 **구현 경로가 없는 상태**였다.

```
GameStateSO { day, minuteOfDay }
     ↑ write                    ↓ read
WorldDayNightManager      WorldDeadlineManager
     └─→ WorldEvents.ClockTicked (분 경계에서만)
```

- 매니저 간 직접 참조·폴링 0
- 프레임 데이터가 이벤트로 흐르지 않음
- §5 "GameState = 날짜·시각 보관"과 정합 → **문서 수정 불필요**

### ② `WalkableVolume` 신규 생성
아키텍처 §1에 규격이 명시돼 있으나 스크립트 매니페스트에 파일이 누락돼 있었다. 발주 범위 안이라 판단해 생성.
`BoxCollider` 트리거 → `PlayerLocomotionManager`가 진입으로 수집 → 목표 Z를 클램프. 볼륨이 하나도 없으면 제한하지 않는다(그레이박스 편의).

### ③ `WorldDayNightManager`는 시계 진행부만
P3·남규 담당이지만, 시계 진행 주체가 없으면 `WorldDeadlineManager`가 죽은 코드가 된다. 조명·LUT·컬러그레이딩은 **손대지 않음**.

### ④ 입력 = 코드 내 `InputAction` 정의
`.inputactions` 에셋을 만들지 않아 남규 작업 0, 즉시 동작. 리바인딩이 필요해지면 에셋으로 승격.
바인딩: WASD/화살표/좌스틱, Space/buttonSouth(점프), E/buttonWest(상호작용).

### ⑤ 하이라이트 베이스 클래스 만들지 않음
`PickupBox`/`DeliveryPoint`가 머티리얼 스왑을 각자 구현(각 5줄). 매니페스트에 없는 파일을 만들지 않는 쪽을 택했다.

### ⑥ `DeliveryPoint` 하이라이트는 2갈래 OR
근접 포커스(`SetHighlight`)와 목적지 표시(`PackagePickedUp` 구독) 중 **하나라도 켜지면 켠다**. 근접 해제가 목적지 표시를 꺼버리는 버그 방지.

---

## 3. 검증 결과 (셀프 검증 3종)

① 컴파일 통과 ② 콘솔 에러·워닝 0 ③ Play 모드 동작 확인

**사람(Director) 확인분**
- 보도(WalkableVolume) 밖으로 Z 이동이 밀리지 않음
- 상자 근접 시 시안 발광 → `E` 픽업 → 캐리 중 감속 → 문 앞 `E` 인증
- `money 0→5000`, `cargo 1→0`, `completedCount 1`

**자동 검증분**
- 9:42 `DeadlineWarned #7` (경고 임계 30분) → 중복 발행 없음
- 10:00 `DeliveryFailed #7` → 적재 자동 제거(`cargo=0`), `lateCount=1`
- 스택트레이스로 체인 확인:
  `DayNight.Update → ClockTicked → Deadline.OnClockTicked → DeadlineWarned/DeliveryFailed`

---

## 4. 검증 중 배운 것 (재발 방지)

### ⚠ 에디터 모드에선 `Awake`/`OnEnable`이 호출되지 않는다
`AddComponent`로 매니저를 조립해도 **이벤트 구독이 성립하지 않는다**. 직접 메서드 호출(`AcceptOrder`)만 동작해서, 처음엔 Deadline 로직이 고장난 줄 알았다. **이벤트 배선 검증은 반드시 Play 모드에서.**

### ⚠ Unity 창에 포커스가 없으면 Play 모드 프레임이 멈춘다
`Time.frameCount`가 1에 머물러 시계가 안 도는 것처럼 보였다. 코드 문제가 아니다.
자동 검증할 때는 `Application.runInBackground = true`를 먼저 넣을 것 (또는 Project Settings의 Run In Background).

### ⚠ `GameObject.Find`는 비활성 오브젝트를 못 찾는다
픽업된 상자는 `SetActive(false)` 상태 → `Find`가 null 반환.
`FindObjectsByType<T>(FindObjectsInactive.Include)` 사용. (게임 코드에선 어차피 검색 API 금지)

### ⚠ Unity 6.5에서 `FindObjectsSortMode` 오버로드는 폐기됨
`FindObjectsByType<T>(FindObjectsInactive)` 형태를 쓸 것. 안 그러면 CS0618 워닝.

### ⚠ `[Header]` 뒤에 XML doc 주석(`///`)을 두지 말 것
attribute와 필드 사이에 오면 경고 대상. 일반 주석(`//`)으로 쓴다.

---

## 5. 그레이박스 무대 공유 방식

씬 파일을 만들지 않고 **에디터 메뉴로 재현**하는 쪽을 택했다.

- 메뉴 `DontLate/Build Greybox Stage` → 현재 씬에 무대 조립 (멱등: 다시 누르면 지우고 새로)
- 메뉴 `DontLate/Clear Greybox Stage` → 제거
- 생성물은 전부 `__gb_` 접두어
- SO 참조는 `SerializedObject`로 주입
- 씬 저장 불필요 → **병합 충돌 0, 남규 영역 미침범**
- 코드가 바뀌어도 무대가 낡지 않는다(항상 최신 코드로 조립)

무대 구성: Ground(120×80) / Lane(보도 z −3~+3) / WalkableVolume / Box(x=−5) / Door(x=+6, z=+2.6) / Player(CharacterController + 허브 계열) / Managers 3종 / 카메라(FOV 22°, 하향 10°, 거리 41u)

---

## 6. 사람 통합 필요 (남규)

- Core 씬에 `World*Manager` 배치 + `GameState`/`Tuning` 에셋 연결
- 플레이어 프리팹에 `PlayerManager` 계열 부착 (`InteractionSensor`는 **자식 오브젝트**)
- Animator 파라미터 3종: `Speed`(float), `IsCarrying`(bool), `IsGrounded`(bool)
- `FadeScreen`: `CanvasGroup` + "늦지마!" 컷인 오브젝트 연결
- 씬 5종(Core/Main/Home/Camp/Travel/District)을 빌드 세팅에 등록
  — 없으면 `WorldSceneFlowManager`가 경고만 남기고 상태만 전이한다
