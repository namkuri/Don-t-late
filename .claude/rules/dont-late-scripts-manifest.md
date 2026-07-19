# 늦지마 — Scripts 매니페스트 v1
## 폴더 역할 정의 + 생성할 스크립트 전량 (발주 단위)

> 시점 태그: **P1**=D4~5 저녁봉투 · **P2**=D6~7 주말스프린트① (그레이박스 루프 목표) ·
> **P3**=D8~12 양산 · **P4**=D13~14 (M3 분위기). 담당: 정수=게임플레이 / 남규=에디터도구·통합.

## 1. Scripts/ 폴더 역할

| 폴더 | 역할 | 들어가는 것 |
|---|---|---|
| `Events/` | 도메인 경계 통신 | WorldEvents 정적 허브 + 이벤트 페이로드 struct |
| `SO/` | ScriptableObject **정의**(클래스) | GameState·시나리오·배송건·튜닝 SO 스크립트 (에셋 인스턴스는 Data/에) |
| `Managers/` | World 싱글톤 (Core 씬 상주) | World*Manager 전부 |
| `Player/` | 플레이어 도메인 (허브+서브) | PlayerManager와 서브매니저·상호작용 센서 |
| `Interactables/` | IInteractable 구현체 | 상자·문·적재존·드링크 등 월드 오브젝트 |
| `UI/` | 뷰 계층 (로직 없음 — 이벤트 구독·표시만) | HUD·대화창·미니게임·이동맵·페이드 |
| `Utils/` | 공용 헬퍼 | 트윈 헬퍼·타이머 등 |
| `Editor/Importer/` | **에디터 전용** (빌드 제외 — Unity 규칙) | 아트 자동 임포트·프리팹 팩토리·검수 리포트 |

## 2. 스크립트 전량

### Events/ 
| 파일 | 책임 | 시점 | 담당 |
|---|---|---|---|
| WorldEvents.cs | 정적 이벤트 허브 (event + Raise 헬퍼) | **P1** | 정수 |
| EventPayloads.cs | DeliveryData·PhoneCall·MinigameResult·DayPhase 등 struct | **P1** | 정수 |

### SO/
| 파일 | 책임 | 시점 | 담당 |
|---|---|---|---|
| GameStateSO.cs | 전역 상태: 시각·돈/빚·적재목록·진행·지각수 (로직 없음) | **P1** | 정수 |
| DeliveryOrderSO.cs | 배송 건: 주소·층·마감·보상·메모 참조 | **P2** | 정수 |
| DialogueScenarioSO.cs | 대사 라인 배열(화자·초상·텍스트·다음) — 박말순 | **P3** | 정수 |
| TuningConfigSO.cs | 이동·스태미나·마감 등 튜닝값 노출 (M3 조정용) | **P2** | 정수 |

### Managers/ (World 싱글톤)
| 파일 | 책임 | 시점 | 담당 |
|---|---|---|---|
| CoreBootstrap.cs | Core 씬 초기화 순서·매니저 기동 | **P2** | 정수 |
| WorldSceneFlowManager.cs | 씬 상태기계(Main→Home→Camp→Travel→District)+페이드 | **P2** | 정수 |
| WorldDeliveryManager.cs | 배송 수명주기(픽업→운반→인증→완료) — 코어 심장 | **P2** | 정수 |
| WorldDeadlineManager.cs | "늦지마" 마감·지각 판정 (DayNight와 시계 공유) | **P2** | 정수 |
| WorldDebtManager.cs | 정산·빚 게이지 (Camp) | **P3** | 정수 |
| WorldDayNightManager.cs | 시간 진행 → Directional·포인트·LUT 구동 | **P3** | 남규* |
| WorldDialogueManager.cs | 시나리오 SO 재생·입력 대기·초상 전환 | **P3** | 정수 |
| WorldMinigameManager.cs | 전화→리듬 오버레이 구동·결과 이벤트 | **P3** | 정수 |
| WorldAudioManager.cs | BGM/SFX 재생·믹스 | **P4** | 정수 |
| WorldJuiceManager.cs | 이벤트→연출 매핑 (JUICE.md 표 시공) | **P4** | 정수 |

*DayNight는 조명·룩 튜닝과 한 몸이라 통합 담당(남규)이 Claude Code로 — 경계는 협의.

### Player/
| 파일 | 책임 | 시점 | 담당 |
|---|---|---|---|
| PlayerManager.cs | 도메인 허브 — 서브매니저 소유·연결 | **P2** | 정수 |
| PlayerInputHandler.cs | Input System 읽기 (⚠ PlayerInputManager 명명 금지) | **P1** | 정수 |
| PlayerLocomotionManager.cs | X/Z 이동·WalkableVolume 제한·점프·캐리 페널티·**facing 회전** | **P2** | 정수 |
| PlayerAnimationManager.cs | **Animator 파라미터 구동·이동방향 회전(45° 스냅 옵션)** — v3.3 교체 | **P2** | 정수 |
| PlayerStatusManager.cs | 스태미나·캐리 상태·에너지드링크 효과 | **P2** | 정수 |
| InteractionSensor.cs | 근접 감지·SetHighlight 관리·상호작용 실행 | **P2** | 정수 |
| PlayerEffectsManager.cs | 로컬 이펙트 (먼지·드링크) — **P4 전 생성 금지** | P4 | 정수 |

### Interactables/
| 파일 | 책임 | 시점 | 담당 |
|---|---|---|---|
| IInteractable.cs + PlayerContext.cs | 계약 인터페이스 (**동결** — 시그니처 변경=사람 게이트) | **P1** | 정수 |
| PickupBox.cs | 택배상자 픽업 | **P2** | 정수 |
| DeliveryPoint.cs | 문앞 인증·목적지 하이라이트 | **P2** | 정수 |
| LoadingZone.cs | Camp 짐싣기 | **P3** | 정수 |
| EnergyDrinkPickup.cs | 드링크 아이템 | **P3** | 정수 |
| (보류) ElevatorPanel.cs | 건물 내부·엘베 스코프 확정 후 | 미정 | — |

### UI/ (뷰 = 이벤트 구독·표시만, 게임 로직 금지)
| 파일 | 책임 | 시점 | 담당 |
|---|---|---|---|
| HUDView.cs | 시계·마감·스태미나·빚 표시 | **P3** | 정수 |
| DialogueView.cs | 대화 박스·초상 (스크린샷의 포켓몬식) | **P3** | 정수 |
| MinigameRhythmView.cs | 방향키 리듬 오버레이 | **P3** | 정수 |
| TravelMapView.cs | 이동(미니맵) 씬 UI — 노드 선택·시간 소모 | **P3** | 정수 |
| FadeScreen.cs | 전환 페이드·"늦지마!" 컷인 | **P2** | 정수 |

### Utils/
| 파일 | 책임 | 시점 | 담당 |
|---|---|---|---|
| SpriteTween.cs | 문 개폐 등 코드 트윈 헬퍼 (또는 DOTween reuse+라이선스 기록) | **P2** | 정수 |
| GameTimer.cs | 공용 타이머 | **P2** | 정수 |

### Editor/Importer/ (에디터 전용 — 남규 소유)
| 파일 | 책임 | 시점 | 담당 |
|---|---|---|---|
| ArtImportPostprocessor.cs | Art/폴더 경로 감지 → PPU·Point·압축·피벗 자동 주입 | **P1~2** | 남규 |
| CategoryPrefabFactory.cs | Prefabs/Auto/ 프리팹 생성·갱신 (멱등·Hand 불가침) | **P2** | 남규 |
| ArtAuditReport.cs | 팔레트·치수 이탈 경고 리포트 (채점기 — 차단 아님) | **P3** | 남규 |

## 3. 생성 순서 요약 (의존 위상)
```
P1: WorldEvents·Payloads → IInteractable → GameStateSO → PlayerInputHandler → ArtImportPostprocessor
P2: PlayerManager 허브+서브 3종 → SceneFlow → Delivery+Deadline → PickupBox·DeliveryPoint
    → FadeScreen → PrefabFactory  ⇒ 목표: 그레이박스 배송 1건 완주 (D7)
P3: Debt·DayNight·Dialogue·Minigame → UI 뷰 4종 → LoadingZone·Drink → AuditReport
P4: Audio·Juice·PlayerEffects
```
합계 **34개** (보류 1 제외). P1은 전부 저녁 봉투 크기(각 1~2h)로 설계됨.
