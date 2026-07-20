# CONNECTIONS.md — 외부 연결 대기판

> 외부 AI 도구·서비스 연결의 **단일 진실.** 모든 상태는 `미검증`으로 시작한다 —
> 준비 기간에 1회 관통 검증(+속도 측정)으로 `연결됨`으로 올리는 것이 레일 구축의 핵심.
> 에이전트는 여기 없는 도구를 임의로 쓰지 않는다. 미연결 발견 시 CONNECT_REQUEST(HARNESS.md §5).

## 상태: 연결됨 ✅ | 미검증 ⬜ | 사람매개 🖐 (API 없음, 사람 손 필요) | 불가 ❌

| # | 연결 | 용도 | 파이프라인 | 상태 | 연결 방법 (사람이 할 일) | 폴백 |
|---|---|---|---|---|---|---|
| 1 | **unity-cli** (youngwoocho02) | 씬·컴파일·임포트·스왑·빌드 | logic-unity, build-deploy | ✅ | 완료 — CLI + 커넥터 0.3.22, 6000.5.3f1 ready (2026-07-20) | **없음 — 치명** |
| 2 | **Meshy** (API/MCP) | 3D 소품·캐릭터 생성 | asset-3d, character-anim | ⬜ | API 키 발급 → MCP/스크립트 등록 | #3 Trellis |
| 3 | **Trellis** (RunPod) | 3D 생성 **1순위로 승격** | asset-3d | 🔄 셋업중(민지 · 2026-07-20~) | RunPod 인스턴스 → 생성 1회 관통 + 시간 실측 | #2 Meshy |
| 4 | **Nano Banana** (이미지 생성) | 컨셉 원화 (방향 고정용 1장) | asset-3d §0 | ⬜ | API 키 또는 웹 UI | 🖐 사람이 웹에서 수동 생성 |
| 5 | **Blender headless** (bpy CLI) | 후처리: 폴리 축소·원점 교정·익스포트 | asset-3d | ⬜ | blender 설치 + CLI 경로 확인 | 🖐 사람 수동 Blender |
| 6 | **Mixamo** | 자동리깅·애니 리타깃 | character-anim | 🖐 | API 없음 — 웹 업로드/다운로드 절차서 준비 | 애니 세트 축소·정적 캐릭터 |
| 7 | **Cascadeur** | 애니 보정 | character-anim | 🖐 | 데스크톱 앱 설치 | Mixamo 프리셋 그대로 사용 |
| 8 | **Suno** | BGM 루프 1곡 | audio | ⬜ | 웹/API | #10 Freesound 루프 |
| 9 | **ElevenLabs** | SFX·보이스 | audio | ⬜ | API 키 | #10 Freesound |
| 10 | **Freesound** | SFX 라이브러리 | audio | ⬜ | 웹/API (라이선스 확인 필수) | 무음 + 최소 신디 |
| 11 | **git + GitHub** | 소스·커밋 증거(제출 규정) | build-deploy | ✅ | 완료 — origin/main + `namkuri/dontlate-web` (2026-07-20) | 로컬 git만 |
| 12 | **GitHub Pages** | WebGL 배포·공개 링크 | build-deploy | ✅ | 완료 — https://namkuri.github.io/dontlate-web/ 로드 확인 (Brotli+fallback) | 대안 정적 호스팅 |
| 13 | 스키마 검증 스크립트 | 데이터 검증 (내장) | content-data | ⬜ | scripts/validate_schema.py 준비기간 제작 | 수동 JSON 검토(느림) |

## 준비 기간 검증 의무 (레일 체크리스트에 편입)
- [x] #1 unity-cli: 관통 완료 (2026-07-20) — `status` ready · `exec`→"6000.5.3f1" · `console` 정상.
      **아키텍처 §8-5 "6.5 호환 리스크" 종료.** 미소화 1수: `screenshot`(씬 조립 후 의미 생김)
- [x] #11+#12: 관통 완료 (2026-07-20) — Web 모듈 설치 → Main 씬 빌드(Brotli+Decompression Fallback)
      → `dontlate-web` 레포 → Pages 로드 확인. 잔여 1수: **폰 등 타 기기에서 열림 확인** 권장
- [ ] #2/#3: 소품 1개 생성→#5 후처리→#1 임포트 전 구간 관통 + 소요시간 기록(eta_min 근거)
- [ ] #6: Mixamo 수동 절차를 절차서로 문서화 (본선에서 헤매지 않게)
- [ ] #8~10: 클립 1개 확보→라이선스 기록→임포트 관통
- [ ] 각 관통 결과를 skill-accumulation 트래젝토리에 기록 (속도·성공률 = 모델/도구 캘리브레이션)
- [ ] **HARNESS §8 훅 5종 제작·테스트** (pre-commit 컴파일 · freeze-guard · 라이선스 대조 · dest 대조 · 커밋태그) — 본선 중 신축 금지이므로 반드시 준비기간에
- [ ] **채점기 3종 제작** (HARNESS §8 하단: 팔레트 히스토그램 · 스크린샷 번들 · 씬 통계) — 사람 판정 예산을 아끼는 레일

## 도구 교체 공지 — #1 Unity MCP → unity-cli (youngwoocho02/unity-cli, MIT, v0.3.22 핀)
- 설치: CLI 바이너리(install.ps1) + Unity 패키지 `...unity-cli.git?path=unity-connector#v0.3.22` + Interaction Mode=No Throttling
- 관통 4수: `status` → `exec "return Application.unityVersion;"` → `console --type error` → `screenshot`
- **6000.5.3f1 호환은 미검증 — 이 관통이 판정.** 실패 시 폴백: 버전 다운핀 시도 → exec 불가 시 에디터 수동+파일 기반으로 강등
- 상세: docs/UNITY_CLI.md
