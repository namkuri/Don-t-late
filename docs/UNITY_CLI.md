# unity-cli 운용 가이드 — Unity MCP 대체
## youngwoocho02/unity-cli (MIT · v0.3.22 핀 고정) — Claude Code의 유니티 손

> **정체**: 단일 Go 바이너리 + Unity 패키지(커넥터). 에디터가 열리면 커넥터가 로컬 HTTP
> 리스너를 자동 기동(설정 0) → CLI가 명령 전송. **셸을 쓸 수 있는 모든 에이전트가 즉시 사용**
> — MCP 등록·서버·프로토콜 계층 소멸. 컴파일·도메인 리로드 중엔 CLI가 자동 대기.

## 1. 왜 채택 (MCP 대비)
- 의존성 0 (Python·릴레이·설정 파일 없음) — CONNECTIONS 최대 리스크였던 관통이 3분 일로
- `reserialize` — YAML 텍스트 편집을 유니티 시리얼라이저로 재직렬화 → **텍스트 기반 씬·프리팹 편집이 안전해짐**
- `console`·`test`·`play --wait` — 셀프 검증 3종이 전부 명령어로 기계화
- `screenshot` — 채점기(팔레트 검사)·체크포인트 번들의 입력 공급
- 리스크: 개인 프로젝트(버스팩터) · **6000.5.3f1 호환 미검증 → M0 관통이 판정** · 버전 핀 필수

## 2. 설치 (M0 절차)
```bash
# ① CLI (Windows PowerShell)
irm https://raw.githubusercontent.com/youngwoocho02/unity-cli/master/install.ps1 | iex
# ② Unity 패키지 — Packages/manifest.json (버전 핀)
"com.youngwoocho02.unity-cli-connector":
  "https://github.com/youngwoocho02/unity-cli.git?path=unity-connector#v0.3.22"
# ③ 에디터 설정: Edit→Preferences→General→Interaction Mode = No Throttling (백그라운드 응답성)
# ④ 관통 검증
unity-cli status                                  # 연결·프로젝트·버전 확인
unity-cli exec "return Application.unityVersion;" # exec 왕복
unity-cli console --type error                    # 콘솔 읽기
unity-cli screenshot                              # 캡처 1장
```

## 3. 핵심 명령 (우리 용도 매핑)
| 명령 | 우리 용도 |
|---|---|
| `status` | 연결 상태 — 모든 작업 전 기동점검 |
| `exec "C#"` (stdin 파이프 가능) | **씬 셋팅·오브젝트 생성·배치·컴포넌트 부착·에셋 생성** — 만능 통로 |
| `editor play --wait` / `stop` | Play 검증 진입·종료 |
| `console --type error,warning` | 검증 ②콘솔 0 확인 · 에러 원문 수집(요약 금지 규칙) |
| `test --mode EditMode/PlayMode` | 회귀 테스트 (있는 경우) |
| `reserialize <paths>` | **YAML 텍스트 편집 후 의무 실행** — 재직렬화로 유효성 보장 |
| `screenshot` | 검수·채점기 입력 (씬/게임뷰 PNG) |
| `menu "File/Save Project"` | 저장 등 메뉴 실행 |
| `editor refresh --compile` | 임포트·재컴파일 트리거 |

## 4. 워크플로 장면 5 (Claude Code가 쓰는 법)
```bash
# ① 씬 셋팅 (소켓 배치·볼륨 생성) — exec 우선 경로
echo 'var v=new GameObject("WalkableVolume_A"); v.AddComponent<BoxCollider>().isTrigger=true;
v.transform.position=new Vector3(12,0,2); v.transform.localScale=new Vector3(20,1,4);
UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty(); return v.name;' | unity-cli exec

# ② 대량·구조적 씬 편집 — YAML 텍스트 편집 + 재직렬화 (reserialize 없이 커밋 금지)
#    (파일 편집) → unity-cli reserialize Assets/Scenes/District.unity

# ③ 구현 후 검증 루프 (unity-dev·정수 공통)
unity-cli editor refresh --compile && unity-cli console --type error   # ①컴파일 ②콘솔
unity-cli editor play --wait && unity-cli console --lines 30 && unity-cli editor stop  # ③Play 관찰

# ④ 검수·채점기 공급 (남규)
unity-cli screenshot   # → ArtAuditReport(팔레트·치수) 입력 / 체크포인트 번들

# ⑤ 프리팹 팩토리 CLI 노출 — Importer를 [UnityCliTool]로 감싸면:
unity-cli make_prefabs --params '{"folder":"Art/Buildings"}'
```

## 5. 사용 규칙 (하네스 연동)
1. **경로 우선순위**: 씬·에셋 조작은 ⓐ exec(에디터 API) 우선 → ⓑ YAML 편집은 **reserialize 동반 시에만** 허용. reserialize 없는 YAML 편집 = 여전히 금지(무효 에셋 위험).
2. **exec 산출물 규율**: 씬을 바꾸는 exec 후엔 저장(menu "File/Save Project") + 결과 관찰 보고. 씬 커밋은 여전히 **남규만**(사람 협업 규칙 불변 — 정수의 .cs 자유도 불변).
3. **에러 원문**: console 출력은 요약하지 말고 원문 전달 (HARNESS 규칙 유지).
4. exec 기본이 async/코루틴 차단 — 지연 동작 검증은 play --wait + console 관찰로.
5. 커스텀 툴 이름 충돌 주의(첫 발견만 등록) — 우리 툴은 `dl_` 접두(dl_make_prefabs 등).
