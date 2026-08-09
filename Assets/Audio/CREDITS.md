# Assets/Audio — 출처·라이선스 대장

> `pipelines/audio.md` §2-2: **확보 즉시 라이선스 기록 = 입장권** (누락 = 반입 차단, 교정 불가).
> 이 파일은 커밋 대상. 음원 원본(`*.wav`)은 컷 판정 전까지 `.gitignore`로 제외한다 (D-042).

---

## BGM — Eleven Music (ElevenLabs)

| 항목 | 내용 |
|---|---|
| 생성 도구 | **Eleven Music** (ElevenLabs) |
| 계정 | **정수 개인 계정 — Creator 플랜 ($22/월, 유료)** (당초 "Director 개인 구독" 표기는 오기 — D-048 주체 정정, 플랜은 2026-07-22 남규가 정수에게 직접 확인) |
| 권리 | **상업적 사용 가능 · 기간 무제한.** 유료 구독 중 생성한 콘텐츠는 상업 이용 가능하며, 전 유료 플랜에 상업 라이선스가 포함된다 (Beta Services 사용 시 제외). Eleven Music은 레이블·퍼블리셔·아티스트 협업으로 제작돼 **게이밍을 포함한 거의 모든 상업 용도에 클리어**되어 있다 |
| 표기 의무 | **없음** — "Eleven Music" 표기 의무는 무료 플랜 한정 |
| 근거 | ElevenLabs Help Center "Can I publish the content I generate on the platform?" · ElevenLabs Docs "Eleven Music" (2026-07-21 확인) |
| 생성일 | 2026-07-19 ~ 2026-07-20 (파일명 타임스탬프) |
| 반입일 | 2026-07-21 |

### 곡 목록 (채택 5곡 · 48kHz / 16bit / stereo PCM)

2026-07-21 청취 판정 완료. 아래 5곡만 프로젝트에 남고 커밋된다
(`.gitignore` 예외 + `assets_manifest.md` 등재). 미채택분은 **폐기 이력** 참조.

파일명 = ElevenLabs 원제 + 생성 타임스탬프. **`bom_id` 리네임은 하지 않는다** — 플레이리스트(D-046)로
슬롯당 다곡이라 `bom_id` 1:1 대응이 성립하지 않고, 스왑 계약은 `BgmLibrary.asset`(SO) 참조로 성립한다.

| 파일 | 슬롯 | 길이 | PCM MD5(앞 12) |
|---|---|---|---|
| `Sunlit_Seoul_Afternoon_2026-07-20T154627.wav` | **Day** | 60.0s | `e12de724acbd` |
| `Seoul_Alley_Reflection_2026-07-20T161148.wav` | **Day** | 60.0s | `3194df4c88a7` |
| `Breezy_Town_Stroll_2026-07-20T161422.wav` | **Night** | **180.0s** | `93c74a16b7f6` |
| `Seoul_Afternoon_Stroll_2026-07-20T155537.wav` | **Night** | 60.0s | `be66e3688257` |
| `Seoul_Pixel_Breeze_2026-07-19T103406.wav` | **Night** | 60.0s | `5427eddf1af7` |

**⚠ 제목으로 낮/밤을 추정하지 말 것.** `Seoul_Alley_Reflection`(골목 사색)이 분류상 **낮**,
`Seoul_Afternoon_Stroll`(오후 산책)이 분류상 **밤**이었다. ElevenLabs가 붙인 제목은 프롬프트 무드와 무관하다.

### 생성 프롬프트 (스타일 근거)

앵커: **VA-11 HALL-A (Garoad) OST 계열.**

**낮 · 오후 · 마을**
```
major key city pop, retro 80s, bright FM electric piano, sparkling bell synth,
punchy analog synths, driving synth bass, bright arpeggiated synth, clean bright synth lead,
crisp dry drum machine, glossy pads, warm analog, cheerful, sunny afternoon,
cozy neighborhood, breezy town stroll, instrumental, 105 BPM,
no vocals, no jazz, no saxophone, no acoustic guitar, no neon, no nighttime mood
```

**밤 · 심야** (앵커 트랙 "Every Day Is Night")
```
downtempo synthwave city pop, lo-fi, warm analog synth pads, round mellow synth bass,
dreamy nostalgic lead synth, dusty laid-back drum machine beat, soft bell tones,
vinyl warmth, minor key, jazzy 7th chords, melancholic, cozy, neon nightscape,
late-night introspective, hazy, instrumental, 88 BPM
```

설계 원칙(요약): AI 음악 모델은 장면 단어보다 **장르·악기·음색 태그**를 무겁게 반영하므로
시간대를 반드시 음향 특성(조성·밝기·음역·리버브·어택·템포)으로 번역한다.
`synthwave`/`neon`/`retrowave` 앵커는 학습상 밤으로 강하게 편향돼 있어, 낮을 원하면
`city pop` 비중을 키우고 `synthwave`/`neon`을 뒤로 빼거나 제거한다.

원본 설계서 전문: `Don-t-late-bgm/afternoon-bgm.md` · `afternoon-bgm-02.md` · `night-bgm.md`

### 폐기 이력

- `afternoon-bgm-03`(60s, FLAC md5 `5a6990bb…`) · `night-bgm-03`(60s, `ba8b964b…`)
  — WAV 대응본 부재, 재확보 포기 (2026-07-21 정수 결정 — 주체 D-048 정정). FLAC 원본은 삭제됨.
- `Late_for_Work_8-Bit_Panic_2026-07-19T072529`(60s, PCM md5 `0c251eeedd11`)
  — **8비트 사운드로 나머지 곡과 분위기 불일치** (2026-07-21 정수 청취 판정 — 위임 D-045 범위, 주체 D-048 정정). 프로젝트·아카이브 양쪽에서 삭제.
  유일한 Title 슬롯 곡이었으므로 한동안 Title 슬롯 공백 — **2026-07-24 Suno 곡 `Pixel_Night_Funk_Don-T-Late`로 충원**(위 "BGM (타이틀) — Suno" 절).
- **미채택 4곡** (2026-07-21 청취 판정 — 최종 컷). 프로젝트에서 제거, 원본 아카이브(`Don-t-late-bgm/`)에는 보존:
  `Ironic_Stillness`(`6cd06cf4ba1a`) · `Pixel_Seoul_Breeze`(`3f398520c39c`) ·
  `Seoul_Pixel_Boulevard`(`4c1169ca957b`) · `Sunlit_Stroll_in_Seoul`(`4ffa4f0689f9`).
  `Ironic_Stillness`는 원본에서 낮·밤 양쪽에 중복 배치돼 있던 곡이다.

---

## BGM (타이틀) — Suno

| 항목 | 내용 |
|---|---|
| 생성 도구 | **Suno** (AI 음악 생성) |
| 계정 | Director 개인 계정 — **유료 플랜 (Pro/Premier)** (2026-07-24 반입 시 Director 확인) |
| 권리 | **상업적 사용 가능 · 소유권 사용자 귀속 · 기간 무제한.** Suno 유료 구독 중 생성한 콘텐츠는 상업 이용이 허용되며 소유권이 생성자에게 귀속된다 (무료 플랜은 비상업 + 소유권 미귀속이라 반입 불가) |
| 표기 의무 | **없음** — 유료 플랜 한정 (무료 플랜만 "Made with Suno" 표기 의무) |
| 근거 | Suno 이용약관 유료 플랜 상업 라이선스 조항 · Director 플랜 확인 (2026-07-24) |
| 반입일 | 2026-07-24 |

### 곡 목록 (Suno 타이틀 · 2곡 — 보컬본 보관 + 보컬제거본 재생)

| 파일 | 슬롯 | 길이 | 파일 MD5(앞 12) | 비고 |
|---|---|---|---|---|
| `Pixel_Night_Funk_Don-T-Late_NoVocal.wav` | **Title** | 195.6s | `02a1e5057f1a` | **현 타이틀 곡** — Suno 스템 분리 보컬제거본 |
| `Pixel_Night_Funk_Don-T-Late.wav` | Unsorted | 195.6s | `f9b29ce1614c` | 보컬본 — **보관**(2026-07-24 Director 지시, 삭제 안 함). Unsorted라 추첨 제외 |

- 2026-07-24 Director 교체 지시("보컬 없는 곡으로 교체·기존은 보관"). 보컬제거본은 **Suno 스템 분리** 산출(원곡과 동일 저작권·라이선스). `1 Lead Vocal.wav` 스템(무음비 5.8%·RMS 4352 = 인스트루멘탈, 격리 보컬본 `0 Lead Vocal` 무음비 31.9%와 대비로 확인).

- ElevenLabs 절 채택 5곡이 컷했던 Title 슬롯(구 `Late_for_Work_8-Bit_Panic` — 8비트 불일치로 폐기, Title 공백)을 이 곡이 채운다. `BgmLibrary.asset` slot=3(Title) 배선. WebGL 임포트는 AudioImportPostprocessor 자동(Vorbis · CompressedInMemory · q0.30 · 스테레오).
- 인게임 재생: 타이틀 화면(Main)은 인트로 대화까지 무음(S-009), 대화 종료 후 크로스페이드 인.
- **파일 MD5는 WAV 파일 전체 해시**(ElevenLabs 절의 PCM MD5와 계산 기준이 다름 — 디코드 없이 식별용).

---

## SFX

**실음원 미확보 · 합성 플레이스홀더 3종 가동** (D-045). `SfxSynthGenerator`가 코드로 합성하며
파일이 있으면 덮지 않는다 — **실음원을 같은 파일명으로 넣으면 그대로 교체**된다(BOM §8 스왑 계약).
합성물은 빌더가 재생성하므로 커밋 대상이 아니다.

| bom_id | 트리거 | 상태 |
|---|---|---|
| `sfx_pickup` | `PackagePickedUp` | 합성 플레이스홀더 (0.12s · 17KB) |
| `sfx_delivery_ok` | `DeliveryCompleted` | 합성 플레이스홀더 (0.55s · 54KB) |
| `sfx_late_buzzer` | `DeliveryFailed` | 합성 플레이스홀더 (0.45s · 45KB) |
| `sfx_dialogue_blip` | 대화 글자 진행 | 합성 (대화 스택 소유 — `CoreSceneBuilder`가 생성) |
| `sfx_footstep` | Locomotion 훅 | **미착수** — Player 도메인 별건 |

나머지 7종은 JUICE 개정안 **J-1 승인 게이트** 대기 중.

### 실음원 반입 — ElevenLabs Sound Effects (2026-07-22 · AU-007+AU-008)

| 항목 | 내용 |
|---|---|
| 생성 도구 | **ElevenLabs Sound Effects** (`POST /v1/sound-generation` · `output_format=pcm_44100` → WAV 래핑) |
| 계정 | 정수 개인 계정 — Creator 플랜 ($22/월, 유료) — BGM 절과 동일 계정 |
| 권리 | 상업적 사용 가능 · 기간 무제한 (유료 플랜 상업 라이선스 — BGM 절 근거와 동일, 2026-07-21 확인) |
| 표기 의무 | 없음 (유료 플랜) |
| 생성일 | 2026-07-22 (19종 일괄) |
| 착지 | `Assets/_intake/ElevenLabs/SFX/<bom_id>.wav` (발주 AU-007/008 계약 경로) |
| 재현 | 프롬프트 원본 = `scripts/audio/prompts/<bom_id>.md` · 아래 seed로 복원 가능 |
| 판정 | **사람 청취 판정 전** — `Assets/Audio/SFX/` 배치는 검증용 로컬 사본(D-042 미커밋), 채택 판정 후 커밋 해제 |

**세대 이력** (구세대 seed는 git 이력에 보존):
- 1세대(2026-07-22 21:01 · retro pixel-art 앵커) → 사람 판정: 음량 낮음·과장·8bit 부족.
- 2세대(21:24~ · 8bit/chiptune 앵커) → 사람 판정: **전량 기각**.
- 3세대(21:50~ · VA-11 HALL-A 소프트 신스) → 사람 판정: 기각.
- 4세대(JRPG 벨·차임 — **샘플 4종만**, 미전개) → 사람 판정: 기각, Director가 스펙 직지정으로 전환.
- 5세대(22:10~ · Director 스펙 직지정 — lo-fi bit-crushed 8-bit + 비트크러시 후처리) → 사람 판정: 기각.
- **6세대 (현행 · 22:30~)**: 동물의 숲 참조 — 음향 특성 번역: `cozy cute toy-like · soft wooden
  marimba · rounded synth plucks · playful little pitch bends · light and bouncy`.
  **비트크러시 후처리 끔**(토이 톤과 상극). 후처리 = 피크 -1dBFS → RMS -14dB 부스트(amb_night 제외).
  샘플 4종(pickup·box_break·coin·barcode) 사람 승인 후 전량. dialogue_blip 40ms 컷은 5세대
  스펙 전용이라 미적용(0.5s 원본 — 트림은 컷 판정 후 후공정).

| bom_id | 요청 길이 | seed | 프롬프트 SHA1 |
|---|---|---|---|
| `sfx_pickup` | 1.0s | 29411712 | `ff8928525255` |
| `sfx_delivery_ok` | 1.2s | 557024446 | `001aec94cbf0` |
| `sfx_late_buzzer` | 1.0s | 1707186366 | `53b1cecac53f` |
| `sfx_footstep` | 0.5s | 933899639 | `92dbd12fccdd` |
| `sfx_deadline_warn` | 0.8s | 1323807017 | `711dcefc740d` |
| `sfx_phone_ring` | 1.2s | 1978063182 | `8adaf71cc293` |
| `sfx_dialogue_blip` | 0.5s | 351262149 | `293af4da85f5` |
| `sfx_rhythm_hit` | 0.5s | 1869022787 | `2c31d316ad9d` |
| `sfx_rhythm_miss` | 0.5s | 458265916 | `1b9e53299464` |
| `sfx_drink` | 1.2s | 648434745 | `38a49260fea5` |
| `sfx_scene_whoosh` | 1.0s | 1133156534 | `e3dc7048a0e2` |
| `amb_night` | 5.0s | 2044289405 | `6ac7c35653fc` |
| `sfx_box_break` | 1.0s | 776020186 | `a6af784b7149` |
| `sfx_vending` | 1.2s | 125690113 | `9667ba987784` |
| `sfx_throw` | 0.6s | 695468578 | `be51e1622dc0` |
| `sfx_barcode` | 0.5s | 675090231 | `ef52b091a725` |
| `sfx_penalty` | 0.8s | 1373751068 | `5c0ba190b293` |
| `sfx_coin` | 0.6s | 142154480 | `58d0dce3f4fe` |
| `sfx_phone` | 0.5s | 1784947598 | `c64ffd4c7fa0` |

- AU-008 7종(`sfx_box_break`~`sfx_phone`)은 **BOM §8 미등재** — 발주서(AU-008 2026-07-22 19:10)가 근거.
  BOM·JUICE 행 추가는 관제 몫으로 위임(동결 문서 사람 게이트).
- 후공정(앞 무음 트림·피크 정규화)은 **사람 청취 판정 후** — GAME-SFX-RULES §6·§7 절차.

### AU-010 신규 4종 (2026-07-23 · 6세대 토이 톤 · 계정·권리는 위 표와 동일)

**세대 이력**: 1차(2026-07-23 20:27 · 장면 서술형 태그) → Director 청취 판정 기각("맥 빠짐" — satisfied/deflated/gentle 등
무기력 단어가 처진 소리로 반영). **2차(현행 · 20:50)**: 승격 19종 프롬프트 패턴 모사 — 짧은 명사구 + 음형 개수 명시
(four quick notes) + 에너지 단어(cheerful·bright·bouncy·sparkly·snappy). 1차 seed는 git 이력에 보존.

| bom_id | 요청 길이 | seed (2차) | 프롬프트 SHA1 |
|---|---|---|---|
| `sfx_settle_ok` | 1.5s | 2064277677 | 2차 재작성 |
| `sfx_settle_bad` | 1.5s | 1816447184 | 2차 재작성 |
| `sfx_furniture_place` | 0.6s→0.35s 트림 | 31843002 | 2차 재작성 |
| `sfx_ui_tick` | 0.5s→0.3s 트림 | 784741584 | 2차 재작성 |

- 후공정 적용 완료(6세대 표준): 앞 무음 트림 → 피크 -1dB → RMS -14dB (2차 실측 전종 -14.0~-15.4dB). 정산 쌍(ok/bad)은 같은 마림바 계열 상행/하행 대비(규칙 §2 쌍 규칙).

**기존 2종 교체 (2026-07-23 21:00 · Director 인게임 지목 기각 — "걷는 소리·전환음 처짐")**:
19종 중 최약체 프롬프트(soft·gentle·light 3연발)였던 2종을 에너지 패턴으로 재생성. 구세대 seed는 git 이력 보존.

| bom_id | 요청 길이 | seed (교체본) | 비고 |
|---|---|---|---|
| `sfx_footstep` | 0.5s | 652700656 | bouncy hop + woody knock — 연타 전제 dry 유지 |
| `sfx_scene_whoosh` | 1.0s | 1210195857 | 상행 스윕 + 피치 벤드 (§3 riser 예외 대상) |
- BOM §8 미등재 — 발주서(AU-010 2026-07-23 20:21)가 근거. 행 추가는 관제 몫(R16 ③에 4종 합류 요청).

### AU-011 구역 앰비언스 2종 + 지도 앱 SFX 3종 (2026-07-23 · 6세대 토이 톤 · 계정·권리는 위 표와 동일)

| bom_id | 요청 길이 | seed | 후처리 실측 |
|---|---|---|---|
| `amb_villatown` | 5.0s | 483489003 | 피크 -1dB만 (amb 선례) · RMS -22.6dB |
| `amb_foodalley` | 5.0s | 281030895 | 피크 -1dB만 · RMS -26.1dB |
| `sfx_map_pin` | 0.5s | 377050407 | 트림→피크→RMS -17.7dB (클립 가드 0.81%로 -14 미달) |
| `sfx_map_route` | 0.5s | 144456593 | 트림→피크 · RMS -13.5dB (부스트 불요) |
| `sfx_map_depart` | 0.6s | 11700560 | 트림→피크→RMS -14.0dB |

- **발주 편차 (AU-011 "루프 60s±")**: amb 2종은 **5.0s 루프**로 납품 — ① sound-generation API 실상한 22s
  ② 파이프라인 SFX 캡 5.0s(amb_night 승격 선례) ③ BGM 루트는 음악 앵커 주입이라 환경음 불가.
  반복감 기각 시 후속 = 파이프라인 캡 상향(5→22s) 재생성 제안.
- 파이프라인 수리 1건: `bom_audio.fallback()`이 미등재 `amb_*`를 bgm으로 오분류(BGM 루프 규격+시티팝 앵커 주입) → `amb_` 접두어 SFX 분류 추가.
- BOM §8 미등재 5종 — 발주서(AU-011 2026-07-23 20:59)가 근거. 행 추가는 관제 몫(R16 ③ 합류 요청).

### AU-017/AU-019 재생성 (구번호 S-054/055 — 관제 재번호) (2026-07-25 · Director 지시 · 계정·권리 위 표와 동일)

맵이동 3종 + 대사 블립을 ElevenLabs로 재생성(같은 토이톤 프롬프트 → 자체 후공정 트림·피크 -1dB·RMS -14dB → 제자리 교체, guid 불변).
⚠ **SFX는 API가 seed를 안 받아 seed로 복원 불가**(음악만 seed 복원 가능). AU-017(구 S-054) ledger의 seed는 로컬 기록일 뿐.
맵 3종은 각 5후보 생성 후 **Director 청취 선택**(AU-019 — pin_1·route_5·depart_2).

| bom_id | 출처 | 후공정 실측 |
|---|---|---|
| `sfx_dialogue_blip` | S-054 단일 | 트림→피크 -1.0dB · RMS -22.3dB(피크형 무클립) |
| `sfx_map_pin` | S-055 선택(5후보 중 pin_1) | 피크 -1.0dB · RMS -17.4dB |
| `sfx_map_route` | S-055 선택(route_5) | 피크 -3.5dB · RMS -14.0dB |
| `sfx_map_depart` | S-055 선택(depart_2) | 피크 -2.2dB · RMS -14.0dB |

### scene_whoosh cand16 교체 (2026-07-25 · Director 지시 · 계정·권리 위 표와 동일)

전환음(`sfx_scene_whoosh`)을 **레트로 city chiptune**(멜로우·재즈 없음)으로 교체 — 기존 토이톤/상행스윕 폐기.
17후보 생성 후 **Director 청취 선택(cand16)**. ⚠ SFX는 API가 seed를 안 받아 **seed 복원 불가** — 프롬프트로 재생.
제자리 교체(guid 불변 `3e4e0175…`). 후공정 = 피크 -1.0dB (RMS 미부스트 — 필요 시 후속).

| bom_id | 출처 | 프롬프트 SHA1 | 후공정 |
|---|---|---|---|
| `sfx_scene_whoosh` | 17후보 중 cand16 (Director 선택) | `217dec5cb9a8` | 피크 -1.0dB |

### AU-018 ④ 배송지 도착 차임 (2026-07-26 · Director 지시 "④ 이어서 진행" · 계정·권리 위 표와 동일)

빈 상호작용(배송지 진입)에 도착 차임 신규 배선. 토이톤 프롬프트로 ElevenLabs 생성 → 자체 후공정
(trim → g=min(RMS→-14dB, peak→-1dB) 무클립). ⚠ **SFX는 API가 seed를 안 받아 seed 복원 불가**(seed는 로컬 기록).
배선: `WorldAudioManager.OnSceneTransitionCompleted`에서 District·Apartment·Hillside 진입 시 발화(whoosh=떠남과 짝).
※ 같은 세션 초안 5종(travel_loop/ping·loading_tick/done·arrive 중 arrive만) 중 arrive만 채택 — 나머지 4종은
이동/로딩 연출 UX 부재로 **보류**(Director 결정). map_depart 재생성본은 폐기, 기존 인게임 파일 유지.

| bom_id | 출처 | 프롬프트 SHA1 | seed(로컬) | 후공정 실측 |
|---|---|---|---|---|
| `sfx_arrive` | 단일 생성 (0.6s→trim 0.46s) | `f0d1f91dd067` | 838478889 | 피크 -1.0dB · RMS -14.8dB (peak-limited) |

### AU-018 ③ 액션 SFX (2026-07-27 · Director 지시 "5종 이어서 진행" · 계정·권리 위 표와 동일)

액션 3종 신규 배선 + box_damage 훅(오디오 후속). 토이톤 프롬프트로 ElevenLabs 생성 → 자체 후공정
(trim → g=min(RMS→-14dB, peak→-1dB) 무클립). ⚠ SFX는 API가 seed 미수용 → seed 복원 불가(로컬 기록).
- 배선: `sfx_jump`=Locomotion 점프 · `sfx_land`=착지 엣지 · `sfx_footstep_snow`=WorldWeatherManager.HasSnowCover(>0.25) 전환 시 발소리 스왑(SnowCoverChanged 이벤트 경유).
- **box_damage**: 토이톤(marimba)이 충격음과 충돌해 3회 근무음(RMS -44~-48) → **--raw로 조립기 우회**(비토이톤 임팩트 프롬프트)로 재생성 채택(Director 결정). 크런치는 노이즈 질감이라 부스트 자연.
- **box_roll 폐기**: 굴림 감지 시스템(신규 컴포넌트) 필요 — 배선 홈 부재, YAGNI로 폐기(Director 결정).

| bom_id | 출처 | 프롬프트 SHA1 | seed(로컬) | 후공정 실측 |
|---|---|---|---|---|
| `sfx_jump` | 단일 생성 (0.5s) | `a85292a7aa8a` | 990680310 | 피크 -1.0dB · RMS -16.1dB |
| `sfx_land` | 단일 생성 (0.5s→trim 0.25s) | `b89fd307e1c3` | 1251878742 | 피크 -1.0dB · RMS -17.6dB |
| `sfx_footstep_snow` | 단일 생성 (0.5s→trim 0.28s) | `69144d354128` | 27761564 | 피크 -1.0dB · RMS -20.5dB |
| `sfx_box_damage` | --raw 비토이톤 (0.5s→trim 0.22s) | `09be3099e6cc` | 70264330 | 피크 -1.0dB · RMS -16.1dB |

### AU-018 ① 날씨 앰비언스 3종 (2026-07-27 · 남규님 지시 · 계정·권리 위 표와 동일)

Rain·Snow·Heat 날씨별 앰비언스 루프 베드. **사실적 환경음**이라 토이톤 앵커를 뺀다(`--no-anchors` —
compose_sfx가 앵커를 존중하도록 파이프라인 수정). API 상한 22s를 넘기려 **2테이크 등파워 크로스페이드
스티칭 → 심리스 루프 랩**(scratchpad `stitch_amb.py`)으로 **40s 루프** 제작(D-068 "≥30s·클립 내 무반복" 정신 승계).
후공정 = RMS -20dB 타겟 + 피크 -1dB 소프트리밋(3종 라우드니스 일관). ⚠ SFX는 API가 seed 미수용 → 복원 불가(로컬 기록).

- **눈은 본래 조용** — 1차 "hushed stillness" 태그가 API를 무음(RMS -53)으로 유도 → 바람 중심 태그로 재생성(가청).
- **캡 해제**: `prompt_builder`의 SFX 5.0s 캡을 `amb_*` 한정 22s로 상향(AU-012 의도 승계) · `compose_sfx`가 `--no-anchors` 존중.
- **임포터**: `amb_*`(긴 루프)는 DecompressOnLoad 대신 CompressedInMemory+저비트레이트(40s×3 RAM 낭비 방지).

| bom_id | 출처 | 프롬프트 SHA1 | seed(로컬·take B) | 후공정 실측(스티칭 후) |
|---|---|---|---|---|
| `amb_weather_rain` | 22s×2 스티칭 → 40s 루프 | `b53475782b7e` | 2057602207 | 피크 -1.7dB · RMS -22.5dB |
| `amb_weather_snow` | 22s×2 스티칭 → 40s 루프 (바람 중심 재생성) | `08c6d9f90402` | 915216587 | 피크 -1.3dB · RMS -20.5dB |
| `amb_weather_heat` | 22s×2 스티칭 → 40s 루프 | `943f6741648a` | 605911711 | 피크 -4.8dB · RMS -20.9dB |

## BGM (날씨 · AU-018 ②) — Suno · 2026-07-27

날씨(Rain·Snow·Heat·Fog) 무드 BGM 4곡. Director가 곡 목록(`planning/audio-weather-bgm-songlist.md`)의
Suno 프롬프트로 직접 생성 → 공장이 반입·트림·배선.

| 항목 | 값 |
|---|---|
| 생성 도구 | **Suno** (AI 음악 생성) |
| 계정/플랜 | Director 유료 플랜 (Pro/Premier) |
| 권리 | **상업적 사용 가능 · 소유권 생성자 귀속 · 기간 무제한** (유료 플랜 상업 라이선스) |
| 표기 의무 | **없음** (유료 플랜 한정) |
| 근거 | Suno 이용약관 유료 플랜 상업 라이선스 조항 · Director 플랜 확인 (2026-07-24 동일 근거) |

### 곡 목록 (원제 유지 · 48kHz/16bit/stereo · 루프용 페이드 트림 후)

| 파일명 | 날씨 | 길이(트림후) | 후처리 |
|---|---|---|---|
| `Neon Rain.wav` | Rain(밤) | 147.9s → 145.6s | 인트로 페이드인 0.3s + 아웃트로 페이드아웃 2.0s 트림 (풀레벨 루프 경계) |
| `Rain on the Window.wav` | Rain(낮) | 152.9s → 148.6s | 아웃트로 페이드아웃 4.1s 트림 + 컷엣지 15ms 마이크로페이드 (AU-025) |
| `Neon Snowfall.wav` | Snow(밤) | 84.2s → 82.4s | 아웃트로 페이드 1.8s 트림 |
| `Daylight Snowfall.wav` | Snow(낮) | 52.8s → 49.3s | 아웃트로 페이드아웃 3.5s 트림 + 컷엣지 15ms 마이크로페이드 (AU-026) |
| `Heatwave Afternoon.wav` | Heat(낮) | 60.1s → 59.9s | 미세 트림(거의 플랫) · **원제 `Midnight Heatwave`** (AU-032 개명) |
| `Heatwave Night Drive.wav` | Heat(밤) | 86.1s → 82.5s | 아웃트로 페이드아웃 3.6s 트림 + 컷엣지 15ms 마이크로페이드 (AU-032) · **원제 `Sunny Afternoon Drive`** |
| `Pale White Haze.wav` | Fog(낮) | 149.6s → 143.5s | 아웃트로 페이드아웃 6.1s 트림 + 컷엣지 15ms 마이크로페이드 (AU-032) · 원제 유지 |
| `Sodium Fog.wav` | Fog(밤) | 65.2s → 65.2s | 트림 불요(플랫) |

- **트림 이유**: Suno 곡은 앞뒤 페이드가 붙어 루프하면 이음새에서 음량이 꺼진다. 바디 RMS(-17dB) 대비
  -5dB 아래 앞뒤 램프를 잘라 풀레벨 경계로 만들고(scratchpad `trim_bgm.py`), 컷 엣지 15ms 마이크로페이드(클릭 방지).
  플레이리스트 3s 크로스페이드가 풀레벨↔풀레벨을 섞어 매끄럽게 루프. 원본은 Downloads 보존.
- **배선**: `WeatherChanged` 구독 → 날씨 ∈ {Rain·Snow·Heat·Fog} 이고 곡 있으면 시간대(낮/밤) 슬롯을 override,
  Clear·Cloudy는 기존 Day/Night 곡 유지(amb 우선순위와 동형). 단곡이라 PlaylistTick 셀프 크로스페이드로 루프.
- **비만 낮/밤 분리 (AU-025 · 2026-08-01)**: Rain은 `_bgmRainDay`(Rain on the Window)/`_bgmRainNight`(Neon Rain)
  2곡. `WorldAudioManager`가 `_phase`(Evening·Night→밤곡, else→낮곡) 참조로 선택하고 `DayPhaseChanged`에서도
  재평가 → 비 오는 중 낮↔밤 전환 시 곡이 크로스페이드로 교체된다. 폭염·안개는 여전히 낮밤 공용 1곡.
- **눈도 낮/밤 분리 (AU-026 · 2026-08-01)**: Snow는 `_bgmSnowDay`(Daylight Snowfall)/`_bgmSnowNight`(Neon Snowfall)
  2곡. 비와 동일 로직(`_phase` 참조 + `DayPhaseChanged` 재평가).
- **폭염·안개도 낮/밤 분리 (AU-032 · 2026-08-08)**: Heat는 `_bgmHeatDay`(Heatwave Afternoon)/
  `_bgmHeatNight`(Heatwave Night Drive), Fog는 `_bgmFogDay`(Pale White Haze)/`_bgmFogNight`(Sodium Fog).
  이로써 **곡이 있는 날씨 4종 전부 시간대 분리 완료** — 남은 것은 Storm(곡 없음, 시간대 슬롯 유지).
  - **개명 2건**: 신곡 원제 `Sunny Afternoon Drive`가 실제로는 **밤** 곡이고(Director 청취 판정),
    분리하면 `Midnight Heatwave`가 **낮** 곡이 되어 둘 다 이름이 역할과 반대가 됐다. 이름 교환(덮어쓰기)
    대신 충돌 없는 새 이름 2개로 정리했고 원제는 이 표에 병기해 라이선스 추적선을 유지한다.
- 라우드니스: 4곡 rms -16.6~-17.2dB(Suno 자체 정규화 일관) · peak -3.2~-3.8dB. 정규화 불요.

## sfx_footstep 교체 (걷는 소리 재생성) — ElevenLabs · 2026-07-28

Director 판정: 기존 걷는 소리가 **쇳소리(metallic)** — 원인 = 토이 앵커의 synth pluck 고역 링.
후보 다수 청취 끝에 **rubber sole 클린 탭**(`--no-anchors`, 비금속·비grit·중역 존재감) 채택.
같은 파일명 덮어씀(guid 불변 = 코드·씬 재작업 0). ⚠ SFX는 API가 seed 미수용 → 복원 불가(seed 로컬 기록).

- 채택 프롬프트(창작 태그): `single rubber sole footstep, soft firm step with a clean dry tap, present and smooth, no scuff no ring`
- prompt SHA1 `b529abed6cc1` · seed(로컬) 1957209730 · 생성 0.5s → 트림 0.22s
- 후공정: 트림 → 소프트 컴프레션(tanh, 크레스트 감소=존재감) → 피크 -1dB. 임포트 실측 rms **-9.4dB**(가청 확보 — 저역 muffled 후보들이 작은 스피커서 안 들린 문제 해결).
- 탐색 이력: 저역 thud 계열(boot/concrete/muffled)은 스피커서 약함 · 거친 scuff는 가청이나 거슬림 → "중역 클린 탭"이 가청∧매끈∧비금속 교점.

## AU-020 교통사고 SFX (끼익!!쿵!) — ElevenLabs · 2026-07-29

차에 치일 때(`TrafficCar`) 타이어 스키드 → 충돌 임팩트 연속음 1클립. **비토이톤**(`--no-anchors` — 충격음은
토이 앵커와 충돌, box_damage 선례). 3 take 생성(seed 상이) → **Director 청취 판정 take1 채택**.
⚠ SFX는 API가 seed 미수용 → seed 복원 불가(로컬 기록).

- 채택 프롬프트(창작 태그): `sudden tire skid screech, then heavy car collision crash, crunching metal impact and low thud`
- prompt SHA1 `21e1a3ebf762` · seed(로컬·take1) 411312833 · 생성 1.36s → 트림 1.02s
- 후공정: stereo→mono → 트림(≤-40dBFS) → 피크 -1.0dB 정규화(트랜지언트라 peak 한계) → 페이드(in 2ms/out 20ms). 임포트 실측 rms -11.9dB.
- 배선: `WorldAudioManager._sfxCarCrash` 소켓 기시공(S-066 ③) → `TrafficCar` 치임 시 `PlayCarCrashSfx()`. CoreSceneBuilder `LoadSfx("sfx_car_crash")` 자동 배선(클립 로드 실증 1.02s·mono). 클립 도착 전 무음.
- 미채택: take2(1.36s 최대밀도·여운 김) · take3(0.73s 즉발 펀치).

## AU-022 천둥 SFX (럼블+크랙) — ElevenLabs · 2026-07-29

비·태풍 중 번개 섬광과 동시 재생(`WorldWeatherManager.ThunderFlash` S-088 ⑥). **비토이톤**(`--no-anchors`).
방향 판정: 초안 3 take(distant rumble 계열) **Director 전량 기각** → 프롬프트 단계 재협의 → **A안 "크랙 선행"**
채택(섬광 동기엔 날카로운 어택이 선행해야 번쩍+쩍이 한 방에 인지). 3 take → **Director 청취 판정 take2 채택**.
⚠ SFX는 API가 seed 미수용 → seed 복원 불가(로컬 기록).

- 채택 프롬프트(창작 태그): `sharp electric thunderclap crack with a bright snapping attack, then a deep rolling rumble tail, powerful and close`
- prompt SHA1 `18a164910131` · seed(로컬·take2) 1946017365 · 생성 2.20s → 트림 1.31s(꼬리 자연 감쇠 컷)
- 후공정: stereo→mono → 트림(≤-45dBFS) → 피크 -1.0dB(크랙 트랜지언트 peak 한계) → 페이드(in 1ms 어택 보존/out 40ms 럼블 꼬리). 최종 1.31s 모노 · rms -12.6dB.
- 배선: `WorldAudioManager._sfxThunder` 소켓 기시공(S-088 ⑥) → `WorldWeatherManager.ThunderFlash()`에서 `PlayThunderSfx()`(섬광과 동시). CoreSceneBuilder `LoadSfx("sfx_thunder")` 자동 배선(클립 로드 실증 1.31s·mono). 클립 도착 전 무음(섬광만).
- 미채택: take1(2.20s 럼블 꼬리 긴 차분) · take3(2.20s 중간).
## sfx_fanfare (개척 해금 팡파레) — ElevenLabs · 2026-07-29

S-086 소켓(WorldAudioManager `_sfxFanfare`) 충전 — 정산 개척 해금/트럭 지급 순간 재생.
팡파레는 축하 멜로디 스팅 본질 → GAME-SFX-RULES §3 melody/jingle 금지의 의도적 예외(whoosh riser 선례).
3 take 생성 → Director 청취 판정 **take1 채택**(차분·성김).

- 채택 프롬프트(창작 태그): `triumphant toy fanfare, quick rising marimba run into a sparkling bell chime and final ding, bright chiptune brass stab, celebratory grand yet cute` (API 450자 한계로 승인 B에서 트림 — 요소 전부 유지)
- seed(로컬) 1699124614 · 생성 2.0s → 트림 0.94s. ⚠ SFX는 API가 seed 미수용 → 복원 불가(로컬 wav 보존).
- 후공정: 트림(≤-40dBFS) → peak -1dB 정규화(트랜지언트라 peak 한계 · RMS -19.4 < -14는 규칙상 정상) → 8ms 페이드아웃. 모노 pcm44100.
- 배선: #21(S-086) 머지 후 CoreSceneBuilder `LoadSfx("sfx_fanfare")`가 자동 주입. 도착 전엔 settle_ok 폴백.

## AU-023 엔딩 BGM (bgm_ending) — Suno · 2026-07-31

엔딩 전용 BGM. 출처 = **Suno**(Director 유료 Pro/Premier) — 상업 사용 가능·소유권 귀속·무기한·표기 불요.
프롬프트 = **방향5(그윈 오마주·Rhodes 피아노 리드)**. 레퍼런스: Dark Souls 1 "Gwyn, Lord of Cinder"(솔로 피아노·다이어토닉·루바토·밀도 아크·비극미) — **형식만 계승, 감정은 온기로 전환**(비극→따뜻한 이별+감사). Director 청취 채택: 피아노 메인 생존 + 애잔 톤.

- 파일: `Fading Into Dawn.wav` (원제 유지) · 149.8s · 48kHz 스테레오 · MD5(앞12) `f0859b4bb526`.
- 프롬프트:
  `bittersweet warm ending ballad, piano-led with a soulful retro touch, warm Rhodes electric piano as the main lead, gentle acoustic piano accents, soft warm analog pads, mellow round bass, brushed drums with a laid-back groove, subtle warm strings swelling only at the peak, jazzy major 7th and add9 chords, tender rubato intro settling into a slow gentle groove, farewell and gratitude turning to hope, minor lament resolving to a warm major, soft hall reverb with space, nostalgic retro city game ending, slow ~72 BPM, instrumental`
  Exclude: `vocals, epic bombast, brass fanfare, aggressive drums, edm drop, harsh, cold, dark minor, sudden ending`
- **원샷(A) 곡**: 인트로 성김(-20dB)→정점 75~85s(-14dB)→아웃트로 페이드아웃 145~150s(-31dB). 날씨곡과 달리 페이드 트림 안 함(페이드=아크의 일부). 실측 peak -3.2dB · rms -17.1dB — 다이내믹=감정 핵심이라 평탄화·정규화 안 함.
- 임포트: Vorbis q30 · Compressed In Memory · 스테레오(BGM 규격 자동, 실측 검증 loadType=CompressedInMemory).
- 배선: **엔딩 원샷 재생 소켓(S-107)은 관제 몫**(남규). BgmLibrary Ending 슬롯 등재 + WorldAudioManager 엔딩 훅 배선 필요. 배선 전엔 무음.

## AU-027 sfx_level_up (레벨업) — ElevenLabs · 2026-08-05

S-174 ③ 소켓(WorldAudioManager `_sfxLevelUp`) 충전 — `PlayerLeveledUp` 이벤트에 재생.
발주 사양은 "팡파레보다 작고 짧게" — 레벨업은 정산 화면에 자주 나와 축포급이면 물린다.

- 채택 프롬프트(창작 태그): `short level up chime, three quick ascending notes, bright cheerful chiptune arpeggio, tiny sparkle tail` + SFX 토이톤 앵커(마림바·둥근 신스 플럭), 요청 길이 0.9s
- **16 take 생성 → Director 청취 2라운드**. 1차 7 take(1.2s·0.9s 혼합) 중 4종 발신 → "B(0.59s)가 낫다" → B 프롬프트 고정하고 4계열 변주 9 take 재생성 → **V02 채택**(V0 계열 = B와 완전 동일 프롬프트의 다른 take).
- ⚠ SFX는 API가 seed 미수용 → 같은 프롬프트도 매 gen 새 결과, 복원 불가(로컬 wav가 정본).
- 실측: 0.57s · 모노 pcm44100 · peak -3.5dB · rms -14.0dB(**밀집음이라 RMS가 한계** — peak가 -1에 못 미치는 건 규칙상 정상). 후공정 = 트림(≤-40dBFS) → 단일게인 min(RMS→-14dB, peak→-1dB) = -3.5dB → 8ms 페이드아웃.
- 발주서 반증 1건: "파일만 넣으면 자동으로 울린다"는 **거짓이었다** — 이벤트 체인(MasteryProgress→Raise→구독→PlaySfx)은 전부 실재했으나 `CoreSceneBuilder`에 `LoadSfx("sfx_level_up")` 주입 라인이 없어 재빌드해도 null 유지. 본 PR에서 1줄 추가.

## AU-028 sfx_tutorial_step (튜토리얼 단계 성공) — ElevenLabs · 2026-08-07

S-162 튜토리얼 미션 카드가 초록으로 바뀌는 순간에 1회. 9단계 내내 반복되므로 발주 사양이
"축하보다 **확인**에 가깝게" — 팡파레(AU-021 개척 해금)와 겹치면 안 된다.

- 채택 프롬프트(창작 태그): `two-note ascending confirm chime, soft wooden marimba, crisp and short, bright and light`
  + SFX 토이톤 앵커(마림바·둥근 신스 플럭), 요청 길이 0.5s
- **10 take 생성 → Director 청취 2라운드**. 1차 3계열 6 take(A 마림바 / B 글로켄슈필 / C 칩튠) 발신
  → **A 계열 지목** → A 프롬프트를 고정하고 4 take 추가(태그 변주 없음 — AU-027 교훈) → **A6 채택**.
  AU-027과 같은 결론: 계열이 정해지면 태그를 흔들기보다 같은 프롬프트로 take를 더 뽑는 쪽이 이긴다.
- ⚠ SFX는 API가 seed를 받지 않는다(`elevenlabs_client` SFX 경로 body = `{text, duration_seconds}` — seed 미전송).
  gen.json의 seed는 클라이언트 기록일 뿐 복원에 쓸 수 없다. **로컬 wav가 정본**.
- 실측: 0.259s · 모노 pcm44100 · peak -1.0dB · rms -16.1dB(트랜지언트라 **peak가 한계** — rms가 -14에 못
  미치는 건 규칙상 정상). 후공정 = 트림(≤-40dBFS) → 단일게인 min(RMS→-14dB, peak→-1dB) = +4.0dB → 8ms 페이드아웃.
- 발주 사양 이탈 1건: **길이 0.3~0.6s 미달**(채택본 0.259s). 1차 후보 중 사양 정합은 A2(0.37s)·A3(0.48s)였으나
  Director 청취에서 짧은 A6이 채택됐다. AU-027과 동일 — 발주 의도("짧고 가볍게, 확인에 가깝게")가
  수치 사양보다 우선한 판정으로 기록한다.
- 소켓 4겹은 **도착 전부터 전부 실재**했다(AU-027 반증 이후 관제가 채워 둠): `_sfxTutorialStep` 필드 ·
  `PlayTutorialStepSfx()` · `TutorialMissionCardView.OnStepCleared` 발화 · `CoreSceneBuilder`의
  `LoadSfx("sfx_tutorial_step")` 주입. 본 PR은 코드 변경 없이 파일만 채운다.
