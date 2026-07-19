# 남은 작업

기준: 커밋 `f2ff808` (2026-07-19) 이후. 매니페스트 34개 중 **25개 완료**, 9개 미착수.

---

## 0. 먼저 풀어야 할 결정 (블로커)

구현이 막혀 있는 것들. 사람 판단이 필요하다.

### B-1. 임포트 규칙 충돌 — `ArtImportPostprocessor` 착수 불가 🔴
| 위치 | 내용 |
|---|---|
| 아키텍처 §7-1 (L119) | "명명 규칙이 곧 계약: `bd_*` `pr_*` `ch_*` `bg_*` `ui_*` `po_*`" |
| 아키텍처 §9 (L144) | "⭐ 폴더 경로 = 임포트 규칙 트리거 (**접두어 폐지**)" |

두 규칙이 정면 충돌. §7-1이 v3.2 이전 잔재로 보인다. §7-1의 L120도 "접두어/폴더 감지"로 양다리를 걸고 있다.
**필요**: 폴더 트리거로 통일할지 확정 → 그 후 P1~2 임포터 착수 가능.

### B-2. 카메라 밀도 앵커 — 카메라 리그 발주 불가 🟠
§2가 "주 레인에서 1아트픽셀 ≈ 3~4스크린픽셀"이라 규정하지만 **기준 해상도가 없다**. 1080p인지 1440p인지 모르면 카메라 거리를 계산할 수 없다.
현재 그레이박스는 임의값(FOV 22°, 거리 41u)으로 세워둔 상태.
**필요**: 타겟 해상도 1줄 확정.

### B-3. 점프 유지 여부 (§8-6)
Z 레인 이동과 점프가 조작상 충돌하는지 그레이박스에서 판정하기로 돼 있다. 현재 코드는 점프를 넣어둔 상태.
**필요**: Director 판정. 빼기로 하면 `PlayerLocomotionManager`에서 제거.

### B-4. 캐릭터 제작 도구·리그 방식 (§8-8, §8-9)
MagicaVoxel vs Meshy / Mixamo 휴머노이드 vs 제네릭. 결정 대기 중.
`PlayerAnimationManager`가 기대하는 파라미터는 이미 고정: `Speed`, `IsCarrying`, `IsGrounded`.

---

## 1. P2 잔여 — 사람 작업 (남규)

코드는 납품 완료. 씬·프리팹 결합만 남았다.

- [ ] Core 씬 생성 + `World*Manager` 배치 + `GameState`/`Tuning` 에셋 연결
- [ ] 플레이어 프리팹 (`PlayerManager` 계열 부착, `InteractionSensor`는 자식)
- [ ] Animator 컨트롤러 + 파라미터 3종
- [ ] `FadeScreen` UI (`CanvasGroup` + "늦지마!" 컷인)
- [ ] 씬 5종을 빌드 세팅에 등록 (Main/Home/Camp/Travel/District)
- [ ] `CategoryPrefabFactory` (P2, 남규) — B-1 해결 후

**Play 중 Unity 창 포커스가 없으면 프레임이 멈춘다.** 자동 검증할 거면 Run In Background를 켤 것.

---

## 2. P3 — 양산 (스크립트 8개)

| 파일 | 책임 | 선행 조건 |
|---|---|---|
| `WorldDebtManager` | 정산·빚 게이지 (Camp) | — |
| `WorldDayNightManager` **조명부** | Directional·포인트·LUT 구동 | 시계 진행부는 이미 있음. 조명 리그 필요 |
| `WorldDialogueManager` | 시나리오 SO 재생·초상 전환 | `DialogueScenarioSO` 선행 |
| `DialogueScenarioSO` | 대사 라인 배열 (박말순) | LLM 배치 생성물 스키마 확정 |
| `WorldMinigameManager` | 전화 → 리듬 오버레이 구동 | — |
| `HUDView` | 시계·마감·스태미나·빚 표시 | 구독할 이벤트는 이미 전부 있음 |
| `DialogueView` | 대화 박스·초상 | — |
| `MinigameRhythmView` | 방향키 리듬 오버레이 | — |
| `TravelMapView` | 노드 선택·시간 소모 | — |
| `LoadingZone` | Camp 짐싣기 | — |
| `EnergyDrinkPickup` | 드링크 아이템 | `PlayerStatusManager.RecoverStamina` 이미 있음 |
| `ArtAuditReport` | 팔레트·치수 이탈 경고 (남규) | B-1 |

### P3 착수 시 참고
- 이벤트는 이미 다 뚫려 있다: `StaminaChanged`, `CarryStateChanged`, `ClockTicked`, `DayPhaseChanged`, `OrderAccepted`, `PackagePickedUp`, `DeliveryCompleted`, `DeliveryFailed`, `DeadlineWarned`, `SceneTransitionStarted/Completed`
- `PhoneCall` / `MinigameResult` 페이로드 struct는 이미 정의돼 있다 (`EventPayloads.cs`)
- 전화 → 대화 → 미니게임 체인 이벤트(`PhoneRang`, `MinigameRequested`, `MinigameEnded`)는 **아직 없다**. Minigame 착수 시 `WorldEvents`에 추가.

---

## 3. P4 — 분위기

- `WorldAudioManager` — BGM/SFX 재생·믹스
- `WorldJuiceManager` — 이벤트→연출 매핑
- `PlayerEffectsManager` — 로컬 이펙트 (먼지·드링크). **P4 전 생성 금지**

---

## 4. 문서 정합성 정리 (선택)

`.claude/rules/`는 이번 작업에서 **수정하지 않았다**. 아래는 발견된 불일치로, 다음 개정 때 반영 후보.

| # | 위치 | 내용 |
|---|---|---|
| 1 | §5 표 (L64) | `Stamina` 매니저 행 — 실제 담당은 `PlayerStatusManager`. World 목록·매니페스트에 없음 → 삭제 대상 |
| 2 | §6 (L105) | `SideController` — 매니페스트에 없는 유령 클래스. 실제는 `PlayerLocomotionManager` |
| 3 | §5 표 / §4 (L49-50) | 짧은 이름(`GameState`/`SceneFlow`)이 §5.5 풀명 규약과 불일치 |
| 4 | §5.5 (L80-81) | World 싱글톤 목록에 `WorldAudioManager`·`WorldJuiceManager` 누락 |
| 5 | §7 (L114, L119) | 캐릭터 "시트" 표현 잔존 — v3.3에서 3D 모델로 교체됨 |
| 6 | §8 (L128-136) | 항목 번호 순서 깨짐 (`1,2,3,4,5,6,8,9,7`) |
| 7 | §8-5 (L132) | "Unity MCP 6.5 호환" — 실제 도구는 `unity-cli` connector, 6000.5.3f1에서 동작 확인됨. 닫아도 되는 항목 |
| 8 | §5.7 (L96) | "스프라이트 시트 → Animator" 화살표가 대체인지 변환인지 모호 |

**해결된 것** (문서 수정 없이 코드로 회피):
- 시계 소유권 → `GameStateSO` 단일 소유
- `WalkableVolume` 누락 → 규격대로 신규 생성

---

## 5. 실무 리스크 (미착수, 터지기 전에)

- **URP 라이트 제한**: 밤 가로등 다수 → Forward 렌더러의 오브젝트당 추가 라이트 상한에 걸려 팝핑 가능. **Forward+ 사용**을 렌더러 에셋에 못박는 게 싸다 (URP 17 기본 지원)
- **Core 씬 부트 순서**: 개발 중 `District` 씬을 에디터에서 직접 Play하면 매니저가 전무해 NullReference. `CoreBootstrap`에 "Core 미로드 시 자가 Additive 로드"를 넣을지 결정 필요
- **간판 발광** (§8-7): 밤 전용 이미시브는 베이스+이미시브 2레이어 에셋 규칙이 필요
