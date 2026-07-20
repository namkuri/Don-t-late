# STATUS.md — 관제 상태 (오케스트레이터 소유 · 사람은 읽을 필요 없음)

```yaml
mode: PRETASK              # 사전과제 모드 (PRETASK.md) — 시계=캘린더 이정표
t0: "2026-07-20 17:09"     # 1차 관제 세션 기동 (KST)
deadline: "2026-08-10"     # M5 제출 마감 (D-21)
milestone: M1              # M1 뼈대 — 그레이박스 루프 완주 확인됨
phase: 1                   # PROCESS 국면 1 · 뼈대
stage: "국면 1 출구 — WebGL 관통 ✅ (M0-03 done). 동결 잔여 조건 = 민지 상의(D-013·014). BOM 초안 작성됨(planning/BOM.md — Q2에서 동결)"
session_host: WINDOWS_LOCAL   # unity-cli 직접 실행 가능 (2026-07-20 17:23 전환)
pending_decisions:
  - "Q1 게이트: ARCH/STYLE/TECH_SPEC 동결 승인 — 루프 완주했으므로 사거리 진입"
  - "B-4 캐릭터 도구·리그 — M0-04 Meshy 관통 실측 후에 결정 (그 전엔 보류)"
blocked:
  - "M0-03 WebGL→Pages 관통 미검증 (제출 규정 직결 · 긴급도: 높음)"
  - "CONNECTIONS #2~#10 미검증 — M2 양산 진입 전 관통 필요"
last_checkpoint: "2026-07-20 17:4x — 그레이박스 루프 완주 검증"
clock_note: "정상 — 제출까지 21일"
```

## 확정된 사실 (실측 기반 · 2026-07-20)

- **unity-cli 연결됨** — CLI + 커넥터 0.3.22 / Unity 6000.5.3f1 `ready`.
  `status`·`exec`·`console`·`screenshot` 4수 전부 관통. 아키텍처 §8-5 호환 리스크 **종료**.
- **관제 세션 = 로컬 Windows PC.** 에디터 조작·컴파일·플레이 검증을 관제가 직접 수행 가능.
  (구 기록의 "리눅스 샌드박스라 실행 불가"는 무효)
- **스크립트 25종 실재** (`Assets/Scripts/`). 정수 작성. Input System 액션은 **에셋 없이 코드 정의**라
  별도 셋업 불필요.
- **`GreyboxStageBuilder`(에디터 메뉴 `DontLate/Build Greybox Stage`)가 무대 조립 전체를 소유** —
  씬을 손으로 짜지 않는다. 멱등이라 다시 눌러도 안전하고, 데이터·적재까지 초기화한다.
- **그레이박스 배송 1건 완주 확인** (`Greybox.unity`, Play, 콘솔 0건):
  픽업 → 운반 → 문앞 인증 → `done=1 / money=+5000 / cargo=0 / late=0`.
- 씬 자산: `Main.unity`(사람 샌드박스 · 수동 셋업 보존) · `Greybox.unity`(관제 생성) ·
  `SampleScene.unity`(URP 템플릿 잔재).
- 워킹트리 미커밋 **8건** 실측 — 씬·프리팹 없음, 커밋 경계 규칙 위반 없음.

## 결함 이력

- ~~**지각 처리 구멍**~~ → **M1-13 수정됨 (review 대기)** · 2026-07-20.
  `PlayerStatusManager`가 `WorldEvents.DeliveryFailed`를 구독해, 실패 건이 든 것과 같으면
  `ReleaseCarry()` 호출. 경계 통신은 이벤트만 사용(§3 준수). 수정 파일 1개.
  검증: 마감 초과 시점 `IsCarrying=False / late=1 / cargo=0` · 해피패스 무회귀
  `done=1 / money=+5000 / late=0` · 콘솔 0건.
- **미처리 (사람 판단 대기)**: `DeliveryPoint.Interact`의 mutate-then-check 순서 결함.
  `ReleaseCarry()`를 `CompleteDelivery()` 성공 확인 전에 호출한다. 위 수정으로 **도달 불가**가
  됐으나 순서 자체는 틀렸다. `IsInCargo()`로 한 줄 방어 가능 — 단 도달 불가 상태의 방어는
  YAGNI 위반이라 사람 판단에 올림. 상세: `orders/M1-13.md` 부수 발견.

## 관제 운용 메모

- **사람 처리함 = [[INBOX]]** (2026-07-20 신설): 판정 대기·결정 대기·손 작업 대기를 한 파일에 유지.
  관제는 브리핑·상태 변화 때마다 INBOX를 동기화한다 — 사람 대기 항목이 채팅에만 존재하면 규칙 위반.
- **저맥락 링크 규칙 (2026-07-20 사람 지시 · D-023으로 전 문서 확장)**: **모든 문서**(planning·docs·rules)
  내부에서 ID(M1-xx·D-xxx·B-x·Q-x·M2 같은 이정표 포함)나 타 문서를 언급하면 Foam 링크를 건다 —
  기존 문서는 손댈 때마다 소급 적용(일괄 개정은 하지 않음, 동결 문서는 개정 기회에만).
  planning/ 문서 간 상호참조는 **Foam 위키링크**
  (`[[TASKS]]` `[[BOM]]` `[[open-questions]]` `[[decisions]]` — 파일명 기준). 브리핑·문서에서
  ID(M1-xx·D-xxx·B-x·Q-x·bom_id)를 언급할 때는 **첫 언급에 링크 + 한 줄 평문 설명 병기**.
  미결 결정은 [[open-questions]]에 5줄 구조(무엇/왜/선택지/재료/풀리는 것)로 유지 — "B-4" 같은
  ID 단독 언급 금지 (사람이 이해 불가 = 고맥락 실패).

- **PowerShell에서 `unity-cli exec` 금지** — C# 문자열 리터럴 따옴표가 깨져 컴파일 에러.
  **Bash 툴 + 작은따옴표**로 실행할 것.
- **임베디드 텍스처·머티리얼 자동 추출 규칙 (2026-07-21 사람 지시)**: 계약 경로(Art/) 모델 임포트 시
  임베디드 텍스처는 `<모델 폴더>/Textures/`로 자동 추출 + URP Lit BaseMap 연결까지 임포터가 한다.
  발주 상태: ArtImportPostprocessor 확장 대기열 (수동 Extract는 과도기 — GS25는 사람이 수동 추출했음).
- **_intake 폴더 보존 규칙 (2026-07-21 사람 지시)**: 반입물 처리 시 **파일만 이동·폐기하고
  폴더 구조(`_intake/art/<도구>/<분류>/`)는 절대 삭제하지 않는다** — 민지가 반복 사용하는 상시
  투입구다. 검역 발주 봉투에 이 규칙을 반드시 명시할 것. (실제 사고: 1차 검역이 빈 폴더까지 청소함)
- ⚠ **CLI로 Play 검증할 땐 `Application.runInBackground = true`를 먼저 켠다.**
  Unity 창이 포커스를 잃으면 Play가 프레임 2에서 얼어붙어 시계·물리가 안 돈다
  (실측: 6초 대기해도 `Time.frameCount=2`, 시계 정지 → 검증이 조용히 거짓 음성).
  켠 뒤 같은 대기에서 `frameCount=1773`, 마감 판정 정상 발화.
  → 상시화하려면 Player Settings의 Run In Background를 켜야 하나 `ProjectSettings/`는
  커밋 금지 영역이라 **사람 판단 필요**.
- 킷의 `logs/`는 Unity의 `Logs/`와 충돌 → 관제 로그는 `planning/` 아래.
