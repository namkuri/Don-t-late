# Assets/Audio — 출처·라이선스 대장

> `pipelines/audio.md` §2-2: **확보 즉시 라이선스 기록 = 입장권** (누락 = 반입 차단, 교정 불가).
> 이 파일은 커밋 대상. 음원 원본(`*.wav`)은 컷 판정 전까지 `.gitignore`로 제외한다 (D-042).

---

## BGM — Eleven Music (ElevenLabs)

| 항목 | 내용 |
|---|---|
| 생성 도구 | **Eleven Music** (ElevenLabs) |
| 계정 | Director 개인 **유료 구독** |
| 권리 | **상업적 사용 가능 · 기간 무제한.** 유료 구독 중 생성한 콘텐츠는 상업 이용 가능하며, 전 유료 플랜에 상업 라이선스가 포함된다 (Beta Services 사용 시 제외). Eleven Music은 레이블·퍼블리셔·아티스트 협업으로 제작돼 **게이밍을 포함한 거의 모든 상업 용도에 클리어**되어 있다 |
| 표기 의무 | **없음** — "Eleven Music" 표기 의무는 무료 플랜 한정 |
| 근거 | ElevenLabs Help Center "Can I publish the content I generate on the platform?" · ElevenLabs Docs "Eleven Music" (2026-07-21 확인) |
| 생성일 | 2026-07-19 ~ 2026-07-20 (파일명 타임스탬프) |
| 반입일 | 2026-07-21 |

### 곡 목록 (10곡 · 48kHz / 16bit / stereo PCM)

파일명 = ElevenLabs 원제 + 생성 타임스탬프. `bom_id` 리네임은 컷·분류 확정 후 (D-042).

| 파일 | 길이 | PCM MD5(앞 12) | 원본 분류 |
|---|---|---|---|
| `Ironic_Stillness_2026-07-20T145653.wav` | 60.0s | `6cd06cf4ba1a` | 낮·밤 양쪽에 중복 배치돼 있었음 |
| `Sunlit_Seoul_Afternoon_2026-07-20T154627.wav` | 60.0s | `e12de724acbd` | 낮 |
| `Seoul_Alley_Reflection_2026-07-20T161148.wav` | 60.0s | `3194df4c88a7` | 낮 |
| `Breezy_Town_Stroll_2026-07-20T161422.wav` | **180.0s** | `93c74a16b7f6` | 낮 |
| `Seoul_Afternoon_Stroll_2026-07-20T155537.wav` | 60.0s | `be66e3688257` | 밤 |
| `Late_for_Work_8-Bit_Panic_2026-07-19T072529.wav` | 60.0s | `0c251eeedd11` | 미분류 — 8비트, Title 후보 |
| `Pixel_Seoul_Breeze_2026-07-19T103036.wav` | 60.0s | `3f398520c39c` | 미분류 |
| `Seoul_Pixel_Breeze_2026-07-19T103406.wav` | 60.0s | `5427eddf1af7` | 미분류 |
| `Seoul_Pixel_Boulevard_2026-07-19T103537.wav` | 60.0s | `4c1169ca957b` | 미분류 |
| `Sunlit_Stroll_in_Seoul_2026-07-20T154103.wav` | 60.0s | `4ffa4f0689f9` | 미분류 |

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
  — WAV 대응본 부재, 재확보 포기 (2026-07-21 Director 결정). FLAC 원본은 삭제됨.

---

## SFX

미확보. `BOM.md` §8 참조 — 필수 4종(`sfx_delivery_ok`·`sfx_late_buzzer`·`sfx_pickup`·`sfx_footstep`)은
발주 가능, 나머지 7종은 JUICE 개정안 **J-1 승인 게이트** 대기 중.
