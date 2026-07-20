# 늦지마 — 코드 규칙 (CODE_RULES v1)
## 이 문서만 읽으면 규칙에 맞는 코드를 쓸 수 있다

> 문서 3형제: **ARCHITECTURE.md**(왜·구조) · **SCRIPTS.md**(무엇을 만들지·순서) · **이 문서**(어떻게 쓸지).
> 발주서(작업 지시)에 없는 판단이 필요하면 구현하지 말고 물어볼 것 — 추측 구현이 재작업의 근원.

---

## 1. 네이밍

| 대상 | 규칙 | 예 |
|---|---|---|
| World 싱글톤 | `World`+기능+`Manager` | WorldDeliveryManager, WorldDayNightManager |
| Player 서브 | `Player`+기능+`Manager` | PlayerLocomotionManager |
| 입력 | ⚠ `PlayerInputHandler` — **PlayerInputManager 금지** (Unity Input System에 동명 클래스 실존) | |
| UI | 기능+`View` (로직 없음) | HUDView, DialogueView |
| SO 클래스 | 기능+`SO` | GameStateSO, DeliveryOrderSO |
| 이벤트 | **과거형** 동사 | DeliveryCompleted, PhoneRang, MinigameEnded |
| 필드 | private `_camelCase` / 프로퍼티 `PascalCase` / 상수 `UPPER_SNAKE` | `_moveSpeed`, `IsCarrying` |
| 파일 | **1클래스 1파일**, 파일명=클래스명 | |

## 2. 폴더 배치 (요약 — 상세는 SCRIPTS.md §1)

`Events/` 이벤트 허브·페이로드 · `SO/` SO 클래스 정의(에셋 인스턴스는 Data/) · `Managers/` World 싱글톤 ·
`Player/` 허브+서브 · `Interactables/` IInteractable 구현체 · `UI/` View(표시만) · `Utils/` 헬퍼 ·
`Editor/Importer/` 에디터 전용(빌드 제외 — **Editor 폴더 밖에 에디터 코드 두면 빌드 깨짐**)

## 3. 통신 규칙 (2층) — 이 프로젝트의 헌법

```
도메인 내부 (Player.*)         : 허브(PlayerManager) 경유 직접 참조 OK  ← 프레임 단위 데이터
도메인 경계 (Player ↔ World.*) : WorldEvents 이벤트만                  ← 상태 변화 통지
World 매니저 간                : WorldEvents 이벤트만
```

### 3.1 WorldEvents — 정의·발행·구독
```csharp
// Events/WorldEvents.cs
public static class WorldEvents {
    public static event Action<DeliveryData> DeliveryCompleted;
    public static void RaiseDeliveryCompleted(DeliveryData d) => DeliveryCompleted?.Invoke(d);
}

// 구독자 — OnEnable/OnDisable 짝 맞춤 필수 (안 지키면 씬 전환 시 유령 구독 버그)
void OnEnable()  { WorldEvents.DeliveryCompleted += OnDelivered; }
void OnDisable() { WorldEvents.DeliveryCompleted -= OnDelivered; }
```

- 실수→규칙 (2026-07-20): InputAction을 새로 추가하면 **생성·Enable·Disable·Dispose 4곳을 같은 편집에서** 맞춘다 — run 액션에서 Enable 누락 실제 발생 (컴파일은 통과하나 입력이 영원히 죽는 무증상 버그).

### 3.2 도메인 허브 — Player 내부
```csharp
public class PlayerManager : MonoBehaviour {
    public PlayerInputHandler Input { get; private set; }
    public PlayerLocomotionManager Locomotion { get; private set; }
    void Awake() { Input = GetComponent<PlayerInputHandler>(); /* ... */ }
}
// 서브매니저는 허브로만 형제를 본다:
public class PlayerLocomotionManager : MonoBehaviour {
    PlayerManager _hub;
    void Awake() => _hub = GetComponent<PlayerManager>();
    void Update() { var move = _hub.Input.MoveVector; /* X/Z 이동 */ }
}
```

### 3.3 금지
- `FindObjectOfType` · `GameObject.Find` · 태그 검색 — 전면 금지
- 경계 너머 직접 참조 (예: PlayerStatusManager가 WorldDebtManager.Instance의 상태를 폴링) — 이벤트로
- 프레임 데이터를 이벤트로 흘리기 (입력→이동은 허브 몫)

## 4. World 싱글톤 규약

```csharp
public class WorldDeliveryManager : MonoBehaviour {
    public static WorldDeliveryManager Instance { get; private set; }
    void Awake() {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;   // Core 씬 상주 — DontDestroyOnLoad 쓰지 않는다
    }
}
```
- **Core 씬에만 존재**. `Instance`는 **명령 호출용**(`WorldDeliveryManager.Instance.AcceptOrder(o)`),
  상태 변화를 알고 싶으면 폴링 말고 **이벤트 구독**.

## 5. ScriptableObject 규칙

```csharp
[CreateAssetMenu(menuName = "DontLate/DeliveryOrder")]
public class DeliveryOrderSO : ScriptableObject {
    public string address; public int floor; public float deadline; public int reward;
}
```
- 클래스는 `Scripts/SO/`, 에셋 인스턴스는 `Data/`에 생성.
- **GameStateSO는 데이터만** — 로직(계산·판정) 넣지 말 것. 로직은 매니저 몫.

## 6. Unity 작업 경계 (⚠ 제일 중요한 협업 규칙)

- **커밋 가능**: `Scripts/` 아래 .cs 전부, SO 클래스. **커밋 금지**: 씬(.unity)·프리팹(.prefab)·
  프로젝트 설정 — **씬·프리팹은 남규 독점**(병합 지옥 방지). 프리팹에 컴포넌트 부착이 필요하면
  "어느 프리팹에 뭘 붙일지"를 납품 보고에 적으면 남규가 통합.
- Inspector 노출은 `[SerializeField] private` — public 필드 금지.
- 문·엘베 등 물체 개폐 = **코드 트윈** (애니메이션 클립 금지). 수치는 발주서에 명시됨.
- `IInteractable` 시그니처는 **동결** — 변경 필요하면 구현하지 말고 남규에게.

```csharp
public interface IInteractable {
    void Interact(PlayerContext ctx);
    void SetHighlight(bool on);
}
```

- 실수→규칙 (2026-07-20, scn_core 발주): 에디터 빌더에서 **새로 AddComponent한 컴포넌트에
  SerializedObject로 에셋 참조를 주입하고 SaveScene 하면 `{fileID: 0}`으로 유실**될 수 있다
  (enum 등 값 타입은 살고 오브젝트 참조만 죽음). 씬을 저장하는 빌더는 리플렉션 직접 주입을 쓰고,
  저장된 씬 YAML에서 guid 존재를 검증한다.

- 실수→규칙 (2026-07-20, 임포터 발주): ① 백그라운드/HTTP 구동 에디터에선 `EditorApplication.delayCall`이
  발화하지 않을 수 있다 — 임포트 후처리는 `OnPostprocessAllAssets`에서 직접 처리 (생성물이 계약경로 밖이면
  재귀 임포트 없음). ② 팩토리 프리팹을 `PrefabUtility.InstantiatePrefab`로 만들면 소스의 **Variant**로 결합되어
  소스 삭제 시 Missing parent 에러 — 독립 프리팹은 `Object.Instantiate` 클론으로 만든다.

## 7. 과잉 방지 (YAGNI)

- 발주서에 없는 기능·옵션·추상화·예외처리를 **덧붙이지 않는다.**
- 일어날 수 없는 상황의 방어 코드 금지. 검증은 시스템 경계(입력·외부 데이터)에서만.
- "나중에 쓸 것 같은" 베이스 클래스·인터페이스 선제작 금지 — 두 번째 사용처가 실제로 생기면 추출.

## 8. Claude Code 사용 규칙 (AI 공정)

- 발주서(목표·입력·기대·수용기준·실패시)를 그대로 프롬프트에 — 모호하면 **구현 전에 되묻기**.
- 생성 후 **셀프 검증 3종을 직접**: ① 컴파일 통과 ② 콘솔 에러·워닝 0 ③ Play모드에서 기대 동작 확인.
- 통과 못 한 코드는 고칠 것 — **검증 조건을 완화하거나 기대값을 하드코딩해서 통과시키는 것 금지**
  (막히면 위장 말고 [BLOCKED]).

## 9. 납품 규칙

- 납품 = 셀프 검증 3종 통과 → git push → 보고.
- **보고는 관찰로**: "완료했습니다" ✗ → "E키 입력 시 문이 0.4초에 슬라이드되는 것 확인" ○
  (프리팹 부착 필요사항 있으면 여기 명시)
- 커밋 메시지: `[P2] PlayerLocomotionManager: Z레인 이동+캐리 페널티 (via ClaudeCode) [self-tested]`
- 컴파일 실패·콘솔 에러 상태로 push 금지.

## 9.5 이벤트 콘솔 로깅 (관측 규칙)

> 목적: 코어루프가 "왜 안 도는지"를 콘솔만 보고 판정한다. 로그가 없으면 매번 exec로 상태를
> 찍어봐야 하고, 그게 검증 시간의 대부분을 먹는다.

- **로깅 지점은 `WorldEvents`의 `Raise*` 헬퍼 한 곳뿐.** 경계 이벤트는 전부 여기를 지나므로
  단일 길목이다. 매니저·뷰에 개별 `Debug.Log`를 뿌리지 않는다(중복·누락·정리 지옥).
- **에디터·개발빌드 전용**: `[Conditional("UNITY_EDITOR")]` + `[Conditional("DEVELOPMENT_BUILD")]`.
  릴리스 빌드에는 호출 자체가 사라진다 — WebGL 용량·성능에 0 비용.
- 형식: `[EVENT] <이벤트명> <핵심 필드>` · 접두어는 시안 `#35e0c8`(상호작용 색과 통일)로 컬러태그.
  콘솔 검색창에 `[EVENT]`를 치면 코어루프 흐름만 걸러진다.

### 로깅 대상 — 저빈도 "상태 변화 통지"만
`OrderAccepted` · `PackagePickedUp` · `DeliveryCompleted` · `DeliveryFailed` · `DeadlineWarned` ·
`CarryStateChanged` · `DayPhaseChanged` · `SceneTransitionStarted` · `SceneTransitionCompleted`

### 로깅 금지 — 고빈도
`ClockTicked`(게임 분마다 = 현 튜닝 초당 2회) · `StaminaChanged`(연속값, 5% 스텝이어도 잦다).
**고빈도 이벤트를 로그에 올리면 콘솔이 쓰레기통이 되어 규칙 전체가 무력화된다.**
이 둘의 상태가 필요하면 로그가 아니라 HUD나 `unity-cli exec`로 본다.

- 새 이벤트를 `WorldEvents`에 추가할 땐 **저빈도면 로그를 함께 단다.** 고빈도면 위 금지 목록에 이름을 올린다.

## 10. 막히면

```
[BLOCKED] 막힌 것 / 시도한 것 / 필요한 것(결정·정보·연결) / 긴급도
```
30분 이상 혼자 붙잡지 말 것 — 막힘 보고는 실력 문제가 아니라 공정 신호다.
