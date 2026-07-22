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
  유일한 Title 슬롯 곡이었으므로 **Title 슬롯은 현재 공백** — Unsorted 5곡 중에서 재지정 필요.
- **미채택 4곡** (2026-07-21 청취 판정 — 최종 컷). 프로젝트에서 제거, 원본 아카이브(`Don-t-late-bgm/`)에는 보존:
  `Ironic_Stillness`(`6cd06cf4ba1a`) · `Pixel_Seoul_Breeze`(`3f398520c39c`) ·
  `Seoul_Pixel_Boulevard`(`4c1169ca957b`) · `Sunlit_Stroll_in_Seoul`(`4ffa4f0689f9`).
  `Ironic_Stillness`는 원본에서 낮·밤 양쪽에 중복 배치돼 있던 곡이다.

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

**세대 이력**: 1세대(2026-07-22 21:01 · retro pixel-art 앵커) → **사람 판정: 음량 낮음·과장·8bit 부족**
→ **2세대 재생성**(같은 날 21:24~ · 8bit/chiptune 앵커 + 절제 태그 + 후처리: 피크 -1dBFS 정규화
→ RMS -14dB 부스트(클립 ≤1% 관리 · amb_night는 배경이라 피크 정규화만) — 볼륨 샘플 승인 후 전량 적용).
1세대 seed는 git 이력에 보존. ⚠ 일부 항목은 재조립 중 길이가 기본값 2.0s로 생성됨 — 여분 꼬리는
컷 판정 후 트림 대상(프롬프트 md의 요청 길이는 원복 완료).

| bom_id | 길이(요청→실측) | seed | 프롬프트 SHA1 |
|---|---|---|---|
| `sfx_pickup` | 2.0s→2.0s | 716063497 | `37bbfda3456c` |
| `sfx_delivery_ok` | 2.0s→2.0s | 847665090 | `f935466e104c` |
| `sfx_late_buzzer` | 2.0s→2.0s | 97751457 | `77ab3fad1a29` |
| `sfx_footstep` | 0.5s→0.48s | 1657813957 | `fed69c1af383` |
| `sfx_deadline_warn` | 0.8s→0.8s | 294880740 | `737af8b2af20` |
| `sfx_phone_ring` | 2.0s→2.0s | 805364262 | `19b323d2bb42` |
| `sfx_dialogue_blip` | 2.0s→2.0s | 43152987 | `31225c48af11` |
| `sfx_rhythm_hit` | 2.0s→2.0s | 1236020364 | `d0c3ab150645` |
| `sfx_rhythm_miss` | 2.0s→2.0s | 1376692253 | `a61afdb32cc2` |
| `sfx_drink` | 2.0s→2.0s | 96405065 | `8a6d5157bbbd` |
| `sfx_scene_whoosh` | 2.0s→2.0s | 1659543443 | `78608146bd98` |
| `amb_night` | 2.0s→2.0s | 966281685 | `b50a4f26f1f7` |
| `sfx_box_break` | 1.0s→1.0s | 61818053 | `dbe8a66ef3a0` |
| `sfx_vending` | 2.0s→2.0s | 740942144 | `c4ab202bb71f` |
| `sfx_throw` | 2.0s→2.0s | 369256461 | `cf135838455d` |
| `sfx_barcode` | 0.5s→0.48s | 2076524728 | `ba21bacfaaf0` |
| `sfx_penalty` | 2.0s→2.0s | 252797062 | `ee3bae5f2425` |
| `sfx_coin` | 0.6s→0.6s | 50546780 | `ee9ac3cc7aed` |
| `sfx_phone` | 0.5s→0.48s | 2118114110 | `4df6f7770dbc` |

- AU-008 7종(`sfx_box_break`~`sfx_phone`)은 **BOM §8 미등재** — 발주서(AU-008 2026-07-22 19:10)가 근거.
  BOM·JUICE 행 추가는 관제 몫으로 위임(동결 문서 사람 게이트).
- 후공정(앞 무음 트림·피크 정규화)은 **사람 청취 판정 후** — GAME-SFX-RULES §6·§7 절차.
