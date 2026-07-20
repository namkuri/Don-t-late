# AAPP 방법론 정의서
## Agent-Aided Process Planning — 에이전트 기반 공정 계획

> **한 줄 정의** — AAPP는 CAPP(변형형)에서 파생된 방법론으로, AI가 생성하는 디지털 에셋의 **공정 계획을 자동 생성하고 그 실행까지 에이전트가 수행**하는 시스템이다. 오케스트레이터가 에셋의 선언적 피처(태그)를 인식해 표준 공정을 검색·변형하고, 라우팅 시트를 발행해, 전문 에이전트에게 공정 내 검증과 함께 위임한다.

- **분류**: 방법론 정의 (마스터 실행 시스템의 공정 계획 계층)
- **계보**: CAPP (Computer-Aided Process Planning) → **AAPP**
- **적용 도메인**: AI 지원 게임/소프트웨어 에셋 제작 (예: 심야배송 · NAN 2026)
- **표어**: *AI가 계획하고, 에이전트가 수행한다 (AI-planned, Agent-executed).*

---

## 0. 왜 새 이름인가 — 명명 근거

기존 CAPP를 그대로 쓰지 않고 AAPP로 명명하는 이유는 **본질적 차이가 이름값을 하기 때문**이다. CAPP는 공정 *계획서를 생성*해 사람/기계에 넘긴다(계획과 실행이 분리). AAPP는 계획을 생성하는 **즉시 같은 에이전트 루프 안에서 실행·검증**한다(계획-실행 통합). 이 통합이 "Agent-aided"라는 이름의 실체다.

> **주의** — 이름은 실체의 라벨이다. AAPP는 (1) 피처→공정 매핑 테이블, (2) 표준 공정 템플릿 라이브러리, (3) 라우팅 시트 자동 생성, (4) 에이전트 실행+검증이라는 **작동하는 실체**를 가리켜야 한다. 실체 없이 파이프라인에 이름만 붙이는 것은 이 정의서의 위반이다.

---

## 1. 배경 & 동기

기존 실행 시스템에는 **BOM(무엇을)** 과 **TASKS(어디까지)** 는 있었으나, 그 사이의 **"어떻게"(각 에셋이 어떤 공정을 어떤 순서로 거치는가)** 가 암묵적이었다. 사람이 매번 "이건 이 파이프라인 태워"를 지정했다. 이는 세 가지 낭비를 낳는다.

- **판단 반복** — 유사 에셋마다 같은 공정 지정을 되풀이.
- **암묵지 의존** — 공정 지식이 문서화되지 않아 재현·위임 불가.
- **스케줄 불가** — 공정별 표준시간이 없어 시간 예산·병목 계산 불가.

AAPP는 이 "어떻게" 층을 **자동 생성**해 세 낭비를 제거한다.

---

## 2. 핵심 명제

> **CAPP와 AAPP의 진짜 차이는 "AI가 계획을 짠다"가 아니라 "계획과 실행이 분리되지 않는다"이다.**

제조 CAPP: `공정 계획 생성` → (인계) → `공장에서 실행`. 두 주체가 다르다.
AAPP: 오케스트레이터가 `라우팅 시트 발행` → 즉시 `에이전트가 실행·검증` → `공정 로그 축적`. 하나의 루프.

이 통합이 낳는 이차 효과: 계획이 곧 실행 명령이고, 실행 로그가 곧 계획의 검증 기록이며, 그 전체가 감사 증거(에이전트 설계서의 자료)가 된다.

---

## 3. CAPP 계보 & 대비

### 3.1 CAPP의 두 방식

- **변형형(Variant)** — 부품을 유형 분류(GT)해, 유형별 **표준 공정을 검색·변형**. 처음부터 안 짬.
- **생성형(Generative)** — 부품 피처를 인식해 규칙 기반으로 공정을 **처음부터 합성**.

### 3.2 AAPP는 변형형 기반

에셋 유형이 6~8개로 적으므로, 유형별 표준 공정 템플릿을 미리 두고(레일) 검색·변형하는 **변형형**이 최적. 생성형 규칙엔진은 48h 맥락에 과잉.

### 3.3 대비표

| 축 | CAPP | **AAPP** |
|---|---|---|
| 도메인 | 물리 부품 | 디지털 에셋 |
| 피처 출처 | CAD 지오메트리 인식 | **선언적 태그(BOM)** — 사람/AI 작성 |
| 접근 | 변형형 또는 생성형 | **변형형** (유형 소수) |
| 계획–실행 | 분리 (계획서 인계) | **통합** (즉시 실행) |
| 실행 주체 | 기계·사람 | **AI 에이전트** |
| 검증 | 사후 QC | **공정 내 게이트** (단계마다) |
| 산출물 | 공정 시트 | 라우팅 시트 + **실행·검증 로그** |

---

## 4. 공리 (Axioms)

AAPP를 규정하는 7개 공리. 위반 시 그것은 AAPP가 아니다.

- **A1 선언적 피처** — 공정은 에셋의 **선언적 태그**에서 파생된다. 지오메트리 인식 엔진 불필요.
- **A2 변형 우선** — 유형별 표준 템플릿을 검색·변형한다. 생성형 규칙엔진 배제.
- **A3 계획–실행 통합** — 라우팅 시트 발행 즉시 에이전트가 실행한다. 계획서를 외부에 인계하지 않는다.
- **A4 공정 내 검증** — 각 공정 단계 끝에 검증 게이트가 있다. 사후 QC가 아니다.
- **A5 표준시간 기반** — 각 공정에 표준시간이 붙고, 이것이 스케줄링·시간예산의 입력이다.
- **A6 얇은 구현** — 규칙 테이블 + 템플릿 라이브러리로 구현한다. 무거운 엔진을 만들지 않는다.
- **A7 계획이 곧 증거** — 라우팅 시트·실행 로그가 감사 증거로 축적된다.

---

## 5. 구성요소

```
[피처 계층] → [매핑 규칙] → [템플릿 라이브러리] → [라우팅 생성기] → [실행·검증] → [스케줄 훅]
   BOM 태그      규칙 테이블      유형별 표준공정        오케스트레이터      에이전트+reviewer   DES
```

| # | 구성요소 | 역할 | 시간대 |
|---|---|---|---|
| C1 | **피처 계층** | BOM 태그(`fabrication`·`source`·`interface`·`min_strategy`·`group`)가 곧 피처 | 콘텐츠 |
| C2 | **매핑 규칙** | 태그 조합 → 어떤 템플릿·변형을 쓸지 결정하는 규칙 테이블 | 레일 |
| C3 | **템플릿 라이브러리** | 에셋 유형 × 표준 공정 시트 (변형형 CAPP 코어) | 레일 |
| C4 | **라우팅 생성기** | 오케스트레이터: 분류→검색→변형→라우팅 시트 발행 | 실행 |
| C5 | **실행·검증** | 에이전트가 단계별 수행, reviewer가 단계마다 게이트 | 실행 |
| C6 | **스케줄 훅** | 표준시간을 DES(병목·LPT·WIP)에 공급 | 실행 |

---

## 6. AAPP 루프 (Workflow)

```
BOM 항목 (피처 태그)
   │
   ▼ C4 라우팅 생성기
[1] 유형 분류        ── source=fake? → 트릭공정 or 스킵 (파이프라인 미탑재)
[2] 표준공정 검색     ── 유형 → 템플릿 라이브러리(C3)에서 검색
[3] 변형 적용        ── 태그(fabrication/interface 등)로 단계 가감
[4] 라우팅 시트 발행  ── 단계·담당에이전트·표준시간·선행조건 확정
   │
   ▼ TASKS 실행 큐 등록 (스케줄 훅 C6: LPT·WIP·병목 반영)
[5] 에이전트 단계 실행 ── 각 단계 = seize 자원 → 수행 → release
[6] 공정 내 검증(A4)  ── 단계마다 reviewer 게이트 (수용기준+인터페이스)
   │   └ 미달 → 재프롬프트(≤2) or 에스컬레이션
[7] 소켓 스왑 / 통합  ── placeholder 교체(module) 또는 직접 통합(cast)
[8] 공정 로그 축적(A7) ── 라우팅·검증·이터레이션 기록
```

---

## 7. 핵심 산출물 & 스키마

### 7.1 피처→공정 매핑 규칙 (C2 · 레일)

```yaml
# 태그 조합 → 템플릿 선택 (위에서부터 우선 매칭)
rules:
  - when: {source: fake}                         → template: STD-FAKE       # 스킵/트릭
  - when: {source: reuse}                        → template: STD-REUSE
  - when: {fabrication: cast}                    → template: STD-CAST       # 통짜, 늦게, 사람게이트
  - when: {type: prop_3d, source: generate}      → template: STD-PROP-3D
  - when: {type: character, source: generate}    → template: STD-CHARACTER
  - when: {type: data}                           → template: STD-DATA
  - when: {type: audio}                          → template: STD-AUDIO
```

### 7.2 표준 공정 시트 (C3 · 레일 · 유형별)

```yaml
# STD-PROP-3D (3D 소품 · module)
template: STD-PROP-3D
fabrication: module
steps:
  - op: generate     agent: asset-module   tool: Meshy      std_min: 8
  - op: postprocess  agent: asset-module   tool: Blender    std_min: 3
  - op: import       agent: unity-dev      tool: Unity MCP  std_min: 1
  - op: verify       agent: reviewer       gate: acceptance+interface  std_min: 1
  - op: swap         agent: unity-dev      target: placeholder  std_min: 1
est_total_min: 14

# STD-CHARACTER (캐릭터 · module · 장(長)공정 → LPT 우선)
template: STD-CHARACTER
fabrication: module
steps:
  - op: concept      agent: concept        tool: Nano Banana  std_min: 5
  - op: generate     agent: asset-module   tool: Meshy        std_min: 10
  - op: rig          agent: asset-module   tool: SkinTokens   std_min: 12
  - op: animate      agent: asset-module   tool: Cascadeur    std_min: 10  # 사람 개입 큼
  - op: import+verify agent: unity-dev/reviewer                std_min: 3
est_total_min: 40      # 길다 → 국면2 시작에 먼저 투입

# STD-CAST (환경블록·긴밀로직 · 통짜)
template: STD-CAST
fabrication: cast
gate_before: [scope_locked, core_loop_done, human_approval]   # 늦게, 사람게이트
steps:
  - op: cast_generate  agent: casting-agent  note: "통짜 생성, worktree 독점"
  - op: verify         agent: reviewer
  - op: integrate      agent: unity-dev      note: "병합 없음, 직접 통합"
```

### 7.3 라우팅 시트 (C4 산출 · 인스턴스)

```yaml
# BOM 항목 하나에 대해 발행된 실제 공정 계획
routing:
  bom_id: prop_porter
  asset_type: prop_3d
  template: STD-PROP-3D
  variations: []                 # 태그로 인한 단계 가감 (없으면 표준 그대로)
  steps: [generate, postprocess, import, verify, swap]
  target_socket: gray_porter
  est_total_min: 14
  status: queued                 # queued|running|done|blocked
```

---

## 8. 인접 프레임워크와의 관계

AAPP는 고립된 게 아니라 제조 계획의 표준 흐름을 소프트웨어 도메인에 이식한 것이다.

```
WBS(-lite)  →  BOM  →  AAPP  →  스케줄링(DES)  →  에이전트 실행
 분해·계층·소유   자재    공정계획    순서·타이밍         수행
```

| 프레임워크 | 담당 질문 | 산출물 |
|---|---|---|
| **WBS-lite** | 무엇을 어떤 덩어리로 (계층·소유) | BOM의 `group` 필드 |
| **BOM** | 무엇을 (자재·피처) | ASSET_BOM |
| **AAPP** | 어떻게 (공정 계획) | 라우팅 시트 |
| **DES** | 언제·어떤 순서로 | 스케줄(병목·LPT·WIP) |
| **에이전트** | 실제 수행 | 씬에 꽂힌 산출물 |

> **WBS 주석** — 본격 WBS는 과잉. `group` 필드 한 겹의 얇은 WBS면 충분하며, 이를 공정분리(cast/module)·소유권(2인 시 `OWNERSHIP.md`)과 정렬시킨다. 1인·소규모는 평면 BOM으로 생략 가능.

---

## 9. 경계 — AAPP가 아닌 것

- **생성형 CAPP가 아니다** — 지오메트리에서 신규 공정을 합성하는 규칙엔진이 아니다(A2).
- **CAD 피처 인식기가 아니다** — 피처는 탐지되는 게 아니라 **선언**된다(A1).
- **무거운 소프트웨어가 아니다** — 규칙 테이블 + 템플릿 라이브러리로 충분(A6).
- **계획 전용이 아니다** — 실행이 통합돼 있다(A3). 계획만 뽑고 끝나면 그것은 CAPP다.
- **범용 프로젝트 관리가 아니다** — 에셋 공정 계획에 특화. 일정·인력 관리는 인접 도구(WBS·DES)에 위임.

---

## 10. 적용 예시 — 심야배송

**입력 BOM 항목 3종의 공정 자동 생성:**

| 항목 | 피처 태그 | 매핑된 템플릿 | 공정 요약 |
|---|---|---|---|
| 포터 트럭 | type=prop_3d, generate, module | STD-PROP-3D | Meshy→Blender→임포트→검증→소켓스왑 (~14min) |
| 주인공 | type=character, generate, module | STD-CHARACTER | 원화→Meshy→리깅→애니→검증 (~40min, 국면2 먼저 투입) |
| 배송 메모 200종 | type=data, generate | STD-DATA | 스키마확인→LLM배치생성→JSON검증→번들(빌드 포함) (~8min) |
| 단지 A 블록 | fabrication=cast | STD-CAST | 통짜 생성→검증→직접통합 (스코프락 후, 사람게이트) |
| 먼 산 | source=fake | STD-FAKE | 무료에셋+안개, 생성 파이프라인 미탑재 |

사람은 BOM 태그만 채운다. **공정 선택·순서·담당·표준시간은 AAPP가 자동 파생.** 오케스트레이터가 라우팅 시트를 발행하고 에이전트가 실행·검증한다.

---

## 부록 A. 용어집

- **피처(Feature)** — 공정을 결정하는 에셋의 선언적 속성(BOM 태그).
- **템플릿(Standard Process Sheet)** — 에셋 유형별 표준 공정 정의(레일).
- **라우팅 시트(Routing Sheet)** — 특정 BOM 항목에 대해 발행된 실제 공정 계획.
- **표준시간(std_min)** — 각 공정 단계의 예상 소요, 스케줄링 입력.
- **소켓 스왑(Socket Swap)** — placeholder를 완성 에셋으로 교체하는 통합(module).
- **캐스트/모듈(Cast/Module)** — 통짜 생성(병합 제거) / 병렬 생산(병합 사소화).

## 부록 B. 심사 관점

AAPP를 정의·명명했다는 것 자체가 신호다 — 기존 산업공학 프레임워크(CAPP)를 이해하고 새 도메인에 이식·명명할 수 있는 역량. "도구를 썼다"가 아니라 "**AI 제작 공정을 계획·자동화하는 방법론을 설계했다**"는 것이며, 이는 슬로건 "AI의 다음 단계를 설계할 디렉터"의 정확한 실물이다. 단, §0 주의대로 **실체가 이름을 뒷받침할 때만** 유효하다.

---

*본 정의서는 `finals-execution-system-v2.md`의 공정 계획 계층(§2.5 공정분리 · §4 파이프라인 · §7 태스크 흐름)을 방법론으로 정식화한 것이다.*
