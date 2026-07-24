# 프롬프트 원본 — `sfx_scene_whoosh`

> ⚙ **자동 생성 문서** — `scripts/audio/prompt_builder.py` 가 조립한다. 규격 부분을 손으로 고치지 말 것
> (다음 build 때 덮어쓰인다). 바꾸려면 **창작 태그**를 고치거나 BOM·규격 문서를 고쳐라.
>
> 규격 출처: `scripts/audio/rules/GAME-BGM-RULES.md` (충돌 시 스타일보다 우선) ·
> 스타일: `rules/afternoon-bgm-02.md`(낮) · `rules/night-bgm.md`(밤)

## 대상 스펙 (출처: BOM §8)

| 항목 | 값 |
|---|---|
| bom_id | `sfx_scene_whoosh` |
| 종류 | SFX |
| 트리거 | `SceneTransitionStarted` |
| 소리 | 전환 휙 |
| 요청 길이 | 1.0s |
| dest | `Assets/Audio/SFX/sfx_scene_whoosh.wav` |

## 창작 태그 (사람이 고치는 유일한 칸)

<!-- NOTE:BEGIN -->
mellow retro city chiptune transition, soft rolling 8-bit blips with a cozy urban groove, laid-back and warm
<!-- NOTE:END -->

## 전송 프롬프트 (조립 결과 — 그대로 API에 투입)

<!-- PROMPT:BEGIN -->
```
mellow retro city chiptune transition, soft rolling 8-bit blips with a cozy urban groove, laid-back and warm. Duration about 1.0 seconds. Style: retro 8-bit chiptune game sound, warm urban city vibe, pulse and square waves, mellow and cozy, clean and readable in a busy mix. Single isolated sound effect, dry and close, no background music, no vocals, no long reverb tail.
```
<!-- PROMPT:END -->

## 규격 검사

- 금칙어 **통과** (`no background music` · `no vocals`)

## 생성 파라미터

| 파라미터 | 값 |
|---|---|
| 엔드포인트 | `POST /v1/sound-generation` (REST 직호출) |
| 모델 | `기본` |
| 출력 포맷 | `output_format=pcm_44100` → **PCM 16bit를 WAV로 래핑** |
| mp3 금지 근거 | 규격 §7 — 인코더가 앞뒤 무음 패딩을 붙여 매 루프마다 공백이 생긴다 |

## 톤 근거 (INTENT.md — 동결)

`tone: 다크코미디` · `one_emotion: 늦지마!! — 쫓기며 웃픈 하루` · `player_fantasy: 쫓기는 밑바닥 노동자`

## 재생산 절차

```bash
python scripts/audio/prompt_builder.py build --bom-id sfx_scene_whoosh
python scripts/audio/elevenlabs_client.py gen  --bom-id sfx_scene_whoosh
python scripts/audio/audio_pipeline.py intake --bom-id sfx_scene_whoosh
```

## 세대 이력 (append-only)

| gen | 일자 | 변경 |
|---|---|---|
| 1 | 2026-07-22 | 최초 조립 |
| 2 | 2026-07-22 | 재조립 |
| 3 | 2026-07-22 | 재조립 |
| 4 | 2026-07-22 | 재조립 |
| 5 | 2026-07-22 | 재조립 |
| 6 | 2026-07-22 | 재조립 |
| 7 | 2026-07-22 | 재조립 |
| 8 | 2026-07-23 | 재조립 |
| 9 | 2026-07-25 | cand16 채택 — 레트로 city chiptune(멜로우·재즈X) 교체, 토이톤/상행스윕 폐기 · SFX seed 미수신(프롬프트로 재생) |
