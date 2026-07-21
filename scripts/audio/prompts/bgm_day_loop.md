# 프롬프트 원본 — `bgm_day_loop`

> ⚙ **자동 생성 문서** — `scripts/audio/prompt_builder.py` 가 조립한다. 규격 부분을 손으로 고치지 말 것
> (다음 build 때 덮어쓰인다). 바꾸려면 **창작 태그**를 고치거나 BOM·규격 문서를 고쳐라.
>
> 규격 출처: `scripts/audio/rules/GAME-BGM-RULES.md` (충돌 시 스타일보다 우선) ·
> 스타일: `rules/afternoon-bgm-02.md`(낮) · `rules/night-bgm.md`(밤)

## 대상 스펙 (출처: BOM §8)

| 항목 | 값 |
|---|---|
| bom_id | `bgm_day_loop` |
| 종류 | BGM · 슬롯 `day` |
| 용도 | 낮 거리 BGM |
| 스펙 | 60~90s 루프 · 심리스 루프포인트 |
| 요청 길이 | 200.0s |
| dest | `Assets/Audio/BGM/bgm_day_loop.wav` |

## 창작 태그 (사람이 고치는 유일한 칸)

<!-- NOTE:BEGIN -->
city pop, retro 80s, sunny afternoon, breezy, cozy neighborhood
<!-- NOTE:END -->

## 전송 프롬프트 (조립 결과 — 그대로 API에 투입)

<!-- PROMPT:BEGIN -->
```
major key city pop, retro 80s, bright FM electric piano, sparkling bell synth, punchy analog synths, driving synth bass, bright arpeggiated synth, clean bright synth lead, crisp dry drum machine, glossy pads, warm analog, city pop, sunny afternoon, breezy, cozy neighborhood, major key, no intro fade-in, no outro fade-out, no ending, continuous groove from start to finish, consistent dynamics, steady level, no dramatic build-ups, no drops, background music for a game, understated melody, texture-driven, unobtrusive, minimal arrangement, sparse midrange, leave space in the mids, instrumental, 96 BPM, no vocals, no jazz, no saxophone, no acoustic guitar, no neon, no nighttime mood
```
<!-- PROMPT:END -->

## 편집 인계 (규격 §5)

```
── 편집 인계 ──
BPM        : 96
1마디      : 2.50초   (240 ÷ 96)
권장 루프  : 32마디 = 80.00초   (대안: 16마디 = 40.00초)
반복 내성  : 30분 플레이 시 22회 반복 — 🟡 최소선 미달 — 64마디(160s) 검토 권장
요청 길이  : 200초
편집 경고  : 없음
```

## 규격 검사

- 필수 태그 15종 · 금지 태그 9종: **통과**
- 조성 `major key` · BPM `96`(정수) · instrumental 명시

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
python scripts/audio/prompt_builder.py build --bom-id bgm_day_loop
python scripts/audio/elevenlabs_client.py plan --bom-id bgm_day_loop   # 구성 확인 (크레딧 0)
python scripts/audio/elevenlabs_client.py gen  --bom-id bgm_day_loop --use-plan [--seed N]
python scripts/audio/audio_pipeline.py intake --bom-id bgm_day_loop
```

## 세대 이력 (append-only)

| gen | 일자 | 변경 |
|---|---|---|
| 1 | 2026-07-21 | 최초 조립 |
| 2 | 2026-07-21 | 재조립 |
| 3 | 2026-07-21 | 재조립 |
| 4 | 2026-07-21 | 재조립 |
