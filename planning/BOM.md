# BOM.md — 통합 발주 대장 v0.3 (⚠ 미동결 — Q2 게이트에서 수량·eta 확정 후 동결)

> v0.2 → v0.3: **에셋만이 아니라 발주 가능한 전부** — 셰이더·스크립트·SO 데이터 인스턴스·씬 셋업 편입 (D-017).
> 스크립트 정본은 `.claude/rules/dont-late-scripts-manifest.md` — 여기서는 발주 단위·순서·담당만 (중복 정의 금지).
> **담당 원칙 (D-017)**: 조립·코드·데이터 = **CLI(관제)** / 폴리싱·판정·조명 = **사람(남규)** / 아트 생산 = **민지**.
> **소켓 좌표 정본 = [[socket-map]]** — bom_id ↔ placeholder ↔ 좌표(x,y,z) ↔ dest 매핑. 씬 발주 완료마다 실물 덤프로 갱신.
> eta_min 전부 미실측 — Trellis/RunPod(#3) 관통이 채운다. 반입 = 사람 수동 → `_intake/<도구명>/` (D-009).

## 0. WebGL 예산 배분 (TECH_SPEC: tris<200k · DC<150 · tex 96MB)

| 블록 | tri 배분 | 근거 |
|---|---|---|
| 캐릭터 (배달부 1) | 5k | <5000 상한 그대로 |
| District 거리 (건물 12채 배치·모듈 6종) | 36k | 3000×12 인스턴스 |
| District 소품 (가로등10·간판12·잡소품~20) | 25k | 500~800급 |
| 도로·보도 모듈 | 5k | — |
| Camp (트럭·창고 셸·짐더미) | 12k | — |
| Home (셸+소품 3 — sacrifice ② 연동 최소투자) | 6k | — |
| Main·Travel (재사용+UI 위주) | 5k | — |
| **합계** | **~94k** | **예산의 47% — 밤 라이트·여유분 확보** |

## 1. 캐릭터 (LPT 최선두 — B-4 해소 즉시 발사)

| bom_id | 수량 | 규격 | 기술 속성 | fab/source | dest → 소켓 |
|---|---|---|---|---|---|
| chr_courier | 1 | 1.8u · <5000tri · 256px | **휴머노이드 아바타** · 애니 6종(idle/walk/run/jump/pickup/carry — TECH_SPEC 계약) · Animator 파라미터 3종(Speed·IsCarrying·IsGrounded)은 코드 완비 | module / generate+rig(**B-4**) | Art/Characters/ → `__gb_Player` 캡슐 |

- 리깅 프리팹은 **수제**(Auto 팩토리 제외 — 아키텍처 §5.7). run은 Speed 임계값 블렌드로 이미 대비됨.
- 군중 NPC = **fake** (원경 빌보드 0~4, 없어도 성립). 박말순은 3D 없음 — 초상만(§5).

## 2. District — 배송지 거리 (코어루프 무대, 최대 물량)

| bom_id | 항목 | 수량(종/배치) | 규격·기술 속성 | source | 소켓 |
|---|---|---|---|---|---|
| bld_module_* | 건물 모듈 | **6종/12채** | 층고 3.0u 배수 · <3000tri · 원점=바닥중심 · 박스 콜라이더 셸 · 1층 문 위치 규격화(문 소켓 간격 통일) | generate(카탈로그 pull) | `BuildingSlot` ×12 (M2 씬 마커) |
| bld_door_entry | 현관문 | 1종/8배치 | 2.1u · <800tri · **프레임/도어 분리 메시 + 힌지 엣지 피벗** ← 코드 트윈 회전용 · **시각물 전용** ([[decisions]] D-024 — 인증은 비콘) | generate | `__gb_Door` (시각) |
| prop_beacon | 인증 비콘 패드 | 1종/8배치 | 발광 패드(크기=`DeliveryPoint._padSize` 인스펙터 파라미터) · 포커스=패드 위 한정(IFocusGate) · 배송 시 상자 물리 드롭(재픽업 불가) | **build(CLI)** | **review** — v2 검증 5항 통과 ([[INBOX]] R6) |
| prop_box_parcel | 택배상자 | 1종/N | 0.4~0.75u · <1500tri · 원점=바닥중심 · **캐리 앵커 스케일 검증**(들었을 때 가슴 앞 0.45u에서 시야 안 가림) | generate | `__gb_Box` + 캐리 앵커 |
| prop_streetlamp | 가로등 | 1종/10 | 4.0u · <500tri · **포인트 라이트 앵커 자식**(앰버 #ff9f45 — 밤 배당) | generate | `PropSlot` |
| ~~prop_sign_*~~ | 간판 → **건물 통짜 편입** ([[decisions]] D-026) | — | 별도 간판 에셋 폐지. 가상 브랜드명 규칙(INTENT)은 건물 생성 프롬프트로 이동 | — | — |
| fx_sign_glow | 간판 발광판 (B안 채택) | 셰이더1+스크립트1/배치 12 | SignGlow.shader(additive·HDR·소프트 폴오프) + SignGlowPlate.cs(저녁 자동 점등) — 간판 위 쿼드 배치. 데칼(A안) 승격은 선택 | build(CLI) | **review** ([[INBOX]] R11) · 배치=건물별 에디터 |
| fx_beacon_rise | 비콘 빛기둥 이펙트 | 셰이더 1 + 쿼드 링 | 테두리 수직 그라디언트 상승 스크롤 · 상태 3단(기본→패드 위 반투명 0.3→배송 종결 시 소멸) — 전부 기존 훅(SetHighlight·OnDeliverySettled) · 색·알파 인스펙터 노출(기본 초록, 팔레트 시안 검토는 사람) | build(CLI) | `__gb_Beacon` 자식 · **발주 대기열** |
| prop_street_* | 잡소품(화분·입간판·박스더미·쓰레기봉투 등) | 6~8종/~20 | <800tri · 풋프린트 콜라이더(팩토리 자동) | generate(pull) | `PropSlot` |
| env_road_set | 도로·보도 모듈 | 3종/타일링 | 폭 규격: 보도=WalkableVolume Z폭과 일치(현 6u) | generate 또는 build(그레이박스 승격) | 씬 직배치 |
| env_sky_night | 스카이 그라디언트(낮/밤) | 2 | 스카이박스 머티리얼 or 카메라 솔리드+원경 빌보드 | build(남규) | 렌더 설정 |

## 3. Camp — 물류캠프 (짐싣기·정산)

| bom_id | 항목 | 수량 | 기술 속성 | source |
|---|---|---|---|---|
| prop_truck | 트럭 (연출 소품 — 주행 없음) | 1 | 6.5u · <3000tri · 정적(도어 애니 불요) | **reuse 1순위**(무료+라이선스 기록) → 실패 시 generate |
| bld_warehouse_shell | 창고 셸(배경) | 1 | <3000tri · 개방형 단면(카메라가 사이드뷰) | generate |
| prop_cargo_pile | 짐더미·팔레트 | 2~3종 | <800tri | generate(pull) |
| — | LoadingZone 마커 | — | 코드 존재(P3 LoadingZone.cs 미작성 — 스크립트 매니페스트 몫) | build |

## 4. Home (⚠ sacrifice ② "인트로 흡수" 후보 — 최소 투자 원칙)

| bom_id | 항목 | 수량 | 비고 |
|---|---|---|---|
| bld_room_shell | 방 셸 | 1 | <2000tri · sacrifice 집행 시 Main 인트로 배경으로 강등 |
| prop_bed / prop_phone | 침대·폰(박말순 전화 연출 앵커) | 2 | <500tri씩 |

## 5. Travel — 미니맵 씬 (3D 없음 — UI 그래픽 씬)

| bom_id | 항목 | 수량 | 규격 |
|---|---|---|---|
| ui_travelmap_bg | 지도 배경 일러스트 | 1 | Tier H 2D · 노드 4~6개 표시 공간 |
| ui_travelmap_node | 노드·경로 아이콘 | 3~4 | 시안=선택가능 · 앰버=목적지 (color_meaning 준수) |

## 6. UI·폰트 (Tier H — 풀해상 오버레이, 픽셀화 제외)

| bom_id | 항목 | 수량 | 기술 속성 | source |
|---|---|---|---|---|
| font_kr_tmp | **한글 TMP 폰트 SDF** | 1 | Pretendard 또는 Noto Sans KR — **OFL 라이선스 기록 필수** · SDF 아틀라스 베이크(다이나믹 금지 — WebGL 용량) | **reuse** |
| ui_hud_set | HUD 아이콘 세트 | ~6 | 시계·마감 경고·스태미나 바·빚 게이지·적재 카운트 · 팔레트 4색 준수 | generate(이미지) |
| ui_dialogue_box | 대화 박스 9-slice + 이름표 | 1 | 포켓몬식 하단 박스 | generate |
| por_parkmalsoon | 박말순 초상 (감정 2~3종) | 2~3 | Tier H 2D 일러스트 · DialogueView(P3) 소비 | generate(이미지) |
| ui_rhythm_set | 리듬 미니게임: 방향키 4종+노트 트랙+판정선 | ~6 | MinigameRhythmView(P3) 소비 | generate |
| ui_title | 타이틀 로고 "늦지마" | 1 | Main 씬 + itch/문서 재사용 | generate |
| ui_cutin_late | "늦지마!" 컷인 그래픽 | 1 | FadeScreen._lateCutIn 소켓 (**코드 존재 — 즉시 소비 가능**) | generate |

## 6.5 씬별 UI 구성 (조립 명세 — 씬 빌더·UI 뷰 발주서의 원본)

> **캔버스 구조 원칙**: 전 캔버스 Screen Space-**Overlay**(Tier H 풀해상 — 픽셀화 제외, 실측 검증됨) ·
> Canvas Scaler = Scale With Screen Size 1920×1080 (D-003 앵커) · **EventSystem은 Core에 1개 상주**
> (⚠ Input System 프로젝트라 `InputSystemUIInputModule` — StandaloneInputModule 쓰면 안 먹음).
> **선행 의존: font_kr_tmp** — 한글 TMP 폰트 없이는 모든 텍스트가 □□□ (반입 승인 대기).

### Core 상주 캔버스 스택 (씬 무관 — sortOrder로 층 분리)

| 캔버스 | sortOrder | 구성 요소 | 데이터 소스 (이벤트만 — UI 로직 금지) | 스크립트 | 상태 |
|---|---|---|---|---|---|
| FadeCanvas | 100 | 검은 풀스크린 Image + "늦지마!" 컷인(ui_cutin_late) | SceneTransition* · DeliveryFailed | FadeScreen ✅ | scn_core 조립 중 |
| DialogueCanvas | 90 | 하단 대화박스(ui_dialogue_box 9-slice) + 초상(por_parkmalsoon, 좌) + 이름표 + 타이핑 텍스트(sfx_dialogue_blip 짝) + 입력대기 화살표 | Dialogue 이벤트(P3 매니저) | DialogueView (P3) | hold |
| MinigameCanvas | 80 | 방향키 노트 트랙(ui_rhythm_set) + 판정선 + 히트/미스 플래시(JUICE) + 결과 패널 | MinigameRequested/Ended(P3) | MinigameRhythmView (P3) | hold |
| HUDCanvas | 10 | ⓐ 시계 "Day 1 · 08:15"(우상) ⓑ 현재 배송 카드: 주소·남은시간, 경고 시 앰버 펄스(좌상) ⓒ 스태미나 바(좌하) ⓓ 돈/빚 카운터 + 보상 숫자 튐(우상 아래) ⓔ 캐리 중 주소 라벨 | ClockTicked · DeadlineWarned · StaminaChanged · DeliveryCompleted/Failed · CarryStateChanged — **전부 실존 이벤트** | HUDView (P3) | hold — **UI-a 결정 필요** |

### 씬 로컬 캔버스

| 씬 | 구성 요소 | 에셋 | 스크립트 | 비고 |
|---|---|---|---|---|
| Main | 타이틀 로고(ui_title) + "시작" 버튼 → SceneFlow.Request(Home) + 조작 안내 1줄 | ui_title | 버튼 1개 — 전용 뷰 불요(UnityEvent로 충분) | |
| Home | "하루 시작" 버튼 + 전화 수신 연출 앵커 | — | — | sacrifice ② 연동 최소 |
| Camp | ⓐ 수주 패널: 오늘 배송건 리스트(so_orders 소비) → 선택=적재(OrderAccepted) ⓑ 정산 패널: 수익·지각벌금·빚 반영 | ui_hud_set 일부 | 수주=LoadingZone(P3)과 짝 · 정산=WorldDebtManager(P3) | hold(P3) |
| Travel | 지도 배경(ui_travelmap_bg) + 노드 버튼 4~6(시안=선택가능·앰버=목적지) + 소요시간 미리보기 + 확정 버튼 | ui_travelmap_bg·node | TravelMapView (P3) | **첫 인터랙티브 UI — EventSystem 필수 검증 지점** |
| District | 씬 로컬 UI 없음 — HUD(상주)가 전담 | — | — | |

### UI 설계 결정 대기 ([[open-questions]] 등재)

- **UI-a · HUD 거처**: A) Core 상주 + 씬별 가시성 토글(SceneTransitionCompleted 구독) ← **권고** (상태 유실 없음, 씬마다 재조립 불요) / B) 씬마다 개별 HUD
- **UI-b · 상호작용 "E" 프롬프트**: 현재는 월드 하이라이트(시안)가 전담. 화면 UI 프롬프트를 추가하면 근접 대상은 프레임 데이터라 **통신 규칙과 충돌 소지** — 그레이박스 판정으로 "하이라이트로 충분한지" 결정 권고

## 7. 렌더·VFX (JUICE 표에서만 도출)

| bom_id | 항목 | 수량 | 비고 |
|---|---|---|---|
| sys_daynight_visual | 시간→하늘·빛 구동 ([[decisions]] D-027) | 1 | 4컷 실증(아침~밤) · 감각값 전부 인스펙터 노출 · ⚠ District 태양 배선은 태양 소유권 교정(폰트+HUD 발주에 포함)으로 해소 | **review** ([[INBOX]] R9) |
| fx_streetlamp_light | 가로등 스팟라이트 프리팹 | 1종 (Prefabs/Hand) | 저녁 자동 점등 + 플리커 시계열 실측(진동→안정) · 조사각 45° — 90° 전환은 [[INBOX]] D-c | **review** (R9와 한 묶음) |
| lut_day / lut_night | 컬러그레이딩 LUT 2종 | 2 | WorldDayNightManager 소비 · **build(남규 — 조명 감각은 사람 영역)** |
| vfx_delivery_pop | 완료 플래시+체크팝+보상숫자 | 1 | 파티클=W층(픽셀화) · 숫자=H층 |
| vfx_late_vignette | 실패 레드 비네트 펄스 | 1 | H층 포스트 |
| vfx_pickup_pop | 픽업 아이템 팝 | 1 | W층 |
| vfx_dust_run | 달리기 먼지 | 1 | P4 (PlayerEffectsManager 생성 전 금지) |
| fx_starfield | 밤하늘 도트 별밭 | 셰이더1+쿼드1 | v3 완료: 정사각 8×8 실측·색 4계열·남보라 하늘·크기 0.02~0.12 | **review** ([[INBOX]] R13) |
| pp_bloom | 글로벌 블룸(약) | Volume 1 | threshold 0.9 · intensity 0.35 — 픽셀화 뒤 적용, 낮 과노출 없음 실측 · Settings 에셋 무변경(프로파일+카메라 플래그만) | **review** |
| fx_moon | 달 | 셰이더 쿼드 1 | 도트 원판+블룸 달무리 · 밤 페이드 · 위치 (−15, 4, 69)·scale 4.5 — **위치 확정은 사람** | **review** |

## 8. 오디오 (JUICE 도출 원칙 유지 — 목록 확장은 JUICE 개정안 J-1 승인 후 유효)

> 소비자: `WorldAudioManager`(P4 — §11) · 임포트 규격: SFX=Vorbis q70·**Decompress On Load**·2D /
> BGM=Vorbis·**Streaming**. dest = `Assets/Audio/{BGM,SFX}/` (아키텍처 §9에 Audio 폴더 직교 추가 필요).
> 파일명=bom_id · 총 오디오 예산 ≤ 10MB (WebGL 다운로드 체감 보호) · 전 건 라이선스 기록 필수.

### BGM

| bom_id | 항목 | 스펙 | source | 비고 |
|---|---|---|---|---|
| bgm_day_loop | 낮 거리 BGM | 60~90s 루프 · 심리스 루프포인트 | Suno(#8)→폴백 #10 | 필수 |
| bgm_night_var | 밤 변주 | **별도 곡 대신 낮 곡 + 로우패스/리버브 변주 1순위** (DayPhaseChanged 훅) | 믹스로 해결 | 전용 곡은 sacrifice 후보 |

### SFX — WorldEvents 트리거 매핑 (Unity 소비 관점)

| bom_id | 트리거 (실존 이벤트) | 소리 | JUICE 근거 | 상태 |
|---|---|---|---|---|
| sfx_delivery_ok | `DeliveryCompleted` | 딩동+동전 | ✓ 배송 완료 | 필수 |
| sfx_late_buzzer | `DeliveryFailed` | 낮은 부저 | ✓ 시간초과 실패 | 필수 |
| sfx_pickup | `PackagePickedUp` | 집는 소리 | ✓ 택배 픽업 | 필수 |
| sfx_footstep | Locomotion 이동중 (도메인 내부 훅) | 발소리+숨소리(달리기 가중) | ✓ 계단 오르기 | 필수 |
| sfx_deadline_warn | `DeadlineWarned` | 짧은 경고 틱 | ❌ **J-1 개정 필요** | 필수 — 마감 압박의 청각 축 |
| sfx_phone_ring | `PhoneRang`(P3 예정) | 전화벨 (박말순) | ❌ J-1 | 필수 — must_have 이벤트인데 표에 없음 |
| sfx_dialogue_blip | 대화 글자 진행 | 포켓몬식 블립 | ❌ J-1 | 권장 |
| sfx_rhythm_hit / _miss | 미니게임 판정 | 히트/미스 2종 | ❌ J-1 | 필수 — 리듬게임에 판정음 없으면 성립 불가 |
| sfx_drink | 에너지드링크 사용 | 캔 따기+꿀꺽 | ❌ J-1 | 권장 (SCOPE 아이템) |
| sfx_scene_whoosh | `SceneTransitionStarted` | 전환 휙 | ❌ J-1 | 선택 |
| amb_night | `DayPhaseChanged(Night)` | 밤 환경음(귀뚜라미·먼 차소리) | ❌ J-1 | 선택 — sacrifice 후보 |

- source 공통: ElevenLabs(#9) 또는 Freesound(#10 — CC0/CC-BY만, 표기 기록) · M0-06이 관통 관문.
- ~~주차 성공(브레이크+성공음)~~ = 탑다운 잔재 → J-1에서 삭제.

### J-1 · JUICE 개정안 (동결 문서 — 사람 승인 게이트)

① 주차 성공 행 **삭제** ② 추가 행 7건: 마감 경고 · 전화벨 · 대화 블립 · 리듬 히트/미스 ·
드링크 · 씬 전환 · 밤 환경음 (각 레이어는 "작은 순간 1~2" 과잉 방지 원칙 준수)
→ **승인 시 관제가 JUICE.md 개정 실행.** 기각 시 ❌ 항목 전부 발주 불가(임의 추가 금지 원칙).

## 9. 데이터 콘텐츠 (LLM 배치 생성 — 빌드에 굽기, INTENT ai_axis)

| bom_id | 항목 | 수량 | 스키마 |
|---|---|---|---|
| data_orders | DeliveryOrderSO 인스턴스 | 6~10건 | 주소(가상)·층·마감·보상·메모 — Data/Content/ |
| data_dialogue_pms | 박말순 시나리오 SO | 2~3편 | DialogueScenarioSO(P3 클래스 미작성) — 대사 LLM 생성→검수 |
| data_brands | 가상 브랜드명 리스트 | ~10 | 간판·상자 로고 공급 (실상표 금지 집행) |

## 10. 셰이더·렌더 파이프라인 (CLI 발주 가능 — Shader Graph 대신 HLSL 코드로)

> Shader Graph(.shadergraph)는 JSON이라 CLI 제작이 비현실적 → **HLSL .shader 파일 = 코드 납품물**로 전환.
> guides/pixel-shader-guide.md의 수동 절차는 이 발주가 대체한다.

| bom_id | 항목 | 내용 | 담당 | 상태 |
|---|---|---|---|---|
| shd_pixelate | 픽셀화 풀스크린 셰이더 | HLSL: 스크린 UV를 480×270 그리드로 스냅 샘플링 ([[decisions]] D-011) · `Assets/Art/Shaders/Pixelate.shader` | **CLI** | **done** — 사람 룩 판정 "좋다" (2026-07-20, D-025). STYLE L1 문구 개정은 Q1 절차에서 |
| shd_pixelate_feature | Full Screen Pass Renderer Feature 장착 | PC_Renderer.asset · BeforePostProcess · 체크박스로 원복 가능(사람이 언제든 걷어냄) | **CLI** | **done** (위와 한 몸) |
| shd_highlight_rim | 상호작용 시안 림/미광 (STYLE readability) | 현 머티리얼 스왑 방식 유지 → M3에 림 승격 판단 | CLI | hold(M3) |
| lut_day / lut_night | 컬러그레이딩 LUT 2종 | §7과 동일 — **사람 전속(조명 AI 금지)** | 사람 | hold(M3) |

## 11. 스크립트 발주 (정본=매니페스트 · 잔여 12+3종)

| 발주 묶음 | 파일 | 시점 | 담당 | 비고 |
|---|---|---|---|---|
| UI 뷰 4종 | HUDView · DialogueView · MinigameRhythmView · TravelMapView | P3 | **CLI** | 이벤트 구독·표시만(로직 금지) — 씬 셋업 §13과 짝 |
| World 매니저 4종 | WorldDebtManager · WorldDialogueManager · WorldMinigameManager · WorldAudioManager | P3 | **CLI** | Debt는 Camp 정산과 짝 |
| 상호작용 2종 | LoadingZone · EnergyDrinkPickup | P3 | **CLI** | IInteractable 동결 시그니처 |
| SO 클래스 1종 | DialogueScenarioSO | P3 | **CLI** | data_dialogue_pms 선행 조건 |
| 임포터 2종 | ArtImportPostprocessor · CategoryPrefabFactory | P1~2 | **CLI** | **review** — .obj 관통 테스트 3항(생성·콜라이더·멱등) 통과, 텍스처 Point·256 확인. ArtAuditReport는 P3 보류 |
| P4 3종 | WorldJuiceManager · PlayerEffectsManager (+ Audio 연동) | P4 | CLI | M3 전 생성 금지 |

## 12. SO 데이터 인스턴스 (CLI가 exec로 생성 — Data/)

| bom_id | 항목 | 수량 | 담당 | 상태 |
|---|---|---|---|---|
| so_gamestate / so_tuning | GameState.asset · Tuning.asset | 각 1 | CLI | **done** (그레이박스 빌더가 생성) |
| so_orders | DeliveryOrderSO 인스턴스 (주소·층·마감·보상) | 6~10 | **CLI** (내용은 LLM 생성→사람 검수) | todo — 가상 주소·브랜드 준수 |
| so_dialogue | 박말순 시나리오 인스턴스 | 2~3 | CLI | hold(DialogueScenarioSO 클래스) |
| so_daynight_curve | 조명 커브·색 프리셋 (DayNight 소비) | 1 | CLI 골격 → **사람 튜닝** | todo |

## 13. 씬 셋업 발주 (빌더 패턴 — 씬 커밋 없이 코드로 재현, 사람이 폴리싱)

> 방식: `GreyboxStageBuilder`처럼 **씬별 에디터 빌더**를 CLI가 작성·실행 → 사람은 에디터에서
> 가다듬기(조명·배치 미세조정)만. 씬 파일은 커밋 안 함 — 빌더가 정본.

| bom_id | 항목 | 내용 | 담당 | 상태 |
|---|---|---|---|---|
| scn_core | Core 씬 조립 | CoreSceneBuilder.cs(멱등) — Core에서 Play→Main 자동 로드 | **CLI** | **done** — 사람 확인 2026-07-20 (경고 원인이던 Main 중복 Core도 사람이 정리) |
| scn_build_settings | 씬 6종 생성+빌드세팅 등록 | Core(0)~District(5) 전부 enabled | **CLI** | **done** (위와 한 발주) |
| scn_district | District 무대 조립 | 슬롯 22개(실측 좌표=[[socket-map]]) + 무대 일습 + 매니저 0(Core 상주) — 전이 체인·배송 완주 검증 | **CLI** | **review** ([[INBOX]] R7) |
| scn_camp | Camp 조립 | 트럭 소켓 + LoadingZone + 정산 트리거 | CLI | hold(LoadingZone 스크립트) |
| scn_travel | Travel 조립 | 캔버스 + TravelMapView 소켓 + 노드 데이터 | CLI | hold(TravelMapView) |
| scn_home_main | Home·Main 최소 조립 | sacrifice ② 연동 — 최소 투자 | CLI | todo |
| scn_camera_rig | 카메라 리그 확정 | FOV·거리·데드존 팔로우 — **M1-09** · 픽셀화(§10)와 한 몸 | CLI 조립 → **사람 프레이밍 판정** | todo |
| scn_lighting | 씬별 조명·포인트라이트 배치 | — | **사람 전속** | hold(M3) |

## 14. 라우팅 (AAPP) · 차단 현황

- **로직·셰이더·SO·씬 셋업** (§10~13) → **unity-cli 레인(관제 직행)** — 외부 연결 불요, **지금 발주 가능**
- **3D generate 전체** → asset-generator 레인: 🔄 **Trellis@RunPod 셋업중(민지)** — 관통+eta 실측이 M2 발사 조건. Meshy(#2)는 보류.
- 이미지 2D → #4 미검증 / 폴백 🖐 웹 수동→_intake · 오디오 → #8~10 미검증(M0-06)
- **reuse 1순위**: 트럭·한글 폰트 (라이선스 기록 필수) · **fake**: 군중·원경
- **cast 후보 0건 유지** — District 구획이 커지면 후보화, 사람 승인 게이트
- **build(사람 전속)**: LUT 2종·스카이·조명 (AI 금지 영역) + 전 항목 폴리싱·경험검수

## 15. 동결 전 해소 필요 (Q2 선행)

1. **B-4** (chr_courier 리그) — 민지 상의 + Trellis 관통 실측
2. **#3 관통 + eta 실측** — 전 generate 항목 eta의 근거
3. **§8-7 간판 이미시브 규칙** — 간판 4종 반입 전
4. 수량 확정(건물 6종/12채가 예산과 그림에 맞는지 — 민지 생산력 실측 후)
5. sacrifice 3개 확정: ① 미니게임 1단계 ② Home 흡수 ③ 간판 이미시브 B세트 (+ 후보: 군중 빌보드 전면 컷)
