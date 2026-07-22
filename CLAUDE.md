# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트

**늦지마 (Don't Late)** — Unity 6.5 (6000.5.3f1) / URP 3D / 2.5D 배송 게임.
현재 상태 (2026-07-22 갱신): **매니페스트 34종 중 31종 납품 완료** (S-005~007로 Debt·Minigame·LoadingZone·드링크·TravelMapView·리듬뷰 추가 — 잔여는 P4 3종: Audio 완료 제외 Juice·PlayerEffects + ArtAuditReport).
`Assets/Scripts/` 아래 Events·SO·Managers·Player·Interactables·UI·Utils 전부 실재한다 — **새로 만들기 전에 먼저 읽어라.**
미착수는 P3·P4 12종 + 임포터 2종. 남은 작업·블로커는 `docs/plan/remaining-work.md`, 발주 상태는 `planning/TASKS.md`.
Core 씬·플레이어 프리팹은 **아직 미조립**이라 그레이박스 루프는 완주되지 않았다.

## 설계 문서 (자동 로드됨 — 코딩 전 필독)

| 문서 | 내용 |
|---|---|
| `.claude/rules/dont-late-architecture.md` | 왜·구조 (공간모델·카메라·씬 흐름·매니저 층·아트 파이프라인) |
| `.claude/rules/dont-late-scripts-manifest.md` | 무엇을 언제 만들지 (스크립트 34개 전량 + P1~P4 순서) |
| `.claude/rules/dont-late-code-rules.md` | 어떻게 쓸지 (네이밍·통신 2층 규칙·YAGNI·납품 규칙) |

새 스크립트를 만들 때는 매니페스트에 있는 파일인지 먼저 확인한다. 없는 파일을 새로 발명하지 않는다.

## Unity 조작 = `unity-cli` (핵심 워크플로우)

Unity 에디터가 **열려 있어야** 동작한다 (`com.youngwoocho02.unity-cli-connector` 패키지가 에디터 안에서 HTTP 서버를 띄운다). 에디터가 닫혀 있으면 명령이 실패하므로, Director에게 Unity를 켜달라고 요청한다.

```bash
unity-cli status                      # 에디터 상태 (ready / compiling)
unity-cli editor refresh --compile    # 스크립트 재컴파일 후 완료까지 대기  ← 코드 수정 후 필수
unity-cli console --type error,warning        # 콘솔 에러/워닝 읽기 (셀프검증 ②)
unity-cli console --clear             # ⚠ 장시간 Play 관측 전 필수 — 버퍼 상한 차면 최신 로그가 조회 누락(거짓 음성, 2026-07-22 실측)
unity-cli console --lines 20 --stacktrace full
unity-cli editor play --wait          # 플레이모드 진입 후 대기 (셀프검증 ③)
unity-cli editor stop
unity-cli test                        # EditMode 테스트
unity-cli test --mode PlayMode
unity-cli test --filter <namespace|class|full-test-name>   # 단일 테스트
unity-cli exec "return Time.time;"    # 에디터 안에서 C# 즉시 실행
unity-cli screenshot --view game      # 결과 눈으로 확인 (⚠ 오버레이 UI 비포착 — 카메라 렌더만)
# 오버레이 UI 포함 캡처 (S-027 확립): Play 중에 실행 — 프로젝트루트/Screenshots/에 떨어진다.
unity-cli exec 'UnityEngine.ScreenCapture.CaptureScreenshot("Screenshots/x.png"); return "ok";'
```

**셀프 검증 3종**(CODE_RULES §8)의 실제 명령 = `editor refresh --compile` → `console` 0건 → `editor play --wait` + `screenshot`. 이 3종 통과 전에는 push 금지.

## Git 경계 (병합 지옥 방지 — 위반 주의)

- **커밋 가능**: `Assets/**/*.cs`, SO 클래스·에셋, 문서, **프리팹(빌더·팩토리 산출물 — D-061 개정)**, 씬 **`.meta`**, `Assets/Settings/`·`ProjectSettings/`(D-032, 수정은 남규만).
- **커밋 금지**: `.unity` 씬 **본문** — 씬은 빌더가 정본, 각 PC에서 `DontLate/Build ...` 메뉴로 재현(병합 지옥 방지).
- 커밋 메시지 형식: `[P2] PlayerLocomotionManager: Z레인 이동+캐리 페널티 (via ClaudeCode) [self-tested]`
- 현재 브랜치 `feature/jjs`, 메인은 `main`.

## 코드 규칙 요약 (상세는 code-rules 문서)

- 통신 2층: Player 도메인 내부는 `PlayerManager` 허브 경유 직접 참조 / 도메인 경계·World 매니저 간은 `WorldEvents` 정적 이벤트만.
- `FindObjectOfType` · `GameObject.Find` · 태그 검색 **전면 금지**.
- World 싱글톤은 Core 씬 상주 — `DontDestroyOnLoad` 쓰지 않는다.
- 이벤트 구독은 `OnEnable`/`OnDisable` 짝 필수.
- Inspector 노출은 `[SerializeField] private`, public 필드 금지.
- 에디터 전용 코드는 반드시 `Editor/` 폴더 안 (밖에 두면 빌드 깨짐).
- `IInteractable` 시그니처는 동결 — 변경 필요하면 구현하지 말고 사람에게 묻는다.
- YAGNI: 발주서에 없는 기능·추상화·방어코드를 덧붙이지 않는다.

## 세션 분기 — 3모드 (D-055 확정 · 2026-07-22)

- **공장 모드 (정수 · 코드+오디오)**: 위 내용 + `planning/guides/factory-mode.md`가 운영 규칙.
  브랜치→PR로만 반입 (main 직접 push는 pre-push 훅이 차단 — `dontlate.role` 미설정 상태가 곧 공장).
- **아트 모드 (민지 · 생성·반입)**: `planning/guides/art-mode.md`. Unity 불요 — 반입 PR을 열면
  검역 리포트·프리뷰 스크린샷을 관제가 회신. 라이선스 기록 누락 = 반입 차단.
- **관제 모드 (남규 · 디렉터)**: 사람이 "관제 시작" 또는 "PRETASK 모드"를 선언하면 —
  `PRETASK.md`(모드 헌장·이정표 M0~M6) → `PROCESS.md`(상태기계) → `HARNESS.md` → `CONNECTIONS.md` 로드,
  `.claude/agents/orchestrator.md` 역할로 가동. 상태는 `planning/STATUS.md`, 발주는 `planning/TASKS.md`
  (첫 관제 세션이 생성). 시뮬 기록은 `_sim/`(드라이런 — 실산출물 아님).
- **기획 규격 (디렉터 관리 · 동결 문서)**: `docs/INTENT.md`(의도) · `docs/SCOPE.md`(범위) ·
  `docs/STYLE.md`(아트 규격) · `docs/TECH_SPEC.md`(기술 규격) · `docs/JUICE.md`(연출 명세).
  설계 3종(.claude/rules/)과 충돌 시 **INTENT > SCOPE > architecture** 순서가 이긴다.
- 단일 소스: 설계 3종은 `.claude/rules/`가 정본 (docs/에 중복 두지 않는다).
