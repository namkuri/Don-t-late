# BOM.md — 에셋 명세 초안 v0.1 (⚠ 미동결 — Q2 게이트에서 동결)

> 근거: SCOPE v5 · TECH_SPEC 치수표 · 아키텍처 §7(min-BOM push + 카탈로그 pull) · JUICE 표.
> eta_min은 **전부 미실측** — M0-04(Meshy 관통 실측)가 캘리브레이션을 채운 뒤 Q2에서 합산·동결.
> 반입 경로: 민지 산출물 = 사람 수동 → `Assets/_intake/<도구명>/` (D-009) → 검역 → dest 이동.

## A. min-BOM (push — 마감 있음, 코어루프 성립 필수)

| bom_id | 항목 | 규격 (TECH_SPEC) | fab | source | dest | 현 placeholder(소켓) | rigor | 상태 |
|---|---|---|---|---|---|---|---|---|
| chr_courier | 주인공 배달부 | 1.8u · <5000tri · 256px Point | module | generate + rig(**B-4 미결**) | Art/Characters/ | `__gb_Player` 캡슐+코 | core | **hold(B-4)** — LPT 최선두 발사 대상 |
| prop_box_parcel | 택배상자(중) | 0.4~0.75u · <1500tri | module | generate | Art/Props/ | `__gb_Box` 큐브 (캐리 앵커 부착 소켓 겸용) | core | todo |
| bld_door_entry | 현관문 | 2.1u | module | generate | Art/Buildings/ | `__gb_Door` 큐브 | core | todo |
| prop_truck | 트럭 (연출 소품 — 주행 없음) | 길이 6.5u · <3000tri | module | generate **또는 reuse**(무료+라이선스 기록) | Art/Props/ | 없음 — Camp 씬 미조립 | peripheral | todo |
| por_parkmalsoon | 박말순 초상 | Tier H · 2D 일러스트 (픽셀화 제외) | module | generate(이미지 — #4 미검증 / 🖐 폴백) | Art/Portraits/ | DialogueView 미작성(P3) | core | hold(P3 UI) |
| ui_title | 타이틀 로고 "늦지마" | Tier H · 2D | module | generate(이미지) | Art/UI/ | Main 씬 | peripheral | todo |

## B. 카탈로그 (pull — 민지 생산분, 마감 없음, 잘 나오는 대로)

| 카테고리 | 규격 | dest | 소켓 방식 |
|---|---|---|---|
| 건물 모듈 | 층고 3.0u 배수 · <3000tri | Art/Buildings/ | `BuildingSlot` 마커 (M2에 씬 생성) |
| 간판 | 밤 이미시브 **2레이어 규칙 미결(§8-7)** — 결정 전 반입분은 베이스만 | Art/Props/ | `PropSlot` |
| 거리 소품 (가로등 4.0u 등) | <1500tri | Art/Props/ | `PropSlot` |
| 군중/원경 | **빌보드 허용** — source=fake 우선 검토 (없어도 코어루프 성립) | Art/Backgrounds/ | 원경 레이어 |

## C. 오디오 (JUICE 표에서만 도출 — 임의 추가 금지)

| bom_id | 항목 | source | 비고 |
|---|---|---|---|
| bgm_day_loop | BGM 루프 1곡 | Suno(#8) → 폴백 Freesound(#10) | M0-06이 관통 관문 |
| sfx_delivery_ok | 딩동+동전 | #9/#10 | JUICE "배송 완료" |
| sfx_late_buzzer | 낮은 부저 | 〃 | JUICE "시간초과 실패" |
| sfx_pickup | 집는 소리 | 〃 | JUICE "택배 픽업" |
| sfx_footstep | 발소리+숨소리 | 〃 | JUICE "계단 오르기" |
| ~~주차 성공(브레이크+성공음)~~ | — | — | ⚠ **탑다운 주행 폐기 잔재 — JUICE 표 개정 대상** |

## D. AAPP 라우팅 (레인별)

- **module+generate 3D** (A·B 전체) → asset-generator 레인 — 🔴 **CONNECTIONS #2/#3 미검증. M0-04가 개통 관문이자 eta 캘리브레이션.**
- **이미지 2D** (초상·타이틀) → #4 Nano Banana 미검증 / 폴백 🖐 사람 웹 생성 → _intake
- **오디오** → #8~10 미검증 (M0-06)
- **cast 후보: 현재 0건** — District 거리 구획이 후보로 떠오르면 사람 승인 게이트 선행 (조기 발주 금지)
- **fake/reuse 우선**: 군중=fake · 트럭=reuse 검토 — 코어루프 밖 generate는 낭비 신호

## E. sacrifice 후보 (Q2에서 3개 확정 — SCOPE 제안 승계)

① 리듬 미니게임 다단계 → 1단계 ② 집 씬 → 인트로 흡수 ③ 간판 밤 이미시브 B세트

## 동결 전 해소 필요 (Q2 선행 조건)

1. **B-4** — chr_courier가 여기 물려 있음 (민지 상의 예정 · M0-04 실측이 재료)
2. **M0-04** — 전 generate 항목의 eta 근거
3. **§8-7 간판 이미시브 규칙** — 카탈로그 간판 반입 전 필요
4. JUICE 표 개정 (주차 행 삭제 — 동결 문서 아님, 관제가 개정안 준비)
