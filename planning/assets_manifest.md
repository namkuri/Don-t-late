# 반입 에셋 매니페스트 (INTAKE 기록)

외부 반입 에셋의 출처·버전·라이선스·용도를 기록한다. 재현 정보는 여기가 단일 소유 —
`_intake/` 원본은 기록 후 삭제한다(원본 보관 아님, 이 표가 재현을 보장).

| 에셋 | 출처 URL | 버전 | 라이선스 | 용도 | 반입일 |
|---|---|---|---|---|---|
| Pretendard-Regular.ttf | https://github.com/orioncactus/pretendard/releases/download/v1.3.9/Pretendard-1.3.9.zip (내부 `public/static/alternative/Pretendard-Regular.ttf`) | v1.3.9 | SIL Open Font License 1.1 | 전 UI 폰트 (TMP 폰트 에셋 `Pretendard-Regular SDF`) | 2026-07-21 |

## Pretendard v1.3.9 — 라이선스 전문 요지
- Copyright (c) 2021, Kil Hyung-jin (https://github.com/orioncactus/pretendard), Reserved Font Name "Pretendard".
- SIL Open Font License, Version 1.1 — https://scripts.sil.org/OFL
- 재배포·임베드·수정 허용, 폰트 자체 판매 금지, 라이선스·저작권 고지 유지 의무.
- dest 경로: `Assets/Art/UI/Fonts/Pretendard-Regular.ttf` + TMP 에셋(Dynamic·아틀라스 4096).

## Tripo INTAKE (민지) — 2026-07-21

> 출처 = Tripo 생성물(민지). **라이선스 = 플랜 미확인 플래그** — Tripo 유료 플랜=산출물 소유권 /
> 무료 플랜=CC-BY(출처 표기 의무). **확인 전 커밋 보류** (라이선스 확정 후 커밋). gen=1.

| bom_id | 원파일명 | dest | gen | 출처 | 라이선스 | 반입일 |
|---|---|---|---|---|---|---|
| moon_pixel | moon.png | `Assets/Art/Backgrounds/moon_pixel.png` (덮어쓰기·GUID 보존) | 1 | Tripo(민지) | ⚠ 플랜 미확인 (유료=소유/무료=CC-BY) — 커밋 보류 | 2026-07-21 |
| prop_streetlamp | 가로등-tripo.fbx | `Assets/Art/Props/prop_streetlamp.fbx` | 1 | Tripo(민지) | ⚠ 플랜 미확인 — 커밋 보류 | 2026-07-21 |
| chr_courier | coupang.fbx | `Assets/Art/Characters/chr_courier.fbx` | 1 | Tripo(민지) | ⚠ 플랜 미확인 — 커밋 보류 | 2026-07-21 |
| A_chr_courier_run | A_coupang_run.fbx | `Assets/Art/Characters/A_chr_courier_run.fbx` | 1 | Tripo(민지) | ⚠ 플랜 미확인 — 커밋 보류 | 2026-07-21 |

### 검역 수치 (INTAKE 2026-07-21)
- **moon_pixel**: png 256×256 RGB24, Point·무압축 자동 적용. ⚠ 알파 채널 없음 → Moon 셰이더 원판 마스크(tex.a)가 무력화되어 밤 컷에서 달 주위 **정사각 헤일로** 발생. 민지 재출력(투명 배경) 필요.
- **prop_streetlamp**: fbx tris=10405 (**Props 상한 1500 초과 → 경고**, 차단 아님). 원본 mesh.bounds 퇴화(≈0) → 컬링 리스크(임포터 RecalculateBounds 권고). 자연 높이 1.0u(Y-up, X270° 임포트 보정), 4.0u로 스케일(×4)해 스왑.
- **chr_courier**: fbx tris=4714 (<5000 통과). animType=Generic(휴머노이드 아님·Mixamo 자동리깅 미적용, 스켈레톤 有 SkinnedMeshRenderer×1). 높이 **1.06u** — 앵커 1.8u ±30%(1.26~2.34u) **미달 → 경고**(≈1.7배 업스케일 필요). 원점=발바닥(min.y≈0). T-포즈. 텍스처 없음(머티리얼 albedo none). 로고 육안: 메시에 "쿠팡/coupang" 글자·데칼 없음 — 단 **원파일명 "coupang"=쿠팡(실상표)** → chr_courier로 개명해 브랜딩 제거.
- **A_chr_courier_run**: fbx animType=Generic, 클립 1개 "mixamo.com" 0.53s @30fps(frames 0-16), loop=False(임포트 시 loop 설정 필요). Mixamo 달리기 모션.

## Character 교체 (late_man) + Walk 애니 — 2026-07-21

> 쿠팡맨(coupang.fbx) → **late_man 캐릭터로 교체**. chr_courier.fbx **내용만 덮어쓰기(GUID·.meta 보존)** —
> 하류 참조(프리팹·컨트롤러·아바타 배선) 무손실. Walk 모션 추가로 Speed 1D 블렌드(Walk/Run) 완성.

| bom_id | 원파일명 | dest | gen | 출처 | 라이선스 | 반입일 |
|---|---|---|---|---|---|---|
| chr_courier | late_man.fbx | `Assets/Art/Characters/chr_courier.fbx` (덮어쓰기·GUID 보존, 구 coupang.fbx 대체) | 2 | Tripo(민지) | ⚠ 과금제=산출물 소유권 (D-029) — 플랜 확인 후 커밋 | 2026-07-21 |
| A_chr_courier_walk | A_late_man_walking.fbx | `Assets/Art/Characters/A_chr_courier_walk.fbx` | 1 | **Mixamo**(Adobe·민지 매개) | Adobe 무료 라이선스(Mixamo) | 2026-07-21 |

### 검역 수치 (교체 INTAKE 2026-07-21)
- **chr_courier(late_man)**: fbx tris=**5432 (Characters 상한 5000 초과 432 → 경고**, 차단 아님·데시메이트 권고), verts=8809. **animType=Human 셋업 성공** — 아바타 isValid=True·isHuman=True (Mixamo 리그 mixamorig: 접두 32본, 계층 chr_courier/Armature/Root/mixamorig:Hips). 원임포트 높이 **1.07u**(앵커 1.8u ±30% 미달·경고) → 빌더가 렌더바운즈 기준 ×1.686 스케일로 **1.800u 정규화**·발끝 y=0 정렬. **텍스처 없음**(임베디드 0·버텍스컬러 없음·FBX 텍스처참조 경로 무효 `E:\dontlate`) → 회색 렌더. **쿠팡 로고: 텍스처 미포함으로 확인 불가**(후속 텍스처 반입 시 자동추출 규칙 처리 예정).
- **A_chr_courier_walk**: Mixamo 걷기, clip "mixamo.com" **0.967s**, animType=Human(아바타 CopyFromOther=chr_courier)·loop=True·isHumanMotion=True. 리타깃 경고(수치 아님)만.
- **A_chr_courier_run**(기존): Human 재셋업, clip 0.533s·loop=True·human=True.
- **AC_chr_courier.controller**: 파라미터 3종(Speed float·IsCarrying bool·IsGrounded bool — PlayerAnimationManager 계약) + 기본 스테이트 **Locomotion = Speed 1D 블렌드트리**(Walk@0·Walk@2.5·Run@4.5 — idle 클립 미납품이라 0 구간은 Walk 대체).

## ElevenLabs BGM INTAKE — 2026-07-21

> 출처 = **Eleven Music (ElevenLabs)**, **정수 개인 Creator 플랜($22/월, 유료)** 생성물 (주체 D-048 정정 · 플랜 2026-07-22 확인).
> 권리 = **상업적 사용 가능·기간 무제한**. 전 유료 플랜에 상업 라이선스 포함(Beta Services 제외).
> Eleven Music은 레이블·퍼블리셔·아티스트 협업으로 제작돼 **게이밍 포함 거의 모든 상업 용도에 클리어**.
> 표기 의무는 무료 플랜 한정이므로 **없음**. 근거: ElevenLabs Help Center "Can I publish the content
> I generate on the platform?" · Docs "Eleven Music" (2026-07-21 확인).
> 상세 대장은 `Assets/Audio/CREDITS.md` (프롬프트 설계서·PCM MD5·폐기 이력 포함).
>
> **파일명은 원제 유지** — BGM은 슬롯당 다곡 플레이리스트(D-046)라 `bom_id` 1:1 대응이 성립하지 않고,
> 스왑 계약은 파일명이 아니라 `Assets/Data/BgmLibrary.asset`(SO) 참조로 성립한다(BOM §8 개정분).

| 파일명 | 슬롯 | 길이 | dest | 출처 | 라이선스 | 반입일 |
|---|---|---|---|---|---|---|
| Seoul_Alley_Reflection_2026-07-20T161148.wav | Day | 60s | `Assets/Audio/BGM/` | Eleven Music (유료 구독) | 상업 사용 가능·무기한 | 2026-07-21 |
| Sunlit_Seoul_Afternoon_2026-07-20T154627.wav | Day | 60s | 〃 | 〃 | 〃 | 2026-07-21 |
| Breezy_Town_Stroll_2026-07-20T161422.wav | Night | 180s | 〃 | 〃 | 〃 | 2026-07-21 |
| Seoul_Afternoon_Stroll_2026-07-20T155537.wav | Night | 60s | 〃 | 〃 | 〃 | 2026-07-21 |
| Seoul_Pixel_Breeze_2026-07-19T103406.wav | Night | 60s | 〃 | 〃 | 〃 | 2026-07-21 |

### 검역 수치
- 전 5곡 원본 48kHz/16bit/stereo PCM. 임포트 = **Vorbis q30 · Compressed In Memory · 스테레오**(D-040·D-043).
  Streaming은 WebGL 미지원이라 금지, DecompressOnLoad는 60s 스테레오 1곡이 RAM 11.5MB 생PCM이라 기각.
- 압축 후 크기: 60s곡 0.78~0.88MB(109~124kbps) · `Breezy_Town_Stroll`(180s) 2.53MB. **채택 5곡 합 ≈ 5.6MB**.
- **컷 4곡**(`Ironic_Stillness`·`Pixel_Seoul_Breeze`·`Seoul_Pixel_Boulevard`·`Sunlit_Stroll_in_Seoul`)
  — 2026-07-21 청취 판정으로 최종 미채택. 프로젝트에서 제거, 원본 아카이브(`Don-t-late-bgm/`)에는 보존.
  **BGM 청취 판정 종료** — 반입 10곡 → 채택 5곡.
- **폐기 1곡**: `Late_for_Work_8-Bit_Panic` — 8비트로 분위기 불일치(Director 청취 판정). 프로젝트·아카이브 삭제.

## Suno BGM INTAKE (타이틀) — 2026-07-24

> 출처 = **Suno** (AI 음악 생성), **Director 개인 유료 플랜(Pro/Premier)** 생성물 (2026-07-24 반입 시 확인).
> 권리 = **상업적 사용 가능·소유권 생성자 귀속·기간 무제한**. Suno 유료 구독 생성물은 상업 이용 허용,
> 소유권 귀속(무료 플랜은 비상업+소유권 미귀속이라 반입 불가). 표기 의무 = **없음**(유료 플랜 한정).
> 근거: Suno 이용약관 유료 플랜 상업 라이선스 조항 · Director 플랜 확인(2026-07-24). 상세 = `Assets/Audio/CREDITS.md`.
> ElevenLabs 절 채택 5곡이 컷했던 **Title 슬롯 공백을 충원**. 스왑 계약은 `Assets/Data/BgmLibrary.asset`(slot=3).

| 파일명 | 슬롯 | 길이 | dest | 출처 | 라이선스 | 반입일 |
|---|---|---|---|---|---|---|
| Pixel_Night_Funk_Don-T-Late_NoVocal.wav | **Title** (현 타이틀 곡) | 195.6s | `Assets/Audio/BGM/` | Suno (유료 Pro/Premier) — 스템 분리 보컬제거본 | 상업 사용 가능·소유권 귀속·무기한 | 2026-07-24 |
| Pixel_Night_Funk_Don-T-Late.wav | Unsorted (보관) | 195.6s | `Assets/Audio/BGM/` | Suno (유료 Pro/Premier) — 보컬본 | 〃 | 2026-07-24 |

- 임포트 = **Vorbis q30 · Compressed In Memory · 스테레오**(AudioImportPostprocessor 자동, BGM 규격 · WebGL안전). 파일 MD5(앞12): NoVocal `02a1e5057f1a` · 보컬본 `f9b29ce1614c`.
- **2026-07-24 교체**(Director 지시): 타이틀 곡을 보컬제거본으로 교체, 보컬본은 삭제 없이 Unsorted 강등해 보관(추첨 제외). 보컬제거본은 원곡 Suno 스템이라 라이선스 동일.

## Trellis2 INTAKE — 2026-07-22

> 출처 = **RunPod 셀프호스팅 TRELLIS** (Microsoft · MIT) · 민지 생성. 생성물 상업 사용 제약 없음.

| 파일 | dest | tris(실측) | 상태 |
|---|---|---|---|
| store_2.fbx (편의점) | `Art/Buildings/store_2.fbx` | **485,891 ⚠**(상한 3,000) | District 슬롯 배치 완료 · 데시메이트·텍스처 대기(H12) |
| street_lamp_wood.fbx (한국식 가로등) | `Art/Props/prop_streetlamp.fbx` (전략 B 덮어쓰기) | **95,724 ⚠**(상한 1,500) | 8기 일괄 교체 완료 · 데시메이트·텍스처 대기(H12) |

## Hand INTAKE (민지 수제) — 2026-07-22

> 출처 = **민지 직접 모델링** (수제 — 생성 AI 아님). 권리 = 팀 자작, 제약 없음.

| bom_id | 원파일명 | dest | tris | 상태 |
|---|---|---|---|---|
| prop_box_parcel | box.fbx | `Art/Props/prop_box_parcel.fbx` | **106 ✓**(상한 1,500 — 첫 예산 통과 반입) | 원크기 2.48u→0.7u 정규화, 머티리얼 컬러 포함(테이프 디테일). Camp 3·District 1·트럭 적재 스택에 배선 |

## ElevenLabs SFX INTAKE (AU-007·008) — 2026-07-22

> 출처 = **ElevenLabs SFX** · 정수 Creator 플랜($22/월, 유료 — D-049 확인) 생성물. 상업 사용 가능·표기 의무 없음.
> 상세(프롬프트·생성 파라미터)는 `Assets/Audio/CREDITS.md` — 정수 PR#10 기록.

| bom_id | dest | 용도 | 반입일 |
|---|---|---|---|
| sfx_pickup·sfx_delivery_ok·sfx_late_buzzer·sfx_dialogue_blip | `Assets/Audio/SFX/` | 합성 플레이스홀더 → **실음원 교체**(스왑 계약) | 2026-07-22 |
| sfx_footstep·sfx_scene_whoosh·sfx_rhythm_hit·sfx_rhythm_miss·sfx_phone_ring·sfx_drink·sfx_deadline_warn·amb_night | 〃 | AU-007 11종분 | 〃 |
| sfx_box_break·sfx_barcode·sfx_penalty·sfx_vending·sfx_throw·sfx_coin·sfx_phone | 〃 | AU-008 신기능 7종 | 〃 |

### 파일별 등재 (훅 대조용 — 파일명 전체)

| 파일 | dest | 출처 | 라이선스 | 반입일 |
|---|---|---|---|---|
| amb_night.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_barcode.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_box_break.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_coin.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_deadline_warn.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_delivery_ok.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_dialogue_blip.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_drink.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_footstep.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_late_buzzer.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_penalty.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_phone.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_phone_ring.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_pickup.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_rhythm_hit.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_rhythm_miss.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_scene_whoosh.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_throw.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_vending.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) | 상업 가능·표기 불요 | 2026-07-22 |
| sfx_settle_ok.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-010 | 상업 가능·표기 불요 | 2026-07-23 |
| sfx_settle_bad.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-010 | 상업 가능·표기 불요 | 2026-07-23 |
| sfx_furniture_place.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-010 | 상업 가능·표기 불요 | 2026-07-23 |
| sfx_ui_tick.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-010 | 상업 가능·표기 불요 | 2026-07-23 |
| amb_villatown.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-011 | 상업 가능·표기 불요 | 2026-07-23 |
| amb_foodalley.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-011 | 상업 가능·표기 불요 | 2026-07-23 |
| sfx_map_pin.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-011 | 상업 가능·표기 불요 | 2026-07-23 |
| sfx_map_route.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-011 | 상업 가능·표기 불요 | 2026-07-23 |
| sfx_map_depart.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-011 | 상업 가능·표기 불요 | 2026-07-23 |
| sfx_dialogue_blip_1.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — S-053 8비트 rapid_stepped | 상업 가능·표기 불요 | 2026-07-25 |
| sfx_dialogue_blip_2.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — S-053 8비트 rapid_stepped | 상업 가능·표기 불요 | 2026-07-25 |

- S-053 대사 블립 변주 2종(문자마다 랜덤): DialogueView `_blipClips` 풀 배선. 기존 `sfx_dialogue_blip.wav`는 삭제 없이 `_blipClip` 폴백으로 보관.

## ChatGPT UI INTAKE (민지) — 2026-07-22

| 파일 | dest | 출처 | 라이선스 | 반입일 |
|---|---|---|---|---|
| ui_title.png | (원명 logo.png) `Assets/Art/UI/` | ChatGPT 생성(민지·구두 계약 2026-07-22) | 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-07-22 |
| ui_title_sub.png | (원명 sub_logo.png) `Assets/Art/UI/` | ChatGPT 생성(민지·구두 계약 2026-07-22) | 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-07-22 |
| ui_title_man.png | (원명 man.png) `Assets/Art/UI/` | ChatGPT 생성(민지·구두 계약 2026-07-22) | 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-07-22 |
| ui_dialogue_box.png | (원명 chat_box.png) `Assets/Art/UI/` | ChatGPT 생성(민지·구두 계약 2026-07-22) | 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-07-22 |
| ui_dialogue_arrow.png | (원명 chat_box_box.png) `Assets/Art/UI/` | ChatGPT 생성(민지·구두 계약 2026-07-22) | 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-07-22 |
| ui_start_button.png | (원명 run_button.png) `Assets/Art/UI/` | ChatGPT 생성(민지·구두 계약 2026-07-22) | 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-07-22 |
| ui_phone_frame.png | 폰 겉면 프레임 (민트) | ChatGPT 생성 (민지 · 원명 mint_phone.png — UI 구두 계약 라인) | 상업 사용 가능 | 2026-07-23 |
