# INBOX.md — 남규 처리함 (사람이 볼 파일은 이거 하나)

> 규칙: 관제가 **사람 손이 필요한 모든 것**을 여기 모아 유지한다 (브리핑 때마다 동기화).
> 처리하면 항목에 답만 주면 됨 — 체크·삭제는 관제가 한다. 배경 해설은 [[open-questions]].
> 갱신: 2026-07-20 (관제).

## ① 판정 대기 (review)

| # | 항목 | 한 줄 설명 | 참조 문서 |
|---|---|---|---|
| R4 | 자동 임포트 ([[BOM]] §11) | 파일을 `Art/Buildings` 등에 넣으면 텍스처·모델 세팅 자동 적용. **닫힘 조건 = 민지 첫 반입 때 자연 검증** (님 답 "Art에 도착하면" 그대로 유지) | 코드: `Assets/Scripts/Editor/Importer/ArtImportPostprocessor.cs` |
| R5 | 프리팹 자동 생성 ([[BOM]] §11) | **R4의 쌍둥이** — R4가 "임포트 세팅"이라면 R5는 그 모델로 **씬에 끌어놓을 수 있는 프리팹을 `Prefabs/Auto/`에 자동 생성**(바닥 콜라이더 포함)까지 해주는 것. 검증 시점도 R4와 같음 → **같이 "Art 도착 시" 취급 권고** | 코드: `Assets/Scripts/Editor/Importer/CategoryPrefabFactory.cs` |
| R6 | 비콘 v2 | 포커스=패드 위 한정 · 패드 크기 인스펙터 조절(`_padSize`) · 배송 상자 물리 드롭 — **다음 플레이 때 감각 확인** | [[BOM]] §2 · [[iterations]] · 발주 이력: [[decisions]] D-024 |
| R7 | District 무대 조립 | 전이 체인 5단→District 배송 완주·슬롯 22개·매니저 0(Core 상주 실증) — Core에서 Play로 직접 확인 가능 | [[socket-map]] · [[BOM]] §13 |

| R9 | 낮밤 비주얼 + 가로등 자동점등·플리커 | 4컷(아침~밤) + 플리커 시계열 실측. **감각값 전부 인스펙터 노출** — `WorldDayNightManager`(_sunColor·_sunIntensity·_ambientColor·_skyExposure 등)·`StreetLampLight`(색·플리커 시간). 밤 하늘 톤 등 튜닝은 님 몫 | [[BOM]] §7 · Screenshots/dn_*.png |

| R10 | 폰트+HUD+태양 교정 | HUD 6요소 이벤트 반응 실측 · 한글 정상(Pretendard, OFL 기록: [[assets_manifest]]) · District 이중광원 0 · **Main(타이틀)에선 HUD 숨김으로 확정(관제 판단 — 뒤집으려면 한마디)** | [[BOM]] §6.5 · Screenshots/hud_*.png |

| R12 | 가로등 8기 열 배치 (프리팹 링크·Visual/Light 분리) | 밤 8기 점등·아침 소등 실측 · **실루엣 판정 부탁** — 스왑은 [[swap-strategy]] 전략 A | Screenshots/lamp_rows_night.png |

| R13 | 밤하늘 패키지 v4 (별밭+블룸+달 토끼) | 반려 3회 반영 완료 · **달 토끼 텍스처 판정 요망** (32×32 코드 그림 — 마음에 안 들면 `Art/Backgrounds/moon_pixel.png`를 직접 그린 그림으로 덮어쓰면 끝, 빌더가 보존) · 달 위치(−15, 4)도 확정 요망 | Screenshots/crop_moon_v4.png |

| R15 | 하네스 도구 10종 (S-001) | 훅 4(컴파일 게이트·freeze-guard·라이선스 대조·태그) 전부 시나리오 테스트 exit code 검증 · 채점기 3 실측 가동 · AAPP 자동화 3 (calibration.md 첫 행 자동 생성) | [[orders/system]] S-001 결과 블록 |

## ② 결정 대기

| # | 질문 | 권고 | 참조 |
|---|---|---|---|
| ~~D-a~~ | **B안(발광판) 구현 완료** — SignGlow.shader + SignGlowPlate.cs(저녁 자동 점등·HDR 색·소프트 폴오프). A안(발광 데칼 SG) 승격은 여유 때 선택 사항으로 격하 | **R11로 이동** | Screenshots/sign_glow_night.png |
| R11 | 간판 발광판 (밤 컷 확인) | 저녁 점등·아침 소등 실측 · 밤 컷에 간판+가로등+빛기둥 공존 | [[BOM]] §2 fx_sign_glow |
| R8 | 비콘 빛기둥 3단 상태 (기본→반투명→소멸) | 스크린샷 3장 + α 실측 1→0.3 — **색·알파·속도 인스펙터 노출됨** (초록 기본, 팔레트 시안 전환은 님 한 클릭) | [[BOM]] §7 fx_beacon_rise |
| D-b | **B-4 확정 제안**: 단일검증 통과로 실측 완료 — "**Tripo 모델 + Mixamo 리깅·애니**"로 확정할까? (Humanoid 아바타 유효 · Walk/Run 리타깃 성공 · 본 구동 실측 · 루프 무회귀) 확정 시 TECH_SPEC rig 항목 그대로 동결 가능 | **승인 대기** | [[open-questions]] §B-4 · [[decisions]] D-030 |
| D-c | **가로등 조사각**: 현재 45°(빛이 폴 앞쪽 지면에 맺힘 — 저녁 컷 참조) vs 정통 가로등처럼 바로 아래(90°) | 스크린샷 보고 취향 선택 | Screenshots/dn_evening.png |

## ③ 손 작업 대기 (CLI가 못 하는 것)

| # | 작업 | 왜 | 참조 |
| H5 | Greybox 씬의 떠돌이 `StreetLampLight` 오브젝트(위치 17.5, 1.1, 0.2 — 폴 없는 맨 스팟라이트) 삭제 여부 | 빌더 산물이 아니라 수동 배치물(아마 님 테스트 잔재) — `__gb_` 접두어가 없어 멱등 정리 대상 밖. 밤에 이것까지 켜져 광원 9개가 됨 (안개 광추도 9번째가 여기 붙음) | 씬=님 독점이라 관제가 안 지움 |
|---|---|---|---|
| H1 | `Assets/Art/Building`→`Buildings` · `Car`→`Props` 정리 | 임포터(R4·R5)가 **복수형 계약 경로만** 감지 | [[BOM]] §11 · [[decisions]] D-002 |
| H2 | **실상표 일괄 제거 (제출 전 필수)**: GS25 건물 + 캐릭터 쿠팡 로고(민지 삭제 예정 — D-029 인지됨) | `docs/INTENT.md` 금지: 실상표(택배사·편의점 명시) — 실격급 | [[BOM]] §9 data_brands |
| H3 | 폰에서 https://namkuri.github.io/dontlate-web/ 열기 (1분) | 타 기기 확인 = 제출 규정 검증 완결 | [[TASKS]] M0-03 |
| H4 | (민지) RunPod Trellis 관통 → 소품 1개 실측 | 양산·캐릭터 결정의 관문 | [[TASKS]] M0-04 · [[open-questions]] §B-4 |
| H7 | 🔴 **(민지) GS25 건물 데시메이트 요청 — 단독 1,499,400 tris** (WebGL 전체 예산 200k의 **7.5배**를 건물 1채가 소비. 씬 통계 채점기가 첫 가동에서 적발) | 이대로 WebGL 빌드하면 로딩·프레임 참사. 목표 <3,000 tris (건물 모듈 상한) — Tripo 재생성 시 폴리 옵션 or Blender 데시메이트 | scene_stats 실측 · [[BOM]] §0 예산표 |

## 처리 완료 (최근)

- ~~H6 Main 씬 `Core` 오브젝트 부활~~ ✅ 사람이 재삭제 (2026-07-21) — "Main→Main" 워닝 소멸 예상, 다음 Play에서 확인

- ~~R1 지각 캐리 수정~~ ✅ ~~R2 이벤트 로깅~~ ✅ ~~R3 Core 씬+빌드세팅~~ ✅ (사람 판정 2026-07-20)
- ~~Main.unity 중복 Core 삭제~~ ✅ · ~~픽셀화 룩~~ ✅ [[decisions]] D-025 · ~~UI-a/b·폰트~~ ✅ D-020~022 · ~~J-1~~ ✅ D-018
