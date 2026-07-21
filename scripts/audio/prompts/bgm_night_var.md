# 프롬프트 원본 — `bgm_night_var`

> ⚙ **자동 생성 문서** — `scripts/audio/prompt_builder.py` 가 조립한다. 규격 부분을 손으로 고치지 말 것
> (다음 build 때 덮어쓰인다). 바꾸려면 **감독 노트**를 고치거나 BOM·INTENT를 고쳐라.
>
> HARNESS §9 ②는 "프롬프트 기록 = 재현성"을 요구한다. 착지 원본은 승격 후 폐기되므로
> **이 파일과 `planning/assets_manifest.md`가 유일한 재생산 근거**다.

## 대상 스펙 (출처: BOM §8 — 여기서 고치지 말 것)

| 항목 | 값 |
|---|---|
| bom_id | `bgm_night_var` |
| 종류 | BGM |
| 용도 | 밤 변주 |
| 스펙 | **별도 곡 대신 낮 곡 + 로우패스/리버브 변주 1순위** (DayPhaseChanged 훅) |
| 길이 | 75.0s |
| dest | `Assets/Audio/BGM/bgm_night_var.wav` |
| 임포트 | Vorbis · Streaming |

## 톤 근거 (출처: INTENT.md — 동결 · 자동 주입)

`tone: 다크코미디` · `one_emotion: 늦지마!! — 쫓기며 웃픈 하루` · `player_fantasy: 쫓기는 밑바닥 노동자`

## 감독 노트 (사람이 고치는 유일한 칸 — 창작 지시)

<!-- NOTE:BEGIN -->
Night variant of the day theme: same chord motion, slower feel, muted drums, soft pad wash, distant reverb, sparse melody.
<!-- NOTE:END -->

## 전송 프롬프트 (조립 결과 — 그대로 MCP에 투입)

<!-- PROMPT:BEGIN -->
```
Night variant of the day theme: same chord motion, slower feel, muted drums, soft pad wash, distant reverb, sparse melody. Background music loop for the same city street at night in a 3D pixel-art side-view delivery arcade game. Target length 75 seconds. Overall tone: dark comedy — wry, never heroic; the feeling is a hectic day that is funny and bitter at once; the player is a bottom-rung worker perpetually chased by the clock. Setting reference: 3D pixel-art look (lighting and shadows quantized into pixels), Korean city street with shop signs, convenience stores and churches; grounded slice-of-life dark comedy. Clean loopable structure, no intro sweep, no ending fade, no vocals.
```
<!-- PROMPT:END -->

금칙어 검사: **통과** (`loopable` · `no intro` · `no ending fade` · `no vocals`)

## 생성 파라미터

| 파라미터 | 값 |
|---|---|
| MCP 툴 | `compose_music` |
| 모델 | `music_v2` |
| 길이 | 75.0s |
| 출력 포맷 | **PCM 16bit WAV** (mp3 패딩 = 심리스 루프 불가 · 후공정도 WAV 전용) |

## 구성 계획 게이트 (BGM 전용)

`compose_music` 직행 전에 **`create_composition_plan`으로 구조를 먼저 받는다.**
섹션 구성·길이 배분을 눈으로 확인하고 고친 뒤 작곡에 들어가면, 마음에 안 들 때
곡 전체를 재생성하지 않아도 되어 크레딧과 시간을 아낀다. SFX는 5초 이하라 이 단계를 건너뛴다.

## 재생산 절차

```bash
python scripts/audio/prompt_builder.py build --bom-id bgm_night_var
# ↑ 전송 프롬프트로 MCP 생성 → _audio_intake/elevenlabs/ 착지
python scripts/audio/audio_pipeline.py intake    --bom-id bgm_night_var
python scripts/audio/audio_pipeline.py normalize --bom-id bgm_night_var --loop-crossfade-ms 500
python scripts/audio/audio_pipeline.py promote   --bom-id bgm_night_var --yes   # ⚠ 별도 승인
```

## 세대 이력 (append-only)

| gen | 일자 | 변경 |
|---|---|---|
| 1 | 2026-07-21 | 최초 조립 |
