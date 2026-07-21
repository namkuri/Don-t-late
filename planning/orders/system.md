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
