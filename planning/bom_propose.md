# A-008 수동 아트 반입 제안

- 작성일: 2026-07-29
- 반입자: 민지
- 상태: `_intake` 검역 요청
- 총 반입: 251파일
- 계약 경로: `Assets/_intake/art/<생성 도구>/<분류>/`

> 이 문서는 카탈로그성 자유 물량을 포함한 반입 제안서다. 발주품은 파일명이
> `bom_id`와 정확히 일치할 때만 자동 장착 대상으로 본다. 이름만 비슷한 파일은
> 관제가 검역 후 BOM에 매핑하거나 리네임한다.

## 반입 요약

| 생성 도구 | 분류 | 파일 수 | 투입 디렉터리 | 제안 용도 |
|---|---:|---:|---|---|
| Trellis2 | Buildings | 46 | `Assets/_intake/art/Trellis2/Buildings/` | 건물 카탈로그 |
| Trellis2 | Props | 36 | `Assets/_intake/art/Trellis2/Props/` | 소품·가구·차량 카탈로그 |
| Qwen | Buildings | 57 | `Assets/_intake/art/Qwen/Buildings/` | 건물 참조·텍스처 이미지 |
| Qwen | Props | 48 | `Assets/_intake/art/Qwen/Props/` | 소품 참조·텍스처 이미지 |
| ChatGPT | UI | 41 | `Assets/_intake/art/ChatGPT/UI/` | UI·배경·아이콘 후보 |
| ChatGPT | Portraits | 9 | `Assets/_intake/art/ChatGPT/Portraits/` | 캐릭터·콘셉트 이미지 |
| Mixamo | Animations | 9 | `Assets/_intake/art/Mixamo/Animations/` | 캐릭터 리그·애니메이션 |
| Tripo | Characters | 4 | `Assets/_intake/art/Tripo/Characters/` | `late_man` 캐릭터 원본·텍스처 |
| Hand | UI | 1 | `Assets/_intake/art/Hand/UI/` | 수제 로고 |

## 파일 목록

### `Trellis2/Buildings` (46)

`Amusement_Park.fbx`, `black_building.fbx`, `black_modern_house.fbx`,
`black_modern_residence.fbx`, `Blue_Apartment_2.fbx`, `blue_house.fbx`,
`blue_narroow_house.fbx`, `blue_store_house.fbx`, `brown_cafe.fbx`,
`brown_hall.fbx`, `chicken_house.fbx`, `Construction_unity.fbx`,
`control_tower.fbx`, `Cream_home_unity.fbx`, `door.fbx`, `fire_house.fbx`,
`golden_building.fbx`, `Hardware_store.fbx`, `hospital.fbx`,
`korean_cafe_2.fbx`, `korean_cafe.fbx`, `korean_church.fbx`,
`Laundry_Home_unity.fbx`, `logi_center.fbx`, `Logistics_Center.fbx`,
`mint_house.fbx`, `modern_apartment.fbx`, `old_apartment.fbx`,
`old_blue_roof.fbx`, `old_korea_house.fbx`, `old_stair.fbx`,
`Photo_Building_unity.fbx`, `pink_korea_house_2.fbx`, `pink_korea_house.fbx`,
`police.fbx`, `Pub_unity.fbx`, `Red_Church_unity.fbx`,
`red_korean_house.fbx`, `residence.fbx`, `retro_korean_house.fbx`,
`stair_building.fbx`, `sub_center.fbx`, `twin_apartment.fbx`,
`white_brown_house.fbx`, `white_korea_house.fbx`,
`white_modern_apartment.fbx`.

### `Trellis2/Props` (36)

`3_trash.fbx`, `basic_tree.fbx`, `Beacon_unity.fbx`,
`Bed_dafault_unity.fbx`, `belt.fbx`, `Bench_unity.fbx`,
`Bending_Mechine.fbx`, `black_Trash_unity.fbx`, `blossom_tree.fbx`,
`bycle.fbx`, `cafe.fbx`, `chair.fbx`, `chicken_house.fbx`, `clock.fbx`,
`couch.fbx`, `desk.fbx`, `dirty_box.fbx`, `Energy_Drink_unity.fbx`,
`Food_cart_unity.fbx`, `low_tv.fbx`, `market.fbx`, `modern_TV.fbx`,
`Old_Tv.fbx`, `orange_market.fbx`, `poster.fbx`, `Pot_unity.fbx`,
`Rug_unity.fbx`, `Signboard_unity.fbx`, `teddy_bear.fbx`,
`teddy_bunny.fbx`, `Trash_Bin_unity.fbx`, `trash_spot.fbx`, `truck.fbx`,
`White_Trash_unity.fbx`, `white_van.fbx`, `yellow_taxi.fbx`.

### `Qwen/Buildings` (57)

`amusement_park_Image_0.png`, `Amusement_Park.png`, `black_building.png`,
`black_modern_house.png`, `black_modern_residence.png`,
`Blue_Apartment_2.png`, `blue_house.png`, `blue_narroow_house.png`,
`blue_store_house.png`, `brown_cafe.png`, `brown_hall.png`,
`chicken_house.png`, `Construction_unity_Image_0.png`, `Construction.png`,
`control_tower.png`, `Cream_home_unity_Image_0.png`, `Cream_home.png`,
`fire_house.png`, `Food_cart_unity_Image_0.png`, `golden_building.png`,
`Hardware_store.png`, `hospital.png`, `korean_cafe_2.png`,
`korean_cafe.png`, `korean_church.png`, `Laundry_Home_unity_Image_0.png`,
`Laundry_Home.png`, `logi_center.png`, `Logistics_Center_Image_0.png`,
`Logistics_Center.png`, `mint_house.png`, `modern_apartment.png`,
`old_apartment.png`, `old_blue_roof.png`, `old_korea_house.png`,
`old_stair.png`, `Photo_Building_unity_Image_0 1.png`,
`Photo_Building_unity_Image_0.png`, `Photo_Building_unity.png`,
`pink_korea_house_2.png`, `pink_korea_house.png`, `police.png`,
`Pub_unity_Image_0.png`, `Pub.png`, `Red_Church_unity_Image_0.png`,
`Red_Church.png`, `red_korean_house.png`, `residence.png`,
`retro_korean_house.png`, `stair_building.png`, `sub_center_Image_0.png`,
`sub_center.png`, `trellis2_20260723_112830_1632967523_unity_Image_0.png`,
`twin_apartment.png`, `white_brown_house.png`, `white_korea_house.png`,
`white_modern_apartment.png`.

### `Qwen/Props` (48)

`3_trash.png`, `basic_tree.png`, `Beacon_unity_Image_0.png`, `Beacon.png`,
`Bed_dafault_unity_Image_0.png`, `Bed_dafault.png`, `belt_Image_0.png`,
`belt.png`, `Bench_unity_Image_0.png`, `Bench.png`,
`Bending_Mechine_Image_0.png`, `Bending_Mechine.png`,
`black_Trash_unity_Image_0.png`, `Black_Trash.png`, `blossom_tree.png`,
`bycle.png`, `chair.png`, `clock.png`, `couch.png`, `desk.png`,
`dirty_box.png`, `Energy_Drink_unity_Image_0.png`, `Energy_Drink.png`,
`Food_Cart.png`, `low_tv.png`, `modern_TV.png`, `Old_Tv.png`,
`poster.png`, `Pot_unity_Image_0.png`, `Pot.png`, `Rug_unity_Image_0.png`,
`Rug.png`, `Signboard_unity_Image_0.png`, `Signboard.png`,
`teddy_bear.png`, `teddy_bunny.png`, `Trash_Bin_unity_Image_0.png`,
`Trash_Bin.png`, `trash_spot_Image_0.png`, `trash_spot.png`,
`trellis2_20260723_111432_705094569_unity_Image_0.png`, `truck.png`,
`tv_Image_0.png`, `Untitled_Image_0.png`, `White_Trash_unity_Image_0.png`,
`White_Trash.png`, `qwen_image_00057_.png`, `qwen_image_00058_.png`.

### `ChatGPT/UI` (41)

`현수막.png`, `Arrow_gpt.png`, `arrow.png`, `car_road_gpt.png`,
`chat_box_box_gpt.png`, `check.png`, `cloud1.png`, `cloud2.png`,
`cross_road.png`, `hand.png`, `late_death_gpt.png`, `logis_logo_gpt.png`,
`logo_gpt.png`, `logo.png`, `man+gpt.png`, `Mold_gpt.png`, `moon.png`,
`one_blossom.png`, `One-Way Street_헷.png`, `one.png`,
`Question_Mark.png`, `quick_apt.png`, `right_up_main_ui - 복사본.png`,
`road_2_gpt.png`, `road_gpt.png`, `rolling.png`, `run_button_gpt.png`,
`sky_bg.png`, `sub_logo_gpt.png`, `sun.png`, `test__.png`, `ui_clock.png`,
`ui_coin.png`, `ui_dialogue_box.png`, `ui_phone_frame.png`, `x.png`,
`bar/bar.png`, `bar/kaz.png`, `bar/pointer.png`, `bar/previewe.png`,
`bar/snsrma.png`.

### `ChatGPT/Portraits` (9)

`basic_character.png`, `concept-art.png`, `late_man.png`, `npc (1).jpg`,
`npc (2).jpg`, `npc (3).jpg`,
`3f7a9853-821b-44f1-9e63-24a14bd748d3.png`, `content.png`,
`gs_girl.png`.

### `Mixamo/Animations` (9)

`gs_girl_mixamo_rig.fbx`, `gs_girl_walking.fbx`,
`A_Late_Man/Drunk Walk.fbx`, `A_Late_Man/Dwarf Idle.fbx`,
`A_Late_Man/Dying.fbx`, `A_Late_Man/Idle.fbx`,
`A_Late_Man/Rumba Dancing.fbx`, `A_Late_Man/Running.fbx`,
`A_Late_Man/Walking.fbx`.

### `Tripo/Characters` (4)

`late_man_raw.glb`, `late_man_rigged.glb`, `late-man_.fbx`,
`Texture/late_man.jpg`.

### `Hand/UI` (1)

`gocart_logo.png`.

## 열린 발주와의 후보 매핑

아래는 이름/형태에 따른 **검역 제안**이며 자동 장착용 확정 매핑이 아니다.

| 열린 발주 | 후보 파일 | 조치 |
|---|---|---|
| A-002 `fur_bed` | `Trellis2/Props/Bed_dafault_unity.fbx` | 검역 후 `fur_bed.fbx` 리네임 후보 |
| A-002 `fur_plant` | `Trellis2/Props/Pot_unity.fbx` | 식물 포함 여부 확인 후 리네임 후보 |
| A-002 `fur_rug` | `Trellis2/Props/Rug_unity.fbx` | 검역 후 `fur_rug.fbx` 리네임 후보 |
| A-002 `fur_tv` | `Old_Tv.fbx`, `modern_TV.fbx`, `low_tv.fbx` | 1종 선택 후 `fur_tv.fbx` 리네임 후보 |
| A-005 구름 3종 | `ChatGPT/UI/cloud1.png`, `cloud2.png` | 투명도·규격 확인 후 `Backgrounds` 이동 및 BOM 리네임 후보 |
| A-003 폰 프레임 | `ChatGPT/UI/ui_phone_frame.png` | 기반입 결과와 중복 여부 확인 |

`fur_lamp`, A-003 앱 아이콘 5종, A-004 `ui_map_town`, A-005
`fx_cloud_c`·지도 파츠, A-007 지정 파일명과 정확히 일치하는 파일은 이번 묶음에서
확인되지 않았다.

## 라이선스/검역 메모

- Trellis2: `art.md`에 기록된 **RunPod 셀프호스팅 TRELLIS
  (Microsoft, MIT)** 생성 라인으로 제안한다.
- Hand: 수제 원본으로 제안한다.
- ChatGPT: OpenAI 이용약관의 Content 조항에 따라, 법이 허용하는 범위에서
  사용자가 Output을 소유하고 OpenAI가 보유 권리를 사용자에게 양도한다.
  개인/유료 플랜과 무관하게 적용되는 일반 이용약관 근거로 제안한다.
  - 근거: https://openai.com/policies/terms-of-use/
- Mixamo: Adobe ID만 있으면 무료로 사용할 수 있고, 캐릭터와 애니메이션을
  비디오 게임을 포함한 개인·상업·비영리 프로젝트에 로열티 프리로 사용할 수
  있다는 Adobe 공식 FAQ 근거로 제안한다.
  - 근거: https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html
- Tripo: 반입자 확인 결과 **Tripo API만 사용**했다. 공식 약관은 Output의
  합법적인 상업·비상업 이용을 허용하며, 유료 사용자는 복제·수정·배포·
  라이선스·수익화 등 광범위한 권리를 갖는다고 명시한다. PR 출처 표기는
  `Tripo API / late_man 원본 묶음 / catalog character`로 제안한다.
  - 근거: https://www.tripo3d.ai/terms
- Qwen: 로컬/셀프호스팅 `Qwen-Image` 사용 시 공식 모델·코드가
  Apache-2.0으로 공개되어 상업 프로젝트 사용 근거로 제안할 수 있다.
  Qwen 사용 정책도 상업적 플랫폼/API/오픈소스 모델 사용에 적용된다고
  명시한다. 다만 **Qwen Chat 웹 서비스에서 생성한 경우인지, 로컬
  Qwen-Image에서 생성한 경우인지 확인이 필요**하다.
  - 모델 라이선스: https://github.com/QwenLM/Qwen-Image/blob/main/LICENSE
  - 사용 정책: https://qwen.ai/usagepolicy
- FBX의 Embed Media, 원점=바닥 중심, Y-up, 폴리 예산, 간판 분리 여부는
  파일 배치만으로 검증되지 않았으므로 관제 검역이 필요하다.
- PR 본문에는 파일별 원파일명·의도 `bom_id`를 남기고, 자유 카탈로그 물량은
  의도를 `catalog`로 기록한다.

### 병합 전 남은 확인

1. `Qwen` 이미지가 로컬/셀프호스팅 Qwen-Image 산출물인지 확인.
2. 입력 이미지에 제3자 상표·저작물·무단 레퍼런스가 포함되지 않았는지 확인.
