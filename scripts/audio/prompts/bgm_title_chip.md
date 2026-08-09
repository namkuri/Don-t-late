# 프롬프트 원본 — `bgm_title_chip`

> ⚙ **자동 생성 문서** — `scripts/audio/prompt_builder.py` 가 조립한다. 규격 부분을 손으로 고치지 말 것
> (다음 build 때 덮어쓰인다). 바꾸려면 **창작 태그**를 고치거나 BOM·규격 문서를 고쳐라.
>
> 규격 출처: `scripts/audio/rules/GAME-BGM-RULES.md` (충돌 시 스타일보다 우선) ·
> 스타일: `rules/afternoon-bgm-02.md`(낮) · `rules/night-bgm.md`(밤)

## 대상 스펙 (출처: BOM §8)

| 항목 | 값 |
|---|---|
| bom_id | `bgm_title_chip` |
| 종류 | BGM · 슬롯 `title` |
| 용도 | (BOM 미등재) |
| 스펙 | (BOM 미등재 — 생성은 규격만으로 진행) |
| 요청 길이 | 60.0s |
| dest | `Assets/Audio/BGM/bgm_title_chip.wav` |

## 창작 태그 (사람이 고치는 유일한 칸)

<!-- NOTE:BEGIN -->
8-bit chiptune, retro game console, square wave lead, triangle wave bass, bouncy and comedic, catchy hook, playful title theme, bright and energetic, pixel art game opening fanfare
<!-- NOTE:END -->

## 전송 프롬프트 (조립 결과 — 그대로 API에 투입)

<!-- PROMPT:BEGIN -->
```
8-bit chiptune, retro game console, square wave lead, triangle wave bass, bouncy and comedic, catchy hook, playful title theme, bright and energetic, pixel art game opening fanfare, major key, instrumental, 112 BPM, no vocals, no jazz, no acoustic guitar
```
<!-- PROMPT:END -->

## 편집 인계 (규격 §5)

```
── 편집 인계 ──
BPM        : 112
1마디      : 2.14초   (240 ÷ 112)
권장 루프  : 16마디 = 34.29초   (대안: 8마디 = 17.14초)
반복 내성  : 단발 연출 — 반복 내성 규칙 비적용(§3 예외)
요청 길이  : 60초
편집 경고  : 없음
```

## 규격 검사

- 필수 태그 15종 · 금지 태그 9종: **통과**  (타이틀=단발 연출이라 §1·§3 면제)
- 조성 `minor key` · BPM `112`(정수) · instrumental 명시

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
python scripts/audio/prompt_builder.py build --bom-id bgm_title_chip
python scripts/audio/elevenlabs_client.py plan --bom-id bgm_title_chip   # 구성 확인 (크레딧 0)
python scripts/audio/elevenlabs_client.py gen  --bom-id bgm_title_chip --use-plan [--seed N]
python scripts/audio/audio_pipeline.py intake --bom-id bgm_title_chip
```

## 세대 이력 (append-only)

| gen | 일자 | 변경 |
|---|---|---|
| 1 | 2026-07-22 | 최초 조립 |
