# 프롬프트 원본 — `bgm_night_var`

> ⚙ **자동 생성 문서** — `scripts/audio/prompt_builder.py` 가 조립한다. 규격 부분을 손으로 고치지 말 것
> (다음 build 때 덮어쓰인다). 바꾸려면 **창작 태그**를 고치거나 BOM·규격 문서를 고쳐라.
>
> 규격 출처: `scripts/audio/rules/GAME-BGM-RULES.md` (충돌 시 스타일보다 우선) ·
> 스타일: `rules/afternoon-bgm-02.md`(낮) · `rules/night-bgm.md`(밤)

## 대상 스펙 (출처: BOM §8)

| 항목 | 값 |
|---|---|
| bom_id | `bgm_night_var` |
| 종류 | BGM · 슬롯 `night` |
| 용도 | 밤 변주 |
| 스펙 | **별도 곡 대신 낮 곡 + 로우패스/리버브 변주 1순위** (DayPhaseChanged 훅) |
| 요청 길이 | 60.0s |
| dest | `Assets/Audio/BGM/bgm_night_var.wav` |

## 창작 태그 (사람이 고치는 유일한 칸)

<!-- NOTE:BEGIN -->
city pop, retro 80s, lonely muted saxophone, pre-dawn empty streets, melancholic
<!-- NOTE:END -->

## 전송 프롬프트 (조립 결과 — 그대로 API에 투입)

<!-- PROMPT:BEGIN -->
```
downtempo synthwave city pop, lo-fi, warm analog synth pads, round mellow synth bass, dreamy nostalgic lead synth, dusty laid-back drum machine beat, soft bell tones, vinyl warmth, jazzy 7th chords, city pop, retro 80s, lonely muted saxophone, pre-dawn empty streets, melancholic, minor key, no intro fade-in, no outro fade-out, no ending, continuous groove from start to finish, consistent dynamics, steady level, no dramatic build-ups, no drops, background music for a game, understated melody, texture-driven, unobtrusive, minimal arrangement, sparse midrange, leave space in the mids, instrumental, 88 BPM, no vocals, no EDM drops
```
<!-- PROMPT:END -->

## 편집 인계 (규격 §5)

```
── 편집 인계 ──
BPM        : 88
1마디      : 2.73초   (240 ÷ 88)
권장 루프  : 16마디 = 43.64초   (대안: 8마디 = 21.82초)
반복 내성  : 30분 플레이 시 41회 반복 — 🟡 최소선 미달 — 32마디(87s) 검토 권장
요청 길이  : 60초
편집 경고  : ⚠ 편집 경고: 긴 잔향/지속 노이즈 포함. 루프 이음매에 테일 랩 필요. [dreamy, vinyl warmth, dusty, lo-fi]
```

## 규격 검사

- 필수 태그 15종 · 금지 태그 9종: **통과**
- 조성 `minor key` · BPM `88`(정수) · instrumental 명시
- 🔴 루프 난이도: `dreamy`, `vinyl warmth`, `dusty`, `lo-fi` → ⚠ 편집 경고: 긴 잔향/지속 노이즈 포함. 루프 이음매에 테일 랩 필요.

## 생성 파라미터

| 파라미터 | 값 |
|---|---|
| 엔드포인트 | `POST /v1/music` (REST 직호출) |
| 모델 | `music_v2` |
| 출력 포맷 | `output_format=pcm_44100` → **PCM 16bit를 WAV로 래핑** |
| mp3 금지 근거 | 규격 §7 — 인코더가 앞뒤 무음 패딩을 붙여 매 루프마다 공백이 생긴다 |

## 톤 근거 (INTENT.md — 동결)

`tone: 다크코미디` · `one_emotion: 늦지마!! — 쫓기며 웃픈 하루` · `player_fantasy: 쫓기는 밑바닥 노동자`

## 재생산 절차

```bash
python scripts/audio/prompt_builder.py build --bom-id bgm_night_var
python scripts/audio/elevenlabs_client.py plan --bom-id bgm_night_var   # 구성 확인 (크레딧 0)
python scripts/audio/elevenlabs_client.py gen  --bom-id bgm_night_var --use-plan [--seed N]
python scripts/audio/audio_pipeline.py intake --bom-id bgm_night_var
```

## 세대 이력 (append-only)

| gen | 일자 | 변경 |
|---|---|---|
| 1 | 2026-07-21 | 최초 조립 |
| 2 | 2026-07-21 | 재조립 |
| 3 | 2026-07-21 | 재조립 |
| 4 | 2026-07-21 | 재조립 |
| 5 | 2026-07-21 | 재조립 |
