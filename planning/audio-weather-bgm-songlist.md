# 날씨 BGM 필요 곡 목록 (AU-018 ②) — Director Suno 수동용

> 작성: 정수 공장 2026-07-27. **공장은 BGM 생성 안 함**(D-055 · 메모리) — 이 문서는 Director가
> Suno로 직접 뽑을 **곡 목록 + 바로 넣을 프롬프트 초안 + 통합 설계**다. 곡이 확보되면 배선은 별건.
> 스타일 정본: `scripts/audio/rules/GAME-BGM-RULES.md`(루프·반복내성 규격 — 우선) ·
> `night-bgm.md`(밤 무드) · `afternoon-bgm-02.md`(낮 무드). 코어 DNA = VA-11 HALL-A(Garoad) 시티팝×신스웨이브×lo-fi.

## 원칙 — 날씨는 "새 장르"가 아니라 기존 팔레트의 무드 틴트

- 게임 BGM 세계(시티팝/신스웨이브/lo-fi)는 그대로 두고 **날씨가 무드만 물들인다.** 장르를 바꾸지 않는다.
- 날씨 곡은 **시간대(낮/밤) 슬롯을 덮어쓴다** — 비 오는 날은 낮이든 밤이든 "비 무드"가 지배. 그래서
  날씨당 **1곡이면 최소선 충족**(낮·밤 분리는 뒤로 미룰 수 있는 확장 — 아래 §확장).
- Clear·Cloudy = **기존 Day/Night 곡 그대로**(신곡 불요). 날씨 곡은 체감 큰 날씨만.

## 곡 목록

### 필수 (P1 — 발주 최소선 "비/눈 무드 변주 ≥1")

| bom_id 후보 | 날씨 | 무드 한줄 | 슬롯 규칙 |
|---|---|---|---|
| `bgm_rain` | Rain | 젖은 창가, 멜랑콜리 lo-fi — 게임 톤(밑바닥·웃픔)과 가장 잘 붙는다 | Rain 진입 시 Day/Night 덮어씀 |
| `bgm_snow` | Snow | 포근한 겨울, 따뜻한 벨·패드 — 정적 속 온기 | Snow 진입 시 덮어씀 |

### 권장 (P2 — 여유 되면)

| bom_id 후보 | 날씨 | 무드 한줄 | 슬롯 규칙 |
|---|---|---|---|
| `bgm_storm` | Storm | 태풍 — 어둡고 긴장된 압박, 강풍·천둥(AU-022) 아래 무겁게 조인다 (**체감 최대 — P2 최우선**) | Storm 진입 시 덮어씀 |
| `bgm_heat` | Heat | 나른한 폭염, 아지랑이로 늘어진 여름 오후 | Heat 진입 시 덮어씀 |
| `bgm_fog` | Fog | 안개 속 정적, 옅고 몽롱·불안 (드럼 최소/무드럼) | Fog 진입 시 덮어씀 |

---

## Suno 프롬프트 초안 (그대로 투입 가능 · instrumental·BPM·네거티브 포함)

> 규격 준수: 상시 BGM이라 `catchy hook`·`energetic`·`bright uplifting` 금지(GAME-BGM-RULES §3).
> ⚠ **루프 난이도**: `dusty`·`vinyl warmth`·`hazy`는 긴 잔향+지속 노이즈라 루프 이음새 🔴(§4) —
> 무드상 유지하되, 확보 후 앞뒤 이음새 크로스페이드/트림 편집 필요. 곡 끝 페이드아웃 없이 뽑을 것.

**`bgm_rain` (Rain · 필수)**
```
downtempo synthwave city pop, lo-fi, warm analog synth pads, round mellow synth bass, dreamy nostalgic lead synth, dusty laid-back drum machine, soft bell tones, vinyl warmth, minor key, jazzy 7th chords, melancholic, rainy window mood, muted and hazy, gentle introspective, instrumental, 84 BPM, no vocals, no EDM drops, no bright uplifting
```

**`bgm_snow` (Snow · 필수)**
```
downtempo city pop, lo-fi, warm analog synth pads, round mellow synth bass, soft twinkling bell tones, gentle nostalgic lead synth, brushed soft drum machine, cozy warm, wintry stillness, tender comforting, major-leaning with jazzy 7th chords, instrumental, 80 BPM, no vocals, no EDM drops, no aggressive drums
```

**`bgm_storm` (Storm · 권장 최우선 — 체감 최대)**
```
dark downtempo synthwave city pop, lo-fi, brooding low analog synth pads, deep heavy synth bass, sparse tense lead synth, distant rolling low rumble, restrained pulsing drum machine, ominous foreboding, oppressive stormy tension, cold muted, minor key with dissonant jazzy 7th chords, instrumental, 78 BPM, no vocals, no EDM drops, no bright uplifting, no catchy hook
```

**`bgm_heat` (Heat · 권장)**
```
very downtempo synthwave city pop, lo-fi, warm analog synth pads, slow round synth bass, shimmering hazy lead synth, sparse laid-back drum machine, drowsy sultry, heat-warped summer afternoon, languid dreamy, minor key with jazzy 7th chords, instrumental, 76 BPM, no vocals, no EDM drops, no bright uplifting
```

**`bgm_fog` (Fog · 권장)**
```
ambient downtempo synthwave, lo-fi, sparse warm synth pads, deep soft synth bass, distant reverberant lead synth, minimal drumless texture, mysterious muted, foggy uneasy stillness, cold hazy, minor key, instrumental, 74 BPM, no vocals, no EDM drops, no catchy hook
```

---

## 통합 설계 (곡 확보 후 구현 — 지금은 설계만, YAGNI)

날씨 앰비언스(AU-018 ①)와 **같은 우선순위 패턴**을 BGM에도 적용:

1. **BgmLibrarySO 확장**: `Entry`에 선택 필드 `WeatherType weather`(기본 = 미지정) 추가.
   미지정 = 기존 Day/Night/Title 슬롯 로직 그대로. 지정 = 그 날씨의 무드 곡.
2. **WorldAudioManager**: `WeatherChanged` 구독(이미 amb용으로 구독 중 — 확장) →
   날씨 ∈ {Rain·Snow·Storm·Heat·Fog} 이고 해당 곡이 있으면 **그 곡으로 크로스페이드(시간대 슬롯 덮어씀)**,
   Clear·Cloudy 복귀 시 Day/Night 슬롯으로 되돌림. amb의 `UpdateAmbient` 우선순위와 동형.
3. 곡이 없으면 = 기존 Day/Night 유지(무음 아님). 폴백 안전.

## 확장(뒤로 미뤄도 됨)

- **날씨×시간대 분리**: 비 낮 vs 비 밤을 나누려면 곡 2배(`bgm_rain_day`/`bgm_rain_night`).
  최소선에선 날씨 1곡이 낮·밤 공용. 필요 판정은 Director 청취 후.
- **밤 곡 우회로**(night-bgm.md §4): 밤 전용 신곡 대신 **낮 곡 + 로우패스 + 리버브 변주**로 밤 무드를
  만들 수 있다 — 날씨 곡에도 적용 가능(예산 절약). Suno 재생성 대신 후처리 변주.

## Director 액션

1. 위 프롬프트로 **`bgm_rain`·`bgm_snow` 우선 뽑기**(필수) → 마음에 들면 `bgm_storm`(체감 최대)·Heat·Fog.
2. 확보한 WAV를 `Assets/_intake/ElevenLabs/BGM/`(또는 지정 위치)에 두고 공장에 통보 →
   임포트·BgmLibrary 등재·WeatherChanged 배선은 공장이 처리(위 설계대로).
3. 곡명·권리(Suno 플랜·상업권) 기록은 반입 시 CREDITS.md에.
