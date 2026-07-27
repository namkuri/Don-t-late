# toolbox.md — 도구 인벤토리 (세션 기동 시 1회 조회)

> 회고 2·3차 백로그 이행 (2026-07-28 신설). **규칙: 관제 세션은 기동 후 첫 작업 전에 이 문서를 1회 조회**해
> "이미 있는 도구를 다시 만들거나 잊는" §4-4류 망각을 막는다. 새 도구를 만들면 여기 1줄 추가.

## 스킬 (.claude/skills/ — Skill 도구로 호출)
| 스킬 | 용도 |
|---|---|
| /order | 발주 접수: 대장 append(MDA 판정)+[발주] 커밋 선행 (D-060/070) |
| /deliver | 납품 마감: 셀프검증→기록→커밋→캡처 발신 (함정 4종 내장) |
| /pr-check | PR 검수·반입·브랜치 삭제 닫기 (gh 없이 git만) |
| /midpoint-review | 하네스 중간점검 감사 (L0~L4 매트릭스·백로그 델타) |

## git 훅 (hooks/ — core.hooksPath)
| 훅 | 기능 |
|---|---|
| pre-commit | ① freeze-guard(frozen 문서 수정 차단) ② 라이선스 대조(manifest 미등재 바이너리 차단) ③ .cs 컴파일 게이트(**Play 중이면 행잉** — 정지 후 커밋) |
| post-commit | orders/INBOX diff → 디스코드 📦✅🔔 자동 알림 (webhook 설정 PC만) |
| pre-push | main 직push 차단(공장 모드 — dontlate.role 미설정 시) |

## 스크립트 (scripts/)
| 스크립트 | 용도 |
|---|---|
| discord_notify.py "msg" --file x.png | #클로드 발신 (--file은 **1개만** — 여러 장이면 반복 호출) |
| leadtime_report.py | 발주→납품 리드 집계 |
| new_order.py | 발주 봉투 생성 보조 |
| palette_check.py / scene_stats.py / screenshot_bundle.py | 아트 검역·씬 통계·캡처 묶음 |
| audio/ (prompt_builder 등) | 정수님 오디오 파이프라인 (관제는 읽기만) |

## unity-cli 요체 (상세는 CLAUDE.md)
- 함정: exec 안 for/foreach=행잉 · Play 중 compile/test 불가 · 트랜지언트 "no Unity instances"=재시도.
- 오버레이 캡처: `exec 'UnityEngine.ScreenCapture.CaptureScreenshot("Screenshots/x.png"); return "ok";'` (Play 중).

## 빌더 메뉴 (DontLate/Build/)
★ All Scenes(전체) · Core Scene · Camp/Home/District/Apartment/Hillside Stage · Scene Flow UI ·
Art Test Scene · **Art Test WebGL Build**(→ gh-pages /art-test/).

## 배포
- 본게임: WebGL 빌드(Builds/WebGL) → gh-pages **루트** push → https://namkuri.github.io/Don-t-late/
- 아트테스트: Builds/ArtTestWebGL → gh-pages **/art-test/** → 같은 도메인 하위 경로.
- gh-pages 워크트리: scratchpad에 `git worktree add <dir> gh-pages` (삭제 가드 — 정리는 사람 승인).

## 문서 지도 (정본 위치)
규칙(자동 로드)=CLAUDE.md+.claude/rules 3종 · 결정=planning/decisions.md · 발주=planning/orders/* ·
재미 가설=docs/MDA.md · 밸런스=docs/BALANCE.md · 루프 지도=docs/LOOP.md · 회고=planning/retrospective-*.
