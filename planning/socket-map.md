# socket-map.md — 소켓·좌표 정본 (bom_id ↔ placeholder ↔ 좌표 ↔ dest)

> **좌표계**: X=진행(우+) · Y=수직 · Z=깊이(+Z=길 안쪽) · 1u=1m ([[BOM]]·TECH_SPEC 앵커).
> **정본 규칙**: 씬은 커밋 안 하므로 좌표의 정본은 **빌더 코드**다. 이 문서는 빌더에서 추출한
> 사본이며, 씬 발주가 완료될 때마다 관제가 **씬 실물 덤프(exec)로 재추출·갱신**한다 (수기 편집 금지 —
> 드리프트 방지). 아트 스왑 시 좌표는 여기서 조회한다 — 배치는 조회지 고민이 아니다.

## Greybox.unity (정본: `GreyboxStageBuilder.cs` · 루프 완주 실측 좌표 ✅)

| placeholder | 좌표 (x, y, z) | 크기/비고 | 스왑 대상 bom_id | dest |
|---|---|---|---|---|
| `__gb_Player` | (0, 0.1, 0) | CharacterController h=1.8u r=0.35 · 초기 회전 Y+90° | chr_courier | Art/Characters/ |
| `__gb_CarryAnchor` | 플레이어 자식 (0, 1.05, 0.45) | 든 상자 부착점 — 상자 스케일 검증 기준 | (앵커 — 스왑 없음) | — |
| `__gb_Box` | (-5, 0.4, 0) | 0.8×0.8×0.8 · 트리거 | prop_box_parcel | Art/Props/ |
| `__gb_Door` | (6, 1, 2.6) | 1×2×0.2 · **시각물** (콜라이더 없음, D-024) | bld_door_entry | Art/Buildings/ |
| `__gb_Beacon` | (6, 0, 0) | 패드 `_padSize`=1×1 (비주얼 1×0.06×1) · 트리거 1.2×2×1.2 | prop_beacon (build — 완성) | — |
| `__gb_Ground` | (0, 0, 0) | Plane 스케일 12×8 (=120×80u) | env_road_set | Art/Backgrounds/ |
| `__gb_Lane` | (0, 0.02, 0) | 40×0.04×6 — 보도 띠 | env_road_set | 〃 |
| `__gb_Walkable` | (0, 0, 0) | 트리거 40×4×6 center(0,2,0) — **Z 이동 허용 구간의 정의** | (규칙 볼륨 — 스왑 없음) | — |
| `__gb_StreetLamp_01~05` | 앞줄 Z=−2.4 · X = −16/−8/0/8/16 | StreetLamp.prefab 인스턴스(링크 Connected) — 스왑=[[swap-strategy]] 전략 A | fx_streetlamp_light | Prefabs/Hand |
| `__gb_StreetLamp_06~08` | 뒷줄 Z=+2.4 · X = −12/−4/12 (문 X=6 회피) · yaw 180° | 〃 | 〃 | 〃 |
| Main Camera | (0, 8.1, −40.4) | FOV 22° · pitch 10° — [[TASKS]] M1-09 확정 전 잠정 | scn_camera_rig | — |
| `__gb_Managers` | (0, 0, 0) | ⚠ Greybox 단독 테스트 전용 — District에는 없음(Core 상주) | — | — |

## District.unity (✅ 현행화 2026-07-29 — RT-20260729-01 · 정본: `DistrictSceneBuilder.cs`)

| 소켓 | 좌표/규칙 | 스왑 대상 | dest |
|---|---|---|---|
| `slot_building_*` | X −44~44 간격 8u · Z=2.6 — **|X|<5 스킵**(S-074 도로 자리, 실 슬롯 10칸) | bld_module_* | Art/Buildings/ |
| `slot_prop_01`~`10` | X −36~36 간격 8u · Z=−2.6 보도변 | prop_street_* | Art/Props/ |
| `CrossRoad` | (0, 0.01, 0) · 4.2×20 — 교차 도로 (차 z주행·방향별 차선 x=±1.05) | env_road_set | Art/Backgrounds/ |
| `Crosswalk` | x −1.6~1.6 5줄(0.42×5.6·z길쭉 — S-079 90°) y=0.045 | env_road_set | 〃 |
| `TrafficLight` | (2.6, 0, −3.4) · 3색 등(적 3.72/황 3.35/녹 2.98) | prop_traffic_light | Art/Props/ |
| `SpawnedBeacon_*` | X ∈ BEACON_SLOTS_X {−8,8,16,−16,24,−24,32,−32} · Z=0 (dayOrders 인덱스 순 — 도로 회피) | prop_beacon | — |
| `SpawnedBox_*` | 기본 (−16+i×1.2, 0, −1.2) · droppedCargo/placed 위치 우선 | prop_box_parcel | Art/Props/ |
| `EdgeGate_Prev/Next` | (∓19, 0, 0) — 도보 게이트 | (규칙 볼륨) | — |
| `Walker_A/B/C` | (−8,0,2.2)/(6,0,2.6)/(18,0,2.0) — 신호 준수 행인 (npcId walker_a~c) | chr_npc_* | Art/Characters/ |
| `ErrandGranny` | (12,0,−1.8)→(−6,0,−1.8) 심부름 왕복 | chr_npc_granny | 〃 |
| `__gb_Player` 스폰 | (−6, 0.1, 0) — S-074 도로 회피 이설 (엣지 도착 시 게이트 앞 ±2.5) | chr_courier | Art/Characters/ |

## Camp.unity (✅ 신규 기록 2026-07-29 — 정본: `CampStageBuilder.cs`)

| 소켓 | 좌표 | 스왑 대상 | dest |
|---|---|---|---|
| `Truck` | (9, 0, 1.8) — Cargo/Cab/Wheel 파츠 + LoadingZone 트리거(−0.8,1,−1.8) | veh_truck (통짜 예정) | Art/Props/ |
| `Truck/DepartPoint` | 루트+( 3.4, 0.6, −0.6) 트리거 — hasTruck 시 E→Travel (Cab 비의존 — 통짜 대비) | — | — |
| `CampBox_01~04` | (−7+i%2×0.9, i/2×0.72, 1.5) 피라미드 — 하루 물량(dayOrders) | prop_box_parcel | Art/Props/ |
| `EdgeGate_Next/Home` | (14,0,0)/(−14,0,0) | (규칙 볼륨) | — |
| `Walker_A/B` | (−9,0,2.4)/(4,0,2.8) — npcId camp_walker_a/b | chr_npc_* | Art/Characters/ |

## Camp.unity (⬜ 예정 좌표 — 씬 빌더 발주 스펙, 조립 후 실측 갱신)

| 소켓 | 예정 좌표 | 크기/비고 | 스왑 대상 bom_id | dest |
|---|---|---|---|---|
| `slot_truck` | (0, 0, 2.5) | 길이 6.5u X축 정렬 — 창고 앞 정차 (연출 소품) | prop_truck | Art/Props/ |
| `slot_warehouse` | (0, 0, 4.5) | 배경 벽면 — 개방 단면(사이드뷰) | bld_warehouse_shell | Art/Buildings/ |
| `__loading_zone` | (−4, 0, 0.5) | 트럭 후미 · 트리거 2×2×2 — LoadingZone(P3) 부착점 | (build) | — |
| `slot_cargo_01`~`02` | (−7, 0, 2) · (6, 0, 2.2) | 짐더미 | prop_cargo_pile | Art/Props/ |
| `__settle_point` | (8, 0, 0) | 정산 트리거(빚 게이지 UI 호출) — Debt(P3)와 짝 | (build) | — |
| Player 스폰 | (−10, 0.1, 0) | Greybox 플레이어와 동일 규격 | chr_courier | — |
| 카메라 | (0, 8.1, −40.4) FOV 22 | 표준 리그 재사용 | scn_camera_rig | — |

## Home.unity (⬜ 예정 좌표 — sacrifice ② 후보라 최소 · 흡수 시 이 소켓들은 Main으로 이관)

| 소켓 | 예정 좌표 | 스왑 대상 | dest |
|---|---|---|---|
| `slot_room` | (0, 0, 0) | bld_room_shell (내부 6×4u) | Art/Buildings/ |
| `slot_bed` | (−2, 0, 1.5) | prop_bed | Art/Props/ |
| `slot_phone` | (1.5, 0.8, 1.5) | prop_phone (박말순 전화 연출 앵커) | Art/Props/ |
| Player 스폰 | (0, 0.1, 0) | — | — |

## Main.unity · Travel.unity — 3D 좌표계 없음 (의도적)

- Main: 타이틀 로고·시작 버튼 = UI 캔버스([[BOM]] §6.5). 배경은 District 카메라 재사용 검토.
- Travel: 순수 UI 씬 (지도·노드 버튼) — 3D 배치 없음.

## Core.unity (정본: `CoreSceneBuilder.cs`)

| 오브젝트 | 비고 |
|---|---|
| Managers (SceneFlow·Delivery·Deadline·DayNight) | 위치 무의미 (로직 전용) |
| FadeCanvas (sortOrder 100) | Screen Space-Overlay — 좌표계 밖 |
| (예정) HUDCanvas 10 · DialogueCanvas 90 · MinigameCanvas 80 | [[BOM]] §6.5 — 폰트+HUD 발주에서 |

## 사람 폴리싱과의 동기화 규칙

님이 에디터에서 소켓·오브젝트를 옮기면(폴리싱) 씬과 빌더가 어긋난다 — **옮긴 뒤 관제에 한마디**
주면 씬 실물을 덤프해 빌더·이 문서에 역반영한다 (빌더가 정본이므로, 반영 안 하면 다음 재조립 때
님 배치가 날아간다 — 이게 이 규칙이 존재하는 이유).

## 갱신 이력
- 2026-07-20 초판 — Greybox 빌더 추출(실측) · District 씬 덤프(실측) · Camp/Home 예정 좌표 추가(발주 스펙).
