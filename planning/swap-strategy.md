# swap-strategy.md — Placeholder 교체 전략 (아트가 도착하면 어떻게 갈아끼우나)

> 좌표는 [[socket-map]] · 발주 목록은 [[BOM]] · 임포트 자동화는 ArtImportPostprocessor/CategoryPrefabFactory.
> 원칙: **참조가 어디에 걸려 있느냐**가 전략을 결정한다. 전략은 5종뿐이다.

## 전략 5종

| 전략 | 방법 | 언제 쓰나 | 전파 범위 |
|---|---|---|---|
| **A. 프리팹 Visual 교체** | `Prefabs/Hand/X.prefab` 열고 `Visual` 자식 서브트리만 새 메시로 교체. `Light`·스크립트 자식은 안 건드림 | 반복 배치물 (가로등 등) — 프리팹 링크로 배치돼 있어야 함 | **전 인스턴스 한 번에** |
| **B. 파일명=bom_id 덮어쓰기 (자동)** | 민지 산출물을 `Art/<dest>/<bom_id>.glb`로 넣으면 임포터가 세팅 적용 + `Prefabs/Auto/<bom_id>.prefab` 자동 생성/갱신 | min-BOM 단품 (상자·문·트럭) — 재생성도 같은 이름 덮어쓰기 | Auto 프리팹 참조처 전부 |
| **C. 슬롯 수동 배치 (pull)** | 씬의 `slot_building_01~12`·`slot_prop_*` 마커 위치에 Auto 프리팹을 에디터에서 드래그 | 카탈로그 물량 (건물·잡소품) — "도착한 아무 건물이나 꽂히는" 느슨한 소켓 | 배치한 것만 |
| **D. 빌더 함수 교체** | 빌더(GreyboxStageBuilder 등)의 해당 Build 함수에서 프리미티브 생성부를 프리팹 로드로 교체 → 메뉴 재실행 | 빌더가 직접 만드는 코드 소켓 (플레이어 캡슐·비콘 패드) | 재조립되는 모든 씬 |
| **E. 수제 프리팹 교체** | 사람이 리깅·조립한 프리팹을 직접 편집 | 캐릭터 (Auto 팩토리 제외 대상 — 아키텍처 §5.7) | 해당 프리팹 |

## Placeholder별 지정 전략 (실물 기준)

| placeholder ([[socket-map]]) | 현재 실물 | 교체 전략 | 갈아끼울 때 할 일 |
|---|---|---|---|
| `__gb_StreetLamp_01~08` | StreetLamp.prefab (폴+헤드 프리미티브) | **A** | 프리팹의 Visual만 교체 — 8개 전파. 조명·플리커 무사 |
| `__gb_Box` | 주황 큐브 + PickupBox | **B → D** | `Art/Props/prop_box_parcel.glb` 반입 → Auto 프리팹 생성 → 빌더 BuildPickupBox의 큐브 생성부를 프리팹 로드로 1회 교체 |
| `__gb_Door` (시각물) | 갈색 큐브 | **B → D** | 동일. ⚠ 힌지 엣지 피벗 검수 필수(코드 트윈용 — [[BOM]] §2) |
| `__gb_Beacon` 패드 | 발광 큐브 (build 완성품) | 교체 없음 | 아트 스킨 원하면 D |
| `__gb_BeaconFx` · 간판 발광판 | 셰이더 쿼드 (완성품) | 교체 없음 | 색·강도만 인스펙터 |
| `__gb_Player` | 캡슐+코 | **E** | 리깅 프리팹(수제) 완성 → 빌더 BuildPlayer 교체 + Animator 연결 (B-4 후) |
| `slot_building_01~12` | 빈 마커 | **C** | 민지 건물 Auto 프리팹을 마커 위치에 드래그. 간판 발광판을 간판 위에 얹고 색 지정 |
| `slot_prop_01~10` | 빈 마커 | **C** | 잡소품 동일 |
| Camp/Home 소켓 (예정) | 미조립 | 씬 빌더 발주 시 A~D 배정 | [[socket-map]] 예정 좌표 참조 |

## 규칙 2줄

1. **프리팹 링크를 끊지 마라** — 씬에서 인스턴스를 "Unpack"하면 A 전략(전파)이 죽는다.
2. 사람이 씬에서 소켓을 옮기면(폴리싱) **관제에 한마디** — 빌더에 역반영 안 하면 재조립 때 배치가 날아간다 ([[socket-map]] 동기화 규칙).
