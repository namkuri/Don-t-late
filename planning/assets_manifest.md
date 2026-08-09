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
| A_chr_courier_idle | Idle.fbx | `Assets/Art/Characters/A_chr_courier_idle.fbx` | 1 | **Mixamo**(Adobe) — `_intake/art/Mixamo/Animations/A_Late_Man/` | Adobe 무료 라이선스(Mixamo) | 2026-08-07 |

### 검역 수치 (교체 INTAKE 2026-07-21)
- **chr_courier(late_man)**: fbx tris=**5432 (Characters 상한 5000 초과 432 → 경고**, 차단 아님·데시메이트 권고), verts=8809. **animType=Human 셋업 성공** — 아바타 isValid=True·isHuman=True (Mixamo 리그 mixamorig: 접두 32본, 계층 chr_courier/Armature/Root/mixamorig:Hips). 원임포트 높이 **1.07u**(앵커 1.8u ±30% 미달·경고) → 빌더가 렌더바운즈 기준 ×1.686 스케일로 **1.800u 정규화**·발끝 y=0 정렬. **텍스처 없음**(임베디드 0·버텍스컬러 없음·FBX 텍스처참조 경로 무효 `E:\dontlate`) → 회색 렌더. **쿠팡 로고: 텍스처 미포함으로 확인 불가**(후속 텍스처 반입 시 자동추출 규칙 처리 예정).
- **A_chr_courier_walk**: Mixamo 걷기, clip "mixamo.com" **0.967s**, animType=Human(아바타 CopyFromOther=chr_courier)·loop=True·isHumanMotion=True. 리타깃 경고(수치 아님)만.
- **A_chr_courier_run**(기존): Human 재셋업, clip 0.533s·loop=True·human=True.
- **A_chr_courier_idle**(S-197): Mixamo Idle, clip "Idle" **1.97s**·loop=True·isHumanMotion=True. animType=Human, 아바타는 **CreateFromThisModel**(CopyFromOther로 넣으면 테이크 0개로 잎힌 — 실측). 휴머노이드라 아바타가 달라도 배달원 리그로 리타깃된다.
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

## Suno BGM INTAKE (날씨 BGM · AU-018 ②) — 2026-07-27

> 출처·권리·근거 = 위 Suno 절과 동일(Director 유료 플랜 · 상업 가능·소유권 귀속·무기한·표기 불요).
> 날씨(Rain·Snow·Heat·Fog) 무드 BGM. 스왑 계약은 WorldAudioManager 필드 주입(`_bgmXxxDay/_bgmXxxNight`) —
> 배선 정본은 `CoreSceneBuilder`이고 그 키가 **파일명**이다. 원제는 원칙적으로 유지하되, 원제가 역할과
> 반대라 혼동을 부르는 경우에 한해 개명하고 **원제를 아래 표 '출처'칸에 병기**한다(AU-032 · 라이선스 추적 유지).
> 반입 후 **루프용 페이드 트림**(인트로 페이드인·아웃트로 페이드아웃 램프 제거 — Suno 곡은 앞뒤 페이드가 붙어 루프 부적합). 원본은 Downloads 보존.

| 파일명 | 날씨 | 길이(트림후) | dest | 출처 | 라이선스 | 반입일 |
|---|---|---|---|---|---|---|
| Neon Rain.wav | Rain(밤) | 145.6s | `Assets/Audio/BGM/` | Suno (유료) — 페이드 트림 | 상업 사용 가능·소유권 귀속·무기한 | 2026-07-27 |
| Rain on the Window.wav | Rain(낮) | 148.6s | `Assets/Audio/BGM/` | Suno (유료) — 페이드 트림 (AU-025) | 상업 사용 가능·소유권 귀속·무기한 | 2026-08-01 |
| Neon Snowfall.wav | Snow(밤) | 82.4s | `Assets/Audio/BGM/` | Suno (유료) — 페이드 트림 | 〃 | 2026-07-27 |
| Daylight Snowfall.wav | Snow(낮) | 49.3s | `Assets/Audio/BGM/` | Suno (유료) — 페이드 트림 (AU-026) | 상업 사용 가능·소유권 귀속·무기한 | 2026-08-01 |
| Heatwave Afternoon.wav | Heat(낮) | 59.9s | `Assets/Audio/BGM/` | Suno (유료) — 페이드 트림 · **원제 `Midnight Heatwave`** (AU-032 개명) | 〃 | 2026-07-27 |
| Heatwave Night Drive.wav | Heat(밤) | 82.5s | `Assets/Audio/BGM/` | Suno (유료) — 페이드 트림 · **원제 `Sunny Afternoon Drive`** (AU-032 개명) | 상업 사용 가능·소유권 귀속·무기한 | 2026-08-08 |
| Pale White Haze.wav | Fog(낮) | 143.5s | `Assets/Audio/BGM/` | Suno (유료) — 페이드 트림 (AU-032) · 원제 유지 | 상업 사용 가능·소유권 귀속·무기한 | 2026-08-08 |
| Sodium Fog.wav | Fog(밤) | 65.2s | `Assets/Audio/BGM/` | Suno (유료) — 페이드 트림 | 〃 | 2026-07-27 |

- 임포트 = Vorbis q30 · Compressed In Memory · 스테레오(BGM 규격 자동). 배선 = `WeatherChanged` 구독 → 날씨 무드 곡이 시간대 슬롯 override(amb 우선순위와 동형).

## Suno BGM INTAKE (엔딩 · AU-023) — 2026-07-31

> 출처·권리·근거 = 위 Suno 절과 동일(Director 유료 플랜 · 상업 가능·소유권 귀속·무기한·표기 불요).
> 엔딩 전용 BGM(`bgm_ending` 슬롯). **원제 유지**. 프롬프트 = 방향5(그윈 오마주·Rhodes 피아노 리드) — 상세 `CREDITS.md`.
> **원샷(A) 곡** — 인트로 성김→정점(75~85s)→아웃트로 페이드아웃(145~150s)의 through-composed 아크.
> 날씨곡과 달리 **페이드 트림 안 함**(페이드가 아크의 일부). 소켓 배선(엔딩 원샷 재생, S-107)은 관제 몫.

| 파일명 | 슬롯 | 길이 | dest | 출처 | 라이선스 | 반입일 |
|---|---|---|---|---|---|---|
| Fading Into Dawn.wav | **Ending** | 149.8s | `Assets/Audio/BGM/` | Suno (유료 Pro/Premier) | 상업 사용 가능·소유권 귀속·무기한 | 2026-07-31 |

- 임포트 = Vorbis q30 · Compressed In Memory · 스테레오(BGM 규격 자동, 실측 검증). 파일 MD5(앞12): `f0859b4bb526`. 실측 peak -3.2dB · rms -17.1dB(다이내믹=감정 핵심이라 평탄화 안 함).

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
| sfx_arrive.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-018 ④ | 상업 가능·표기 불요 | 2026-07-26 |
| sfx_jump.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-018 ③ | 상업 가능·표기 불요 | 2026-07-27 |
| sfx_land.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-018 ③ | 상업 가능·표기 불요 | 2026-07-27 |
| sfx_footstep_snow.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료) — AU-018 ③ | 상업 가능·표기 불요 | 2026-07-27 |
| sfx_box_damage.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료 · --raw 비토이톤) — AU-018 ③ | 상업 가능·표기 불요 | 2026-07-27 |
| amb_weather_rain.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료 · 22s×2 스티칭 40s 루프) — AU-018 ① | 상업 가능·표기 불요 | 2026-07-27 |
| amb_weather_snow.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료 · 22s×2 스티칭 40s 루프) — AU-018 ① | 상업 가능·표기 불요 | 2026-07-27 |
| amb_weather_heat.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료 · 22s×2 스티칭 40s 루프) — AU-018 ① | 상업 가능·표기 불요 | 2026-07-27 |
| sfx_car_crash.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료 · --no-anchors 비토이톤) — AU-020 | 상업 가능·표기 불요 | 2026-07-29 |
| sfx_thunder.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료 · --no-anchors 비토이톤) — AU-022 | 상업 가능·표기 불요 | 2026-07-29 |
| sfx_fanfare.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료 · 토이톤+칩튠 브라스 스탭) — AU-021 | 상업 가능·표기 불요 | 2026-07-29 |
| sfx_level_up.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료 · 토이톤 3음 상승 아르페지오) — AU-027 | 상업 가능·표기 불요 | 2026-08-05 |
| sfx_tutorial_step.wav | `Assets/Audio/SFX/` | ElevenLabs SFX (정수 Creator 유료 · 토이톤 마림바 2음 상승 확인음) — AU-028 | 상업 가능·표기 불요 | 2026-08-07 |

## ChatGPT UI INTAKE (민지) — 2026-07-22

| 파일 | dest | 출처 | 라이선스 | 반입일 |
|---|---|---|---|---|
| ui_title.png | (원명 logo.png) `Assets/Art/UI/` | ChatGPT 생성(민지·구두 계약 2026-07-22) | 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-07-22 |
| ui_title_sub.png | (원명 sub_logo.png) `Assets/Art/UI/` | ChatGPT 생성(민지·구두 계약 2026-07-22) | 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-07-22 |
| ui_title_man.png | (원명 man.png) `Assets/Art/UI/` | ChatGPT 생성(민지·구두 계약 2026-07-22) | 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-07-22 |
| ui_dialogue_box.png | 대화 박스 (명찰 탭 신판 — S-117 교체) `Assets/Art/UI/` | ChatGPT 생성(민지·구두 계약 2026-07-22) | 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-07-30 |
| ui_dialogue_arrow.png | (원명 chat_box_box.png) `Assets/Art/UI/` | ChatGPT 생성(민지·구두 계약 2026-07-22) | 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-07-22 |
| ui_start_button.png | (원명 run_button.png) `Assets/Art/UI/` | ChatGPT 생성(민지·구두 계약 2026-07-22) | 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-07-22 |
| ui_phone_frame.png | 폰 겉면 프레임 (크림+네이비 — S-117 교체, 구 민트판 대체) | ChatGPT 생성 (민지 · _intake ChatGPT/UI 라인) | 상업 사용 가능 | 2026-07-30 |
| ui_clock.png | HUD 시계 아이콘 (S-117 신규 소켓) | ChatGPT 생성 (민지 · _intake ChatGPT/UI 라인) | 상업 사용 가능 | 2026-07-30 |
| ui_coin.png | HUD 현금 칩 코인 아이콘 (S-117 신규 소켓) | ChatGPT 생성 (민지 · _intake ChatGPT/UI 라인) | 상업 사용 가능 | 2026-07-30 |
| ui_gauge_fill.png | 게이지 fill용 4×4 순백 사각 `Assets/Art/UI/` | CoreSceneBuilder 코드 자동 생성 (S-070 ② — 외부 소스 없음) | 자체 생성물 — 제약 없음 | 2026-07-28 |
| Assets/Art/Props/fur_bed.fbx | 민지 A-008 (_intake/art/Trellis2/Props/Bed_dafault_unity.fbx 스왑) | RunPod 셀프호스팅 TRELLIS (MIT) | S-109 |
| Assets/Art/Props/fur_plant.fbx | 민지 A-008 (Pot_unity.fbx 스왑) | RunPod 셀프호스팅 TRELLIS (MIT) | S-109 |
| Assets/Art/Props/fur_rug.fbx | 민지 A-008 (Rug_unity.fbx 스왑) | RunPod 셀프호스팅 TRELLIS (MIT) | S-109 |
| Assets/Art/Props/fur_tv.fbx | 민지 A-008 (low_tv.fbx 스왑) | RunPod 셀프호스팅 TRELLIS (MIT) | S-109 |
| Assets/Art/Backgrounds/fx_cloud_a.png | 민지 A-008 (ChatGPT/UI/cloud1.png 스왑) | OpenAI 약관 — Output 사용자 소유 | S-109 |
| Assets/Art/Backgrounds/fx_cloud_b.png | 민지 A-008 (ChatGPT/UI/cloud2.png 스왑) | OpenAI 약관 — Output 사용자 소유 | S-109 |
| Assets/Art/Buildings/Amusement_Park.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/Blue_Apartment_2.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/Construction_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/Cream_home_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/Hardware_store.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/Laundry_Home_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/Logistics_Center.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/Photo_Building_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/Pub_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/Red_Church_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/black_building.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/black_modern_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/black_modern_residence.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/blue_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/blue_narroow_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/blue_store_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/brown_cafe.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/brown_hall.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/chicken_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/control_tower.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/door.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/fire_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/golden_building.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/hospital.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/korean_cafe.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/korean_cafe_2.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/korean_church.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/logi_center.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/mint_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/modern_apartment.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/old_apartment.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/old_blue_roof.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/old_korea_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/old_stair.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/pink_korea_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/pink_korea_house_2.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/police.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/red_korean_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/residence.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/retro_korean_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/stair_building.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/store_2.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/sub_center.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/twin_apartment.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/white_brown_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/white_korea_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/white_modern_apartment.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Buildings 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/3_trash.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/Beacon_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/Bed_dafault_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/Bench_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/Bending_Mechine.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/Energy_Drink_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/Food_cart_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/Old_Tv.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/Pot_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/Rug_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/Signboard_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/Trash_Bin_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/White_Trash_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/basic_tree.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/belt.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/black_Trash_unity.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/blossom_tree.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/bycle.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/cafe.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/chair.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/chicken_house.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/clock.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/couch.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/desk.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/dirty_box.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/low_tv.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/market.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/modern_TV.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/orange_market.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/poster.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/teddy_bear.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/teddy_bunny.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/trash_spot.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/truck.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/white_van.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Props/yellow_taxi.fbx | 민지 A-008 카탈로그 (_intake/art/Trellis2/Props 일괄 반입) | RunPod 셀프호스팅 TRELLIS (MIT) | S-111 |
| Assets/Art/Buildings/Textures/Construction_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/Cream_home_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/Hardware_store_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/Laundry_Home_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/Logistics_Center_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/Photo_Building_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/Pub_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/Red_Church_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/amusement_park_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/black_building_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/black_modern_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/black_modern_residence_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/blue_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/blue_narroow_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/blue_store_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/brown_cafe_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/brown_hall_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/chair_Image_0_3.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/chicken_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/control_tower_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/door_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/fire_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/hospital_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/korean_cafe_2_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/korean_cafe_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/korean_church_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/logi_center_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/mint_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/modern_apartment_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/old_apartment_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/old_blue_roof_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/old_korea_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/old_stair_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/pink_korea_house_2_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/pink_korea_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/police_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/red_korean_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/residence_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/retro_korean_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/stair_building_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/sub_center_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/trellis2_20260723_112830_1632967523_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/twin_apartment_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/white_brown_houose_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/white_korea_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Buildings/Textures/white_modern_apartment_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/Beacon_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/Bed_dafault_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/Bench_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/Bending_Mechine_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/Energy_Drink_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/Food_cart_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/Pot_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/Rug_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/Signboard_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/Trash_Bin_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/Untitled_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/White_Trash_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/basic_tree_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/belt_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/black_Trash_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/blossom_tree_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/cafe_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/chair_Image_0_1.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/chair_Image_0_2.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/chair_Image_0_6.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/chair_Image_0_7.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/chicken_house_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/clock_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/couch_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/dirty_box_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/market_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/orange_market_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/teddy_bear_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/teddy_bunny_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/trash_spot_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/trellis2_20260723_111432_705094569_unity_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/tv_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/white_van_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/yellow_taxi_Image_0.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |
| Assets/Art/Props/Textures/스크린샷 2026-07-22 040147.png | S-113 임베디드 추출 (원본 fbx 파생 — 민지 A-008) | RunPod 셀프호스팅 TRELLIS (MIT) | S-113 |

## PR#26 반입 (A-008 후속 · 계약 경로 교정 반입 — S-121 검역 CONDITIONAL 치유) — 2026-07-30

> 민지님 PR #26의 신규 캐릭터 11파일. 원 PR 경로(`Mixamo/A_*`·`Tripo_API/*`)가 반입 계약
> (`_intake/art/<도구>/<분류>/`)을 벗어나 있어 **main 관례 경로로 교정 반입**했고, main과
> byte-identical 중복 12건 + PR 내부 자기중복 1건(gs_girl.jpg)은 제외했다. Trellis2 82종은
> 동일 경로 oid 교체(교체본 — 아래 별도 행).

| 파일 | 내용 | 출처·라이선스 | 반입일 |
|---|---|---|---|
| Assets/_intake/art/Tripo/Characters/malsoon.fbx | 박말순 3D 모델 (D-073 채택 — 엔딩씬) | Tripo OpenAPI (민지 유료 플랜 · 사용자 출력 권리 — orders/art.md:183) | 2026-07-30 |
| Assets/_intake/art/Tripo/Characters/Texture/malsoon.png | 박말순 텍스처 — ⚠ PNG 내장 그래프상 **OpenAI GPT Image** 산출물(하이브리드 플로우: 이미지=GPT Image / 3D=Tripo) | OpenAI GPT Image (ChatGPT 출력물 사용자 소유 — orders/art.md:182) | 2026-07-30 |
| Assets/_intake/art/Tripo/Characters/Texture/malsoon.fbm.jpg | 박말순 fbx 임베드 텍스처 파생 | Tripo OpenAPI (상동) | 2026-07-30 |
| Assets/_intake/art/Tripo/Characters/juhye_lowpoly.glb | 지혜 저폴리 메시 (용도 미정 — 채택 보류) | Tripo OpenAPI (상동) | 2026-07-30 |
| Assets/_intake/art/Tripo/Characters/juhye_lowpoly_rigged.glb | 지혜 저폴리 리깅본 | Tripo OpenAPI (상동) | 2026-07-30 |
| Assets/_intake/art/Tripo/Characters/juhye_source.glb | 지혜 소스 메시 42MB (정본 결정 대기) | Tripo OpenAPI (상동) | 2026-07-30 |
| Assets/_intake/art/Tripo/Characters/Texture/gs_girl.jpg | gs_girl 텍스처 (PR 내 2중 복제 중 1건만 반입) | Tripo OpenAPI (상동) | 2026-07-30 |
| Assets/_intake/art/Mixamo/Animations/gs_girl_mixamo_rig_final.fbx | gs_girl 리그 최종본 (기존 gs_girl_mixamo_rig.fbx와 별개 — 정본 결정 대기) | Mixamo (Adobe · 상업 로열티 프리 — orders/art.md:183) | 2026-07-30 |
| Assets/_intake/art/Mixamo/Animations/A_jihye/jihye_Standing Greeting.fbx | 지혜 인사 모션 | Mixamo (상동) | 2026-07-30 |
| Assets/_intake/art/Mixamo/Animations/A_malsoon/malsoon_Angry.fbx | 박말순 화난 모션 1 | Mixamo (상동) | 2026-07-30 |
| Assets/_intake/art/Mixamo/Animations/A_malsoon/malsoon_Angry_2.fbx | 박말순 화난 모션 2 | Mixamo (상동) | 2026-07-30 |
| Assets/_intake/art/Trellis2/{Buildings,Props}/*.fbx (82종) | **oid 교체(재출력본)** — 합계 3.49GiB→2.10GiB(−1.39GiB, 커진 파일 0·no-op 0). A-002 오리진 반려 4종(fur_bed/plant/rug/tv) 소스 포함 | RunPod 셀프호스팅 TRELLIS (Microsoft · MIT) · 민지 생성 (assets_manifest §Trellis2 INTAKE) | 2026-07-30 |
| Assets/Art/Terrains/hill.fbx | Hillside 유선형 산 지형 (S-129 — 좌우 대칭 봉우리, 26.4×4.0×2.0u 원본) | **남규(직접 제작)** · Blender (GNU GPL — 산출물은 제작자 소유) | 2026-07-31 |
| Assets/Art/Props/Textures/chair_Image_0_4.png | 의자 베이스맵 2048² (S-132 — chair.fbx 정리 중 복구). 원본 `chair.fbx`에 팩돼 있었으나 참조 경로(`_art_originals/Props/export.fbm/`)가 실재하지 않아 내보내기에 실리지 않던 것을 블렌더에서 풀어 저장 | **기존 chair.fbx 임베드 텍스처와 동일 출처** — RunPod 셀프호스팅 TRELLIS (Microsoft · MIT) · 민지 생성 (assets_manifest §Trellis2 INTAKE) | 2026-08-03 |

## PR#32 반입 (민지 · 계약 경로 교정 반입) — 2026-08-04

> 원 PR 경로 `Assets/Art/intake/`가 반입 계약(`_intake/art/<도구>/`)을 벗어나 있어 교정 반입.
> `Art/` 아래는 Buildings|Props|Characters|Backgrounds|Portraits|UI만 임포트 규칙이 걸리므로
> `Art/intake/`는 자동 임포트가 안 걸리는 사각지대였다(PR#26과 동일 유형).

| 파일 | 내용 | 출처·라이선스 | 반입일 |
|---|---|---|---|
| Assets/_intake/art/ChatGPT/One-Way Street_헷.png | 일방통행 표지판 텍스처 263KB (bom_propose.md:109 제안분) | ChatGPT 생성(민지·구두 계약 2026-07-22) · 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관) | 2026-08-04 |
| Assets/_intake/art/ChatGPT/Materials/One-Way Street_헷.mat | 위 텍스처 머티리얼 (파생물) | 상동 | 2026-08-04 |
| Assets/_intake/art/ChatGPT/Materials/onw-way-logo.mat | 일방통행 로고 머티리얼 (파생물) | 상동 | 2026-08-04 |
| Assets/_intake/art/ChatGPT/Texture/airplane.mat | 비행기 머티리얼 (파생물 — 기존 ChatGPT 라인) | 상동 | 2026-08-04 |
| Assets/Art/UI/ui_dialogue_box.png | 대화 박스 **재출력본**(10.1KB→21.5KB, 채도 조정판 — S-117 신판 대체) | 상동 (manifest:202 동일 항목 갱신) | 2026-08-04 |

## PR#34 반입분 (민지) — 2026-08-05 · 관제 대리 기입

> **기입 경위**: PR#34에 `_intake` 신규 25건이 매니페스트 기록 없이 올라와 반입 차단 사유가 됐다.
> 두 출처 모두 **이미 이 문서에 라이선스 근거가 확립된 라인**이라(위 「ChatGPT UI INTAKE」·
> 「Trellis2 INTAKE」), 새 판단이 아니라 **같은 라인의 연장**으로 관제가 대리 기입한다.
> 폴더 경로(`_intake/art/ChatGPT/`, `_intake/art/Trellis2/`)를 출처 근거로 삼았다 —
> ⚠ 민지님 확인 후 이 문구를 지운다. 다르면 알려주시면 즉시 정정한다.
>
> dest는 아직 `_intake`(검역 대기) — 정식 위치(`Assets/Art/`) 스왑은 소켓 배선 시 기록을 갱신한다.

### ChatGPT 라인 23건 — 라이선스: 산출물 권리 사용자 귀속·상업 가능(OpenAI 약관)

| 파일 | 용도 추정 | 위치 |
|---|---|---|
| car_road_gpt.mat · road_gpt.mat | 도로 머티리얼 | `_intake/art/ChatGPT/Materials/` |
| late_death_gpt.mat | 지각·사고 연출 머티리얼 | `_intake/art/ChatGPT/Materials/` |
| logis_logo_gpt.mat | 물류 로고 머티리얼 | `_intake/art/ChatGPT/Materials/` |
| debt.mat · debt.png | 빚 표시 텍스처 | `_intake/art/ChatGPT/Texture/` |
| gohome.png | 귀가 버튼 텍스처 | `_intake/art/ChatGPT/Texture/` |
| tutorial.png | 튜토리얼 배너 텍스처 | `_intake/art/ChatGPT/Texture/` |
| square-box.png · ui_back.png · xButton.png | 노점 구매창 UI 부품 | `_intake/art/ChatGPT/UI/KioskPanel/` |
| bending_machine_ui.png | 자판기 UI | `_intake/art/ChatGPT/UI/` |
| inventory-ui.png | 가방 UI | `_intake/art/ChatGPT/UI/` |
| cocoa.png · drink.png · odeng.png · water.png · flower.png | 아이템 아이콘 5종 | `_intake/art/ChatGPT/UI/` |
| 현수막.mat · 현수막.png(.meta) | 현수막 | `_intake/art/ChatGPT/UI/` |
| One-Way Street_헷.png(.meta) | 일방통행 표지 | `_intake/art/ChatGPT/UI/` |
| right_up_main_ui - 복사본.png(.meta) | 우상단 HUD 시안 | `_intake/art/ChatGPT/UI/` |

### Trellis2 라인 2건 — 라이선스: RunPod 셀프호스팅 TRELLIS (Microsoft · MIT) · 상업 제약 없음

| 파일 | 용도 | 위치 |
|---|---|---|
| blue_house_Image_0.png | 건물 텍스처 (blue_house) | `_intake/art/Trellis2/Buildings/T/` |
| old_blue_roof_Image_0.png | 건물 텍스처 (old_blue_roof) | `_intake/art/Trellis2/Buildings/T/` |

### S-185 스왑 반영 (2026-08-06)

| 파일 | dest | 출처 | 라이선스 | 반입일 |
|---|---|---|---|---|
| chr_courier_base.jpg | `Assets/Art/Characters/Textures/` (원본 `_intake/art/Tripo/Characters/Texture/late_man.jpg` 스왑) | Tripo 생성(민지) | 위 「Tripo INTAKE」 조건과 동일 | 2026-08-06 |

> 플레이어 머티리얼(`tripo_material_327854b4-…mat`)의 `_BaseMap`이 비어 있어 흰색으로
> 렌더되던 것을 이 텍스처로 채웠다(S-185).
