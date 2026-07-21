# distributed-workflow.md — 분산 협업 프로세스 (관제 ↔ 정수 PC)

> 확정: [[decisions]] D-032(Settings 커밋)·D-033(프로세스). 원리: **씬은 빌더가 정본**이라
> 코드만 오가면 양쪽 에디터에서 같은 게임이 재현된다. 코드의 관문은 git PR — _intake 금지
> (동명 클래스 컴파일 에러·.meta GUID 파손 방지, [[STATUS]] 코드 반입 경로 규칙).

## 0. 정수 PC 최초 셋업 (1회, ~40분)

1. Unity **6000.5.3f1** 설치 (버전 일치 필수) + 저장소 clone
2. (권장) unity-cli 셋업 — 커넥터는 Packages/manifest에 이미 있음, CLI 바이너리만 설치
   (셀프검증 3종을 명령으로 돌리려면 필요 · Claude Code 쓸 경우 필수)
3. 에디터 열고 **씬 재현 메뉴 4개 순서대로 실행**:
   `DontLate/Build Core Scene` → `DontLate/Build Scene Flow UI` → `DontLate/Build District Stage`
   → (개발용) `DontLate/Build Greybox Stage`
4. 확인: `Core.unity` 열고 Play → 타이틀 "늦지마!!" → 클릭 체인으로 하루 사이클 완주되면 셋업 완료
   (픽셀화·블룸이 안 보이면 Settings 커밋분 pull 누락 — git 상태 확인)

## 1. 발주 → 납품 → 판정 루프

```
관제(남규 PC)                              정수 PC
─────────────                              ─────────────
① 발주서(5칸 봉투) 작성
   → planning/orders/<id>.md 커밋+push ──→ ② pull → 봉투를 Claude Code에 그대로 투입
                                              (또는 직접 구현 — 봉투가 수용기준을 정의)
                                           ③ 셀프검증 3종: 컴파일 · 콘솔 0 · Play 기대동작
                                              = "제출 자격" (판정 아님 — HARNESS §6)
                                           ④ feature 브랜치 push → PR 생성
⑤ 관제: PR 감지(gh) → pull →      ←──────    (PR 본문에 관찰 기록 — 판정어 금지)
   로컬 에디터에서 독립 재검증
   → 결과를 PR 코멘트로
⑥ 남규: 코멘트 확인 → Merge 클릭 = 사람 승인
⑦ 관제: merge 감지 → TASKS review→done 자동 전이 (남규 추가 명령 불필요)
   반려 시: PR 코멘트에 사유 → ②로
```

## 2. 규칙 요약 (충돌 방지의 핵심)

| 규칙 | 이유 |
|---|---|
| 코드는 **PR로만** — 파일 복사·_intake 금지 | 동명 클래스 즉시 컴파일 에러 · .meta 유실 시 씬 참조 붕괴 |
| **씬(.unity)·프리팹 커밋 금지** 유지 — 빌더 수정으로 표현 | 병합 지옥 방지. 씬을 바꾸고 싶으면 해당 빌더 코드를 고쳐 PR |
| `Assets/Settings/`·`ProjectSettings/` = **남규만 수정** (커밋은 됨, D-032) | 렌더 설정 소유권 — 양쪽에서 고치면 즉시 충돌 |
| 커밋 태그: `[P2] 파일명: 요지 (via ClaudeCode) [self-tested]` | 기존 규칙 유지 |
| 같은 파일을 만질 발주는 동시에 내보내지 않음 (관제가 조율) | PR 충돌 예방 — 발주 묶음은 파일 비겹침 기준 |
| PR 전 main을 rebase/merge로 최신화 | 관제 커밋과의 드리프트 축소 |

## 3. 레이턴시 운용

- 발주는 **2~3건 묶음**으로 (정수의 한 작업 세션 분량) — 낱개 릴레이는 대기 낭비
- 관제 커밋(main)이 잦으므로 정수는 작업 시작 시 pull 습관
- 급한 소통은 PR 코멘트가 아니라 사람 채널(카톡 등) — 단 **결정·반려 사유는 반드시 PR에 기록**
  (심사 증거 — 기록 없는 반려는 없던 일이 된다)
