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

## ② 결정 대기

| # | 질문 | 권고 | 참조 |
|---|---|---|---|
| D-a | **간판 발광 구현 선택** — PoC 결과: 데칼이 박스 영역 마스킹은 완벽하나 **URP 기본 데칼 셰이더엔 발광(Emission)이 없음**. A) 님이 Shader Graph로 발광 데칼 15분 제작(표면 정합 최상, 가이드 제공) / B) **CLI가 발광 판때기(additive 쿼드) 제작** — 빛기둥 셰이더 기법 재사용, 오늘 가능 (권고: **B로 먼저, A는 여유 때 승격**) | 선택 대기 | [[BOM]] §2 fx_sign_glow · Screenshots/fx_sign_poc.png |
| R8 | 비콘 빛기둥 3단 상태 (기본→반투명→소멸) | 스크린샷 3장 + α 실측 1→0.3 — **색·알파·속도 인스펙터 노출됨** (초록 기본, 팔레트 시안 전환은 님 한 클릭) | [[BOM]] §7 fx_beacon_rise |
| D-b | B-4 캐릭터 도구·리그 | **지금 할 일 아님** — RunPod 관통 후 | [[open-questions]] §B-4 |
| D-c | **가로등 조사각**: 현재 45°(빛이 폴 앞쪽 지면에 맺힘 — 저녁 컷 참조) vs 정통 가로등처럼 바로 아래(90°) | 스크린샷 보고 취향 선택 | Screenshots/dn_evening.png |

## ③ 손 작업 대기 (CLI가 못 하는 것)

| # | 작업 | 왜 | 참조 |
|---|---|---|---|
| H1 | `Assets/Art/Building`→`Buildings` · `Car`→`Props` 정리 | 임포터(R4·R5)가 **복수형 계약 경로만** 감지 | [[BOM]] §11 · [[decisions]] D-002 |
| H2 | GS25 모델 → 가상 브랜드 교체 (제출 전 필수) | `docs/INTENT.md` 금지: 실상표 — 실격급 | [[BOM]] §9 data_brands |
| H3 | 폰에서 https://namkuri.github.io/dontlate-web/ 열기 (1분) | 타 기기 확인 = 제출 규정 검증 완결 | [[TASKS]] M0-03 |
| H4 | (민지) RunPod Trellis 관통 → 소품 1개 실측 | 양산·캐릭터 결정의 관문 | [[TASKS]] M0-04 · [[open-questions]] §B-4 |

## 처리 완료 (최근)

- ~~R1 지각 캐리 수정~~ ✅ ~~R2 이벤트 로깅~~ ✅ ~~R3 Core 씬+빌드세팅~~ ✅ (사람 판정 2026-07-20)
- ~~Main.unity 중복 Core 삭제~~ ✅ · ~~픽셀화 룩~~ ✅ [[decisions]] D-025 · ~~UI-a/b·폰트~~ ✅ D-020~022 · ~~J-1~~ ✅ D-018
