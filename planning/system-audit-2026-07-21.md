# system-audit-2026-07-21.md — 규약 17종 전수 감사 (system/·pipelines/·templates/)

> 발주: 감사 발주(읽기 전용) — 킷 규약이 실전과 정렬돼 있는가.
> 대조 기준: [[retrospective-2026-07-21]](세션 전체 감사 — 최우선) · [[decisions]](D-001~033 결정 대장) ·
> [[STATUS]] · [[TASKS]] · [[ai_evidence]](발주 증거 ~20건) · [[iterations]] · [[INBOX]] · [[BOM]] ·
> [[assets_manifest]] + 파일 실측(logs/·prompts/·calibration.md·ANTIPATTERNS.md·Data/schemas 존재 여부).
> 판정 어휘: **지켜짐**(실전 기록에 소비 증거) · **부분**(정신은 살았으나 명시 절차 일부 미가동) ·
> **위반**(규약이 명시한 것을 실전이 어김) · **미가동**(발동 조건이 아직 안 옴 — 사문 아님) ·
> **사문 위험**(문서엔 있는데 한 번도 안 돌았고, 안 돈 채 본선에 갈 위험). 판단 불가는 판단 불가라 쓴다.

---

## 1. 파일별 감사 표

### system/ (6)

| 파일 | 핵심 규약 요지 (1줄) | 실전 대조 | 근거 | 처분 권고 |
|---|---|---|---|---|
| [[system/aapp-methodology]] — AAPP 정의서 | BOM 태그→템플릿 검색→**라우팅 시트 발행**→에이전트 실행+**단계별 reviewer 게이트**+표준시간(std_min) | **부분** — 레인 분기 개념은 산다([[BOM]] §14 "라우팅 (AAPP)" — unity-cli 레인/asset-generator 레인 분기 실사용). 그러나 §7.3 라우팅 시트 발행 **0회**(발주는 봉투+[[ai_evidence]] 1줄로 직행), std_min 미집계(회고 §4-5), 공정 내 reviewer 게이트 0회(회고 §4-2), 모델 배정 필드 미사용(발주 ~20건 전부 기본 모델). §0 스스로 "실체 없이 이름만 붙이면 정의서 위반"이라 못 박았는데 지금 실체 중 절반(C4 라우팅 시트·C5 reviewer·C6 표준시간)이 미가동이다 | [[BOM]] §14 · [[retrospective-2026-07-21]] §4-2·4-5 · [[ai_evidence]] 전체(모델·라우팅 시트 언급 0) | **개정** — 실전이 채택한 경량형(봉투=라우팅 시트, ai_evidence=실행 로그)을 §7.3에 명문화하거나, 본선 전 표준 시트 1회 리허설. 현행대로면 발표에서 "AAPP 실물" 질문에 답이 얇다 |
| [[system/finals-execution-system-v2]] — 마스터 시스템 | 뼈대 선행·소켓 BOM·공정분리·불변식 8·국면 게이트·4h 체크포인트·WIP≤5 | **부분(불변식 1건 위반)** — 척추 3(뼈대·소켓·cast/module)은 실전 구조 그대로([[socket-map]]·[[BOM]] cast 후보 0건 유지·소켓 스왑 실증). 불변식 ③"뼈대 없는 양산 없음"은 **위반** — 국면 1 미종료(Q1 동결 미완) 상태에서 M3성 작업 다수(회고 §4-3, 관제 자인). 4h 통합 체크포인트 명시 실행 0회. §11 파일 레이아웃(`logs/`·`assets_manifest.json`)은 실전에서 이탈(→모순 목록 #4). LPT(캐릭터 최선두)는 준수([[BOM]] §1) | [[retrospective-2026-07-21]] §4-3 · [[TASKS]] M1-15 todo · [[STATUS]] 운용 메모(경로 이관) | **유지+개정** — 본문은 정본 유지, §11 레이아웃과 체크포인트 조항에 PRETASK 실전값(planning/ 경로·캘린더 이정표) 각주. 불변식 ③은 본선에서 훅/게이트로 강제할 것(권고 아니라 구조로) |
| [[system/hackathon-thesis]] — 디렉터 명제 | 심사 3층(완성도·시스템·**이해**), 처방 4: 채점기·rigor 티어·결정 대장·발표 리허설 | **대체로 지켜짐** — 결정 대장 실가동(D-001~033, 이유 열 전부 기입), rigor 티어 실가동([[TASKS]] core/peripheral 열). **채점기 3종 제작 0**(M0-08 todo — 진단 ①의 처방 미이행), 발표 리허설은 국면 4 몫(미래, 판단 유보) | [[decisions]] 전체 · [[TASKS]] M0-08 · 회고 §5 패턴 진단(사람이 QA가 됨 = 채점기 부재의 실비용 실증) | **유지** — 문서는 정확했다. 이행 쪽(M0-08)을 즉시 이행 목록으로 |
| [[system/harness-design-report]] — 설계 이력 대장 | 레퍼런스 8종→채택 24·기각 7의 판별 기록 (규약이라기보다 이력·발표 원자재) | **지켜짐(이력 문서로서)** — 실전 대조 대상이 적다. 다만 여기 적힌 처방 중 "NEVER 훅화 전수 감사(R7)"·"채점기(R8)"는 미이행, "CONNECT_REQUEST(R4)"는 1회 사용 후 폴백 강등(D-015)으로 실증 1회 | [[decisions]] D-015 · [[TASKS]] M0-07·08 | **유지** — 개정 불요. 발표 원자재로서의 가치가 본체 |
| [[system/skill-accumulation-protocol]] — 트래젝토리→스킬 | `logs/trajectory.md` 한 줄 로그→복기→3조건 승격→`calibration.md`·`ANTIPATTERNS.md`·`prompts/` append | **사문 위험 1순위** — 명시 파일 세트 **전부 부재 실측**(logs/trajectory.md·retro.md·calibration.md·ANTIPATTERNS.md·prompts/ 없음). op×model 캘리브레이션 0건 — 발주 ~20건이 전부 기본 모델로 나간 직접 원인(측정이 없으니 라우팅 근거도 없다). 단 **정신은 다른 경로로 살았다**: "실수→규칙 append"는 [[STATUS]] 운용 메모·CODE_RULES·pipelines §4에 12건+(회고 §3), 스킬 결정화는 `.claude/skills/midpoint-review` 1건 신설. 즉 프로토콜의 몸통(파일 세트·승격 절차)만 죽고 심장(append 규율)은 이식돼 뛰는 중 | 파일 부재 실측(Glob 0건) · 회고 §3·§4-5 · [[.claude/skills/midpoint-review/SKILL]] | **개정 필수** — §1 착지 표를 실전 착지(STATUS 운용메모·CODE_RULES·pipelines §4·.claude/skills/)로 갱신하고 trajectory/calibration은 "eta 실측 기입"(회고 백로그 5)으로 축소하거나 사문 선언. 이대로 두면 본선에서 아무도 안 펼 문서다 |
| [[system/change-freeze-management]] — 동결·확장·변경 | 계단식 동결 + append-only 확장 + **훅 4종(freeze-guard 등)** 집행 | **부분 + §7 사문 위험** — append-only 규율은 지켜짐([[decisions]]·[[iterations]]·[[ai_evidence]] 전부 append 운용, D-027 "매니페스트 직교 추가" 같은 직교 확장 어휘 실사용). 그러나 **훅 4종 제작 0**(M0-07 todo — 동결 집행이 전부 "부탁" 수준)이고, 계단식 동결의 첫 단(Q1)이 미완이라 동결 스케줄 자체가 아직 1회도 안 돌았다. INTENT만 동결 실증(frozen: true) | [[TASKS]] M0-07 · docs/INTENT.md `frozen: true` · 회고 §4-3 | **§7 즉시 이행** — 본선 중 하네스 신축 금지 원칙상 훅은 지금이 유일 기회. 본문 원리는 유지 |

### pipelines/ (7)

| 파일 | 핵심 규약 요지 (1줄) | 실전 대조 | 근거 | 처분 권고 |
|---|---|---|---|---|
| [[pipelines/logic-unity]] — 게임 로직 (관제 스팟체크 완료분 — 간단히) | unity-cli 검증 3종·자동교정 cap2·프리커밋 훅·실수→규칙 | **대체로 지켜짐** (17개 중 최고 정렬) — 검증 3종·봉투 절차가 전 발주에 실사용, §4에 시드 아닌 실전 규칙 2건(GetInstanceID·timeScale) append됨 = 루프 실증. 프리커밋 훅만 미설치(M0-07) | [[ai_evidence]] 전 항목의 "콘솔 0·Play 검증" 정형구 · §4 말미 2행 | **유지** — 훅만 이행 |
| [[pipelines/asset-3d]] — 3D 소품 | Meshy/Trellis 생성→bpy 후처리→`_intake/<도구명>/` 착지→manifest 입장권→검역→스왑 | **미가동 + 경로 규약 드리프트** — 생성 레인 0회 가동(M0-04 todo, Trellis 셋업 중). CONNECT_REQUEST는 Meshy 1회 발행 후 D-015로 폴백 강등(프로토콜 실증 1회). 착지 경로 규약(`assets/_intake/meshy/` 도구명 폴더)은 **D-028로 변형**(`_intake/art/` 단일 — 문서 미개정). manifest는 json→md(모순 #1). 검역·manifest 입장권 정신은 Tripo 반입 4건에서 그대로 집행됨([[assets_manifest]] 검역 수치·라이선스 플래그·커밋 보류) | [[decisions]] D-015·D-028 · [[TASKS]] M0-04 · [[assets_manifest]] Tripo 표 | **개정** — §2의 착지 경로를 D-028 체제로, 도구명에 Tripo 추가. 생성 공정 자체는 M0-04 관통이 리허설 |
| [[pipelines/character-anim]] — 캐릭터·애니 | Meshy 생성→🖐Mixamo 핸드오프(`_intake/mixamo/`)→리타깃 검증→육안 웨이트 판정 | **부분 — 정신 정렬·도구/경로 드리프트** — 실전은 Tripo(민지)+Mixamo 민지 매개로 유사 공정이 1회 완주(D-030 단일검증: Humanoid 아바타·Walk/Run 리타깃·본 좌표 3샘플 실측·높이 1.8u 정규화). 즉 "생성→리깅→리타깃 검증→사람 판정" 뼈대는 그대로 돌았다. 그러나 문서의 도구(Meshy)·핸드오프 경로(`_intake/mixamo/`)·Mixamo 절차서(M0-05 hold)는 전부 실전과 다르거나 미작성. LPT("국면 2 최선두") 원칙은 [[BOM]] §1이 승계 | [[decisions]] D-030·D-031 · [[assets_manifest]] late_man 검역 · [[TASKS]] M0-05 | **개정** — 도구 현실(Tripo 1순위·Mixamo=민지 매개) 반영 + M0-05 절차서를 이 문서에 흡수 |
| [[pipelines/content-data]] — 데이터 콘텐츠 | 스키마 선행→LLM 배치 생성→검증 스크립트→**빌드 반영이 done** | **한 번도 안 돎** — `Data/`에 스키마 0건 실측(Plugins만), validate_schema.py 부재, LLM 배치 생성 0회(so_orders·data_dialogue_pms 전부 todo/hold). 기동 점검 1번("스키마 없으면 생성 거부")에 걸려 시작조차 못 하는 상태 — 규약 위반은 아니고 P3 대기. 단 INTENT ai_axis 2번("대사·배송 콘텐츠 LLM 생성")의 실물이 이 레인이라 심사 직결 | Data/ 폴더 실측 · [[BOM]] §9·§12 · docs/INTENT.md ai_axis | **유지 + 리허설 필수** — 사문 위험 톱5 등재(아래 §3). so_orders 1건이 최소 리허설 |
| [[pipelines/build-deploy]] — 빌드·배포 | T+2h 관통 + **통합 체크포인트마다 재빌드**(상시 트랙) + 프리커밋 훅 | **부분 — 관통 ✔ · 상시 트랙 위반** — 관통 대응물(M0-03 WebGL→Pages) done·타 기기 확인 권장까지 진행. 그러나 "통합마다 재빌드" 원칙은 위반 — 셰이더 7종·TMP 반입 후 **재빌드 0회**, 회고가 스스로 🔴 최대 구멍으로 지목. §4 시드("빌드 확인을 통합 후로 미룸")가 경고한 바로 그 실수를 실전이 재연했다 — 시드가 규칙으로만 있고 강제(훅·체크포인트)가 없어서다 | [[TASKS]] M0-03 · [[retrospective-2026-07-21]] §4-1 · [[STATUS]] 체크포인트 GC 추가 항목 ① | **유지** — 문서가 옳았고 집행이 진 것. 회고 백로그 1(WebGL 회귀 1회)을 즉시 이행 |
| [[pipelines/audio]] — 오디오 | 연결 3종 중 1개 확보→JUICE 이벤트 도출 SFX만→라이선스=입장권→폴백 무음+신디 | **미가동 · 원칙은 선제 준수** — #8~10 전부 미검증(M0-06 todo), 레인 0회. 단 두 원칙이 이미 실전에 반영됨: ① SFX 목록=JUICE 도출([[BOM]] §8이 J-1 개정 승인(D-018) 없이는 발주 불가라 명시 — "임의 추가 금지" 그대로) ② 폴백 최하단("최소 신디")이 sfx_dialogue_blip 코드 합성으로 실증 | [[BOM]] §8 · [[decisions]] D-018 · [[ai_evidence]] dlg_speechbox(블립 WAV 합성) | **유지** — M0-06 관통이 리허설. 개정 불요 |
| [[pipelines/planning]] — 기획 (국면 0) | P0 해부→P1 카드 5장(S→A→D→M)→P2 채점·사람 선택→P3 결정화 + concept-cards 강제 저장 | **미가동(본선 대기) — 판단 불가 부분 있음** — 이번 세션 범위(사전과제·주제 기확정)에선 발동 조건이 없어 0회. §4 시드 13건의 밀도(카드 형식 개정 이력이 구체적)는 준비기간 드라이런의 흔적으로 보이나, 그 기록(concept-cards.md·`_sim/`)이 이 리포지토리에 **없다** — 리허설 완료 여부는 **판단 불가**. P3의 산출물 규격(INTENT 출력 B)은 실물이 완벽 준수(아래 intent-interview 행) | concept-cards.md·`_sim/` 부재 실측 · §4 시드 목록 | **유지** — 단 "리허설 증거 없음"을 인지할 것. 본선 첫 60분에 처음 도는 문서가 되면 리스크 |

### templates/ (5)

| 파일 | 핵심 규약 요지 (1줄) | 실전 대조 | 근거 | 처분 권고 |
|---|---|---|---|---|
| [[templates/question-gates]] — 질문 게이트 | 되돌릴 수 없는 지점(Q0~Q4)에서만 질문형·반론 인용 필수·기각 기록 | **부분 — Q1만 실증** — Q1 게이트가 실전 가동(반론 5건 처리→D-011~014, "R2: 빌드 0회 예산 동결 금지" 같은 인용형 반론이 D-014 사유로 수용 — §0 반론 유효 조건 실증). Q0은 준비기간 몫(판단 불가), Q2·Q2'(4h)·Q3·Q4 미가동. 기각 기록 경로(`logs/iterations.md`)는 실전 경로(planning/)와 불일치(모순 #4) — [[iterations]]에 [Q-게이트 기각] 형식 기록 0건(기각이 없었는지 미기록인지 판별 불가) | [[decisions]] D-011~014 · [[iterations]] 전문 2건 | **유지+경로 개정** — Q2가 BOM 동결 직전이라 곧 첫 가동. sacrifice 3개 표시는 [[BOM]] §15-5가 이미 준비 중(정렬 양호) |
| [[templates/intent-interview]] — INTENT 인터뷰 | 객관식 강제 선택→INTENT.md 출력 B 스키마→실행가능성 테스트→동결 | **지켜짐 (실물 증거)** — docs/INTENT.md가 출력 B 스키마를 필드 단위로 준수(genre/dimension/camera 승계·aesthetic 8종 어휘·ai_axis 빌드타임만·`frozen: true`). never의 "런타임 AI 호출" 항목은 Q7 관문이 설계대로 작동했다는 실물. 마감 관문 체크(장르 3칸·감정 어휘)도 전부 충족 | docs/INTENT.md v5 전문 | **유지** — 가장 잘 소비된 템플릿. 개정 불요 |
| [[templates/tech-spec-interview]] — TECH_SPEC 인터뷰 | 계약(일찍 동결) vs 튜닝(늦게) 2층 + 캐릭터 키 도미노 + 스케일 시트 | **지켜짐** — docs/TECH_SPEC.md 실재·실소비: 1.8u 앵커가 INTAKE 검역의 실측 기준으로 반복 사용([[assets_manifest]] "앵커 1.8u ±30% 미달 경고"), rig 항목은 D-013으로 "잠정 취급"(계약층인데 미확정 — 2층 원리를 결정 대장이 정확히 존중), 애니 세트 계약이 D-012 근거로 인용됨. 튜닝층 유동(안개 0.025→0.012 사람 조정)도 원리대로 | [[assets_manifest]] 검역 수치 · [[decisions]] D-012·D-013 · 회고 §5-9 | **유지** — B-4 확정 시 rig 동결로 완결 |
| [[templates/experience-quality-kit]] — STYLE·JUICE·경험 검수 | A: STYLE 규격 강제 · B: JUICE 이벤트-반응 명세 · **C: 사람 경험 검수 체크리스트(4h마다)** | **부분 — A·B 소비, C 사문 위험** — A: STYLE.md 실재·팔레트 4색(#ff9f45·#35e0c8)이 코드에 실사용(가로등 앰버·간판 시안). 단 STYLE 픽셀화 문구가 실구현(스냅 셰이더 D-011)과 불일치한 채 미동결(회고 §4-3). B: JUICE 도출 원칙이 [[BOM]] §8 오디오 발주의 관문으로 실가동(J-1 절차 포함). **C: 한 번도 안 폈다** — 회고 §4-4가 "검수 체크리스트 부재로 사람이 QA가 됨"이라 썼는데, MODULE C가 바로 그 체크리스트다. **있는 문서를 잊고 부재를 한탄한 것** — 사문화의 전형적 초기 증상. STATUS의 신설 "스크린샷 검수 체크리스트"(봉투 절차 ⓕ)는 C의 축소 재발명 | 회고 §4-4 vs 본 문서 MODULE C · [[STATUS]] 봉투 표준 절차 v2 · [[decisions]] D-025 | **개정** — MODULE C를 STATUS 봉투 절차 ⓕ와 통합(중복 해소)하고 체크포인트 의식에 명시 편입. A는 Q1 동결 때 실구현 반영 |
| [[templates/prompt-cheatsheet]] — 프롬프트 치트시트 | 5칸 계약·상황별 예시·선택적 정제·`prompts/` prompt_ref 재사용 | **부분** — 5칸 계약은 발주 봉투의 실제 골격(회고 §3의 "봉투 결함" 논의 자체가 5칸 체계 위에서 성립). §12 위험 안전장치(커밋=사람 게이트)·§10 과잉 방지(YAGNI)는 실전 규율과 일치(STATUS 결함 이력의 "YAGNI 위반이라 사람 판단에 올림"). 그러나 **`prompts/` 라이브러리·prompt_ref 0건**(폴더 부재 실측) — 정제 결과 결정화 경로 미가동. prompt-refiner 에이전트 사용 기록도 0 | prompts/ 부재 실측 · [[STATUS]] 결함 이력 · 회고 §3 | **유지** — 치트시트 본체는 참조 문서로 기능. prompt_ref 체계는 skill-accum 개정과 함께 존폐 결정 |

---

## 2. 모순·중복 목록 (규약↔규약 · 규약↔실전 결정)

| # | 충돌 지점 | 상태 | 내용 |
|---|---|---|---|
| 1 | HARNESS §9 "assets_manifest.**json**" ↔ 실전 **md** | **해소 확인** — 단 본문 미개정 | [[STATUS]] 운용 메모(2026-07-21 규칙화)가 "HARNESS §9의 json 대신 md로 확정(Foam 가독 우선)"을 명시. 발주문의 추정대로 이미 해소됨. HARNESS 본문은 여전히 json — 개정 기회에 각주 필요 |
| 2 | HARNESS §9 "`_intake/<도구명>/` 착지·폴더명=출처 증거" ↔ [[decisions]] D-028 "`_intake/art/` 단일(도구명 폐지)" | **해소(D-028) — 규약 본문 미개정** | D-028이 트레이드오프까지 기록("출처 증거는 manifest가 유일 — 기록 누락=반입 불가 더 엄격히"). [[pipelines/asset-3d]] §2와 [[pipelines/character-anim]] §2("`_intake/mixamo/` 고정")는 아직 구 규약을 인용 — **두 파이프라인 문서가 미개정 드리프트** |
| 3 | [[system/aapp-methodology]] §7.2 표준 공정에 reviewer verify가 필수 op ↔ 실전 판정 3주체 중 reviewer **0회** | **미해소** | HARNESS §6(판정 분리)은 "기계·reviewer·사람"을 등가로 두는데 AAPP 표준 시트는 reviewer를 공정 필수 단계로 박았다. 실전은 기계+관제 육안+사람으로 전부 소화(회고 §4-2). 둘 중 하나로 정리 필요: 표준 시트의 verify 주체를 "판정 주체 중 1"로 완화하거나, reviewer 리허설로 규약을 살리거나 |
| 4 | [[system/finals-execution-system-v2]] §11 `logs/{ai_evidence,iterations}` · [[templates/question-gates]] §0 `logs/iterations.md` ↔ 실전 `planning/` | **해소 — 본문 미개정** | Windows에서 `logs/`가 Unity `Logs/`와 대소문자 무시 충돌([[ai_evidence]] 헤더가 경위 기록). 실전 경로는 planning/으로 정착. 두 규약 본문의 경로 표기가 낡음 |
| 5 | [[system/skill-accumulation-protocol]] §1 착지(calibration.md·ANTIPATTERNS.md·prompts/) ↔ 실전 착지(STATUS 운용메모·CODE_RULES·pipelines §4·`.claude/skills/`) | **미해소 (구조 드리프트)** | 같은 기능("잘된 것·하지 말 것의 결정화")이 완전히 다른 파일에 산다. 문서 좇으면 빈 파일을 만들게 되고, 실전 좇으면 문서가 죽는다 — §1 표 갱신이 유일한 봉합 |
| 6 | skill-accum §6 승격 3조건(반복성=2회 이상) ↔ midpoint-review 스킬(1회 성공 프롬프트를 즉시 스킬화) | **미해소 (경미)** | 신설 스킬 자체는 프로토콜의 정신("성공 패턴 결정화") 그대로이나 반복성 조건을 건너뜀. 스킬 파일이 "원 프롬프트를 정형화"라고 출처를 밝혀 투명성은 지킴. 승격 조건에 "사람이 명시 요청한 절차화는 1회로 승격 가능" 예외를 두면 정합 |
| 7 | [[system/finals-execution-system-v2]] §8 48h 절대시간 국면표 ↔ 실전 캘린더 이정표(M0~M6·D-21) | **의도된 이원화 — 충돌 아님** | PRETASK 모드가 시계를 캘린더로 재정의([[STATUS]] mode·clock_note). 본선 복귀 시 §8이 다시 정본이 되는 구조 — 명시적이라 문제없음 |
| 8 | [[system/finals-execution-system-v2]] §0.2 ADR/GDD/MDA 문서 체계 ↔ 실전에 ADR·GDD·MDA 파일 부재 | **기능 대체 — 명명 드리프트** | ADR 역할은 [[decisions]](append-only·사유 필수)가, GDD/MDA 역할은 docs/INTENT+SCOPE+아키텍처 3종이 흡수. 기능 공백은 없으나 규약 문서의 파일명 참조가 실물과 어긋남 — 본선에서 "ADR 쓰라"는 지시가 나오면 혼선 소지 |
| 9 | [[BOM]] v0.3 스키마 ↔ [[system/finals-execution-system-v2]] §2 BOM 스키마(fabrication·placeholder_id·scene_slot·eta_min 필드 필수) | **부분 드리프트** | 초반 섹션(§1~2)엔 fab/source 열이 있으나 후반(§10~13 셰이더·스크립트·씬)은 담당·상태만 — 발주문 지적대로 태그가 후반에서 소멸. scene_slot은 [[socket-map]]으로 분리(개선으로 평가 — 좌표 정본 단일화), eta_min은 전부 미실측("Trellis 관통이 채운다" 명시). Q2 동결 전 fab 태그 보완 필요 — cast/module 라우팅([[system/finals-execution-system-v2]] §2.5)이 이 필드에 걸려 있다 |
| 10 | STYLE L1(저해상 RT 렌더) ↔ 실구현(네이티브+스냅 셰이더, D-011) | **인지된 미해소 — Q1 대기** | D-011·D-025가 "STYLE 문구 개정은 Q1 절차에서"로 예약. 회고 §4-3이 동결 지연의 리스크로 재확인. 규약이 실전을 따라가는 정상 경로에 있으나, 동결이 밀리는 한 불일치가 계속 산다 |
| 11 | [[pipelines/audio]]·[[templates/experience-quality-kit]] "JUICE 이벤트 밖 SFX 금지" ↔ [[BOM]] §8 SFX 7종 추가 | **정합 (모범 사례)** | 추가를 임의로 하지 않고 J-1 개정안→사람 승인(D-018)→JUICE 개정 경로를 탔다 — change-freeze §4(확장 4게이트)의 정신이 실제로 집행된 유일한 사례 |
| 12 | [[templates/experience-quality-kit]] MODULE C ↔ [[STATUS]] 봉투 절차 ⓕ 스크린샷 검수 체크리스트 | **중복 (재발명)** | 회고 백로그 2로 신설된 ⓕ가 MODULE C의 부분집합(프레이밍·텍스처·종횡비·팔레트). 항목·시점(4h 체크포인트)이 겹침 — 하나로 통합하지 않으면 둘 다 반쪽으로 산다 |

---

## 3. 사문화 위험 톱5 — "문서엔 있는데 한 번도 안 돈 것" (본선 전 리허설 필요 순)

1. **훅 5종·채점기 3종** ([[system/change-freeze-management]] §7 · [[system/hackathon-thesis]] 진단①) — 제작 0([[TASKS]] M0-07·08). 위험이 최상급인 이유: ① "본선 중 하네스 신축 금지" 원칙상 **지금이 마지막 기회**이고 ② 이번 세션의 실패 다수(빌드 재확인 미룸·동결 미선언·발주서 품질)가 전부 "부탁은 있고 강제가 없어서"였음이 회고로 실증됐다. 훅 없는 동결 규약은 규약이 아니라 희망이다.
2. **reviewer 레인** ([[system/aapp-methodology]] C5 · HARNESS §6) — 서브에이전트 발주 ~20건에 0회. 자기평가 편향 리스크가 이미 관찰됨(회고 §4-2 — 관제가 작성자 겸 검수자인 구간). 다음 core 발주 1건에서 ACCEPT/REJECT 리허설(회고 백로그 3과 동일 처방).
3. **content-data 레인** ([[pipelines/content-data]]) — 스키마·검증 스크립트·배치 생성 전부 0. INTENT ai_axis의 절반("대사·배송 콘텐츠 LLM 생성")이 이 레인이라 **심사 증거 직결** — 안 돌리면 ai_axis가 주장으로만 남는다. so_orders 6건 생성이 최소 리허설.
4. **skill-accumulation-protocol 파일 세트** — trajectory·calibration·ANTIPATTERNS·prompts 전부 부재. 특히 calibration 부재는 "발주 전량 기본 모델"의 구조적 원인 — 측정 없이는 haiku 강등/opus 상향의 근거가 영원히 안 생긴다. eta 실측 기입(회고 백로그 5)부터가 이 프로토콜의 최소 가동이다.
5. **경험 검수 체크리스트 MODULE C** ([[templates/experience-quality-kit]]) — 존재하는데 회고가 "체크리스트 부재"라고 쓸 만큼 완전히 잊혔고, 그 공백을 사람 QA 13건(회고 §5)이 몸으로 메꿨다. STATUS 봉투 절차 ⓕ와 통합해 4h 체크포인트 의식으로 살릴 것. — *차순위(톱5 밖): [[pipelines/planning]] P0~P3와 Q0 게이트. 리허설 기록이 리포지토리에 없어(판단 불가) 본선 첫 60분이 초연이 될 수 있다.*

---

## 4. 총평

킷은 "정신은 이식됐고 기관(器官)은 절반이 미가동"인 상태다. 통신 규율·append-only·판정 분리·검역·동결 어휘 같은 **원리 층은 실전 기록 곳곳에서 실제로 집행됐고**(J-1 개정 절차, INTAKE 검역, 결정 대장 33건, 실수→규칙 12건), intent/tech-spec 인터뷰처럼 실물이 스키마를 필드 단위로 준수하는 모범도 있다. 그러나 **절차 층 — 라우팅 시트·표준시간·reviewer 게이트·훅·채점기·트래젝토리 로그 — 은 한 번도 돌지 않았고**, 그 공백을 관제의 육안과 사람의 몸이 메꿨다는 것이 회고의 실측이다. 더 나쁜 신호는 규약과 실전이 어긋날 때 실전이 이기고 문서가 방치되는 패턴이 이미 4곳(asset-3d 경로, skill-accum 착지, finals §11 레이아웃, MODULE C 재발명)에서 반복된다는 점이다 — 이 드리프트를 지금 개정하지 않으면 본선에서 킷은 "펴보지 않는 문서"가 되고, 심사 3층의 ②(시스템 증명)는 실물 없는 주장이 된다. 처방은 이미 나와 있다: M0-07·08(훅·채점기)을 즉시 이행하고, reviewer·content-data 리허설을 각 1회 돌리고, 미개정 규약 4곳을 D-번호 기준으로 소급 개정하는 것 — 전부 본선 전에만 가능한 일이다.
