# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트

**늦지마 (Don't Late)** — Unity 6.5 (6000.5.3f1) / URP 3D / 2.5D 배송 게임.
현재 상태: **URP 3D 템플릿 그대로.** `Assets/`에는 `Scenes/SampleScene.unity`, `Settings/`(URP 에셋), `TutorialInfo/`(템플릿 Readme)뿐이고 게임 스크립트는 아직 0개다.
즉 `.claude/rules/`의 설계 문서는 **앞으로 만들 것**을 기술한 것이지 현재 코드가 아니다. 문서에 나오는 파일을 찾지 말고, 만들 때 그 규격대로 만든다.

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
unity-cli console --lines 20 --stacktrace full
unity-cli editor play --wait          # 플레이모드 진입 후 대기 (셀프검증 ③)
unity-cli editor stop
unity-cli test                        # EditMode 테스트
unity-cli test --mode PlayMode
unity-cli test --filter <namespace|class|full-test-name>   # 단일 테스트
unity-cli exec "return Time.time;"    # 에디터 안에서 C# 즉시 실행
unity-cli screenshot --view game      # 결과 눈으로 확인
```

**셀프 검증 3종**(CODE_RULES §8)의 실제 명령 = `editor refresh --compile` → `console` 0건 → `editor play --wait` + `screenshot`. 이 3종 통과 전에는 push 금지.

## Git 경계 (병합 지옥 방지 — 위반 주의)

- **커밋 가능**: `Assets/**/*.cs`, SO 클래스, 문서.
- **커밋 금지**: `.unity` 씬, `.prefab` 프리팹, `ProjectSettings/` — 씬·프리팹은 남규 독점. 컴포넌트 부착이 필요하면 코드만 납품하고 "어느 프리팹에 뭘 붙일지"를 보고에 적는다.
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
