# TASKS.md — 발주 대장 (mode: PRETASK · milestone: M1 뼈대)

> 상태: `todo` → `doing` → `review` → `done` / `blocked(원인)` / `hold(의존)`
> **review→done 전이는 판정 주체만** (기계 기준·reviewer·사람 — HARNESS §6). 작성자 자기보고는 제출일 뿐.
> WIP(doing) ≤ 5.

## M1 뼈대 — 전환 조건: 그레이박스 배송 1건 완주 ✅ + 규격 동결(Q1)

| id | 작업 | 담당 | rigor | eta_min | 상태 | 근거·비고 |
|---|---|---|---|---|---|---|
| M1-01 | 그레이박스 무대 조립 (`Greybox.unity`) | 관제 | core | 5 | **done** | `GreyboxStageBuilder` 호출. 루트 9개 확인 |
| M1-02 | 플레이어 조립 (허브+서브 4종+센서) | 관제 | core | — | **done** | 빌더가 소유. RequireComponent로 자동 부착 확인 |
| M1-06 | **그레이박스 루프 1회 완주 검증** | 관제 | core | 30 | **done** | done=1 · money=+5000 · late=0 · 콘솔 0건 |
| M1-13 | **지각 캐리 상태 누수 수정** | 관제 | core | 30 | **done** | 사람 판정 2026-07-20 · 발주서 [[M1-13]] |
| M1-14 | 사람 조작 검증 (WASD 이동 · E 상호작용 실제 키 입력) | 남규(사람) | core | 15 | **done** | 사람 보고 "실제 키 입력 정상" (2026-07-20) |
| M1-16 | 이벤트 콘솔 로깅 (CODE_RULES §9.5 + WorldEvents 시공) | 관제 | peripheral | 20 | **done** | 사람 판정 2026-07-20 |
| M1-17 | **캐리 비주얼 부재** — 상자를 들면 화면에서 그냥 사라진다 | 미정 | core | ? | todo | 매니페스트에 항목 없음 → **설계 공백, 사람 판단 필요** |
| M1-09 | 카메라 리그 확정 (1920×1080 · 480×270 RT · 정수 4× 업스케일) | 관제 | core | 45 | todo | D-003. 현재 프레이밍 과망원 — 피사체가 작다 |
| M1-11 | CoreBootstrap 자가 Additive 로드 (씬 단독 Play 시 NullRef 방지) | 정수 | core | 30 | todo | Main.unity에서 실제 발생 확인됨 |
| M1-03 | Animator 컨트롤러 + 파라미터 3종 | **CLI**(D-017 이관) | core | 30 | **review** | AC_chr_courier — Speed 1D 블렌드(Walk 2.5/Run 4.5) · 계약 파라미터 3종 · idle 미납품이라 0구간=Walk 대행 · [[INBOX]] D-b |
| M1-04 | FadeScreen UI (CanvasGroup + "늦지마!" 컷인) | 남규 | peripheral | 25 | todo | 코드 있음 |
| M1-05 | 씬 5종 빌드세팅 등록 | 남규 | core | 10 | todo | — |
| M1-07 | ArtImportPostprocessor (폴더 경로 트리거) | **CLI**(D-017 이관) | core | 90→실측 ~11분 | **review** | .obj 관통 테스트 통과 · 상세 [[BOM]] §11 |
| M1-08 | CategoryPrefabFactory (Prefabs/Auto 멱등 생성) | **CLI**(D-017 이관) | core | 90→(M1-07과 한 발주) | **review** | 멱등·Variant 함정 회피 확인 |
| M1-10 | URP 렌더러 Forward+ 못박기 | 남규 | core | 15 | todo | 밤 가로등 라이트 상한 선제 차단 |
| M1-12 | 아키텍처 문서 정합성 개정 8건 | 관제 | peripheral | 40 | todo | D-002·D-003 반영분 |
| M1-15 | Q1 게이트 — 반론 처리 → **ARCH/STYLE/TECH_SPEC 동결** | 사람 | core | 30 | todo | 루프 완주했으므로 사거리 진입 |

## M0 잔여 — M1과 병행 (CONNECTIONS 관통)

| id | 작업 | 담당 | 상태 | 비고 |
|---|---|---|---|---|
| M0-01 | unity-cli 관통 | 관제 | **done** | 4수 통과 · CONNECTIONS #1 ✅ |
| M0-02 | git + GitHub 원격 | 남규 | done | origin/main 존재 |
| M0-03 | **WebGL 빌드 → Pages 배포 → 링크** | 남규 | **done** | https://namkuri.github.io/dontlate-web/ 로드 확인 (2026-07-20) · 폰 확인 권장 |
| M0-04 | Trellis@RunPod 소품 1개 → Blender → 임포트 관통 + 실측 | 민지+남규 | todo | [[BOM]] §15 양산(M2) 발사 전제 · **[[open-questions]] §B-4(캐릭터 도구·리그) 판단 재료** |
| M0-05 | Mixamo 수동 절차서 | 남규 | hold(B-4) | — |
| M0-06 | 오디오 3종 클립 1개 관통 + 라이선스 기록 | 남규 | todo | M3 전제 |
| M0-07 | 훅 4종 (컴파일·freeze-guard·라이선스·태그) | CLI | **review** | [[orders/system]] S-001 · 리드 12분 · exit code 검증 · dest 대조는 GC 몫 이월 |
| M0-08 | 채점기 3 + AAPP 자동화 3 | CLI | **review** | S-001 · **씬 통계가 첫 가동에 GS25 150만 tri 적발**([[INBOX]] H7) · calibration.md 개시 |

## M2 이후 (착수 금지 — Q1 동결 후 개봉)

P3 스크립트 12종 · BOM 작성 · cast 발주(사람 승인 게이트).
