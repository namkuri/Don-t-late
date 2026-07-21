# 늦지마 (Don't Late) — 아키텍처 v5 (3D×픽셀화)
## 일반 3D 모델 · 픽셀화 렌더(저해상+셰이더) · 2.5D 앵글 · Unity 6.5 · URP 3D

> 회의 결정(2.5D·사이드뷰+깊이 이동·낮밤·씬 5·아트 pull)을 기술 구조로 번역한 문서.
> 원칙 3(단일책임 매니저·이벤트 통신·조립>상속)은 유지 — 차원이 바뀌어도 원칙은 안 바뀐다.

---

## 1. 공간 모델 — "3D 무대, 픽셀 렌즈"

```
X = 진행 방향(거리)   Y = 수직(점프·층)   Z = 깊이(길 안쪽)
```
- **단위 1u = 1m 유지.** 캐릭터 키 1.8u — 3D 좌표계라 예전 3D 규격이 부활한다.
- **이동**: 사이드 X 자유 + **Z 제한 이동** — 걷기 가능 영역은 `WalkableVolume`(박스 트리거)로
  명시된 보도 구간만 (회의: "이동가능한 길거리 블록 한정"). Z 이동 속도 = X의 0.7배(대각 감).
- **에셋 배치**: 일반 3D 모델(AI 생성 중심) — 실지오메트리, 뎁스가 소팅 해결. 원경만 빌보드.
- **픽셀 일관성 = 렌더가 보장.** 월드 카메라가 저해상 타겟(480×270)에 그리므로 화면 전체가
  균일 픽셀 그리드 — 밀도 근사·복셀 그리드 같은 에셋 측 규칙이 불필요해졌다.

## 2. 카메라 — 픽셀 안정성과 깊이감의 거래

- **Perspective, FOV 20~25°(망원)**, 하향각 8~12°. 망원일수록 원근 왜곡이 줄어
  픽셀 크기 변화가 완만하다 — 깊이감은 살리고 도트는 지킨다.
- **렌더 구성**: 월드 카메라→480×270 렌더타겟→정수 업스케일 / UI 카메라→풀해상 오버레이(한글 선명).
- **타겟 해상도 = 1920×1080 고정** (D-003 확정 · 2026-07-20). 480×270의 **정수 4배** —
  밀도 앵커 "1아트픽셀 = 4스크린픽셀"이 여기서 확정된다. 카메라 거리·FOV는 이 값으로 역산.
- **픽셀화 파이프라인 3레벨 캡** (STYLE v5): L1 저해상+Point(필수) → L2 팔레트 양자화(목표) → L3 아웃라인·스냅(금지). 소유=남규(WorldDayNight와 한 몸).
- 카메라 리그: X 팔로우 + 데드존, Z·각도 고정. 줌 변경 금지(밀도 붕괴).

## 3. 렌더·조명 — 낮밤의 심장 (2.5D 최대 배당)

- **URP 3D 렌더러.** 머티리얼 = URP Lit(256 텍스처·Point·PBR 없음) — **실그림자 ON**, 저해상 렌더가 그림자·광원까지 픽셀화(공짜 감성).
- **DayNightManager**가 구동: Directional(태양/달 — 각도·색·강도 커브) + Ambient 그라디언트
  + 컬러그레이딩 LUT 2개(낮/밤) 블렌드. 밤엔 Directional 약화 + **포인트 라이트 부각**
  (가로등=앰버 #ff9f45, 간판·상호작용=시안 #35e0c8) + 약한 블룸.
- **왜 낮밤이 싸냐**: 에셋 0장 추가 — 같은 거리가 조명·LUT 전환만으로 두 얼굴을 갖는다.
  이게 2.5D 채택의 근거이며 영상 30~60초에서 낮→밤 전환 컷이 강력한 각인 후보다.

## 4. 씬 구조 & 상태 흐름

```
[Core (부트스트랩, Additive 상주)]  ← GameState·매니저·이벤트버스·오디오
     Main(인트로) → Home(집) → Camp(물류: 짐싣기·정산) → Travel(이동/미니맵) → District(배송지)
                      ↖──────────────── 하루 사이클 반복 ────────────────↙
```
- **Core 씬 상주 패턴**: 매니저·상태는 Core에 살고 콘텐츠 씬만 교체 로드 —
  씬 5개여도 상태 유실·중복 초기화 문제가 구조적으로 없다.
- `GameState` (ScriptableObject): 날짜·시각, 돈/빚, 적재 목록, 배송 진행, 지각 카운트.
- `SceneFlowManager`: 위 상태기계 전이 소유. 전환 연출(페이드·"늦지마!" 컷인) 포함.
- Travel 씬 = 미니맵 이동(주행 조작 없음): 노드 선택 → 시간 소모 계산 → District 로드.
  트럭은 연출 요소로 강등 (탑다운 주행 시스템은 폐기 — §8 충돌 목록 참조).

## 5. 매니저 층 (단일책임 · 이벤트 통신)

| 매니저 | 책임 | 비고 |
|---|---|---|
| GameState | 전역 상태 보관 (로직 없음) | SO + 저장 없음(세션제) |
| SceneFlow | 씬 전이·전환 연출 | 상태기계 |
| DayNight | 시간 진행·조명·LUT 구동 | **Deadline과 동일 시계 공유** |
| Deadline | "늦지마" 압박 — 배송 마감·지각 판정 | 구 Timer 개명 |
| Delivery | 픽업→운반→인증 수명주기 | 코어루프 심장 |
| Debt | 수익 정산·빚 게이지 | Camp 정산과 연동 |
| Stamina | 피로·에너지드링크 회복 | 아이템 훅 |
| **Dialogue** | 스피치 시나리오 재생·초상 표시·입력 대기 (박말순) | 시나리오=SO 데이터, LLM 배치 생성물 주입 |
| **Minigame** | 진상 전화 → 방향키 리듬 오버레이 | 씬 아님 — UI 오버레이 모듈, 결과를 이벤트로 방출 |
| Juice/Audio | 이벤트→연출·SFX | 기존 유지 |

### 5.5 통신 2층 규칙 + 도메인 허브 (v3.1)
```
도메인 내부 (Player.*)        : 허브(PlayerManager) 경유 직접 참조 OK — 프레임 단위 데이터
도메인 경계 (Player ↔ World.*) : WorldEvents 이벤트만 — 상태 변화 통지
World 매니저 간               : WorldEvents 이벤트만
```
- **Player 도메인 허브**: `PlayerManager`가 서브매니저를 소유·연결.
  즉시 구현 4: `PlayerInputHandler`(⚠ Unity Input System의 PlayerInputManager와 충돌 방지 개명)
  · `PlayerLocomotionManager` · `PlayerAnimationManager` · `PlayerStatusManager`(스태미나·캐리).
  지연 구현: Effects(M3에) · Inventory(**만들지 않음** — 적재 데이터는 GameState 단일 소유,
  필요 시 파사드만). `CharacterManager` 상속층은 두 번째 캐릭터가 실제 생길 때 추출(YAGNI).
- **World 싱글톤 규약**: `World+기능+Manager` 명명 (WorldDayNightManager·WorldSceneFlowManager·
  WorldDeliveryManager·WorldDebtManager·WorldDeadlineManager·WorldDialogueManager·WorldMinigameManager).
  Core 씬에만 존재(DontDestroyOnLoad 남발 금지) · 명령 호출용, 상태 구독은 이벤트로.
- **WorldEvents 정적 이벤트 허브** (모노 싱글톤 브릿지 대체 — 갓오브젝트·수명주기 리스크 제거):
  정적 클래스에 `event Action<T>` + Raise 헬퍼. 도메인별 partial 분할.
  예: `PhoneRang(박말순)` → Dialogue 재생 → `MinigameRequested` → 오버레이 → `MinigameEnded(결과)`
  → Deadline/Debt 반영. **프레임 데이터는 이벤트 금지**(도메인 내부 몫).

## 5.7 캐릭터 (v5)

- 일반 3D 모델 + Mixamo 휴머노이드 리깅 (복셀 비율 문제 소멸 — **리그 미결 해소, Mixamo 기본**).
- Animator 6종 · 이동방향 회전(45° 스냅 옵션) · Art/Characters 자동 팩토리 제외(리깅 프리팹 수제) 유지.
- 룩 정합은 픽셀화 렌더가 세계와 함께 처리 — 특례 조항 불필요.

## 6. 상호작용·조작

- `IInteractable { Interact(ctx); SetHighlight(bool); }` **유지** — 콜라이더만 3D 트리거로.
  하이라이트 = 시안 림/아웃라인 머티리얼 스왑 (배송지 "목적지 하이라이트" 요구 충족).
- `SideController`: X 이동 + Z 레인 이동(WalkableVolume 내) + 가벼운 점프(유지 여부 §8-6) +
  캐리 상태(상자 들면 속도 페널티). 물리 = CharacterController 또는 Rigidbody kinematic — 단순하게.

## 7. 아트 Pull 파이프라인 — 민지 제안의 기술적 실체 ⭐

> 제안: "기획이 리스트를 push하지 말고, 아트가 pull로 뽑아내고 결과물을 유연하게 밀어넣자."
> 응답: **min-BOM push + 카탈로그 pull 하이브리드** + 임포트 완전 자동화.

- **min-BOM(push, 소수)**: 코어루프 성립에 없으면 게임이 안 되는 것만 발주
  (캐릭터 시트·트럭·문·상자·박말순 초상·타이틀). 이건 마감이 있다.
- **카탈로그(pull, 다수)**: 거리 분위기 물량(건물·간판·소품·군중)은 아트 파이프라인이
  잘 뽑히는 대로 생산 → 디렉터가 **슬롯에 조립**. 좌표를 예약하는 대신 씬에
  `BuildingSlot`/`PropSlot` 마커를 깔아두고, 도착한 아무 건물이나 꽂히게 — 소켓의 느슨한 버전.
- **임포트 자동화 (요구사항 "Import 후 바로 사용" 직결)**:
  1. **폴더 경로가 곧 계약** (D-002 확정 · 2026-07-20 — 접두어 규칙 폐지):
     `Art/Buildings|Props|Characters|Backgrounds|Portraits|UI` 경로가 임포트 규칙을 트리거한다.
     파일명 규약은 min-BOM 항목에 한해 `파일명 = bom_id` (소켓 스왑이 이름에 걸려 있음).
  2. `AssetPostprocessor`: 폴더 경로 감지 → **모델 임포트 자동**(스케일 검증: 1.8u 앵커 대조 · 텍스처 Point·압축 Off · URP Lit 리맵) + **데시메이트·폴리 예산 검사** (Meshy 생성물 과폴리 수습)
  3. 카테고리 프리팹 **자동 생성**: 메시 바운즈 기반 풋프린트 콜라이더 + URP Lit 부착
     → 민지가 png를 넣으면 몇 초 뒤 씬에 드래그 가능한 프리팹이 돼 있다
  4. 검역은 **경고 모드**: 팔레트·치수 이탈은 리포트만(차단 X — pull 유연성 존중).
     단 **라이선스/출처 기록 누락은 여전히 차단** (실격 사유는 타협 없음). _intake 관문 유지.

## 8. 충돌·결정 필요 목록 (정직 — 문서 개정 대상)

1. **씬 5개 ↔ INTENT "씬 3개 초과 금지"** → INTENT 개정 필요 (Core 패턴으로 리스크는 통제됨)
2. **탑다운 주행 폐기** (Travel=미니맵) → SCOPE·치수표에서 트럭(탑다운) 삭제, 트럭=연출 소품으로
3. **티어 규칙 갱신**: "정수 스케일" → "밀도 근사(주 레인 3~4×)" — STYLE.md 개정
4. **치수표는 px+u 병기로 재작성** (3D 공간이라 u가 부활: 캐릭터 48px=1.8u 앵커)
5. ~~Unity 6.5 호환 리스크~~ → **종료**: unity-cli 커넥터 6000.5.3f1 동작 확인 (2026-07-20)
6. **점프 유무 재확인**: Z 레인 이동과 점프 공존 시 조작 혼선 가능 — 그레이박스에서 판정 권고
8. **캐릭터 제작 도구·담당**: MagicaVoxel(민지 권고 — 3D 도트 그 자체, 무료·GPU 불요) vs Meshy(클라우드 생성→남규 수습)
9. **리그 방식**: Mixamo 휴머노이드 vs 제네릭 — 복셀 비율(2등신)이면 자동리깅 어색 가능, D3~4 관통 테스트로 판정
7. 낮밤×조명은 싸지만 **간판 발광(밤 전용 이미시브)** 은 에셋 규칙 필요: 간판은 베이스+이미시브 2레이어

## 9. 디렉토리 (소켓 보드 v3.2 — 풀명칭)

```
Assets/
  _intake/<도구명>/                 # 반입 격리 (유지)
  Art/{Buildings, Props, Characters, Backgrounds, Portraits, UI}/
      # ⭐ 폴더 경로 = 임포트 규칙 트리거 (접두어 폐지 — min-BOM 항목만 파일명=bom_id)
  Art/Shaders/                      # 직교 추가 2026-07-21 — 커스텀 셰이더·머티리얼 (임포트 트리거 비대상)
  Audio/{BGM, SFX}/                 # 직교 추가 2026-07-21 — 파일명=bom_id 스왑 계약 (BOM §8)
  Prefabs/{Auto, Hand}/             # Auto=팩토리 생성(재임포트 시 갱신) · Hand=수제(불가침)
  Scenes/{Core, Main, Home, Camp, Travel, District}/
  Scripts/{Events, SO, Managers, Player, Interactables, UI, Utils}/
  Scripts/Editor/Importer/          # 에디터 전용 (빌드 제외 — Unity 규칙)
  Data/{Dialogue, Schemas, Content}/  # SO 에셋 인스턴스·LLM 산출물
```
- 폴더별 역할·스크립트 전량은 **docs/SCRIPTS.md** (매니페스트) 참조.
