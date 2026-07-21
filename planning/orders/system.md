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
