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

### S-053 대사 블립 8비트 변주 2종 (2026-07-25 · 계정·권리는 위 SFX 표와 동일)

| 파일 | 길이 | 비고 |
|---|---|---|
| `sfx_dialogue_blip_1.wav` | 0.48s | ElevenLabs SFX — 8비트 rapid_stepped (원명 `rapid_stepped_8-bit__#2`) |
| `sfx_dialogue_blip_2.wav` | 0.48s | ElevenLabs SFX — 8비트 rapid_stepped (원명 `rapid_stepped_8-bit__#3`) |

- Director 지시(2026-07-25): 8비트 블립 2종을 대사 효과음에 적용. DialogueView `_blipClips` 풀로 배선해 **문자마다 랜덤**(피치 0.95~1.05 변주 유지). 기존 `sfx_dialogue_blip.wav`는 삭제 없이 `_blipClip` 폴백으로 **보관**.
- 라이선스 = ElevenLabs SFX 유료 플랜(위 표와 동일 계정·권리 — 상업 가능·표기 불요). 임포트 = SFX 규격(Vorbis·Decompress On Load·모노 강제).
