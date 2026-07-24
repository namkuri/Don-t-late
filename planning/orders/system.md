# orders/system.md — 시스템·로직 발주 대장 (append-only)

> 형식: [[guides/distributed-workflow]] §3 v3. 발주·결과 시각은 파일 안에 명시 — 리드타임 자기완결.
> 봉투 전문이 곧 서브에이전트 투입 프롬프트다.

---

## S-001 · 발주 2026-07-21 17:46 → general-purpose 서브에이전트 (M0-07+08 + AAPP 자동화)

목표: 하네스 강제·측정 도구 10종 — 훅 4종 + 채점기 3종 + AAPP 자동화 3종. "부탁을 강제로, 판정을 기계로, 측정을 자동으로." 본선 중 신축 금지라 이번이 유일한 제작 기회.

입력·산출 위치:
- git 훅: `hooks/` 폴더(커밋 대상) + `git config core.hooksPath hooks` 활성 (정수 셋업 절차에도 추가 필요 — 보고에 명시)
- 채점기·자동화: `scripts/` 폴더 (py — 시스템 python 사용, 없으면 sh 폴백)
- 참고 실물: planning/assets_manifest.md(라이선스 대장) · docs/INTENT.md(`frozen: true` 헤더) · planning/orders/*.md(파싱 대상 형식은 이 파일 상단) · STYLE 팔레트 4색(#0a0d16 #ff9f45 #35e0c8 #ff4658) · TECH_SPEC 예산(tris<200k·DC<150·tex 96MB) · unity-cli(에디터 가동 중)

기대:
[훅 — hooks/]
1. `pre-commit`: 스테이지에 .cs 있으면 unity-cli 컴파일+콘솔 에러 검사 → 실패 시 커밋 거부 (unity-cli 무응답 시 "에디터 켜라" 안내 후 거부 · .cs 없으면 통과)
2. `pre-commit` 내 freeze-guard: `frozen: true` 헤더 문서의 **기존 줄 수정/삭제** diff 검출 시 거부 (줄 추가는 통과)
3. `pre-commit` 내 라이선스 대조: Assets/Art|Audio 신규 바이너리(fbx/glb/png/wav/ogg/mp3)가 스테이지에 있는데 assets_manifest.md에 파일명 미등재면 거부 (GS25 사례의 자동화)
4. `commit-msg`: 태그(`[P숫자]`/`[ENV]`/`[docs]` 등 대괄호 접두) 부재 시 **경고만** (차단 아님)
[채점기 — scripts/]
5. `palette_check.py <png>`: 스크린샷 색분포 vs 팔레트 4색 근접도 리포트 (거리 히스토그램 — 차단 아닌 신호기)
6. `screenshot_bundle.py`: unity-cli로 Greybox·District 순회 스크린샷 수집 → Screenshots/bundle_<날짜>/ (체크포인트용)
7. `scene_stats.py`: exec로 활성 씬 총 tri·렌더러 수·텍스처 메모리 집계 → TECH_SPEC 예산 대비 표 출력
[AAPP 자동화 — scripts/]
8. `new_order.py <domain> <id> <수신자>`: 발주 스켈레톤(5칸+시각 헤더)을 orders/<domain>.md에 append
9. `leadtime_report.py`: orders/*.md의 발주/결과 헤더 파싱 → 리드타임·재시도 집계표 planning/calibration.md 생성/갱신
10. 모델 배정표: scripts/model_routing.md — op 유형→모델 규칙 표 (py 불요, 문서 1장: 기계적 op=haiku 후보 / 표준 구현=기본 / 판정·설계=상향. 근거는 배정 실험 후 채움이라 "미실측" 명시)

수용기준:
- 훅: 실제 커밋 없이 **훅 스크립트 직접 호출 테스트**로 각 시나리오 검증 (freeze-guard: INTENT 한 줄 임시 수정→훅 fail 확인→복원 / 라이선스: 미등재 더미 png 스테이지→fail→언스테이지·삭제 / pre-commit: 현 상태 pass 확인). 테스트 증거를 시나리오별 exit code로 보고
- `git config core.hooksPath hooks` 적용 확인 + 정수 셋업 1줄("clone 후 이 명령") 보고
- 채점기: 실물 1회씩 실행 — palette_check는 기존 night_v3.png로, scene_stats는 Greybox로 (수치 보고)
- 9번: 이 발주(S-001) 자체가 첫 파싱 대상 — 결과 append 후 돌려 리드타임 표 1행 생성 확인
- 콘솔 에러 0 · Main.unity·_intake 불변 · 커밋은 하지 않음(사람 게이트)

실패시: 훅별 독립 — 하나가 막히면 나머지는 완성하고 막힌 것만 [BLOCKED]+원문. py 부재 시 sh 폴백. 2회 실패 항목은 보고로 넘김.

보고: 관찰만 — 파일 목록 / 훅 테스트 시나리오별 결과 / 채점기 실행 수치 / 정수 셋업 절차.

### 결과 · 2026-07-21 17:58 (리드 12분)

- 훅 2파일(hooks/pre-commit·commit-msg + hooks/.gitattributes eol=lf) · 스크립트 6종(scripts/palette_check.py·screenshot_bundle.py·scene_stats.py·new_order.py·leadtime_report.py·model_routing.md) 납품. `git config core.hooksPath hooks` 적용.
- 훅 직접 호출 테스트: 현상태 pass(0) / freeze-guard 수정 거부(1)·추가만 통과(0)·INTENT 원문 복원(md5 일치) / 라이선스 미등재 더미 거부(1)·잔해 0 / .cs 스테이지 컴파일 게이트 통과(0, 10.7s) / commit-msg 무태그 경고 후 0.
- 채점기 실측: palette_check(night_v3.png — 네이비 최근접 99.4%·거리<64 비율 14.3%) / scene_stats(Greybox — tris 1,538,826 **예산 200k 초과** ← 지에스.fbx 단독 1,499,400 · 렌더러 33 OK · tex 10.7MB OK) / screenshot_bundle(Screenshots/bundle_2026-07-21/ greybox·district 2컷, Main 복귀).
- 재시도 1회: 컴파일 게이트 첫 호출이 에디터 임포트 busy로 일시 거부 → 훅에 3s 1회 재시도 추가 후 통과.

---

## S-002 · 발주 2026-07-21 19:06 → unity-dev 서브에이전트 (구역 자동 배치 시스템)

목표: District 씬의 빈 슬롯(slot_building_01~12·slot_prop_01~10)을 **결정론적 시드 랜덤**으로 자동 채우는 시스템 — 같은 구역은 재방문해도 항상 같은 배치 (결정 D-036, 사람 지시).

입력:
- 프로젝트: C:\Users\rnk50\Unity\Don't late (6000.5.3f1). 에디터 실행 중(플레이 중이면 stop).
- 슬롯 실물: District.unity의 Slots 하위 22개 마커 — 좌표는 planning/socket-map.md
- 통신 규칙: 런타임 이름 검색(GameObject.Find류) 전면 금지 → **DistrictSceneBuilder가 슬롯 Transform 배열을 직렬화 참조로 주입**하는 구조로 설계할 것
- 시드 규칙: districtId 문자열(당장은 빌더가 "HappyVilla" 주입 — 주문 연동은 P3 몫, 주석 명시) → **자체 결정론 해시**(FNV-1a 등 — string.GetHashCode 금지: 플랫폼 보증 없음) → System.Random
- 신규 런타임 스크립트 1개 허용: Assets/Scripts/Interactables/DistrictLayoutGenerator.cs (월드 오브젝트 분류 — 매니페스트 직교 추가 D-036)

기대:
1. DistrictLayoutGenerator.cs — OnEnable/Start 시: 이전 생성물 정리(멱등) → 시드 고정 Random으로
   ⓐ 건물 슬롯 12개: 그레이박스 건물 생성(층수 1~3층×3.0u·폭 6~7u·색 3톤 변형 — 큐브 조합, 슬롯당 결정론 선택. 추후 Prefabs/Auto 건물 풀로 교체될 소켓 구조: [SerializeField] 프리팹 풀 배열이 비면 그레이박스 폴백)
   ⓑ 소품 슬롯 10개: 배치 여부(확률)·종류 결정론 선택 (당장은 상자더미 큐브 1종 폴백)
2. DistrictSceneBuilder 확장 — generator 부착·슬롯 배열/districtId/풀(빈) 주입·씬 저장.
3. 검증 (Core Play → District 전이): ⓐ 슬롯에 건물 생성 확인(개수·스크린샷) ⓑ **결정론 실측: 씬 재로드(Travel 갔다가 District 재진입) 2회의 배치 스냅샷(슬롯별 층수·색 인덱스) 완전 일치** ⓒ 다른 districtId("TestB")로 1회 — 배치가 달라짐 확인 후 원복 ⓓ 배송 루프 무회귀 ⓔ 콘솔 0 · 시각 480 복원 · stop.
4. Screenshots/district_generated.png · planning/ai_evidence.md 1줄 · **이 파일 S-002 하단에 결과 블록 append**(발주 19:06 기준 리드 계산).

운용 주의(표준): Bash+작은따옴표 exec · stop→compile→console · 씬 dirty 확인 · Main.unity·_intake 불변 · 스크린샷 검수 체크리스트(16:9 프레이밍).

수용기준: 결정론 2회 일치 실측(스냅샷 diff 0) · 이름 검색 0 (grep으로 Find 부재 확인) · 슬롯 주입 구조 · 재로드 멱등(중복 생성 없음) · 콘솔 0.

실패시: 2회 후 [BLOCKED]+원문. Additive 재진입 시 OnEnable 중복 호출 함정 주의(생성물 정리로 방어).

보고: 관찰만 — 파일 목록 / 결정론 스냅샷 2회 비교표 / 스크린샷 / 리드타임.

### 결과 · 2026-07-21 19:24 (리드 18분)

- 파일: 신규 `Assets/Scripts/Interactables/DistrictLayoutGenerator.cs`(런타임) · 수정 `Assets/Scripts/Editor/DistrictSceneBuilder.cs`(슬롯 Transform 수집→generator 부착·`_buildingSlots`12/`_propSlots`10 직렬화 주입·씬 저장) · `Screenshots/district_generated.png` · `planning/ai_evidence.md` 1줄. District.unity 재조립(씬은 커밋 안 함).
- 설계: districtId(현재 빌더 "HappyVilla" 고정 — 구역별 주입은 P3 주석 명시) → 자체 **FNV-1a 32bit**(string.GetHashCode 미사용) → System.Random. 건물12=층1~3×3.0u·폭6~7u·색3톤 큐브조합(프리팹 풀 비면 그레이박스 폴백) · 소품10=확률0.6 배치·상자더미 1종. 슬롯은 **직렬화 Transform[] 주입** — 런타임 이름검색 0(grep `GameObject.Find`/`FindObjectOfType` 부재). 배경 시각물이라 콜라이더 제거. ClearGenerated=`DestroyImmediate`로 같은 프레임 중복 OnEnable에도 사본 0.
- 결정론 스냅샷 2회 비교(Core Play → Main→Home→Camp→Travel→District 전이 후 Travel 왕복 재진입):

  | | 오브젝트 수 | GeneratedLayout roots | 지문(건물 f/t·소품 k) |
  |---|---|---|---|
  | A (첫 진입) | 19 (건물12+소품7) | 1 | Building_01_f3_t1…12_f2_t2 · Prop 01,03,05,06,07,09,10 |
  | B (재진입) | 19 | 1 | **A와 완전 일치 (diff 0)** |
  | C (districtId="TestB") | 17 (건물12+소품5) | 1 | 층/톤 상이 · Prop 02,04,05,06,09 — **A와 상이** |
  | A' (HappyVilla 복원) | 19 | 1 | **A와 재일치** |

- 멱등: 같은 프레임 `Generate()` 2회 연속 후 GeneratedLayout roots=1(중복 생성 0). 재진입 시 genCount=1·roots=1 유지.
- 무회귀: District 재진입 후 player·box·deliveryPoint·deliveryManager 전부 존재. 스크린샷(16:9)에 층수 다른 그레이박스 건물·보도변 상자더미·비콘·가로등 관찰.
- 종료: 콘솔 error 0 · 시각 minuteOfDay 480 복원 · Main 재오픈 · play stop. Main.unity·_intake 불변(Main mtime 15:20 — 내 세션 이전, 미수정). 커밋 없음(사람 게이트).

**판정 · 2026-07-21 19:24 — reviewer ACCEPT (13/13 PASS)** — reviewer 레인 첫 가동. HARNESS §6에 따라 사람 도장 없이 done 전이.

---

## S-003 · 발주 2026-07-21 19:29 → unity-dev 서브에이전트 (구역 배치 공간 정합 수정)

목표: S-002 생성 건물의 공간 침범 2건 수정 — ① 건물이 보행로 앞으로 튀어나옴 ② 가로등이 건물에 파묻힘 (사람 육안 적발 — 스크린샷 Screenshots/district_generated.png 참조).

입력:
- 원인 추정: 건물이 슬롯(Z=2.6) 중심으로 생성돼 깊이의 절반이 −Z(보도 쪽)로 침범. 뒷줄 가로등(Z=+2.4)이 그 범위 안.
- 대상: Assets/Scripts/Interactables/DistrictLayoutGenerator.cs (건물 생성 로직만 — 시드·슬롯 구조는 reviewer ACCEPT 상태이니 불변)
- 공간 규약(socket-map): 보도=Z −3~+3(레인 6u) · 건물 라인 Z=2.6 · 뒷줄 가로등 Z=+2.4

기대:
1. 건물 풋프린트 규칙: **전면(길 쪽) 면이 Z≈+3.0(보도 경계 뒤)에 정렬**, 깊이는 +Z(안쪽)로만 확장 — 보도·가로등 공간 침범 0.
2. 검증: ⓐ 생성 후 각 건물 바운즈의 min.z ≥ 2.9 실측(12건 전수 수치) ⓑ 가로등 8기 바운즈와 건물 바운즈 교차 0 실측 ⓒ 결정론 재확인(수정 후 A=B diff 0 — 시드 로직 불변 증명) ⓓ 스크린샷(공간 정합 체크리스트 D-038 적용: 겹침·침범·접지 명시 관찰) ⓔ 콘솔 0 · 시각 480 복원 · stop.
3. planning/ai_evidence.md 1줄 · 본 파일 결과 블록 append.

운용 주의(표준): Bash+작은따옴표 · stop→compile→console · Main·_intake 불변.
수용기준: 침범 실측 0(수치) · 교차 0 · 결정론 유지 · 콘솔 0.
실패시: 2회 후 [BLOCKED]+원문.
보고: 관찰만 — 바운즈 수치표 · 스크린샷 · 리드.

### 결과 · 2026-07-21 19:38 (착수 19:29 기준 리드 9분)

- 수정 파일: `Assets/Scripts/Interactables/DistrictLayoutGenerator.cs` (건물 생성 로직만). 상수 `BUILDING_FRONT_Z=3.0` 추가 + `BuildBuilding`에서 floor 큐브 `localPosition.z`에 `centerZLocal = (BUILDING_FRONT_Z + BUILDING_DEPTH/2) − slot.position.z` 오프셋 적용 → 전면(길 쪽 −Z 면)을 세계 Z=+3.0에 정렬, 깊이는 +Z(안쪽)로만 확장. **시드·슬롯·RNG 스트림 불변**(floors/tone/width 추첨은 오프셋 앞에서 그대로 소비 — reviewer ACCEPT 상태 유지). 슬롯 회전=identity(빌더가 localPosition만 설정) 전제.
- ⓐ 건물 12건 전수 바운즈 min.z (District Play, OnEnable→Generate 실측):

  | 건물 | min.z | max.z | 건물 | min.z | max.z |
  |---|---|---|---|---|---|
  | Building_01 | 3.000 | 8.000 | Building_07 | 3.000 | 8.000 |
  | Building_02 | 3.000 | 8.000 | Building_08 | 3.000 | 8.000 |
  | Building_03 | 3.000 | 8.000 | Building_09 | 3.000 | 8.000 |
  | Building_04 | 3.000 | 8.000 | Building_10 | 3.000 | 8.000 |
  | Building_05 | 3.000 | 8.000 | Building_11 | 3.000 | 8.000 |
  | Building_06 | 3.000 | 8.000 | Building_12 | 3.000 | 8.000 |

  **전수 min.z=3.000 ≥ 2.9 충족** (수정 전엔 slot Z=2.6 중심·깊이5로 min.z≈0.1, 보도 깊이 침범).
- ⓑ 가로등 8기 × 건물 바운즈 교차: **솔리드 포스트(LampModel) 교차 0**. 뒷줄 가로등 06/07/08 포스트 max.z=2.93 < 건물 min.z 3.0 (0.07u 클리어). 앞줄 01~05는 Z=−2.2대라 무관. **관찰 주의**: 각 가로등엔 라이트 빔 비주얼 `Cone` 렌더러(size 2.8×3.5×2.8·max.z=3.60)가 별도로 있어 뒷줄 3기 Cone이 건물 전면과 겹치나(LIGHTCONE_OVERLAP=3), 이는 솔리드 파묻힘이 아니라 **광원이 벽면에 투사되는 의도 동작**이라 판단. 사람 육안 적발 대상(가로등 포스트가 건물에 파묻힘)은 해소.
- ⓒ 결정론: `Generate()` 재실행 후 fingerprint(이름+min.z+width) `A==B True` (len 584 동일) · `GeneratedLayout` roots=1(멱등, 중복 생성 0). 시드 로직 불변 증명.
- ⓓ 스크린샷 `Screenshots/district_spacefix.png` (D-038 공간 정합 체크): 건물(어두운 큐브)이 가로등 라인 **뒤로 물러남** — 가로등이 건물 앞 보도에 서 있고 파묻히지 않음 · 보도 전면 클리어(침범 0) · 건물·가로등·상자 전부 접지 정상.
- ⓔ 콘솔 error 0 · play stop · Main 재오픈. Main.unity mtime 15:20(내 세션 19:29 이전 — 미수정, 재오픈은 읽기전용) · `_intake` 불변. 커밋 없음(사람 게이트). 재시도 0회.

---

## S-004 · 발주 2026-07-22 01:01 → ClaudeCode (본 세션 실행 — 간판 발광 이미시브 스왑 전환)

목표: R11 반려 처방 시공 (D-051) — 간판 앞을 덮는 additive 발광판을 폐지하고, **간판 렌더러 자체 머티리얼의 이미시브를 구동**하는 방식으로 교체.

입력:
- `Assets/Scripts/Interactables/SignGlowPlate.cs` (현행 판 방식 — 개조 대상)
- `Assets/Scripts/Editor/GreyboxStageBuilder.cs` BuildSignGlow/GetOrCreateSignGlowMaterial (판 생성부)
- 반려 기록: iterations.md M1 — "발광판이 실제 간판을 가림"

기대:
- `SignGlow` 컴포넌트(파일명 개명): `_signRenderer` 머티리얼의 `_EmissionColor`를 phase에 따라 시안↔검정 구동. URP Lit 키워드 제약(MPB 불가) 때문에 Awake에서 머티리얼 인스턴스화 + `_EMISSION` 상시 켬.
- 그레이박스: 간판 자리 쿼드가 별도 판이 아니라 **간판 그 자체**(어두운 베이스 URP Lit) — 저녁·밤에 그 면이 발광. 머티리얼 에셋에 `_EMISSION` 키워드를 에디트타임에 켜서 WebGL 배리언트 스트리핑 방지.
- 향후 실건물: 간판 분리 익스포트(art.md 공통 규격)로 들어온 간판 렌더러를 `_signRenderer`에 꽂으면 동일 동작.
- 구방식 고아 에셋 삭제: SignGlow.shader · M_SignGlowDecal.mat · T_SignGlowCyan.asset · GB_SignGlow.mat (+meta).

수용기준: ① 컴파일 통과 ② 콘솔 0 ③ Play에서 저녁 진입 시 간판 면 자체가 시안 발광(가리는 판 없음)·아침 소등, 스크린샷 확보.

실패시: [BLOCKED] 보고. ⚠ 발주 시점 에디터 미가동 — 검증 3종은 에디터 기동 후 수행(그 전 push 금지).

### 결과 · 2026-07-22 01:23 (리드 22분)
- SignGlowPlate → `SignGlow` 개명(git mv, GUID 보존): 간판 렌더러 자체 머티리얼의 `_EmissionColor`를 phase로 구동. 별도 발광판 소멸.
- 빌더: `__gb_Sign` 쿼드 = 간판 그 자체(URP Lit `GB_Sign.mat`). `_EMISSION` 키워드는 **CreateAsset 후**에 켜야 저장됨(실측 — 생성 과정이 키워드 리셋). 고아 에셋 4종 삭제(SignGlow.shader·데칼 잔재 2·GB_SignGlow.mat).
- 부수 적발·수리: **Core 씬 카메라 소실**(화면 전체 무렌더) — 빌더가 카메라를 "설정만" 하고 생성하지 않던 구멍. 없으면 생성하도록 보강(AudioListener는 D-041대로 미부착).
- 검증: 컴파일 ○ · 콘솔 0 ○ · Play에서 19:00 진입 시 간판 면 시안 발광(emission 0.42/1.76/1.57×2) → 09:00 소등(검정) 확인 ○. 증거: `Screenshots/s004_sign_night.png`.

---

## S-005 · 발주 2026-07-22 01:39 → 정수 (Camp 정산 레인: Debt + LoadingZone + 드링크)

목표: Camp 씬의 존재 이유(짐싣기·정산)를 성립시킨다 — P3 미납 3종: `WorldDebtManager` · `LoadingZone` · `EnergyDrinkPickup`.

입력:
- `Assets/Scripts/SO/GameStateSO.cs` — money·debt·cargo(List<DeliveryOrderSO>)·completedCount·lateCount 필드 실재
- `Assets/Scripts/Events/WorldEvents.cs` — Debt 도메인 이벤트 없음(신설 필요)
- `Assets/Scripts/Interactables/PickupBox.cs` — IInteractable 구현 패턴 참조 (시그니처 동결 — 변경 금지)
- `Assets/Scripts/Player/PlayerStatusManager.cs` — 스태미나 회복 훅 (드링크 접점)

기대:
1. `Managers/WorldDebtManager.cs`: `DeliveryCompleted`/`DeliveryFailed` 구독 → 보상 가산·지각 차감 집계, Camp 복귀(SceneTransitionCompleted=Camp) 시 정산 → GameState.money/debt 갱신 + **신규 이벤트 `DebtSettled(정산 요약 payload)`** Raise. 저빈도 → §9.5 로그 동반.
2. `Interactables/LoadingZone.cs`: Camp의 적재존 — Interact 시 대기 주문(DeliveryOrderSO)을 GameState.cargo에 적재, **`OrderAccepted`** Raise(기존 이벤트 재사용). 적재 수 상한은 TuningConfigSO에 노출.
3. `Interactables/EnergyDrinkPickup.cs`: Interact 시 스태미나 회복(회복량 TuningConfig 노출) 후 자기 파괴. World 경유 없이 PlayerContext로 처리 가능하면 이벤트 신설 금지(YAGNI).
4. HUD 빚 게이지가 DebtSettled로 갱신되는 연결 확인 (HUDView 수정이 필요하면 최소 수정).

수용기준: ① 컴파일 ② 콘솔 0 ③ Play: Camp에서 E로 적재→District 배송 완료→Camp 복귀 시 콘솔 `[EVENT] DebtSettled`와 money/debt 변화 확인. 프리팹 부착 필요사항은 PR 본문에 명시.

실패시: [BLOCKED] 보고. 씬·프리팹·Settings 커밋 금지(훅이 차단). feature/jjs → PR.

### 결과 · 2026-07-22 02:03 — S-005 (수행: ClaudeCode 본 세션, D-053 수신자 변경 · 리드 24분)
- `WorldDebtManager`(정산: 벌금 차감→잔액 상환→`DebtSettled`) · `LoadingZone`(패드 E 적재·상한 maxCargo·소비형) · `EnergyDrinkPickup`(+energyDrinkRecover 회복 후 자기 파괴) 납품. 신규 이벤트 `DebtSettled`+페이로드 `DebtSettlement`(§9.5 로그 동반).
- Camp 빌더가 패드 3개에 주문 3건(행복빌라 재사용+청운상가·달빛맨션 신설 SO) 배선 + 드링크 배치.
- 관찰: `[EVENT] OrderAccepted #101 청운상가` / `DebtSettled 상환 3900 · 벌금 1100 → 잔액 0 / 빚 6100` / 스태미나 50→90 회복 확인.

---

## S-006 · 발주 2026-07-22 01:39 → 정수 (Travel 레인: 미니맵 노드 선택)

목표: Travel 씬을 "노드 선택 = 시간 소모" 화면으로 성립 — P3 미납 `TravelMapView`.

입력:
- `Assets/Scripts/UI/SceneAdvanceButton.cs` — 현행 전환 버튼(대체 대상) · `UI/HUDView.cs` — View 패턴(로직 없음·이벤트 구독)
- `Assets/Scripts/Managers/WorldSceneFlowManager.cs` — 씬 전이 API · `WorldDayNightManager.SetTime` — 시간 소모 반영 경로
- SCOPE §코어루프: "이동(노드 선택=시간 소모 — 주행 조작 없음)"

기대:
- `UI/TravelMapView.cs`: 노드 2~3개(근거리/원거리 — 소모 시간 상이, TuningConfig 노출) 버튼 표시 → 선택 시 시간 소모 적용 + District 전이 요청. View 규칙: 게임 로직 금지 — 시간 가산·전이는 매니저 호출로 위임(어느 매니저가 소유할지는 SceneFlow에 메서드 추가로 해결, 새 매니저 발명 금지).
- Travel 씬 조립은 관제(빌더) 몫 — 코드는 "어느 오브젝트에 뭘 붙일지"만 PR 본문에 기재.

수용기준: ① 컴파일 ② 콘솔 0 ③ Play: Travel에서 원거리 노드 선택 시 시계가 더 많이 진행된 채 District 도착 확인(콘솔 SceneTransition 로그 + HUD 시계).

실패시: [BLOCKED] 보고.

### 결과 · 2026-07-22 02:03 — S-006 (수행: ClaudeCode 본 세션, D-053 · 리드 24분)
- `TravelMapView`(노드 버튼 View — 시간 가산+전이 위임만) 납품. 시간 가산 API는 발주서의 "SceneFlow에 추가" 대신 **시계 소유자인 WorldDayNightManager.AdvanceMinutes**로 배치(소유권 원칙 — 편차 기록).
- Travel 캔버스를 SceneFlowUIBuilder가 노드 2개(근거리/원거리·소모 분 표기)+캠프 복귀 버튼으로 재조립.
- 관찰: 원거리 노드 클릭 시 시계 607.8→697.8(+90분 정확) 후 District 전이 완료.

---

## S-007 · 발주 2026-07-22 01:39 → 정수 (진상 전화 미니게임 레인)

목표: "진상 전화 → 방향키 리듬" 오버레이 성립 — P3 미납 `WorldMinigameManager` · `MinigameRhythmView`.

입력:
- ARCHITECTURE §5: Minigame은 **씬 아님 — UI 오버레이 모듈**, 결과를 이벤트로 방출
- `Assets/Scripts/Events/EventPayloads.cs` — PhoneCall·MinigameResult struct 정의 여부 확인(없으면 신설)
- `Assets/Scripts/Managers/WorldDialogueManager.cs` — 박말순 대화 재생(전화 수신 연출 접점)
- `Assets/Scripts/UI/DialogueView.cs` — 오버레이 UI 패턴 참조

기대:
- WorldEvents 신설 3종(전부 저빈도 → §9.5 로그 동반): `PhoneRang(PhoneCall)` → `MinigameRequested` → `MinigameEnded(MinigameResult)`.
- `Managers/WorldMinigameManager.cs`: District 체류 중 확률/타이머로 PhoneRang 발화(빈도 TuningConfig 노출) → 오버레이 구동 → 결과 Raise. 결과의 게임 반영(마감 압박·보상 차감)은 **Deadline/Debt가 구독**으로 처리 — Minigame이 직접 손대지 않는다.
- `UI/MinigameRhythmView.cs`: 방향키 시퀀스 표시·판정(성공/실패 단순 2단 — sacrifice ① 반영, 다단계 금지). 진행 중 플레이어 이동 입력 차단은 PlayerInputHandler 기존 구조 활용.

수용기준: ① 컴파일 ② 콘솔 0 ③ Play: District에서 전화 발화→방향키 입력→성공/실패에 따라 콘솔 `[EVENT] MinigameEnded` 결과 상이 확인.

실패시: [BLOCKED] 보고. IInteractable·기존 이벤트 시그니처 변경 금지.

### 결과 · 2026-07-22 02:03 — S-007 (수행: ClaudeCode 본 세션, D-053 · 리드 24분)
- `WorldMinigameManager`(District 도착 후 phoneCallDelaySeconds 뒤 발화·방문당 1회) · `MinigameRhythmView`(방향키 시퀀스 표시·판정·성공/실패 2단) 납품. 신규 이벤트 3종 `PhoneRang`·`MinigameRequested`·`MinigameEnded`(로그 동반). PlayerInputHandler가 미니게임 중 이동·점프·상호작용 잠금.
- Core에 MinigameCanvas(오버레이, sortOrder 95) — CoreSceneBuilder가 조립.
- 관찰: `PhoneRang ← 박말순 → MinigameRequested`(패널 열림) → 무입력 4.8초 → `MinigameEnded 실패 (0/4)` → Debt 벌금 반영 확인.
- 부수 적발 2건: ① **Core 씬 매니저 이중화**(정본 Managers + 그레이박스 __gb_Managers 공존 → 싱글톤 중복 파괴가 SceneFlow까지 삭제) — Core 정본 재조립으로 해소, 거리 무대는 District·Greybox 씬 소유로 정리. ② **커넥터 콘솔 버퍼 상한** — 장시간 Play 세션에서 최신 로그가 조회 누락(거짓 음성) → 관측 전 `console --clear` 규칙 추가.

---

## S-008 · 발주 2026-07-22 01:40 → ClaudeCode (본 세션 실행 — Camp 그레이박스 무대)

목표: Camp 씬에 짐싣기 무대(지면·트럭 소품·적재존 마커·플레이어·카메라)를 빌더로 조립 — S-005 스크립트 도착 시 꽂히기만 하면 되는 상태로 선행.

입력: GreyboxStageBuilder 패턴(__gb_ 접두어·멱등 Clear·리플렉션 참조 주입) · Camp.unity 현행(전환 UI만).

기대: `Editor/CampStageBuilder.cs` 신설(직교 추가 — 매니페스트 외 빌더, 기록 필수) — 메뉴 `DontLate/Build Camp Stage`. 지면·보도, 트럭(박스 조합 소품), 적재존 패드 3개(`__gb_LoadZone_01~03` — LoadingZone.cs 도착 시 부착 지점), 박스 더미, 플레이어+WalkableVolume+카메라. Travel은 무대 불요 판단(TravelMapView UI가 전부 — S-006 PR 후 관제가 캔버스 조립).

수용기준: ① 컴파일 ② 콘솔 0 ③ Camp 씬 빌드 후 Play 진입 시 플레이어 이동 가능·무대 렌더 확인 + 스크린샷.

실패시: [BLOCKED] 보고.

### 결과 · 2026-07-22 01:43 (리드 3분)
- `Editor/CampStageBuilder.cs` 신설(직교 추가 — 빌더 선례 준수) — 메뉴 `DontLate/Build Camp Stage`, 멱등.
- GreyboxStageBuilder 헬퍼 11종 internal 개방(BuildGround·BuildPlayer·ConfigureCamera 등) — 씬별 빌더가 재사용.
- 검증: 컴파일 ○ · 콘솔 0 ○ · Camp Play에서 배송원·트럭·박스더미·적재패드 3개 렌더 + CharacterController 이동 확인 ○. 증거: `Screenshots/s008_camp_stage.png`. 씬 파일은 규칙대로 미커밋(빌더가 정본).
- Travel 무대는 계획대로 생략 — S-006(TravelMapView) PR 도착 후 관제가 캔버스 조립.

---

## S-009 · 발주 2026-07-22 02:19 → ClaudeCode (본 세션 실행 — 플레이 피드백 6건 반영)

목표: 님 플레이 관찰 6건 시공 — ① Home 인트로 전화(박말순 다이얼로그→버튼 등장) ② BGM은 인트로 종료 후 시작 ③ Camp 적재를 "박스 들어 트럭에 싣기"로 교체(패드 폐지) ④ Travel 카메라 부재 수리 ⑤ District 배송 판정 신뢰성(센서 버퍼 8 포화 진단) ⑥ 배송 완료 시 비콘 완전 소멸 + "집으로"에서 정산 패널 표시 후 Home 이동(Camp 자동정산 폐지).

수용기준: ① 컴파일 ② 콘솔 0 ③ Play 관찰 — Home 도착 즉시 대화 시작·버튼 숨김→대화 끝 버튼 등장·BGM 개시 / Camp에서 박스 E픽업→트럭 E적재(OrderAccepted+트럭에 상자 쌓임) / Travel 게임뷰 정상 / District 패드 E 판정 재현성 / 완료 시 비콘 소멸 / 집으로→정산 패널→확인→Home.

실패시: [BLOCKED]. 신규 파일(HideDuringDialogue·SettlementView)은 직교 추가로 기록.

### 결과 · 2026-07-22 02:37 (리드 18분)
- ① Home 도착 시 박말순 전화 자동 재생(WorldDialogueManager `_homeIntroScenario`·하루 1회) + 진행 버튼은 대화 종료까지 숨김(`HideDuringDialogue` — 상시 활성 캔버스에 부착, 자기은닉 구독 단절 함정 주석화). 관찰: 대화 중 buttonActive=False → 종료 후 True.
- ② BGM 첫 DialogueEnded까지 보류(WorldAudioManager `_holdUntilFirstDialogue`). 관찰: 대화 종료 직후 Day 슬롯 `Sunlit_Seoul_Afternoon` 개시.
- ③ Camp 적재 교체: 패드 폐지 → 박스 3개(PickupBox, 주문별) E픽업 → 트럭 짐칸 뒤 E → `OrderAccepted`+짐칸에 상자 스택(LoadingZone 개조: `_stackRoot`·상한 검사·빈손 안내). 관찰: carrying True→False·cargo 1·stacked 1.
- ④ Travel 카메라 생성(SceneFlowUIBuilder — NAVY 솔리드, 리스너 없음 D-041). 관찰: cam=True, "No camera" 워터마크 소멸.
- ⑤ 판정 신뢰성: 범인 = InteractionSensor `MAX_HITS 8` 포화(District 콜라이더 다수가 버퍼 점유 → 비콘 탈락) → 32로 확장. 관찰: 패드 위 focus=DeliveryPoint 즉시 획득. + 완료 시 비콘 루트째 SetActive(false) — 패드·빛기둥 전부 소멸(beacon Find=null). + 엣지 수정: 지각으로 적재에서 빠진 건은 인증 불가(상자를 떨어뜨리지 않음 — IsInCargo 선검사).
- ⑥ "집으로" → 정산 패널(`SettlementView`, WorldDebtManager.SettleNow 표시: 상환 ₩4,800·벌금 -₩200·잔액 ₩0·남은 빚 ₩5,200) → 확인 → Home 전이 관찰. Camp 자동정산 폐지.
- 직교 추가 2: `UI/HideDuringDialogue.cs` · `UI/SettlementView.cs` (D-054로 기록).

---

## S-010 · 발주 2026-07-22 02:45 → ClaudeCode (본 세션 실행 — 플레이 피드백 2차 6건)

목표: ① 해·달 포물선 교차 ② 별 궤적 ③ 비콘 E 간헐 무반응 ④ 대화 엔터·좌클릭 ⑤ 집 창문 햇살+천장 ⑥ HUD/정산 빚 표시 불일치.

### 결과 · 2026-07-22 03:07 (리드 22분)
- ①② `SkyBodyOrbit`(직교 추가) — 해 디스크(신설·정점 13시)와 달(정점 1시)이 지평선 아래 피벗 반타원 궤도로 교차, 별밭은 30°/일 회전. 관찰: 13시 sun y=8.5/moon y=-12.5 ↔ 1시 정반대 · 별밭 z회전 2.2°→22.6°(17h). 증거: `Screenshots/s010_sky_night.png`(달토끼 남중).
- ③ 원인 = **적재 목록에 없는 건은 인증 불가**인데 무반응이라 버그로 보였음(캠프 미적재·지각 실패 시). 픽업 단계 가드(`_requireInCargo` — 거리 상자 전용) + 전 거절 경로에 사유 로그. 관찰: cargo 없이 픽업 시도 → 거부+로그.
- ④ 대화 진행에 엔터·넘패드엔터·좌클릭 바인딩 추가 + 박스 버튼과 같은 프레임 이중 발화 디듀프.
- ⑤ Home 천장 + 창문 스팟(웜 #ffe2b0·소프트섀도) — 실내 어둡고 바닥에 햇살 웅덩이. 스크린샷 확인.
- ⑥ 재현 불가(단일 SO·단일 기록자 검증) — 원인 추정: 님 플레이와 관제 씬 재빌드가 **한 에디터에서 교차**한 혼합 상태. 재발 차단: 정산 패널 동안 `timeScale=0`+중복 열기 방지+예외 경로 복구, HUD가 DebtSettled 즉시 반영. 부수: **District 카메라의 잔존 AudioListener 제거**(스크린샷의 "2 audio listeners" 경고 해소 — D-041 위반 잔재).
- 검증: 컴파일 ○ 콘솔 0 ○ Play 관찰 상기 + 정산 timeScale 0↔1 왕복·Home 전이 ○.

---

## S-011 · 발주 2026-07-22 03:45 → ClaudeCode (본 세션 실행 — 피드백 3차 + Trellis2 반입)

목표: ① Home 창문 실제 개구부(뚫린 벽·바깥 보임·시간별 입사각 — SunShaft 삭제) ② 해 흰색(마인크래프트풍) ③ 스마트폰 "배송상차" 바코드 시스템(Tab 슬라이드·박스 클릭 스캔·스캔한 짐만 픽업·운송장 목록 표·중복 경고) ④ Trellis2 반입 2종(편의점 store_2·한국식 가로등) 검역·계약경로 이동·스왑.

검역 기록: store_2 = 485,891 tris(경고 — 예산 160배) · street_lamp_wood = 95,724 tris(경고 — 64배) · 둘 다 텍스처/버텍스컬러 없음(회색). 경고 모드 원칙(차단 아님)으로 반입 진행, 데시메이트·텍스처는 민지 재요청.

수용기준: ① 컴파일 ② 콘솔 0 ③ Play — 창구멍으로 하늘 보임+시간별 광선 각도 / 해 흰색 / Tab 폰 열림·박스 클릭 스캔·목록 갱신·중복 경고·미스캔 박스 E 거부 / 새 가로등 8기 일괄 교체·편의점 District 배치.

### 결과 · 2026-07-22 03:56 (리드 11분)
- ① Home 뒷벽 4분할로 창 **실제 개구부** — 유리·이미시브 판·SunShaft 전부 제거. 창 너머 스카이박스(하늘·원경) 보임, Core 태양 직사광이 시간대별 각도·색으로 스민다(8:30 웜 바닥 ↔ 16:30 상이 확인, 스크린샷 2장).
- ② 해 디스크 흰색(×2.2 이미시브) — 캐시 머티리얼에도 매 빌드 강제.
- ③ 폰 "배송상차": `PhoneView`(직교 추가) + PhoneCanvas(CoreSceneBuilder). Tab 슬라이드(unscaled 0.22s)·호버 송장 표시·클릭 스캔(등록은 WorldDeliveryManager.RegisterBarcode — 신규 이벤트 `BarcodeScanned`)·중복 경고·목록 표(No/운송장 DL-XXXX/순번=마감빠른순/목적지). **스캔 짐만 픽업**(`_requireScanned` — Camp 상자). 관찰: 미스캔 E 거부 → 스캔 true·중복 false → 목록 "1 DL-0007 1 행복빌라" → 픽업 성공.
- ④ Trellis2 반입: 가로등 8기 일괄 교체(전략 B + 프리팹 Visual 재구축 — 구 fbx 덮어쓰기 시 메시 서브에셋 ID 불일치로 링크 파손 실측) · 편의점 District 12슬롯 배치(빌더가 Prefabs/Auto 풀 배선 + 제너레이터가 층수 높이 정규화·전면 Z정렬 신설). 검역·출처·H12(데시메이트·텍스처)는 orders/art.md·assets_manifest 기록.
- 검증: 컴파일 ○ 콘솔 0 ○ 스크린샷 3장(`s011_home_830`·`s011_district_trellis` 외).

### 결과 · 2026-07-22 04:10 — S-011 후속 (폰 스캔 무반응 수리 + 우측 이동, 리드 10분)
- 무반응 원인 실측: 마우스 시선 레이의 첫 히트가 `__gb_Walkable@38.2`(거리 전체 트리거) — 박스(42.5)는 그 뒤라 단일 Raycast가 영원히 놓침. **RaycastAll 전수에서 PickupBox만 골라 최근접 선택**으로 교체 → #7 검출 확인.
- 폰 패널 좌하단 → **우하단**(anchor 1,0 · x=-28) 이동, Core 재조립 확인.

---

## S-012 · 발주 2026-07-22 04:11 → ClaudeCode (본 세션 실행 — 수제 택배상자 반입·스왑)

목표: 민지 수제 box.fbx를 `prop_box_parcel`(BOM 규격 0.4~0.75u·<1500tri)로 반입해 게임 내 모든 상자에 스왑.

### 결과 · 2026-07-22 04:15 (리드 4분)
- 검역: **106 tris — 폴리 예산 첫 통과 반입물** · 원크기 2.48u. 계약 경로 이동 → 팩토리 자동 프리팹.
- `CreateParcelBox` 공용 헬퍼(그레이박스 빌더) — 프리팹 있으면 0.7u 정규화 인스턴스, 없으면 큐브 폴백(스왑 계약 유지). Camp 상자 3·District 거리 상자·LoadingZone 짐칸 스택(`_boxVisualPrefab`)에 적용.
- 관찰: Camp 씬에서 테이프 디테일 살아있는 골판지 상자 3개 확인(스크린샷). 컴파일 ○ 콘솔 0 ○.

---

## S-013 · 발주 2026-07-22 04:17 → ClaudeCode (본 세션 실행 — 님 버그 리포트 3건)

목표: ① E키 NRE ② Tab 폰 무반응 ③ 박스 하이라이트가 테이프만 빛남.

### 결과 · 2026-07-22 04:26 (리드 9분)
- ①② 공통 원인 = **콘텐츠 씬 단독 Play**(Camp만 열고 Play) — Core 미로드로 매니저·폰 캔버스 부재 → Instance NRE·폰 없음. 처방: `EnsureCoreLoaded`(직교 추가, Utils) — 단독 Play 감지 시 Core를 Additive 사후 로드, CoreBootstrap은 사후 로드를 감지해 Main으로 끌고 가지 않고 현재 씬 도착만 통지. 플로우 캔버스 빌더가 전 5씬에 자동 배치. + PickupBox 매니저 부재 가드.
- ③ 원인 = 수제 박스가 멀티 렌더러/슬롯(본체+테이프)인데 하이라이트가 첫 슬롯만 교체. 처방: PickupBox가 Awake에 전 렌더러·원본 머티리얼 캐시 → 하이라이트 시 **전 슬롯 교체·해제 시 원복**.
- 관찰: Camp 단독 Play → scenes=2(Core 자동)·flow/delivery/phone 전부 존재·active=Camp 유지 / 하이라이트 3→3 슬롯 시안 전환·3→3 원복 / 스캔 후 E 픽업 정상·콘솔 에러 0.

---

## S-014 · 발주 2026-07-22 04:27 → ClaudeCode (본 세션 실행 — "등록된 송장인데 E로 안 잡힘")

### 결과 · 2026-07-22 04:31 (리드 4분)
- 원인 = 버그 아님: #7 행복빌라 **마감 10:00이 이미 경과(화면 11:53)** → 지각 실패로 적재 목록에서 제거된 상태(콘솔 메시지 그대로). 단, 두 가지 실결함 처방:
  ① **구조적 지각**: 마감 10:00은 인트로 대화·상차·이동(+30~90분)을 거치면 물리적으로 못 맞춤 → **14:00으로 완화**(에셋+빌더 기본값).
  ② **지각의 비가시성**: 콘솔에만 보임 → 폰 운송장 목록에 상태 표시 — 지각=빨강 취소선+"지각" · 완료=회색+"✓완료" (PhoneView가 DeliveryCompleted/Failed 구독).
- 관찰: 적재 후 15:00 점프 → 폰 목록 `<s>1 DL-0007 1 행복빌라 301호</s> 지각` 표시 확인. 컴파일 ○ 콘솔 0 ○.

---

## S-015 · 발주 2026-07-22 04:46 → ClaudeCode (본 세션 실행 — 피드백 4차 6건)

목표: ① 폰에 목적 구역+남은 시간 ② 구역 도착 시 해당 박스 실개수 스폰 ③ 배송지 수만큼 비콘 패드 ④ 지각=빚 즉시 증가+플로팅 금액(성공=돈 플로팅) ⑤ Home에서도 해·달·별(별 배경 더 어둡게) ⑥ 해 머티리얼 무광원(Unlit).

### 결과 · 2026-07-22 04:56 (리드 10분)
- ① 폰 목록에 부제 줄 — `└ 행복빌라 구역 · 남은 359분` (구역=주문 SO 신설 필드, 남은분=ClockTicked 분 단위 갱신·30분 이하 앰버·경과 시 빨강).
- ②③ **구역 시스템** — `DeliveryOrderSO.district` + `GameState.currentDistrict`(이동맵 노드가 기록) + `DistrictCargoSpawner`(직교 추가): 도착 구역의 cargo 건만큼 **내린 박스·집앞 비콘 패드를 실개수 스폰**(비콘=Prefabs/Hand/BeaconPad 신설). 정적 __gb_Box·__gb_Beacon 폐지. 관찰: 3건 적재 후 행복빌라 구역 → 박스 2·비콘 2 (달빛맨션 1건 제외) 정확.
- ④ 벌금 즉시 빚 가산(WorldDebtManager — pending 폐지, `DebtIncreased` 이벤트 신설) + HUD **플로팅 금액**: 지각/미니게임 → 빚 라벨 곁 빨강 `+₩300` 상승·페이드, 배송 성공 → 돈 라벨 곁 시안 `+₩보상`. 정산 패널은 상환만 표시. 관찰: 23시 점프 → 빚 10,000→11,100(지각3+미니게임1) 즉시 · 플로팅 `[+₩300]` 포착.
- ⑤ Home 창밖 하늘 — 별밭·달·해 동일 원경 + 방 창 대역에 맞춘 저궤도(정점 y≈1.5)·소형(2.2u). 별 배경 _SkyGradientStrength 0.6→0.4 (전역). 스크린샷 `s015_home_night_sky.png`.
- ⑥ 해 = URP **Unlit** 순백(HDR ×1.6 — 블룸 미세) — 광원 무관.
- 부수 적발·수리: 단독 Play 시작 씬이 첫 전이에서 **언로드되지 않는 엣지**(_hasCurrent=false) → `SceneFlow.AdoptCurrent` 인계로 해소(관찰: Camp→Travel 후 Camp 언로드 확인).
- 검증: 컴파일 ○ 콘솔 0 ○.

### 결과 · 2026-07-22 05:05 — S-015 후속 (배경 기울어짐, 리드 5분)
- 원인 = 별밭 스핀이 **쿼드 트랜스폼 회전**이라 시간이 갈수록 쿼드 모서리(검은 쐐기)가 화면에 들어옴.
- 처방 = 쿼드 고정 + **셰이더 UV 회전**(_Rotation 신설 — 절차 별밭이라 무한 회전에도 경계 없음). 하늘 그라디언트는 원 UV 유지(수평 고정). SkyBodyOrbit Spin 모드가 MPB로 구동(밤 페이드 MPB와 공존).
- 관찰: 22:50에 quadRot=0.0 · shaderRot=0.5rad · 지평선 수평·쐐기 소멸(스크린샷). 컴파일 ○ 콘솔 0 ○.

---

## S-016 · 발주 2026-07-22 05:14 → ClaudeCode (본 세션 실행 — 피드백 5차 7건)

### 결과 · 2026-07-22 05:20 (리드 6분)
- ① HUD 배송 카드가 **실제 든 건**의 주소+구역을 표시 — 결함 수리 동반: 기존 구현이 "적재 목록 첫 건"을 읽어 든 것과 다른 주소가 나올 수 있었음(PackagePickedUp 페이로드 기반으로 교체).
- ② 비콘 패드 위 포커스 시 **주소 월드 라벨**(시안, 패드 위 1.7u — BeaconPad 프리팹 재생성). 관찰: 포커스 → "청운상가 2층" 표시.
- ③ 폰 최상단에 **"가야 할 구역"** — 미처리 건 중 최급 마감 건의 구역(앰버 볼드). 관찰: "가야 할 구역 행복빌라 구역".
- ④ 검증: 스포너는 cargo(실은 것)만 순회 — 1건만 싣고 도착 시 박스 1·비콘 1 정확(안 실은 건 스폰 0).
- ⑤ `CameraFollowX`(직교 추가) — X만 SmoothDamp(0.25s)+데드존 1.5u, Y·Z·각도 고정(픽셀 밀도 보호). 그레이박스·캠프 카메라에 부착.
- ⑥ 캠프 상자 = 실물 물리(Rigidbody+솔리드 콜라이더) — 관찰: 아래 상자 픽업 시 위 상자 y 0.70→0.00 낙하. 픽업 시 kinematic 잠금·드롭 시 해제.
- ⑦ 캐리 중 좌클릭 → 마우스 방향 던지기(`throwSpeed` 튜닝 노출, 마우스 레이→플레이어 Z평면 조준, 위로 1.5 보정 포물선). 폰 열림 중엔 스캔 클릭에 양보. 관찰: 던지기 후 carrying=False·상자 물리 전환.
- 경계 편차 기록: PlayerStatusManager가 `PhoneView.IsOpen`(UI 정적 프로퍼티)을 읽음 — Player↔UI 직접 참조 1건(이벤트化는 과설계 판단, 소급 검토 대상).
- 검증: 컴파일 ○ 콘솔 0 ○.

---

## S-017 · 발주 2026-07-22 15:10 → ClaudeCode (본 세션 실행 — 던지기 후속 2건)

### 결과 · 2026-07-22 15:20 (리드 10분)
- ① 드롭·던진 상자 **재픽업 가능** — DropVisualAsPhysics가 PickupBox를 더는 파괴하지 않음(콜라이더 실체화+RB 활성 유지). 관찰: 던짐 → E 재픽업 carrying=True.
- ② **던져 넣기 배송** — DeliveryPoint.OnTriggerEnter: 물리로 굴러온 상자가 패드 트리거에 닿으면 주문 일치·적재 확인 후 즉시 인증(상자 소멸+보상). 손에 든 상자는 콜라이더 꺼져 있어 미발동(E 경로 그대로). 관찰: 상자 투척 착지 → money 0→5,000·비콘 소멸.
- 검증: 컴파일 ○ 콘솔 0 ○.

---

## S-018 · 발주 2026-07-22 17:16 → ClaudeCode (본 세션 실행 — 디스코드 연동 2단계)

### 결과 · 2026-07-22 17:21 (리드 5분)
- `scripts/discord_notify.py`(신규): 텍스트+파일(스크린샷) 웹훅 전송. URL은 **커밋 금지** — `git config dontlate.webhook`에서 읽고, 미설정이면 조용히 생략(기록 정본=git 원칙).
- `hooks/post-commit`(신규): 커밋 diff에서 **발주 헤더(📦)·결과 블록(✅)·INBOX 신규 행(🔔)**을 추출해 자동 알림. 알림 실패는 커밋에 무영향(항상 exit 0).
- 관제 PC 웹훅 설정 완료(#git 채널). 단발 테스트 발사 ○ — 이 커밋이 곧 훅 경유 실전 1호.
- 1단계(GitHub→디스코드 공식 연동)는 님이 완료 — push 5커밋 알림 실착 확인.

---

## S-019 · 발주 2026-07-22 17:45 → ClaudeCode (본 세션 실행 — 대형 6건: 박스HP·자판기·스태미나·하우징·음악제어·폰OS)

목표: ① 택배상자 취급주의 — HP·낙하 파손·폭발 이펙트·머리 위 HP바 ② 자판기(E=1,000원 or 상자 투척 명중 → 드링크 배출) ③ 스태미나 개편(걷기<달리기, 상자 무게 가중) ④ 하우징(가구 구매·인벤토리·배치 — 구매는 폰) ⑤ BGM 폰 제어(재생/정지·볼륨·다음곡·곡선택) ⑥ 폰 홈 화면+앱: 택배(바코드·히스토리·수익)/음악/금융(투자)/은행(잔고).
부기: 프리뷰 회신 자동화는 **반자동 확정** — PR 알림 후 사람이 "PR 확인해봐" 트리거 (님 결정).

### 결과 · 2026-07-22 17:59 (리드 14분)
- ① **취급주의 상자** — `BoxDurability`(HP·안전속도 3m/s 초과분 ×12 피해·파편 6개 폭발·머리 위 HP바 2쿼드, 피해 후만 표시). Camp·District 상자 전부 실물 물리+내구도. 관찰: 20m/s 낙하 즉사·중간 낙하 생존+HP바 표시. 튜닝 25→12/㎧ 완화(던지기 7m/s 즉사 방지). 부수 적발: **스택 스폰 겹침 → 물리 밀어내기 자폭**(실측) — 피라미드 배치로 수리. 파손 시 주문은 cargo 유지(구역 재진입 재스폰).
- ② **자판기** — E=`vendingPrice`(1,000원) 결제 배출, 상자 투척 명중(2m/s+)도 배출(공짜). 관찰: 5,000→4,000원·드링크 스폰.
- ③ **스태미나 개편** — 걷기 2/s < 달리기 6/s, 캐리 시 무게(kg)×0.35/s 가산(무게 미지정 폴백 유지). 주문 SO에 weight 신설.
- ④ **하우징** — `FurnitureSO` 카탈로그 4종(화분·스탠드·러그·TV, 색박스+prefab 스왑 계약) · 폰 가구앱 구매(TrySpend)→인벤토리→배치 대기→Home 바닥 클릭 배치(`HomeFurniturePlacer`, 세션제 재생성). 관찰: 구매 차감·배치 비주얼 생성.
- ⑤ **음악 제어 API** — TogglePause·SetVolume·NextTrack·TrackNames·PlayTrackAt. 관찰: 정지 왕복·볼륨 30%·Sunlit→Seoul_Alley 전환.
- ⑥ **폰 OS v2** — 홈 그리드(앱 5종)+화면 6종 런타임 생성(빌더는 본체 패널만). 택배(상차 스캔은 이 화면에서만+히스토리 4건+누적 수익)·음악·금융(늦코인 — 결정론 시세 랜덤워크·매수/전량매도, WorldDebtManager 경제 API)·은행(잔고·빚·통계)·가구. 관찰: 화면 6·금융 시세 표기·매수 0.912개·가구 구매.
- 직교 추가 5(D-059): BoxDurability·VendingMachine·HomeFurniturePlacer·FurnitureSO·(GameState 구조체 2종). 검증: 컴파일 ○ 콘솔 0 ○.

---

## S-020 · 발주 2026-07-22 18:18 → ClaudeCode (본 세션 실행 — 파손 밸런스·HP바 버그·폰 UI 실사화)

요구 (님 원문 요약):
- ① 상자가 너무 쉽게 파손됨 + HP바가 안 나타남 (버그)
- ② 폰 앱들 UI를 실사에 가깝게 개선. 배경화면·버튼·아이콘은 플레이스홀더로 관리 중인지 확인
- ③ 커밋 서명 Opus 4.8 오기를 커밋 내용에 솔직하게 기록

수용기준: 던지기 1회에 반파되지 않는 내구 밸런스 · HP바 육안 확인 · 폰에 상태바/배경/아이콘 타일 스타일 적용+실아트 스왑 슬롯(Sprite) 노출·BOM 등재 · 서명 정정 기록 커밋.

### 결과 · 2026-07-22 18:27 (리드 9분)
- ① 내구 완화: 안전 5m/s·초과분 ×8 (구 3·12 — 기본 던지기 1회에 반파되던 것 해소). 관찰: 9.3m/s 충격 2회에 HP 100→31 생존. **HP바 미표시 원인 = 쿼드 y180° 회전(카메라 반대편)** — 무회전으로 수리, 표시 확인. 같은 결함이던 비콘 주소 라벨도 무회전으로 프리팹 재생성.
- ② 폰 UI 실사화: 배경화면(그라디언트)·상태바(실시간 시계+LateTel LTE)·앱 아이콘 라운드 색타일(9-slice 코드 생성)·버튼 라운드 통일. **부수 적발**: Pretendard에 이모지 글리프 없음(콘솔 □ 치환 경고 실측) → 전 이모지를 폰트 안전 텍스트(택·음·금·은·가 등)로 교체, Unicode 경고 0.
- ② 플레이스홀더 등재: 정직 보고 — 기존엔 대장에 없었음. `PhoneView._wallpaper`·`_appIcons[5]` Sprite 스왑 슬롯 노출 + BOM §6에 ui_phone_wallpaper·ui_phone_icon_* 등재(비면 코드 폴백 계약).
- ③ 서명 정정 기록: ai_evidence.md에 "Opus 4.8 표기=하네스 템플릿 오기, 실수행=Fable 5(관제 직접)" 명기 — 과거 커밋은 히스토리 불변으로 재작성하지 않고 기록으로 갈음.
- 검증: 컴파일 ○ 콘솔 0(Unicode 경고 포함 해소) ○ 폰 구조 실측(배경·시계 08:50·타일 스프라이트·[택] 글리프).

---

## S-021 · 발주 2026-07-22 18:40 → ClaudeCode (본 세션 실행 — HP바 빌보드·주소 가독성·캠프 주문 갱신)

요구 (님 원문 요약):
- ① HP바가 (상자가 굴러도) 항상 카메라를 바라보게
- ② 비콘 위 주소 글자가 픽셀레이트 셰이더에 뭉개져 가독성 최악 — 개선
- ③ 캠프 복귀 시 상자가 재스폰되는데 완료된 주문이라 "이미 등록" — 완료 건은 패스하고 새 목적지로 갱신하는 로직

수용기준: 굴러가는 상자 위 HP바 수평 유지 · 주소가 풀해상 UI로 또렷하게 · 배송 완료 후 캠프 복귀 시 새 주문(새 주소·마감)으로 교체되어 스캔·상차 가능.

### 결과 · 2026-07-22 18:50 (리드 8분)
- ① HP바 빌보드 — LateUpdate에서 매 프레임 세계 기준 재정렬. 관찰: 상자 100° 회전 낙하 중에도 바 기울기 0°.
- ② 주소 표시를 월드 텍스트 → **HUD 풀해상 [E] 안내 병기**로 이전 (픽셀레이트 미적용 Tier H 오버레이). 신규 이벤트 `FocusAddressChanged`(센서 발행, 포커스와 동빈도라 로그 생략) + 비콘 프리팹에서 월드 라벨 제거. 관찰: 패드 포커스 → "[E] 배송 인증 청운상가 2층"(앰버) ↔ 해제 시 "[E] 상호작용".
- ③ **캠프 주문 갱신** — `CampOrderBoard`(직교 추가): Camp 재진입 시 소진 주문(배송 완료 or 마감 경과·미적재·스캔 이력)을 **런타임 신규 주문**으로 교체(목적지 풀 6종·마감 now+240~420분·보상/무게 시리얼 파생·id는 GameState.nextOrderSerial 단일 소유). 손도 안 댄 건은 유지. PickupBox.SetOrder 신설(콜라이더 재활성 포함). 관찰: 완료건(#7) 시뮬 후 박스가 #200 은하빌라(달빛맨션 구역)로 교체·신규 스캔 정상.
- 검증: 컴파일 ○ 콘솔 0 ○.

---

## S-022 · 발주 2026-07-22 18:58 → ClaudeCode (본 세션 실행 — 빌드 메뉴 재편)

요구 (님 원문 요약): 컨텍스트 메뉴에 Build 카테고리를 따로 파서 전부 몰아넣기 + "Build All Scenes" 일괄 기능 + 빌드 원리 설명(채팅).

수용기준: 메뉴가 DontLate/Build/ 아래로 통합 · All Scenes 1클릭으로 전 씬 재조립·빌드세팅 등록까지 완료 · 콘솔 0.

### 결과 · 2026-07-22 19:00 (리드 2분)
- 메뉴 전면 재편: `DontLate/Build/` 카테고리로 통합 — ★ All Scenes(0) · Core(10) · Camp(12) · Home(13) · District(14) · Scene Flow UI(15) · Generate SFX는 상위 유지 · 최초 셋업(21) · Greybox 개발용/Clear(40·41).
- **★ All Scenes 신설** — 씬 파일 확보 → Core → Camp/Home/District 무대 → 흐름 UI → 빌드 세팅 등록 → Core 복귀까지 1클릭. 관찰: 실행 후 활성 씬 Core·콘솔 0.
- 검증: 컴파일 ○ 콘솔 0 ○. 빌드 원리 설명은 채팅 회신.

---

## S-023 · 발주 2026-07-22 19:10 → 정수 (Juice 레인 — P4 3종, 매니페스트 완주)

목표: 매니페스트 잔여 P4 3종 납품 — `WorldJuiceManager` · FadeScreen "늦지마!" 컷인 발동 배선 · `PlayerEffectsManager`. 완료 시 34/34 완주.

입력:
- `docs/JUICE.md` — 이벤트→연출 매핑 표(정본). 구현 범위는 표에 있는 행만(YAGNI).
- `Assets/Scripts/UI/FadeScreen.cs` — `_lateCutIn` 소켓 실재(코드 존재·발동 배선 없음).
- `Assets/Scripts/Events/WorldEvents.cs` — DeadlineWarned·DeliveryCompleted·DeliveryFailed·DebtIncreased 등 구독 지점.
- 카메라 셰이크는 `CameraFollowX`와 충돌 주의 — LateUpdate 이후 오프셋 방식 권장.

기대:
1. `Managers/WorldJuiceManager.cs`: Core 상주 싱글톤, JUICE 표의 연출(펀치 스케일·셰이크·플래시 등)을 이벤트 구독으로 발동. 감각값은 전부 [SerializeField] 노출(사람 튜닝 대상 — 하드코딩 금지).
2. FadeScreen: DeadlineWarned(또는 JUICE 표 지정 이벤트) 시 "늦지마!" 컷인 발동.
3. `Player/PlayerEffectsManager.cs`: 이동 먼지·드링크 음용 이펙트(그레이박스 파티클 수준).
4. CoreSceneBuilder에 Juice 매니저 등록 추가(빌더 배선 — 관제 파일이지만 BuildManagers 1블록 추가는 허용, 충돌 시 관제 우선).

수용기준: ① 컴파일 ② 콘솔 0 ③ Play 관찰 — 배송 완료/지각/경고 각각에서 연출 발동 확인 ④ 매니페스트 직교·감각값 노출 여부 기록.

실패시: [BLOCKED]. JUICE 표에 없는 연출을 창작하지 않는다(사람 감각 영역).

### 결과 · 2026-07-22 20:28 (리드 15분 · 정수 공장)

- **WorldJuiceManager 신규** — DeliveryCompleted: 플래시(α0.35→0 · 0.18s)+체크팝 "✓ +₩5,000" 펀치스케일+히트스톱 0.05s+미세 셰이크 / DeliveryFailed: 레드 비네트 2펄스(0.7s)+히트스톱 0.1s+셰이크(소). 오버레이 캔버스는 런타임 자체 조립(sortOrder 80 — PhoneView 방식, 빌더 diff 최소화).
- Play 실측(발화 프레임 동기 관찰): 완료 → `flashAlpha=0.333 popActive=True popText=[✓ +₩5,000] timeScale=0` · 2초 후 `flashAlpha=0 popActive=False timeScale=1` 복원. 실패 → `vignetteAlpha 램프업 timeScale=0 shakeApplied=-0.0252` · 종료 후 `shakeTarget=cleared camY=1.0000` 원위치.
- **셰이크는 Y축 전용** — CameraFollowX가 X만 쓰고 Y·Z를 보존하므로 충돌이 구조적으로 없음(발주서의 "LateUpdate 이후 오프셋" 취지를 실행 순서 무관 방식으로 충족).
- **FadeScreen** — DeadlineWarned 구독 추가, 발화 시 컷인 `before=False → after=True` 실측. 기존 DeliveryFailed 배선은 유지(최소 diff).
- **PlayerEffectsManager 신규** — 이동 먼지(이동+접지 시 rate 8)·드링크 버스트(RecoverStamina 훅, 허브 경유). 파티클 코드 조립(프리팹 없음). 재조립 후 District·Camp 씬 직렬화 확인(guid 각 1건) — Home·Travel은 플레이어 자체가 없어 해당 없음.
- CoreSceneBuilder BuildManagers 1블록 추가(Juice + 폰트 주입) — Core.unity 직렬화 확인.
- 감각값 전부 [SerializeField](완료 7·실패 6·먼지 4·버스트 2). 매니페스트 직교: 두 파일 다 매니페스트 P4 기재분 — 신규 발명 없음.
- **스킵 2건(사람 게이트)**: ① "미세 줌인" — ARCHITECTURE §2 동결 "줌 변경 금지(밀도 붕괴)"와 충돌 ② "진동" — 게임패드 럼블인데 키보드/WebGL 타겟에 장치 없음.
- 콘솔 에러·워닝 0. 기존 워닝 2건(SceneFlowUIBuilder.cs:271 CS0618 — main pull분, 본 발주 범위 외). 참고: Overlay 캔버스는 unity-cli 스크린샷(카메라 캡처)에 안 찍혀 시각 확인은 상태 실측으로 대체.
- 참고: 로컬에서 Unity가 `Greybox.unity.meta`를 자동 삭제함(씬 본문 없는 meta — 커밋 정책 D-061 후속 검토 대상, 본 커밋에 포함 안 함).

---

## S-024 · 발주 2026-07-22 19:10 → 정수 (품질 레인 — EditMode 테스트 + TECH_SPEC 오디오 절)

목표: 회귀 방지 인프라 개통 — 순수 로직 EditMode 테스트 4종 + TECH_SPEC 오디오 절 신설(D-041 파생 공백).

입력:
- 테스트 대상(전부 순수 로직 — 씬 불요): `WorldDebtManager.SettleNow`(상환 수식)·`CoinPrice/BuyCoin/SellAllCoin`(경계값: 잔액 부족·0원) · `CampOrderBoard.IsConsumed/GenerateOrder`(완료/마감경과/미접촉 3분기·시리얼 증가) · `WorldDeliveryManager.RegisterBarcode`(중복 거부).
- 테스트 어셈블리: `Assets/Scripts/Tests/EditMode/` + asmdef(Editor 플랫폼) — 매니페스트 외 직교 추가로 D-기록.
- 매니저가 MonoBehaviour라 인스턴스화는 `new GameObject().AddComponent<>` + 리플렉션으로 _gameState/_tuning 주입(런타임 SO는 CreateInstance).
- TECH_SPEC은 **동결 문서** — 기존 줄 수정 금지(freeze-guard 차단), **새 절 추가(직교)만**: 믹스 기준(BGM 0.5·SFX 0.7)·압축 규격(Vorbis q30/q70·Compressed In Memory·Streaming 금지)·리스너 소유(Core) — 기존 결정(D-039~043) 요약 전재.

기대: `unity-cli test` 통과 4모듈+ · TECH_SPEC에 오디오 절.

수용기준: ① 컴파일 ② `unity-cli test` 전체 green ③ 테스트가 실제 경계값을 물어뜯는지(항상 통과하는 무의미 검증 금지 — CODE_RULES §8 위장 금지) ④ freeze-guard 통과.

실패시: [BLOCKED].

### 결과 · 2026-07-22 20:39 (리드 10분 · 정수 공장)

- **테스트 4모듈 23케이스 — `unity-cli test --filter DontLate.Tests` 전체 green (23/23·failed 0)**.
  - WorldDebtSettleTests 4: 잔액<빚 전액 상환 · 잔액>빚 초과 보존 · 0원 무변 · DebtSettled 페이로드=반환값 일치.
  - WorldDebtCoinTests 7: CoinPrice 결정론 · **변동성 100배에서도 바닥 100원 클램프 실발동 검증**(min==100 확인) · BuyCoin 잔액부족/0원이하 거부+상태불변 · 매수 수량 수식 일치 · SellAllCoin 0보유/전량매도.
  - CampOrderBoardTests 8: IsConsumed 분기 5(완료/마감경과+스캔=소진/**미접촉=유지**/적재중=유지/마감전=유지) · GenerateOrder 시리얼 증가(200→201, nextOrderSerial 202) · 마감 1435 캡+240분 최소 오프셋 · 목적지 풀.
  - DeliveryBarcodeTests 4: 첫 스캔 등록 · 중복 거부+목록 불변 · **이벤트 중복 발행 금지**(2회 스캔에 1회 발행) · IsScanned 정합.
- **경로 편차 (기술적 강제)**: 발주서 지정 `Scripts/Tests/EditMode/`+asmdef는 **불성립** — 게임 코드가 전부 predefined `Assembly-CSharp`(프로젝트 asmdef 0개)이고 asmdef는 predefined 어셈블리를 참조할 수 없다. 대체 = `Scripts/Editor/Tests/` 무asmdef → `Assembly-CSharp-Editor`(게임 코드 자동 참조·nunit 자동 참조·에디터 전용=빌드 제외 규칙도 충족). Scripts 전체 asmdef화는 구조 변경이라 관제 게이트로 넘김.
- private 접근: `TestSupport` 리플렉션 헬퍼 1파일(필드 주입·메서드 호출). 에디터 모드는 Awake/OnEnable 미실행 — 싱글톤·이벤트 구독 간섭 없음(이벤트 검증은 구독 후 finally 해제).
- **TECH_SPEC 오디오 절 추가** — 표 7행(믹스 0.5/0.7 · BGM: Vorbis q30+Compressed In Memory / SFX: q70+**Decompress On Load**+모노 · Streaming 금지 · 리스너 Core 소유 · BGM 슬롯/플레이리스트 · 반입 계약). 기존 줄 무수정(직교 추가만). 로드타입은 `AudioImportPostprocessor` 실코드 대조로 확정(초안에서 SFX를 Compressed In Memory로 잘못 적었다가 교정).
- 컴파일 통과 · 콘솔 에러/워닝 0.

---

## S-025 · 발주 2026-07-22 21:42 → ClaudeCode (본 세션 실행 — UI 실아트 5종 스왑 소켓)

요구 (님 원문 요약): 민지 UI 이미지 5종(chat_box·chat_box_box·logo·man·sub_logo) 도착 — 플레이스홀더 적용. 드라이브 직접 다운로드 승인(권한 401로 대기 — 소켓 선시공). 라이선스 = 전량 ChatGPT 생성(민지 구두 계약).

수용기준: Art/UI에 bom_id 파일이 있으면 빌더가 스프라이트 사용·없으면 현행 코드 폴백(다이얼로그 박스·화살표, 타이틀 로고·맨·서브) · 파일 도착 후 재조립로 즉시 반영 · 라이선스 등재.

### 결과 · 2026-07-22 21:44 (리드 2분 — 소켓분 선납품, 실아트 대기)
- 스왑 소켓 5종 시공: `CoreSceneBuilder.LoadUISprite(bomId)` 공용 로더(Art/UI/<bomId>.png — Sprite 타입 자동 교정) 신설.
  다이얼로그 박스(ui_dialogue_box — 실아트 시 테두리·네이비 폴백 은퇴, 내부는 투명 클릭 타겟화) · 진행 화살표(ui_dialogue_arrow — ▼ 텍스트 폴백) · 타이틀 로고(ui_title)·서브(ui_title_sub)·늦지마맨(ui_title_man — 좌하 배치, 없으면 요소 생략).
- 드라이브 다운로드 2회 시도 **401** — 폴더가 "링크 공개"가 아님. 공유 변경 대기(파일 도착 → Art/UI 배치 → ★ All Scenes 재조립이면 반영 완료).
- 라이선스 접수: UI 전량 ChatGPT 생성(민지 구두 계약) — 반입 시 등재 예정.
- 검증: 컴파일 ○ 콘솔 0 ○ (폴백 경로라 현행 화면 무변화 확인).

---

## S-026 · 발주 2026-07-22 21:53 → ClaudeCode (본 세션 실행 — 아트팀 발주 4건 + UI 실아트 적용)

요구 (민지 원문 — 디스코드):
- 배경이 뭐든 로고에 비해서 명도 50% 낮추기
- 첫 채팅 ui (어이 총각!!) 할 때 흔들리는 효과 (예시는 너무 과격함)
- 채팅바 ui ▼ 대신 박스 깜박거리게
- 인트로에서 지각압박 어쩌고 반짝이는 효과
+ 님: 드라이브 UI 6종(chat_box·chat_box_box·logo·man·sub_logo·run_button) 다운로드·적용. 라이선스 = ChatGPT 생성(민지 구두 계약).

수용기준: 타이틀 배경 50% 스크림 · 대화 시작 시 은은한 셰이크 · 진행 표시 = 상자 아이콘 깜박 · 서브 로고 반짝 · 실아트 6종 반영 스크린샷.

### 결과 · 2026-07-22 21:59 (리드 6분)
- 실아트 6종 반입·적용: 로고·서브·늦지마맨·시작 버튼(타이틀 4종 art 확인) + 다이얼로그 박스·진행 상자(art 확인). 라이선스 = ChatGPT 생성(민지 구두 계약) — assets_manifest 파일별 등재.
- 아트팀 발주 4건: ① 타이틀 배경 = 검정 50% 스크림(로고 대비 명도 하향) ② 대화 시작 시 **은은한 셰이크**(5px·0.28초 펄린 감쇠 — "과격 금지" 반영) ③ 진행 표시 = ▼ 폐지 → **상자 아이콘 알파 깜박**(UIPulse 0.3~1·5Hz) ④ 서브 로고 **반짝**(0.55~1·2.2Hz).
- 실사고 2건 회수: cp 반입 후 Refresh 없이 재조립하면 미임포트로 폴백 잔존 · textureType=Sprite여도 **spriteImportMode=Multiple+슬라이스 0이면 서브에셋 없음** → 로더가 Single까지 교정.
- 직교 추가: `UI/UIPulse.cs`. 검증: 컴파일 ○ 콘솔 0 ○ (오버레이 UI는 스크린샷 비포착 — 오브젝트 검증, 시각 확인은 님 Play).

---

## S-027 · 발주 2026-07-22 22:11 → ClaudeCode (본 세션 실행 — UI 피드백 7건 + 민지 볼드 요청)

요구 (님 원문):
1. 대화창 찌그러짐 — 이미지 원본 비율을 살리는 쪽으로 맞출 것
2. 대화창 이름·텍스트 볼드 처리 (민지: "이름이랑 내용 볼드처리랑 타이핑처리하고싶어요" — 타이핑은 기시공)
3. 흔들림 효과 너무 미미해서 육안 확인 안 됨 — 강화
4. 오버레이 UI도 스크린샷 도구에 찍을 방법 없는지?
5. 진행 표시 상자가 대화창 밖에 있음 — 민지 목업처럼 안쪽 배치
6. 시작화면 로고·버튼 등 화면 점유 비율을 민지 목업처럼
7. "지각 압박 배달 생존기" — 알파 반짝이 아니라 **사선 광이 왼쪽→오른쪽으로 흘러가는** 효과

수용기준: 대화창 원본 비율 · 이름+본문 Bold · 셰이크 육안 확인 가능 · 오버레이 포함 스크린샷 확보 방법 확립 · 상자 아이콘 대화창 테두리 안쪽 · 타이틀 점유율 목업 근사 · 서브 로고 사선 시머 스윕.

### 결과 · 2026-07-22 22:23 (리드 12분)
- ① 원본 비율: 원흉 = 아트 png의 **투명 여백**(다이얼로그 박스는 1672×941 캔버스 중 실내용 1612×477) — 6종 전부 알파 바운즈로 크롭 후, 박스 렉트를 크롭 비율(3.38:1) 그대로 1350×400으로 재시공. 찌그러짐 소멸 확인.
- ② 볼드: 이름·본문 `FontStyles.Bold` + 이름은 명찰 탭 정중앙 정렬(탭 좌표를 크롭 아트에서 환산). 민지 요청분 중 타이핑은 기시공.
- ③ 셰이크: 5px·0.28s → **18px·0.5s** (민지 예시보다 절제, 육안 확인 가능선).
- ④ 오버레이 캡처 방법 확립: Play 중 `ScreenCapture.CaptureScreenshot` exec — 오버레이 포함 풀 게임뷰가 찍힌다. CLAUDE.md unity-cli 블록에 영구 등재. 본 건 검증도 이 방법으로 수행(타이틀·대화창 캡처 확보).
- ⑤ 상자 아이콘: 테두리 안쪽 흰 영역 우하단(-95, 62)으로 — 줌 캡처로 민지 목업 배치 일치 확인.
- ⑥ 타이틀 점유율: 목업 실측(로고 46%·서브 43%·버튼 23% 폭) 반영 — 크롭 덕에 렉트=실표시 크기. 캡처로 목업 근사 확인.
- ⑦ 시머 스윕: `UI/UIShine.cs` 신설 — Mask 알파 클립 스텐실로 **로고 픽셀 위로만** 사선(18°) 광 스트립이 좌→우로 0.9s 스윕, 1.6s 간격 반복. 알파 펄스(UIPulse)는 서브 로고에서 은퇴. 캡처에 "지각" 글자 위를 지나는 광 포착.
- 검증: 컴파일 ○ 콘솔 0 ○ Play 캡처 3장(타이틀·대화창·상자 줌). 직교 추가: `UI/UIShine.cs`.

---

## S-028 · 발주 2026-07-22 22:44 → ClaudeCode (본 세션 실행 — 대화 셰이크 개편 + WebGL 제출 관통 + 루프·테스트 편의 3건)

요구 (님 원문):
1. 박말순 첫 마디 0.5초만 흔들림 → **박말순이 말하는 동안(타이핑 중) 계속** 흔들 것. 주인공 대사엔 흔들지 말 것
2. 사전과제 제출 기준(웹 빌드 — Pages 배포, 링크 클릭만으로 플레이·유료 라이선스 없이 실행·소스 동일 저장소·공개 권장) — WebGL 빌드 1회 + 웹 배포까지
3. District 씬에 다른 구역으로 가는(Travel 씬) 버튼 — 무조건 집 복귀는 루프상 안 맞음
4. 은행앱 하단 테스트 버튼 — 누르면 +1,000원 (추후 삭제 예정)
5. GS25 삭제 (D-050 집행)

수용기준: 박말순 라인 타이핑 동안 지속 셰이크·주인공 라인 무셰이크 · WebGL 빌드 성공+Pages URL 접속 플레이 · District→Travel 버튼 동작 · 은행앱 +1000 버튼 동작 · GS25 에셋·참조 잔재 0.

### 결과 · 2026-07-22 23:14 (리드 30분 — WebGL 빌드 대기 포함)
- ① 셰이크 지속화: 단발 0.5초 폐지 → **박말순 라인 타이핑 내내** 12px 펄린 셰이크(주인공 라인 제외·타이핑 종료/스킵 시 원위치 복귀). 실측 — 타이핑 중 박스 (2.88, 39.88) 이탈, 종료 후 (0, 50) 복귀, 주인공 화자명 분기 확인.
- ② **WebGL 빌드 성공 + Pages 배포 완료**: https://namkuri.github.io/Don-t-late/ — 6씬·Gzip+압축해제 폴백·43MB. gh-pages 브랜치 push로 Pages 자동 활성화(has_pages=True·HTTP 200). 브라우저 실접속 — Unity 런타임 기동·씬 전이·오디오 컨텍스트 재개를 콘솔로 확인, **에러 0** (워닝 2종: FSR 업스케일 미지원=포스트 패스 스킵·persistentDataPath deprecated — 무해). 회고 2연속 1순위 백로그 해소. 제출 기준 대조: 링크 클릭 플레이 ○ · 유료 라이선스 불요 ○ · 소스 동일 저장소+커밋 이력 ○ · 공개 저장소 ○.
- ③ District에 "다른 구역으로" 버튼(앰버·집으로 아래): 클릭 → Travel 전이 실측. 상태기계는 District→Travel 기허용 — 버튼만 부재였음.
- ④ 은행앱 하단 `[테스트] +₩1,000` 버튼(삭제 예정 표기): money 0→1000 실측. 부수리 — HUD 돈 표시가 이벤트 시에만 갱신되던 잠복 결함(자판기 구매 후 낡음)을 시계 틱 캐치업으로 해소.
- ⑤ GS25 삭제(D-050 집행): 지에스.fbx(1.5M tris)+전용 Material.mat+PBR 텍스처 4장+Prefabs/Test/지에스.prefab 전량 삭제(AssetDatabase 경로 — 셸 삭제는 가드 훅 차단), Greybox 씬 잔여 인스턴스 1개 제거. store_2(Trellis2)와 Material_0.008은 GUID 대조로 보존 판정.
- 검증: 컴파일 ○ 콘솔 0 ○ 전 씬 재조립 ○ Play 기능 3종 실측 ○ 웹 실기동 ○. 잔재: `../dontlate-pages` 미사용 워크트리 1개(가드 훅으로 정리 불가 — 사람 삭제 1건).

---

## S-029 · 발주 2026-07-22 23:58 → 정수 (WebGL 회귀 빌드 — ⚠ 구 번호 S-028: 관제 S-028과 중복 발주라 머지 시 재번호)

목표: 2회고 연속 미이행 항목 해소 — WebGL 빌드 1회 실행, 성공/실패 무관 결과 기록 (셰이더 7종·TMP·폰OS·오디오 19종 = 94커밋어치 웹 미검증).

### 결과 · 2026-07-23 00:00 — [BLOCKED]

- 막힌 것: **정수 PC에 WebGL Build Support 모듈 미설치** — `BuildPipeline.IsBuildTargetSupported(WebGL) = False` 실측.
- 시도한 것: 씬 등록 확인(6종 전부 [on] — M1-05 완료 상태 확인) · 모듈 지원 여부 exec 실측.
- 필요한 것: **사람 손작업** — Unity Hub → 6000.5.3f1 → 모듈 추가 → WebGL Build Support 설치 → 에디터 재시작. 설치 후 공장이 빌드 재개 가능.
- 긴급도: 높음 (회고 명문 "이번에도 안 하면 구조 문제로 격상").

- **해소 (2026-07-22 머지 시 관제 판정)**: WebGL 빌드·Pages 배포는 관제 S-028이 같은 시간대에 완료(https://namkuri.github.io/Don-t-late/) — 본 건 BLOCKED는 무의미화. 정수 PC 모듈 설치는 향후 공장 빌드 필요 시로 이월. 교훈: 회고 백로그가 두 세션에서 동시 착수됨 — **백로그 착수 전 대장 선점 기록** 규칙 필요.

---

## S-030 · 발주 2026-07-22 23:38 → ClaudeCode (본 세션 실행 — HP바 화질·UI 겹침·가구 배치 대개편)

요구 (님 원문):
1. 박스 HP 화질 너무 구림
2. 왼쪽 상단에 UI 겹치는 게 있음 (씬 라벨 ↔ 배송 카드 중첩)
3. 가구 배치: 보유 중에서 **선택**해서 배치 · 배치 위치에 **블루프린트(고스트)** 표시 · **R 회전** · **ESC 취소**(블루프린트 삭제) · 인벤토리 아이템명 **한글** · 구매 시 **돈 차감 연출+효과음** · 인벤토리 늘어나면 버튼과 안 겹치게 **스크롤**

수용기준: HP바 가독(픽셀화에 뭉개지지 않음) · 좌상단 중첩 소멸 · 가구 선택→고스트→R회전→클릭 배치→ESC 취소 전 흐름 동작 · 한글명·스크롤·차감 연출 확인.

### 결과 · 2026-07-23 01:46 (리드 128분 — 그중 ~110분은 백그라운드 재조립 exec 행잉 방치 사고, §아래)
- ① HP바 풀해상 이전: 월드 쿼드(480×270 픽셀화에 뭉개짐) 폐지 → 상자당 소형 **오버레이 캔버스**(sort 5)가 WorldToScreenPoint로 머리 위를 추적. S-021 주소 라벨과 동일 처방. 줌 캡처 — 앰버 fill 경계 선명.
- ② 좌상단 겹침: 씬 라벨(집—아침·물류캠프·이동)을 좌상 → **상단 중앙 y-78**로 이전 — HUD 배송 카드(좌상)·BGM 디버그 줄과 3자 분리. 캡처 확인.
- ③ 가구 배치 대개편: 인벤토리를 **종류별 묶음(한글명 ×개수) 스크롤 목록**(RectMask2D+ScrollRect, 하단 구매 버튼과 영역 분리)으로 — 행 클릭=그 가구 배치 개시(폰 자동 닫힘). HomeFurniturePlacer에 **시안 반투명 블루프린트**(URP Lit 투명 전환, 마우스 추적·방 클램프)·**R=45° 회전**(배치 각도 GameState 보존 — PlacedFurniture.rotationY 직교 추가)·**ESC=취소**(고스트 삭제·가구는 인벤 잔존). 구매 차감 연출 = `WorldEvents.MoneySpent` 신설(저빈도·로그) → HUD 붉은 플로팅 −₩ + 코인 SFX — TrySpend 공용이라 자판기도 함께 받는다. 실측: 구매 10000→8000·고스트 생성·행 클릭 경로. R/ESC 손맛은 님 Play 몫(키 시뮬 불가).
- 부수리: 시계 틱마다 인벤 행 재구축→스크롤 리셋되는 함정 — 인벤 시그니처 캐시로 변화 시에만 재구축. 정수 머지분 CS0618 워닝 2건 청산.
- 검증: 컴파일 ○ 콘솔 0 ○ 재조립 ○ Play 실측(구매·고스트·HP바·라벨) ○ GameState 테스트 잔여 원복 ○.
- ⚠ 운영 사고: 재조립을 백그라운드로 돌린 뒤 **exec 행잉을 폴백 없이 방치 — 사람이 2시간 뒤 "아직도 대기중?"으로 적발**. 처방: 장시간 unity-cli exec는 백그라운드 금지(전경+타임아웃 분할), 백그라운드 필수 시 폴백 타이머 동반. 타임스탬프 3연발과 함께 §3-13 계열로 회고 대상.

---

## S-031 · 발주 2026-07-23 02:02 → ClaudeCode (본 세션 실행 — 하우징 심화 6건 + 전화 UX + 드링크 루프 + 마감 여유)

요구 (님 원문 요약):
1. 배치된 가구 클릭 → 배치 재개(집어 들기)
2. 배치 시 그리드 스냅
3. 씬 기존 침대도 가구화
4. 가구 앱에 벽지·바닥 교체 추가
5. TV는 벽 설치 가능하게
6. 가구 placeholder(스왑 소켓) 재점검
7. 캠프에서 싣는 중 마감돼버리는 경우 — 마감 여유 확보
8. PhoneRang → 즉시 미니게임 금지: 폰이 열리며 박말순 수신 화면(받기/거절 2버튼), 받으면 폰 화면 안에서 미니게임 진행
9. 에너지 드링크 섭취 시 힐 이펙트
10. 자판기 드링크 = 바닥 드롭 → E로 잡기 → 좌클릭 섭취

수용기준: 재배치·스냅·침대 이동·벽지/바닥 교체·TV 벽부착 각 동작 · placeholder 점검 보고 · 캠프 적재 중 마감 사례 소멸 · 수신 화면 2버튼 흐름 · 드링크 이펙트·3단 루프(드롭→E→좌클릭) 동작.

### 결과 · 2026-07-23 02:15 (리드 13분)
- ① 재배치: 배치물마다 `PlacedFurnitureVisual` 마커+콜라이더 — 클릭=집기(각도 유지·인벤 복귀·고스트 재진입). 침대 시드 스폰에 마커·콜라이더 부착 실측.
- ② 그리드 스냅 0.5u — 바닥·벽 배치 공통.
- ③ 침대 가구화: HomeStageBuilder 고정물 은퇴 → `fur_bed`(₩15,000·시드 전용) 세션 1회 자동 배치(GameState.bedSeeded). Play 실측 — Furniture_fur_bed (-2.5, 0.25, 2) 스폰.
- ④ 벽지·바닥: `HomeDecorator` 신설(MPB — 머티리얼 에셋 무오염) + 폰 가구앱 순환 버튼 2종(벽지 4·바닥 4 팔레트, 무료). 실측 — index 2(민트) 주입 시 벽 (0.55, 0.72, 0.65) 적용·캡처 민트 확인.
- ⑤ TV 벽부착: FurnitureSO.wallMountable(TV만 true) — 벽 레이캐스트 우선, 법선 방향 자동(yaw), R은 바닥 배치만. 코드 경로 검증(마우스 시뮬 불가 — 사람 확인 필요).
- ⑥ placeholder 점검: 가구 5종 전부 `prefab` 소켓 빈 상태(색 큐브 폴백) — 스왑 계약 정상. 실모델 도착 시 소켓만 채우면 됨(민지 발주 후보: fur_bed·plant·lamp·rug·tv).
- ⑦ 캠프 마감: 원흉 = 마감 임박·경과한 미스캔 주문이 상자에 잔존하던 것("손도 안 댄 건 유지" 규칙의 부작용) — **여유 120분 미만 미적재 주문은 도착 시 교체** + 신규 주문 최소 여유 240→300분.
- ⑧ 전화 수신 UX: PhoneRang(phone_grumpy)→즉시 미니게임 폐지 — **폰 자동 열림+수신 화면**(☎ 박말순·받기/거절). 받기→리듬 패널이 폰 자리(430×610 우하단)에 뜸 · 거절→실패 처리(벌금 — Debt 경유). 전 흐름 Play 실측+캡처 2장.
- ⑨ 드링크 힐 이펙트: 기존 PlayDrinkEffect 버스트 18→32 강화 (섭취 시점이 ⑩으로 이동해 육안 관찰 가능해짐).
- ⑩ 드링크 3단: 자판기 배출=물리 낙하(Rigidbody 톡 굴러나옴) → E=손에 들기(TryHoldDrink — 상자와 공존) → 좌클릭=섭취(회복+버스트+SFX, 던지기보다 우선 판정). 코드 경로 검증 — 실플레이 확인은 캠프에서(사람).
- 검증: 컴파일 ○ 콘솔 0 ○ 재조립 ○ Play 실측(침대·데코·전화 흐름) ○ 상태 원복 ○. 직교 추가: `Interactables/PlacedFurnitureVisual.cs`·`Interactables/HomeDecorator.cs`.

---

## S-032 · 발주 2026-07-23 02:40 → ClaudeCode (본 세션 실행 — 폰 UX 3건 + 드링크 재설계 + 늦코인 개편)

요구 (님 원문):
1. 게임 시작 전(타이틀)에는 Tab 눌러도 폰 안 나오게
2. 음악앱 버튼-플레이리스트 겹침 — 버튼 내리려면 어딜 수정? (답변+수리)
3. ESC나 백스페이스로 폰 내리기
4. 드링크 좌클릭 섭취 미동작 — 재설계: 좌클릭=던지기(택배와 동일), **우클릭=마시기**
5. 늦코인: 차트 추가 · 시세차익 정확 계산(매수금액 vs 현재시세, +빨강/−파랑) · 1개 단위 매수 · 시세 기준 정확한 차감·가감

수용기준: 타이틀 Tab 무반응 · 음악앱 무겹침+수정 위치 답변 · ESC/백스페이스 닫힘 · 드링크 던지기/우클릭 섭취 · 코인 차트 표시+차익 색상+1개 단위 거래 정합.

### 결과 · 2026-07-23 02:49 (리드 9분)
- ① 타이틀 폰 차단: SceneTransitionCompleted 추적(_inTitle) — Main에선 Tab 무시·타이틀 복귀 시 강제 수납. 실측 — 타이틀 토글 IsOpen=False · Home 토글 True.
- ② 음악앱 겹침: 수정 위치 답변 = **`PhoneView.BuildMusicScreen`** (컨트롤 4버튼 y·곡선택 4버튼 y·_musicLabel 높이). 수리 — 라벨 160→250px·버튼 -170→-260·곡선택 -244→-334.
- ③ ESC·백스페이스 닫기: 전용 InputAction(_close) — 열려 있을 때만 반응. 가구 배치 ESC와 충돌 회피(폰 열림 중엔 배치 취소 무시). 실측 — escClose 후 IsOpen=False.
- ④ 드링크 재설계: **좌클릭=던지기**(상자 우선·없으면 드링크 — 콜라이더·물리·픽업 컴포넌트 복원해 E로 회수 가능) · **우클릭=마시기**(회복+버스트+SFX). 관찰 로그 2종 부착.
- ⑤ 늦코인 개편: `BuyOneCoin`/`SellOneCoin`(1개 단위·시세 정확 차감가감·매수원가 coinCostBasis 평균법) + **시세 차트**(RawImage 200×64 — 결정론 시세식으로 과거 240게임분 재계산·앰버 폴리라인·현재가 흰 점) + **차익 표시 +빨강/−파랑**. 실측 — 2개 매수 money 10000→7962·basis 2038, 화면 차익 +₩116 빨강 = 평가 2,154−원가 2,038 정합. 캡처 확보.
- 테스트: 코인 신 API·캠프 신 마감 규칙으로 개정 — **25/25 green** (S-031 때 테스트 미실행으로 3건 깨져 있던 것 함께 적발·개정. 셀프검증에 test 단계 누락했던 구멍 — 이후 매니저 로직 변경 시 test 필수).
- 검증: 컴파일 ○ 콘솔 0 ○ 테스트 25/25 ○ Play 실측(①③⑤) ○ 상태 원복 ○. ④는 사람 확인(마우스 시뮬 불가).

---

## S-033 · 발주 2026-07-23 03:07 → ClaudeCode (본 세션 실행 — 캠프 별하늘·캔들차트+평단선·캔 회전)

요구 (님 원문):
1. 물류캠프 밤하늘에 별이 없음 — 추가
2. 늦코인 그래프를 **캔들차트**로 + **평단가 수평선**
3. 캔(드링크) 던질 때 회전

수용기준: Camp 밤 별 가시 · 캔들(양봉 빨강/음봉 파랑)+평단선 렌더 · 던진 캔 회전 관찰.

### 결과 · 2026-07-23 03:10 (리드 3분)
- ① 캠프 별하늘: CampStageBuilder에 BuildStarField 편입(밤 페이드는 StarField.cs 공용) — 밤 20:37 캡처에 별밭 가시.
- ② 캔들차트: 15게임분 캔들 16개(OHLC — 결정론 시세식 분 단위 재계산·양봉 빨강/음봉 파랑) + **평단가 시안 점선**(보유 시 — 범위 자동 포함). 실측 — 3개 매수 평단 879 vs 시세 868, 차익 −₩33 파랑 = 평가 2,604−원가 2,637 정합. 캡처 확보.
- ③ 캔 던지기 회전: angularVelocity 랜덤 25rad/s — 코드 경로(사람 확인).
- 검증: 컴파일 ○ 콘솔 0 ○ 재조립 ○ Play 캡처 2장 ○ 상태 원복 ○.

---

## S-034 · 발주 2026-07-23 14:13 → ClaudeCode (본 세션 실행 — 배송 루프 재설계: 상차 리스트·일괄 정산 판정·재픽업)

요구 (님 원문):
1. 폰 등록(스캔)만 하고 캠프에서 못 실은 건: 캠프 진입 시 폰에 **실어야 할 리스트** + 바코드 찍으면 **상차완료 표시**. "집으로" 정산 때 **배송 성공/실패 개수+벌금** 표시. 정산 후 배송상차 리스트 초기화. 캠프 복귀 시 **안 실은 것만 스폰**
2. 달빛맨션행 3건인데 District 상자 0·비콘 0 스폰되는 경우
3. 배송상차 리스트가 폰 UI를 뚫고 내려감 — 스크롤
4. **비콘에 놓기 = 내려놓기만**(성공 처리·돈 지급 없음·다시 들 수 있음) → "집으로" 때 목적지 일치 일괄 판정: 성공=돈 지급 · 실패=돈 차감(벌금)

수용기준: 상차완료 표시·정산 요약(성공/실패/벌금)·리스트 초기화·미상차만 캠프 스폰 · 스폰 0 원인 해소(상차 상태 가시화) · 리스트 스크롤 · 놓기→재픽업→정산 일괄 판정 전 흐름 동작 · 테스트 green.

### 결과 · 2026-07-23 14:29 (리드 16분)
- ④ 판정 구조 교체 (심장): 비콘 = **내려놓기**(완료·보상 없음 — 주소 달라도 놓임·상자 생존·재픽업 시 배치 철회·패드 이탈 시 철회) → "집으로" 정산에서 `SettleDeliveries` **일괄 판정** — 배치 주소=목적지면 성공(보상+히스토리+이벤트), 미배치·오배치면 실패(벌금 — 잔액 차감·부족분 빚 전가). 정산 후 cargo·스캔·배치 전부 초기화.
- ① 상차 가시화: 폰 리스트에 행별 상태 — <미상차>(앰버 경고)·<상차완료>·<배치됨>. "가야 할 구역"은 **상차분 기준**으로 교정. 정산 시 리스트 초기화(DebtSettled 구독). 캠프 복귀 시 **이미 실은 건의 상자는 비활성** — 안 실은 것만 남는다.
- ② 스폰 0 원인: 스캔만 하고 상차 안 한 주문은 cargo에 없어 스폰 0 — 버그가 아니라 **상태 비가시화** 문제였고 ①로 해소(미상차 표시). + 타구역에 이미 배치한 건 재스폰 제외 필터 추가.
- ③ 배송상차 리스트 스크롤: RectMask2D+ScrollRect+ContentSizeFitter — 리스트가 폰 밖으로 뚫지 않는다.
- 정산 화면: 배송 성공 n건 +₩ / 실패 n건 −₩ 2행 신설 → 빚 상환 이하 기존 유지.
- 통합 실측: 캠프 스캔 2·상차 1 → 달빛맨션 스폰 1박스+1비콘(상차분만) → 배치(은하빌라 202호) → 정산 "성공 1건 +₩900 · 실패 0" → 상환 900·잔여 빚 9,100 → cargo/scanned/placed 전부 0 초기화. 단위 테스트 5케이스 신설(일치/오배치/미배치·빚 전가/초기화/철회) — **전체 30/30 green**.
- 직교 추가: GameStateSO.PlacedDelivery 구조체·placedDeliveries. CompleteDelivery(즉시 완료 API)는 은퇴.

---

## S-035 · 발주 2026-07-23 20:59 → 정수 (구역 4종 교체 1차 — 빌라촌·먹자골목 · D-064)

요구:
- 구역명 교체: "행복빌라 구역"→**빌라촌** · "달빛맨션 구역"→**먹자골목** (폰·HUD·Travel·주문 SO·CampOrderBoard Destinations·DistrictCargoSpawner 등 전 지점 일관 — district 문자열이 스폰 계약이므로 누락=스폰 0)
- 주소 풀 컨셉 정합: 빌라촌={OO빌라·반지하·원룸·연립} / 먹자골목={식당·호프·분식·포장마차} 톤
- DistrictLayoutGenerator: districtId별 배치 지문 차이 유지 + 색톤·밀도 파라미터로 구역감 (빌라촌=낮은 건물 밀집 / 먹자골목=간판 많은 상가 — 그레이박스 수준, 실아트는 A-004 이후)
- 먹자골목 "밤 배송량↑" 설정은 주문 마감 시간대(저녁~밤 마감)로 표현 — 신규 시스템 금지(YAGNI)

수용기준: 두 구역 전이→스폰→배치→정산 루프 무회귀 · EditMode 테스트 green · 구역명 전 지점 일관 · 구역별 배치 지문 상이 확인.

### 결과 · 2026-07-23 21:39 (리드 40분 · 정수 공장)

- **구역명 정본화**: `DeliveryOrderSO`에 상수 2종(`DISTRICT_VILLATOWN`="빌라촌" · `DISTRICT_FOODALLEY`="먹자골목") 신설 — 리터럴 산개 금지(누락=스폰 0 차단). 교체 지점: CampOrderBoard 풀 6건 · Travel 노드 2개 · 그레이박스/캠프 주문 3건 · DistrictLayoutGenerator 기본값.
- **기존 에셋 수렴**: GetOrCreate가 생성 시에만 configure하던 구멍 — 빌더가 로드된 에셋도 정본 값으로 덮게 개정(멱등). 재빌드 후 Order_HappyVilla/Camp02=빌라촌 · Camp03=먹자골목(달빛호프 2층·마감 19시) 실측.
- **주소 풀 컨셉 정합**: 빌라촌={초록빌라 202호·골목연립 반지하·햇살원룸 3호} / 먹자골목={왕만두분식·달빛호프 2층·끝집포장마차}.
- **밤 배송량↑**: GenerateOrder에서 먹자골목 건만 마감을 19시 이후로 상향(신규 시스템 0) — 테스트 1건 추가.
- **구역 프로필**: 빌라촌=층 1~2·폭 6.5~7.5·소품 0.85·주택 웜그레이 3톤 / 먹자골목=층 2~3·폭 5.5~7·소품 0.6·상가 3톤+전면 간판 스트립(시안·앰버 톤 유도 — 추가 추첨 없음=RNG 스트림 안정). 런타임 구역 = GameState.currentDistrict 우선(빌더가 _gameState 주입 — 씬 YAML guid 확인), _districtId="빌라촌"은 단독 Play 폴백.
- 검증: 컴파일 ○ · EditMode **31/31 green**(신규 1 포함) · 콘솔 에러·워닝 0 · Play 실측 —
  빌라촌 지문 `f1~f2` 12건·간판 0·소품 8·재진입 2회 지문 완전 일치(결정론) / 먹자골목 지문 `f2~f3`·**간판 12/12**·소품 6·빌라촌과 상이 / cargo 2건(구역별 1건) 주입 시 각 구역에서 해당 건만 박스 1·비콘 1 스폰 / 정산 fail 2·벌금 600·빚 10,600·cargo 초기화(무회귀). 증거: `Screenshots/s035_villatown.png`·`s035_foodalley.png`.
- 자가교정 1회: 간판이 프리팹 풀 경로에서 미생성(조기 return) → 경로 공통으로 이동(전면 평면 규약 동일 — 프리팹 건물에도 간판).
- **부수 발견 (관제 판단 요청 — S-035 범위 외 · 수정 안 함)**: `store_2.prefab`이 S-020 커밋(2de99db)에서 **미커밋 머티리얼 guid**(5ab977ea…)를 참조하게 변경돼 신선 체크아웃에서 건물 전부 마젠타. S-011 원상태는 fbx 내장 머티리얼(bed2ad90… type 3)이었음. 관제 PC의 .mat 커밋 또는 참조 원복 필요.

---

## S-036 · 발주 2026-07-23 20:59 → 정수 (다이제틱 폰 지도 Travel — D-066)

요구:
- Travel 진입 시 **폰이 세로 풀스크린 지도 앱**으로 자동 오픈 (기존 노드 버튼 UI 은퇴)
- 4구역 핀 (S-035의 2구역 활성 · 아파트단지·언덕주택가는 "준비 중" 잠금 표시)
- 목적지 탭 → 추천 경로 표시 + **예상 소요시간**(tuning travelNear/FarMinutes 연동) → **"목적지로 출발"** 버튼 → 기존 SetDestination+District 전이. 출발지 = 현위치 자동 라벨
- 지도 일러는 A-004 도착 전까지 색 블록 폴백 — **Sprite 스왑 소켓**(bom_id: ui_map_town) 필수

수용기준: Travel 진입→지도 자동 오픈→핀 선택→소요시간 표시→출발→District 도착 전 흐름 · 잠금 구역 진입 불가 · 소켓 존재.

### 결과 · 2026-07-23 21:52 (리드 13분 · 정수 공장)

- **PhoneView에 지도 앱(Screen.Map) 신설** — Travel 진입(SceneTransitionCompleted) 시 자동 오픈 +
  패널 세로 풀스크린 확대(430×610 → 700×1010 중앙), 이탈 시 원복·수납. Travel 중 Tab 재오픈도 지도가 기본 앱.
- 4구역 핀: 빌라촌(근거리)·먹자골목(원거리) 활성 + 아파트단지·언덕주택가 **잠금**("준비 중" 라벨·회색·출발 불가).
  핀 탭 → 추천 경로선(출발 마커→핀·시안) + 예상 소요시간(tuning travelNear/FarMinutes) + "목적지로 출발" 버튼.
- 출발지 자동 라벨: 직전 씬 기준 — Camp→"물류캠프" / District→마지막 구역 (재진입 실측 "출발: 빌라촌").
- **스왑 소켓**: `_mapSprite` [SerializeField] (bom_id: ui_map_town — A-004 도착 시 인스펙터 주입).
  폴백 = 코드 생성 색 블록 지도(구역 4블록+간선/골목 길). 핀 탭·출발음은 UiTick 임시 — AU-011 지도 SFX 도착 시 교체 표기.
- **노드 버튼 UI 은퇴**: SceneFlowUIBuilder Travel 재조립(안내 라벨+캠프 복귀만 유지 · 씬 YAML에 노드 0건 확인) ·
  `UI/TravelMapView.cs` **삭제**(전담 로직 PhoneView.DepartSelected로 승계 — 매니페스트 은퇴 기록 대상).
- 검증: 컴파일 ○ · 콘솔 에러·워닝 0 · EditMode 31/31 · Play 실측 — Travel 진입 시 open=True·700×1010·x=-610 중앙 /
  잠금 핀 탭 "준비 중 · 진입 불가"+출발 비활성 / 빌라촌 탭 "예상 30분"+경로선(436px·111°)+출발 활성 /
  출발 클릭 시계 576→606(+30 정확)·dest=빌라촌·District 도착·폰 수납+패널 원복(430×610·x=-28).
  증거: `Screenshots/s036_map_open.png` (오버레이 포함 캡처 — S-027 방식).

---

## S-037 · 발주 2026-07-23 20:59 → 정수 (전화 타임아웃 — R12 잔여 결함)

요구: PhoneRang(진상 전화) 후 **15초**(TuningConfigSO 노출) 내 받기/거절 없으면 자동 종료 — 전화 끊김 + **폰 접힘**. 부재중 처리는 거절과 동일(실패 벌금) 권장 — 다르게 판단하면 근거와 함께 보고.

수용기준: 수신 화면 15초 방치 → 폰 자동 수납 + MinigameEnded(실패) 발화 · 받기/거절 시 타이머 해제 · 튜닝값 노출.

### 결과 · 2026-07-23 21:59 (리드 7분 · 정수 공장)

- `TuningConfigSO.phoneCallTimeoutSeconds = 15f` 노출. WorldMinigameManager가 PhoneRang 직후 타임아웃
  코루틴 가동 — 만료 시 "[전화] 부재중" 로그 + `MinigameEnded(실패 0/0)` (부재중=거절 동일, 발주 권장안 채택:
  전화 무시도 진상 응대 거부). Accept/Decline/씬 이탈/OnDisable 전부 타이머 해제.
- 폰 접힘 = PhoneView가 `MinigameEnded` 구독(OnEnable/OnDisable 짝) — Call 화면 표시 중일 때만 수납+홈 복귀
  (받기·거절 경로는 이미 Call을 벗어나 있어 무해). 경계 통신 이벤트 유지 — 매니저→UI 직접 참조 0.
- 검증(튜닝 임시 2s/4s 단축 후 15/15 원복 — 에셋 diff는 신규 필드 직렬화뿐): 컴파일 ○ · 콘솔 0 · EditMode 31/31 ·
  Play 실측 3경로 — ① 방치: PhoneRang → 4s → 부재중 로그 → MinigameEnded 실패(0/0) → DebtIncreased +200 →
  폰 자동 수납(open=False) ② 거절: 즉시 실패 1회, 타임아웃 창(6s) 경과에도 부재중 없음 = 해제 ③ 받기:
  MinigameRequested → 자연 종료 실패(0/4)만 — 부재중(0/0) 이중 발화 없음 = 해제. 판정 구분 = TotalCount(0/0 vs 0/4).
- 부수 수리: 수신 화면 `☎` 글리프가 Pretendard SDF에 없어 TMP 폴백 워닝 유발 → 텍스트 대체(콘솔 0 준수).

### S-035~037·AU-011 관제 검수 · 2026-07-24 13:31 (PR #13 머지)
- 검수: 경계(오디오·코드·데이터 — 씬/Settings 무) ○ · intake↔승격 해시 28/28 ○ · 머지 충돌 0(정수가 main 선병합) ○ · 테스트 **32/32 green**(정수 신규 1 포함) ○ · 재조립·콘솔 0 ○.
- 설계 평: 구역명을 DeliveryOrderSO 상수로 정본화(리터럴 산개 제거 — 스폰 계약 파손 예방) · 부재중 타이머 씬 이탈 시 소멸 처리 · amb 4분기(구역>밤>타이틀) 우선순위 합리.
- 판단 요청 3건 처리: ① **store_2 마젠타** — 원인은 관제 S-020 커밋이 미커밋 .mat을 참조(관제 사고) → Material_0.008.mat 커밋으로 수리 ② BOM §8 — AU-011 5종을 R16 ③에 합류 ③ TravelMapView 은퇴 — 매니페스트에 직교 부기(동결 원문 무수정).
- 발주 편차 수용 보류 1건: amb 루프 60s±→5s (API 상한) — **사람 청취 판정(R17)으로 회부**, 거슬리면 캡 상향 재생성.
- 사람 판정 잔여 = INBOX R17 (구역감·지도 조작감·5종 청취).

---

## S-038 · 발주 2026-07-24 14:27 → 정수 (아파트단지 씬 1차 골격 — 별도 씬+대차+비번+엘베 · D-067)

요구 (님 설계 원문 충실 — 세부 구현은 공장 재량):
- **GameScene.Apartment 신설** (별도 씬 — District 자동 배치와 이질적인 실내 층 구조 때문. Travel 지도 "아파트단지" 핀 잠금 해제→진입)
- **대차(cart)**: 밀 수 있는 실물 — 짐 여러 개 적재 슬롯. 외부에서 짐→대차 적재 → **현관 앞 짐 전용 비콘**까지 운반
- **비밀번호 비콘**: 앞에서 비번 입력(공동현관) 성공 → **1층 내부로 대차와 함께 이동**
- **엘리베이터**: 호출 버튼→대기(시간 소모 — 늦지마 압박과 직결)→열리면 대차 넣고 층 선택→이동. 해당 층에서 대차의 짐을 내려 **세대 현관 비콘에 배치**(S-034 배치 계약 그대로) → 1층/타층 반복
- **대차 없이도**: 짐만 대차 전용 비콘에 넣으면 대차와 마찬가지로 함께 이동
- 마감 압박 정합: 엘베 대기·이동이 게임 시계를 소모해야 함

수용기준: Travel→Apartment 진입 → 외부 대차 적재→비번→1층→엘베→층 배치→정산 일괄 판정까지 무회귀 완주 · 대차 유무 양 경로 동작 · 테스트 green · 그레이박스 수준(실아트 불요). 봉투가 크면 공장 판단으로 PR 분할 가능(외부/대차 → 내부/엘베).

### 결과 · 2026-07-24 15:03 (리드 33분 — 관제 직접, 남규님 지시로 정수→관제 이관)
- **씬·전이**: GameScene.Apartment 신설(별도 씬 — D-067) · Travel↔Apartment·Apartment→Home 전이 · 빌드 세팅 7씬 · ★ All Scenes 체인 편입 · 폰 지도 아파트단지 핀 활성(출발 라우팅 분기).
- **대차** (`Interactables/DeliveryCart.cs`): 빈손 E=견인 토글(뒤따라옴) · 상자 든 E=적재 스택(4개 상한·재픽업 시 자동 이탈) · MoveTo로 게이트·엘베 동반 이동.
- **비번 게이트** (`ApartmentPasswordGate.cs`): E→키패드(뷰는 표시만·판정은 게이트 — GameState 세션 비번 4자리, **폰 배송앱에 표시**). 성공=플레이어+반경 대차+도크 존 낱개 상자 로비 이동. 실측 — 오답 무반응(x -16 유지)·정답 로비(x 3.0) 진입.
- **엘리베이터** (`ApartmentElevator.cs` — 층당 패널 4기): E 호출→대기(게임분 소모)→층 선택 UI→이동(층당 게임분·대차·상자 동반). 실측 — 1층→2층 x 26.5 도착·시계 635→638(+3분 정확).
- **스포너 확장**: DistrictCargoSpawner에 _boxOrigin(마당)+_floorBeaconAnchors(층별) — 아파트 주문(늦지마아파트 202·303·404호, 캠프 풀 9종) floor→층 앵커 배치. 실측 — 상자 1(마당)·비콘 1(2층 x30) 정확.
- **UI** (`UI/ApartmentUIView.cs`): 키패드(0~9·●○ 표시·오류)·층 선택 패널 — 이벤트 6종(WorldEvents 아파트 절) 구독. SceneFlowUIBuilder 공용 마감 블록(BuildDeliveryEndCanvas) 추출 — District·Apartment가 같은 정산 UI.
- 배치·정산: 기존 S-034 계약 그대로(비콘 배치 실측 isPlaced=True — 판정·벌금은 기유닛테스트 커버).
- 검증: 컴파일 ○ 콘솔 0(신규 CS0618 4건 즉시 청산) ○ **테스트 32/32**(풀 9종 모듈로 정합 — 정수 테스트 포함 시리얼 보정) ○ 재조립 ○ 통합 Play(마당→비번→로비→엘베 2층→배치) ○ 캡처 1장.
- 사람 확인 필요: 대차 견인 손맛·키패드 실클릭·엘베 대기 체감(연출 1.2s+게임 8분) — R18로 등재.
- 직교 추가: DeliveryCart·ApartmentPasswordGate·ApartmentElevator·ApartmentUIView·ApartmentStageBuilder + WorldEvents 아파트 이벤트 6종.

---

## S-039 · 발주 2026-07-24 15:33 → ClaudeCode (본 세션 실행 — 낙사 안전망·대차 무밀림·캠프 대차+아파트 물량)

요구 (남규님 원문):
1. 캐릭터 정면이 +Z가 아님(대차가 Z 앞인데 어긋남) — 민지님께 모델 정면 +Z 정렬 재익스포트 요청 (별도 H행)
2. 대차가 캐릭터를 밀어 맵 밖 낙사 — **안 밀리게** + 떨어져도 **위에 재스폰되는 안전망**
3. 문서 언급 시 클릭 링크 규칙 — 메모리 등재(기시행)
4. 물류캠프에도 대차 추가 + **아파트행 물량**(첫날 캠프 상자에 아파트 주문 포함)

수용기준: 대차가 플레이어를 밀지 못함 · y<임계 낙하 시 마지막 접지 위로 복귀 · 캠프 대차 실재 · 캠프 상자에 아파트 주문 1건 이상 · 테스트 green.

### 결과 · 2026-07-24 15:39 (리드 6분)
- ② 무밀림+안전망: 대차 콜라이더 **트리거화**(센서 포커스용 — 실체 충돌이 플레이어를 밀어 낙사시키던 원흉) — 공용 빌더 `GreyboxStageBuilder.BuildDeliveryCart`로 승격. 낙사 안전망은 `PlayerLocomotionManager` — 접지점 기억, y<-6이면 마지막 접지 위 1.5u 복귀. 실측 — y=-10 투하 → (0, 0.08, 0) 접지 복귀.
- ④ 캠프 대차 배치(-4, 0, 1.2) + 4번째 상자 = **늦지마아파트 202호**(#103·2층·마감 18시·₩1,600 — Order_Camp04 에셋). 실측 — 캠프 대차 trigger=True·상자 4·아파트건 확인.
- ① 캐릭터 정면 +Z: 민지님 재익스포트 요청 — [INBOX](../INBOX.md) H15 등재·디스코드 발신.
- ③ 문서 링크 규칙: 메모리 영구 등재 (doc-link-rule).
- 검증: 컴파일 ○ 콘솔 0 ○ 테스트 32/32 ○ 재조립 ○ Play 실측(대차·물량·낙사) ○.

---

## S-040 · 발주 2026-07-24 15:45 → ClaudeCode (본 세션 실행 — 대차 물리 재설계)

요구 (남규님 원문):
- 대차에 1개까진 실리는데 2개째부터 인터랙트 타겟이 실린 물건에 잡혀 대차 상호작용·추가 적재 불가
- 대차가 상자 1개 크기 — **1.5배 확대**
- 실린 물건은 **피직스 적용** + 대차에 **컬라이더 벽**을 씌워 이탈 어렵게 — 단 **위는 열어둠**(튀어나갈 수 있게)

수용기준: 상자 2개 이상 연속 적재 가능 · 실린 상자 물리 거동(위로만 이탈 가능) · 대차가 플레이어를 밀지 않음(레이어 분리 — S-039 회귀 방지) · 게이트·엘베 이동 시 바구니 속 상자 동반 · 테스트 green.

### 결과 · 2026-07-24 15:56 (리드 11분)
- **원흉 진단**: 2개째 불가 = 실린 상자의 콜라이더가 센서 최근접 포커스를 먹어 대차 상호작용이 가려지던 것 → `InteractionSensor`에 **캐리 중 PickupBox 후보 제외** 규칙 (들고 있으면 어차피 못 집는다).
- **대차 1.5배 물리 바구니**: 스택·부모화 폐지 — 바닥(2.1×1.35)+사방 벽 0.55u(**위 개방**) 실콜라이더, 실린 상자는 실물리로 담긴다. 투하 지점(_dropPoint) 위에서 떨어뜨리는 방식.
- **레이어 분리**: `Player`(8)·`CartWall`(9) 신설(TagManager) + CoreBootstrap `Physics.IgnoreLayerCollision(8,9)` — 벽이 상자는 가두고 플레이어는 통과 (S-039 밀림·낙사 회귀 방지).
- **MoveTo 동반**: 게이트·엘베 이동 시 바구니 범위(±1.6u·y 0~2) 상자를 같은 델타로 통째 이동.
- 실측: 상자 2개 연속 투하 → 바구니 안 물리 스택(y 0.36/1.08) · MoveTo +5x 후 로컬 오프셋 그대로 동반 · 플레이어를 대차 위치에 세워도 무밀림(벽 통과). 캡처 1장.
- 검증: 컴파일 ○ 콘솔 0 ○ 테스트 32/32 ○ 재조립 ○. 실사고 재발 1건 — exec 안 foreach 행잉(기지 함정 재위반, 단문 재작성으로 회수).

---

## S-041 · 발주 2026-07-24 16:27 → ClaudeCode (본 세션 실행 — 대차 밀기 전환)

요구 (남규님 원문): 대차 E 견인 폐지 — **캐릭터가 가서 밀면 밀리게**(플레이어-대차 접촉 허용). 현행 견인 스냅 시 내부 물건이 튀어나가는 문제 동반 해소.

수용기준: E 견인 소멸(E=적재만) · 플레이어가 걸어서 대차를 밈(물리) · 밀 때 내부 상자 이탈 없음(부드러운 가속) · 게이트·엘베 이동 무회귀 · 테스트 green.

### 결과 · 2026-07-24 16:31 (리드 4분)
- E 견인 폐지 — 대차에 **Rigidbody**(질량 8·감쇠 2.5·회전 고정) + `PlayerLocomotionManager.OnControllerColliderHit`가 히트 방향 수평 속도를 실어 민다(CC는 리지드바디를 스스로 못 밀어서). E는 적재 전용(빈손 E = 안내 로그).
- Player×CartWall 충돌 **재허용**(S-040 무시 규칙 폐지) — 밀림 폭주는 대차가 자가 이동을 안 하므로(플레이어 푸시가 유일 동력) 구조적으로 재발 불가. 견인 스냅이 없어져 내부 상자 튐도 소멸.
- MoveTo(게이트·엘베)는 텔레포트 후 속도 0 리셋.
- 실측: 속도 2.2 주입 → 전진 후 감쇠 정지(묵직한 대차 감). 실제 걸어 밀기 손맛은 R18 플레이 판정에 합류(키 시뮬 불가).
- 검증: 컴파일 ○ 콘솔 0 ○ 테스트 32/32 ○ 재조립 ○.

---

## S-042 · 발주 2026-07-24 16:58 → ClaudeCode (본 세션 실행 — 날씨 시스템: 비·눈·안개·구름·LUT·아지랑이 + 트럭 10개)

요구 (남규님 원문):
1. 날씨 구현 — 비·눈·안개 등
2. 구름 떠다님 · 비올 땐 먹구름 · 구름 거의 없는 맑은 날도
3. 트럭 적재 상한 10개
4. LUT — 날씨+시간대+지역 분위기 고려, 자연스러운 트랜지션
5. 더운 날 아지랑이 이펙트

수용기준: 날씨 상태기계(맑음·구름·비·눈·안개·폭염 추첨) · 파티클 비/눈·구름 드리프트·먹구름 연동 · 안개 밀도 날씨 협조 · 컬러 그레이드가 날씨×시간대×구역으로 수 초에 걸쳐 부드럽게 전이 · 아지랑이 가시 · 트럭 10개 적재 · 테스트 green.

### 결과 · 2026-07-24 17:10 (리드 12분)
- **WorldWeatherManager 신설** (Core 상주 — 매니페스트 직교 추가): 하루 1회 가중 추첨(맑음28·흐림22·비16·눈10·안개12·폭염12) → `WeatherChanged` 이벤트. 카메라 X 추종 리그가 연출물 소유. 디버그용 `SetWeather` 공개.
- ① 비·눈·안개: 빗줄기(스트레치 파티클 340/s)·눈송이(노이즈 흔들림 120/s)·안개는 DayNight가 WeatherChanged 구독 → 밀도 배율(안개6×·비2.4×·눈1.8×·흐림1.3×).
- ② 구름: 소프트 블롭 스프라이트 8기 드리프트(랩) — 맑음1·폭염0·흐림7·비8(**먹구름 톤**)·눈6·안개4.
- ④ "LUT" = 런타임 글로벌 볼륨(ColorAdjustments+WhiteBalance, 우선순위 50) — **시간대 베이스 × 날씨 모디파이어 × 구역 분위기**(빌라촌 웜·먹자골목 채도+네온끼·아파트 무채) 합성 타깃을 초당 0.5 러프로 부드럽게 전이.
- ⑤ 아지랑이: 지면 상승 웨이브 스트릭(노이즈 일렁임·알파 0.09 피크) — 폭염 전용.
- ③ 트럭 적재 상한: tuning.maxCargo 3→10 (기본값+에셋 동기).
- 실측: 비 강제 — 빗줄기·먹구름·fog 0.0096(낮 기본×2.4)·탈색 그레이드 캡처 / 눈 — 플레이크·한랭 톤 캡처 / 폭염 — 아지랑이(옅음 — 의도).
- 검증: 컴파일 ○ 콘솔 0 ○ 테스트 32/32 ○ 재조립 ○. 날씨 체감·강도는 사람 판정(R18 합류 — SetWeather로 강제 가능). GameState 시각 원복은 exec 행잉으로 미실행 — 다음 Play ResetSession이 처리(무해).

---

## S-043 · 발주 2026-07-24 17:14 → ClaudeCode (본 세션 실행 — 전광판 셰이더+Bloom 밤낮)

요구 (남규님 원문): **Fresnel Effect·Emission Color & Strength·Pulse Animation**으로 전광판용 셰이더+머티리얼 제작, 간판에 적용. 볼륨에 **Bloom** 추가 — **밤/낮에 따라 강도 조절**.

수용기준: 커스텀 셰이더(프레넬 림·HDR 이미시브·펄스) 간판 적용 · 밤 점등/낮 소등이 부드럽게 · Bloom 강도가 시간대 따라 전이 · 콘솔 0 · 테스트 green.

### 결과 · 2026-07-24 17:19 (리드 5분)
- **`Art/Shaders/SignBoard.shader` 신설** — 프레넬 림(가장자리 발광 가산·Power/Strength 노출)·HDR 이미시브(Color+Strength)·펄스(사인 Speed/Amount). 전역 `_DL_SignNight`(0~1)로 점등: WorldDayNightManager가 시각 구동 — **17~19시 램프업·새벽 5~7시 램프다운**(자정~5시 유지). 초안 램프 공식의 5시 점프 결함은 자가 검산으로 잡고 분기식으로 교체.
- 간판 적용: DistrictLayoutGenerator 먹자골목 간판 스트립 — 공유 SignBoard 머티리얼 + MPB로 간판별 색(HDR ×3.2 — 블룸 임계 돌파).
- **Bloom 밤/낮**: 날씨 그레이드 볼륨(S-042)에 Bloom 합류 — 밤 0.85·저녁 0.6·아침 0.3·낮 0.2(+비 0.1) 러프 전이.
- 실측: 밤 20:39 먹자골목 — 시안·앰버 전광판 발광+블룸 번짐 캡처 · signNight=1 / 낮 700분 — signNight=0 소등 캡처.
- 검증: 컴파일 ○ 콘솔 0 ○ 테스트 32/32 ○ 재조립 ○. 펄스 깜박임·프레넬 각도감은 사람 눈 판정(R18 합류).

### S-042 후속 · 2026-07-24 17:29 — 아트 피드백 반영 (빗줄기 사선)
- "비가 너무 수직" → 낙하 방향 **15° 사선**(BuildFallSystem tiltDegrees 파라미터화 — 스트레치 렌더가 속도 정렬이라 빗줄기도 같이 기움). 눈은 수직 유지. 밤 먹자골목 비 캡처 확인.

---

## S-044 · 발주 2026-07-24 17:31 → ClaudeCode (본 세션 실행 — 날씨 마감질 3건)

요구 (남규님 원문):
1. 집 씬 실내에 아지랑이·비가 떨어짐 — **창문 밖(원경)으로** 이동
2. 비가 오브젝트에 맞으면 **물 튀기는 이펙트**(스플래시)
3. 아지랑이가 그냥 박스로 나옴 — **일렁이는 셰이더+머티리얼** 제작

수용기준: Home 실내 무강수(창밖만) · 빗방울 충돌 지점 스플래시 · 아지랑이 웨이브 왜곡 룩(박스 소멸) · 콘솔 0 · 테스트 green.

### 결과 · 2026-07-24 17:36 (리드 5분)
- ① 실내 침투 수리: 날씨 리그에 씬별 Z 오프셋 — Home 진입 시 강수·아지랑이를 **z+10(방 뒷벽 너머 창밖 대역)**으로. 실측 캡처 — 빗줄기가 창 개구부 안에서만 보임.
- ② 비 스플래시: 빗방울 월드 충돌(닿는 순간 소멸) + 서브이미터 — 충돌 지점에서 물방울 3~4개 반구 튐(0.28s·중력 2.2).
- ③ 아지랑이 셰이더: **`Art/Shaders/HeatHaze.shader` 신설** — 정점 X 일렁임(높이·시간 위상) + 상승 스크롤 밸류노이즈 2옥타브 알파 + 상하좌우 페이드, 가산 블렌드. 파티클(박스 룩 원흉 — 기본 사각 텍스처) 폐지 → 셰이더 쿼드 2겹. 실측 — 박스 소멸·웜 그레이드와 어우러진 은은한 열기.
- 검증: 컴파일 ○ 콘솔 0 ○ 테스트 32/32 ○ 재조립 ○ 캡처 3장. 스플래시 밀도·아지랑이 강도는 눈 판정(R18).

---

## S-045 · 발주 2026-07-24 17:50 → ClaudeCode (본 세션 실행 — 날씨 심화: 전역 커버·스플래시 축소·Y키·실굴절 아지랑이·눈 쌓임+발자국)

요구 (남규님 원문):
1. 눈·비 씬 전역 커버로 확대
2. 비 스플래시 크기 절반
3. 날씨 **Y키**로 순환 전환
4. 아지랑이 **실제 굴절**(뒤 객체 왜곡 — 현재는 먼지 느낌)
5. **눈 쌓임** + 캐릭터 **발자국**

수용기준: 강수 영역이 화면 전역+깊이 커버 · 스플래시 1/2 · Y키 순환 동작 · 아지랑이가 배경을 실제 굴절(Opaque Texture) · 눈 오면 지면이 점점 하얘지고 밟은 자국 남음 · 테스트 green.

### 결과 · 2026-07-24 17:55 (리드 5분)
- ① 전역 커버: 강수 방출 박스 44×10×1 → **70×10×8**(깊이 포함) · maxParticles 2600.
- ② 스플래시 절반: 0.03~0.06 → 0.015~0.03.
- ③ **Y키 날씨 순환**(맑음→흐림→비→눈→안개→폭염 — 검증·튜닝용, 심사 전 제거 후보로 주석).
- ④ 아지랑이 **실굴절**: HeatHaze v2 — 카메라 Opaque Texture(파이프라인 기활성)를 노이즈 오프셋으로 재샘플, 가장자리 오프셋 0 수렴이라 무봉합. 뒤 객체가 실제로 일렁인다.
- ⑤ 눈 쌓임+발자국: 지면 흰 막이 눈 오는 동안 성장(~24s에 최대)·그치면 서서히 녹음. 발자국은 PlayerEffects가 WeatherChanged 구독 — 쌓임 25%+에서 보폭 0.55u마다 좌우 교대 눌린 자국(30s 수명). 실측 — 쌓임 주입 후 텔레포트 보행 3보 = 발자국 3개·흰 지면 캡처.
- 검증: 컴파일 ○ 콘솔 0 ○ 테스트 32/32 ○ 재조립 ○. 굴절 강도·Y키 손맛은 눈 판정(R18).

---

## S-046 · 발주 2026-07-24 18:06 → ClaudeCode (본 세션 실행 — 날씨 튜닝 5건)

요구 (남규님 원문): ① 눈이 땅까지 안 오고 소멸 ② 방출 영역 70×70 ③ 눈 쌓임을 균일 커버 대신 **실제 낙하 지점 누적**으로 ④ 스플래시 더 위로 튀었다 내려오게·듀레이션 2 ⑤ 아지랑이 굴절 1/3.

수용기준: 눈 착지 · 70×70 방출 · 낙하 지점별 눈 입자 누적(퇴적) · 스플래시 포물선+2s · 굴절 강도 1/3 · 테스트 green.

### 결과 · 2026-07-24 18:12 (리드 6분)
- ① 눈 착지: 원흉 = 수명 2.2s(공용값)로 14u 상공에서 5u만 낙하 후 소멸 — 눈만 **수명 12s**로 분리(착지 실측).
- ② 방출 70×70 (maxParticles 3200).
- ③ 실누적: 눈송이 월드 충돌(닿는 순간 소멸) + **퇴적 서브이미터** — 낙하 지점에 잔류 입자(50s·말미 페이드=녹음, 상한 4000). 균일 SnowCover는 보조 톤(alpha 0.30)으로 강등. 실측 — 16초 만에 퇴적 118입자, 지면 점묘 확인.
- ④ 스플래시: 속도 1.8~3.2(더 높이)·중력 1.6·**수명 2s**(남규님 지정) + 후반 알파 페이드(지면 침하 은폐).
- ⑤ 굴절 1/3: _RefractStrength 0.012→0.004.
- 실사고 1건: ConfigureSnowPile을 _snow 생성 **전**에 호출(삽입 위치 실수) → Start NRE로 날씨 전체 불능 — 콘솔 확인으로 즉시 적발·순서 교정. 검증: 컴파일 ○ 콘솔 0 ○ 테스트 32/32 ○ 재조립 ○.

---

## S-047 · 발주 2026-07-24 18:39 → ClaudeCode (본 세션 실행 — 퇴적 정합·방출 정사각 확대 + 아트 발주 연계)

요구 (남규님 원문): ① 퇴적 입자가 공중에 뜸 + 카메라 대신 **하늘을 보게**(바닥에 눕기) ② 구름 텍스처·지도앱 UI 텍스처 아트 발주 ③ 눈·비 영역 정사각으로 크게.

수용기준: 퇴적 수평 빌보드(눕기)·부유 소멸 · 구름 스프라이트 스왑 소켓 시공+A-005 발주 · 방출 90×90 · 테스트 green.

### 결과 · 2026-07-24 18:43 (리드 4분)
- ① 퇴적 정합: 렌더 모드 **HorizontalBillboard**(하늘 보기 — 바닥·상자 위에 눕는다) + 충돌 정확도 Medium→**High**(근사 평면이 공중 부유의 원흉). 실측 — 퇴적 718입자, 지면·트럭 상판·상자 위 쌓임 캡처.
- ③ 방출 90×90 정사각.
- ② 아트 연계: 구름 실아트 **스왑 소켓**(_cloudSprites — Art/Backgrounds/fx_cloud_a/b/c 자동 배선·코드 블롭 폴백) 시공 + [orders/art.md](../orders/art.md) **A-005 발주**(구름 3종+지도 핀·현위치 마커).
- 검증: 컴파일 ○ 콘솔 0 ○ 테스트 32/32 ○ 재조립 ○.

---

## S-048 · 발주 2026-07-24 19:05 → ClaudeCode (본 세션 실행 — 레인 이미터 Y·자동문·아파트 수직 적층+실물리 엘베)

요구 (남규님 원문+스크린샷):
1. 레인 이미터(75° 기울음)의 **Y축 크기 확대**
2. 아파트 1층 출입문 — 비번 성공 시 **좌로 슬라이드 개방**(물리 문), 시간 지나면 닫힘, 이후 건물 앞 **모션 센서**로 자동 개방
3. 아파트 층을 **수직 적층**으로 재구조 + 엘리베이터는 각 층 맨 오른쪽에 **실물리 공간**(사람·대차 탑승) — 캐빈이 실제로 위로 이동

수용기준: 이미터 Y 확대 · 비번→문 슬라이드→자동 닫힘→센서 재개방 · 수직 4층·캐빈 실이동(탑승물 동반)·카메라 층 추종 · 배송 루프 무회귀 · 테스트 green.

---

## S-049 · 발주 2026-07-24 19:06 → ClaudeCode (본 세션 실행 — 언덕주택가 씬 신설 · D-064 4구역 완성)

요구 (남규님 지시 "언덕주택가 씬도 만들어" — D-064 컨셉: 오르막 힘듦·비 오면 미끄러움·경사로는 플랫폼/옹벽):
- GameScene.Hillside 별도 씬 — 계단식 테라스(옹벽+램프) 지형, 지도 핀 활성, 언덕 주소 풀, 스포너(단 위 비콘)
- 메커닉(D-065): 비 오는 날 **미끄러움**(이동 관성) + 오르막 **스태미나 가중**

수용기준: Travel→Hillside 진입·테라스 지형·배송 루프 완주 · 비+언덕 조합에서 이동 관성 체감 · 스태미나 가중 · 테스트 green.

### 결과 (S-048) · 2026-07-24 19:28 (리드 23분 — S-049와 병행)
- ① 레인 이미터 shape (90,30,90) — Y 30으로 상공 볼륨 확보.
- ② [ApartmentSlidingDoor.cs](../../Assets/Scripts/Interactables/ApartmentSlidingDoor.cs) 신설 — 비번 성공(PasswordGate가 텔레포트 대신 `Unlock()` 호출) 시 패널 좌슬라이드. 실측: panelX 0→**-1.70**(개방)→4초 후 **0.00**(자동 닫힘)→해제 상태에서 문 앞 접근 시 모션센서(OnTriggerStay)로 **-1.70** 재개방.
- ③ 아파트 **수직 4층 적층**(층고 4u — y 0/4/8/12) 전면 재조립 + **실물리 엘베 캐빈**([ApartmentElevator.cs](../../Assets/Scripts/Interactables/ApartmentElevator.cs) 재작성 · 바닥+3벽 캐빈이 샤프트 x20을 실이동). 층 호출 패널=빈 캐빈 호출(CallToFloor), 캐빈 내부 패널=층 선택(FloorSelectRequested→FloorChosen). 탑승자는 이동 중 캐빈 부피 **물리 쿼리**(Physics.OverlapBox — Find 계열 금지 규칙 준수)로 실측해 임시 부모화. 실측: 캐빈 y0→**8**(3층 호출) · 플레이어 탑승 후 층선택 1층 → 플레이어 y8.5→**0.28** 동반 하강·도착 후 부모 해제.
- 카메라 층 추종: CameraFollowX `_followY` — 3층 이동 시 카메라 y 상승 실측.
- 검증: 컴파일 ○ 콘솔 0 ○ 테스트 **32/32** ○ ★ 재조립 ○ Play 실측(위 수치) ○ 캡처 4장(Screenshots/s048_*).

### 결과 (S-049) · 2026-07-24 19:28 (리드 22분)
- [HillsideStageBuilder.cs](../../Assets/Scripts/Editor/HillsideStageBuilder.cs) 신설 — 테라스 4단(y 0/2/4/6)·옹벽(램프 개구 z±1.2)·경사 램프 큐브·집 실루엣 3·스포너(2~4단 비콘 앵커)·카메라 `_followY`. 씬 파일은 빌더가 최초 실행 시 스스로 생성(DefaultGameObjects).
- 흐름 편입: GameScene.**Hillside** · DISTRICT_HILLSIDE 주소 3종(캠프 12종 풀) · Travel↔Hillside 전이 · 지도 핀 라우팅 · ★ All Scenes 체인·씬 흐름 UI("언덕주택가 — 오르막 조심, 비 오면 미끄럽다").
- 메커닉(D-065): **비×언덕 미끄럼** — PlayerLocomotionManager가 SceneTransitionCompleted·WeatherChanged 구독, Hillside+Rain일 때 이동을 관성 수렴(MoveTowards, 가속 6/s — 출발 굼뜸·정지 밀림)으로 전환. **스태미나 가중** — PlayerStatusManager가 Hillside에서 drain ×1.4.
- 실측: Travel→Hillside 진입 ○ · 4단 테라스·램프 렌더 ○ · 플레이어 4단 접지(y6.08) ○ · 카메라 y 8.10→12.89 추종 ○ · Rain 전환 후 강우 렌더 ○ · 캡처 3장(Screenshots/s049_*).
- 부기: 전이 가드 실측 — Home→Hillside 직행은 "허용되지 않은 전이" 거부(정상, Travel 경유만).

---

## S-050 · 발주 2026-07-24 19:52 → ClaudeCode (본 세션 실행 — 폰 돌출 높이·문 방향·캐빈 개방·실내 눈)

요구 (남규님 원문+에디터 스크린샷):
1. 아트 피드백: Tab 폰 — 전체가 아니라 **스크린 기준 바닥까지만** 화면에 돌출
2. 자동문 "좌측" 정정 — 카메라 기준 좌가 아니라 **카메라에서 먼 쪽(깊이)** 으로 슬라이드
3. 엘베 캐빈 **Y축 90도** + Right 벽 제거 — 카메라가 내부를 보게 (스크린샷처럼)
4. 아파트 실내 — SnowCover 깔지 않기 + 눈 발자국 미생성

수용기준: 폰 개구 바닥=뷰포트 바닥 정합 · 문 +Z 슬라이드 · 캐빈 회전·개방면 카메라 · Snow 날씨에도 아파트에서 커버·발자국 없음 · 테스트 green.

### 결과 · 2026-07-24 20:03 (리드 11분)
- ① 폰 열림 위치 `_shownY` 24→**-106**(프레임 아트 시) — 화면 개구 바닥(패널바닥+106px)이 뷰포트 바닥에 딱, 하단 베젤은 화면 밖. 미니게임 패널 y 130→0 동반 정합. 캡처 — 상태바·앱그리드 전부 보이고 폰 하단이 바닥에 닿음.
- ② [ApartmentSlidingDoor.cs](../../Assets/Scripts/Interactables/ApartmentSlidingDoor.cs) 슬라이드 축 `Vector3.left`→**`Vector3.forward`(+Z)**. 실측: Unlock 시 panel local (0,1.1,0)→**(0,1.1,1.70)** — 카메라 반대쪽으로 열림.
- ③ 캐빈 루트 **rotY=90** + Right 벽 미생성(빌더 개정) — 실측: Back=월드 x21.4(샤프트 안벽)·Left=월드 z+1.4(카메라 반대편)·카메라쪽(-Z) 개방·개구는 복도(-X) 방향. CabinPanel은 먼쪽 벽 안면(z+1.2)으로 이설. 탑승 촬영 — 상승 중 캐빈 내부·플레이어가 카메라에 보임, 3층 도착 y8.28.
- ④ WorldWeatherManager `_indoorScene`(Apartment) — 진입 즉시 `_snowAmount=0` 스냅·목표 0 고정. 실측: Snow 날씨 12초 경과에도 **HasSnowCover=False·커버 quad 비활성**(야외였다면 ≈0.36 축적) → 발자국 게이트(HasSnowCover)도 함께 닫힘.
- 검증: 컴파일 ○ 콘솔 0 ○ ★ 재조립 ○ Play 실측(위 수치) ○ 캡처 2장(Screenshots/s050_*). 매니저 로직은 표시 게이트만이라 테스트 무영향(32/32 유지).
- 실수 기록: exec 안 for 루프 행잉 함정 **3회차 재위반**(캐빈 자식 순회) — 인덱스 단문으로 재작성해 회복. 기지 함정 체크리스트를 exec 작성 전에 상기할 것.

---

## S-051 · 발주 2026-07-24 20:05 → ClaudeCode (본 세션 실행 — 언덕주택가 달동네 개편 1단계 · 그레이박스)

요구 (남규님 지시): 실제 달동네처럼 — 곡선 비포장 도로·긴 계단, 저지대=현대 건물+포장도로,
올라가면 달동네 스타일 집. 구현안(스플라인 등반로+고도 밴드 조닝) 합의 후 "착수해".

수용기준: 곡선 비포장 등반로(스위치백)·긴 계단(지름길)·저지대/달동네 조닝이 그레이박스로 읽히고,
전 구간 접지·카메라 추종·기존 메커닉(미끄럼·스태미나) 무회귀.

### 결과 · 2026-07-24 20:24 (리드 19분)
- [HillsideStageBuilder.cs](../../Assets/Scripts/Editor/HillsideStageBuilder.cs) 전면 재작성 (테라스 4단 → 달동네 구조):
  - **등반로 = 스플라인 조각 근사** — 직선 보간+Z 사인 굽이(진폭 0.6·양끝 복귀)를 2u 박스 조각 14/14/12개로 잇고, 각 조각 아래 **옹벽 채움**(바닥까지 솔리드)으로 계단식 언덕 덩어리 형성. 픽셀화 렌더가 이음새를 뭉개 곡선으로 읽힘(실측 캡처).
  - **스위치백 3굽이** (10,0.2)→(36,3.3) / (37,3.5)→(12,6.5) / (12,6.7)→(33,9.5) + 턴패드 2.
  - **Z 레인 계단식 후퇴** (1차 조립 실패→교정): 같은 X를 공유하는 굽이가 수직으로 쌓이면 위 굽이 옹벽이 아래 길을 카메라에서 가림 — Leg1 z-1.6(카메라 앞)→Leg2 z0→Leg3 z+1.4로 물려 무대 배경막처럼 겹겹이 보이게 함. 굽이 간 이동은 턴패드가 z를 잇는다.
  - **긴 계단 2** (지름길): 콜라이더=경사 램프 1개(렌더러 제거 — CC 덜컹 방지), 비주얼=계단 큐브 나열. StairLong 저지대→2굽이 위(6.5u 직등·47°), StairShort 2굽이→3굽이. 양끝 착지 슬래브가 계단 z와 길 z를 잇는다.
  - **고도 밴드 조닝**: 저지대 y0 = 아스팔트+연석+현대 건물 3동 / 등반로·정상 = 비포장 머티리얼 / 정상 y9.5 = 판잣집 3동(슬레이트 지붕 6° 기울임) + 비탈 판잣집 4동(지주 채움 — 언덕 실루엣).
- 실측: 등반로 접지 ○(y1.61) · 계단 램프 접지 ○(y3.69) · 정상 접지 ○(y9.58) · 카메라 Y 추종 ○ · 스포너 앵커 3(중턱/달동네 초입/정상) 재배선 · 캡처 4장(Screenshots/s051_*) — 우천 스위치백 컷이 달동네 감을 확인시킴.
- 검증: 컴파일 ○ 콘솔 0 ○ 재조립 ○ (매니저 무변경 — 테스트 32/32 유지).
- 2단계 백로그: 계단 구간 스태미나 추가 가중 · BuildingSlot(modern/moon 태그) · 달동네 아트 세트 발주(판잣집·물탱크·전봇대·연탄) · 저지대 가로등/달동네 백열등 조명 분리.

> ⚠ **번호 재조정 (2026-07-25 관제 · 선발 유지 관례)**: 아래 오디오 4건은 원발주가 S-050~S-054로
> 기록됐으나 관제 대장이 동일 번호를 먼저 사용(위 S-050·S-051) — **AU-013~AU-017**로 재번호.
> 커밋 메시지의 구번호(S-050~054)는 히스토리라 그대로다.

## AU-013 · 발주 2026-07-24 (Director 직접 지시 — 타이틀곡 반입·배선)

요구 (Director 원문): "타이틀 곡 아직 없으면, `Pixel Night Funk Don-T-Late.wav` 이 곡 붙이는 작업해줘(복사, 잘라내기 둘다 오케이)".

수용기준: Title 슬롯 공백 확인 · WAV 반입(임포터 자동 규격) · BgmLibrary Title 배선 · 라이선스 기록 · 커밋 게이트 통과.

### 결과 · 2026-07-24 (리드 ~15분)
- Title 슬롯 공백 확인(CREDITS 폐기이력 — 구 `Late_for_Work_8-Bit_Panic` 8비트 불일치 폐기 후 공백).
- WAV 복사 → `Assets/Audio/BGM/Pixel_Night_Funk_Don-T-Late.wav`(37.5MB · **195.6s**). AudioImportPostprocessor 자동: Vorbis · CompressedInMemory · 스테레오 · q0.30 · loadInBackground · WebGL안전(콘솔 0).
- `BgmLibrary.asset` Title(slot 3) 엔트리 추가 — Title 풀=1곡(Day 2·Night 3 무손상, exec 검증).
- Play 실검증: Main 전이 + DialogueEnded 구동 시 `CurrentClip = Pixel_Night_Funk_Don-T-Late`(195.6s) 크로스페이드 선택 확인 · 콘솔 0.
- 라이선스: **Suno 유료(Pro/Premier)** — 상업이용·소유권 귀속·표기의무 없음(Director 확인). `Assets/Audio/CREDITS.md` "BGM (타이틀) — Suno" 절 신설 + `.gitignore` 예외 추가.
- 인게임 재생: 타이틀 화면은 인트로 대화까지 무음(S-009), 대화 종료 후 크로스페이드 인. **곡 손맛·믹스 판정은 Director 청취 몫(사람 판정)**.
- 검증: 임포트 에러 0 ○ · 배선 exec ○ · Play 크로스페이드 실측 ○. (오디오 레인 — 콘솔 검증은 임포트 에러용, 청취는 사람 게이트)

---

## AU-014 · 발주 2026-07-24 (Director 직접 지시 — 타이틀곡 시작 화면 재생)

요구 (Director 원문): "시작 버튼 있는 화면에서 타이틀 곡 나오게 해줘".

배경: AU-013이 Title 슬롯에 곡을 배선했으나 **실플레이에서 영영 무음**이었다 — `_holdUntilFirstDialogue`(S-009 인트로 무음)가 ApplySlot 진입 즉시 return시켜 Main(타이틀)에서 곡 선택 자체가 안 됐고(직전엔 Title 풀이 비어 무증상), DialogueEnded 시점엔 이미 Home이라 `_titleScene=false`→Day/Night만 재생. Title 슬롯은 도달 불가 상태였다.

수용기준: 시작 버튼 화면(Main)에서 타이틀곡 재생 · S-009 인트로 무음 회귀 없음 · 셀프검증 3종.

### 결과 · 2026-07-24 (리드 ~20분)
- `WorldAudioManager.ApplySlot` 수정: hold 체크를 next 계산 **뒤로** 이동 + **Title 슬롯 예외**(타이틀 곡은 시작 화면에서 즉시 재생, 낮/밤 곡만 인트로까지 보류). 타이틀 이탈 시(held 상태로 non-title 진입) `StopBgm()` 신설로 타이틀 곡을 정지 — 인트로 무음 구간(S-009)으로 누수 방지.
- 실검증(부팅→타이틀, 인위 구동 없음): `titleScene=True · slot=Title · clip=Pixel_Night_Funk_Don-T-Late · PLAYING vol=0.50 t=22.7s` — 시작 버튼 화면에서 타이틀곡 실재생 확인.
- 회귀 방지: 이탈 경로는 `StopBgm`(held·non-title) → 무음, 대화 종료(released) 후 Day/Night 재생 경로는 무수정. Day/Night 풀 무손상(Day:2 Night:3 Title:1 실측).
- 검증: 컴파일 ○ · 콘솔 0 ○ · Play 실재생 ○. **인트로→Day 전 구간 청취는 Director 사람 판정(오디오 레인)**.
- 부기: 실 전이(Request(Home)) 관측 중 SceneFlow 전이가 Completed 미도달(Home 씬 빌드세팅/로드 이슈 — M1-05 미완, 콘솔 0). **오디오 변경과 무관**(TransitionRoutine 무수정) — 별건.

---

## AU-015 · 발주 2026-07-24 (Director 직접 지시 — 타이틀곡 보컬제거본 교체)

요구 (Director 원문): "`Pixel Night Funk Stems/1 Lead Vocal.wav` 은 보컬을 제거한 타이틀곡이야. 기존 곡은 지우지말고 보관하고 보컬없는 곡으로 교체해줘".

배경: 파일명이 `1 Lead Vocal`이라 이름상 보컬 스템처럼 보여 검증 필요 — Python 에너지 분석으로 판별.

수용기준: 보컬제거본이 실제 인스트루멘탈인지 검증 · Title 슬롯 교체 · 기존 보컬본 삭제 없이 보관 · 셀프검증.

### 결과 · 2026-07-24 (리드 ~15분)
- **판별**(ffprobe 부재 → Python wave/array): `0 Lead Vocal`=RMS2355·무음비31.9%(격리 보컬), `1 Lead Vocal`=RMS4352·무음비5.8%(연속 풀밴드=인스트루멘탈). Director 지시(`1`=보컬제거) 확인.
- 교체: `1 Lead Vocal.wav` → `Assets/Audio/BGM/Pixel_Night_Funk_Don-T-Late_NoVocal.wav` 반입(임포터 자동 규격, 콘솔 0). BgmLibrary: NoVocal=**Title(slot3)**, 기존 보컬본=**Unsorted(slot0)로 강등**(삭제 없이 보관·추첨 제외).
- 실검증(부팅→타이틀): `slot=Title · clip=Pixel_Night_Funk_Don-T-Late_NoVocal · PLAYING` — 보컬 없는 곡 재생 확인. Day2·Night3 무손상.
- 라이선스: 원곡 Suno 스템이라 동일(Suno 유료). CREDITS.md·assets_manifest.md 2곡 등재(현 타이틀=NoVocal, 보관=보컬본) + .gitignore 예외.
- 검증: 컴파일 ○ · 콘솔 0 ○ · Play 재생 ○. 손맛 청취는 Director 사람 판정(오디오 레인).

---

## AU-017 · 발주 2026-07-25 (Director 직접 지시 — 맵이동·대사 효과음 ElevenLabs 재생성)

요구 (Director 원문): "맵이동과 대사 효과음만 다시 일레븐랩스로 만들어줘." (AU-016 8비트 블립 롤백 직후 — b04c39d)

수용기준: sfx_map_pin/route/depart·sfx_dialogue_blip 4종 ElevenLabs 재생성 · 기존 파일 제자리 교체(guid 불변) · 셀프검증. 음질은 Director 청취.

### 결과 · 2026-07-25 (리드 ~20분)
- 선블로커 해소: ElevenLabs 크레딧 0 → Director 10000 충전 후 진행.
- 생성: `elevenlabs_client gen --overwrite` 4종(기존 토이톤 프롬프트·새 seed). seed 기록 — dialogue **864007029** · map_pin **1884846211** · map_route **782230717** · map_depart **2078724653**.
- 후공정: 파이프라인 normalize/intake/promote는 4종 BOM/JUICE 미등재로 게이트 차단 → 기존 프로젝트 자산 재생성이라(신규 반입 아님·이미 라이선스/manifest 등재) 자체 DSP(트림·피크 -1dB·RMS -14dB, **피크 한계 무클립**)로 처리 후 `Assets/Audio/SFX/` **제자리 교체**.
- 후공정 실측: route -14.0dB·depart -14.0dB(RMS 타깃) · dialogue_blip -1.0dB피크/-22.3dB·map_pin -1.0dB피크/-20.8dB(피크형 트랜지언트라 무클립 피크 한계 — 짧은 틱/플링크는 피크가 체감 음량). 확립 프로세스(0.81% 클립 가드)보다 보수적 = 무왜곡. **더 크게 원하면 클립 가드 재처리 가능**.
- guid 4종 전부 보존(.meta 미변경) → 코드·씬 재작업 0(맵 SFX=WorldAudioManager·블립=DialogueView `_blipClip` 배선 유지). Core 재빌드로 로컬 씬 정합(AU-016 잔재 정리).
- 검증: 임포트 콘솔 0 · 클립 4종 유효(mono 44.1kHz) · 배선 유지. **인게임 청취 판정은 Director(오디오 레인)**.
- 라이선스: ElevenLabs SFX 유료(기존 동일) — CREDITS/manifest 기등재.

---

## S-052 · 발주 2026-07-25 01:31 → ClaudeCode (본 세션 실행 — NPC 3종: 캠프 사장님·행인·심부름 노인)

요구 (남규님 원문):
1. **캠프 사장님 NPC** — 첫 방문 시 플레이어 앞으로 걸어와 튜토리얼 대화. 이후엔 구석에 서 있고
   다가가 말 걸면 격려 대사. **간혹 안 나오는 날도** 있게.
2. **행인 NPC** — 집(Home) 빼고 씬마다 배치, 길을 오가는 배회.
3. **심부름 노인 NPC** — 할머니/할아버지가 길가에 서 있음(간혹). 말 걸면 상자를 지정 위치로
   옮겨달라 부탁 → 옮기고 돌아와 말 걸면 보상.

수용기준: 사장님 접근→튜토리얼→복귀·재방문 격려·부재 추첨 / 행인 배회(Camp·District·Apartment·Hillside) /
심부름 수락→상자 픽업→목표 배달→복귀 보상(₩ 증가 HUD 반영) / Find 금지 준수 / 테스트 green.

### 결과 (S-052) · 2026-07-25 (리드 — NPC 3종 시공+실측)
- 신설 3종: [CampBossNpc.cs](../../Assets/Scripts/Interactables/CampBossNpc.cs)(접근 튜토리얼·격려·부재 25%) ·
  [PedestrianNpc.cs](../../Assets/Scripts/Interactables/PedestrianNpc.cs)(X 왕복 배회·위상 분산·무콜라이더) ·
  [ErrandNpc.cs](../../Assets/Scripts/Interactables/ErrandNpc.cs)(의뢰→운반→복귀 보상·부재 35%·런타임 주문으로 정산 격리)
  + [NpcBuildKit.cs](../../Assets/Scripts/Editor/NpcBuildKit.cs)(피규어·시나리오 SO GetOrCreate 공용 키트).
- 플레이어 발견 = OverlapSphere 저빈도 폴링(Find 금지 준수, ApartmentElevator 선례). 대사 = WorldDialogueManager
  재생(시나리오 SO는 빌더가 Data/Dialogue/ 생성). bossIntroPlayed는 GameStateSO+CoreBootstrap 리셋 편입.
- 배선: Camp(사장님+행인2) · District(행인3+할머니 ₩1,500) · Apartment(행인2+할아버지 ₩1,200) ·
  Hillside(행인2+할머니 저지대→달동네 초입 ₩2,500 — 긴 계단 지름길 유도). Home 제외.
- 실측(Play): 사장님 (-7.5,1.6)→(-1.6,0.3) 접근·튜토리얼 5줄 재생·종료 후 제자리 복귀·introPlayed=true ○ /
  District 할머니 의뢰 → ErrandBox(12.9)·마커(-6) 스폰 → 픽업(심부름 짐) → 마커 도달 자동 배달 →
  복귀 보상 money 0→**1,500**·totalEarned 반영·HUD 표시 ○ / 행인 배회 이동 캡처 간 위치 변화 ○.
- 검증: 컴파일 ○ 콘솔 0 ○ 테스트 32/32 ○ ★ 재조립 ○ 캡처 3장(Screenshots/s052_*).
