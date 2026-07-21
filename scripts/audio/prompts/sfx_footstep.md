# 프롬프트 원본 — `sfx_footstep`

> ⚙ **자동 생성 문서** — `scripts/audio/prompt_builder.py` 가 조립한다. 규격 부분을 손으로 고치지 말 것
> (다음 build 때 덮어쓰인다). 바꾸려면 **감독 노트**를 고치거나 BOM·INTENT를 고쳐라.
>
> HARNESS §9 ②는 "프롬프트 기록 = 재현성"을 요구한다. 착지 원본은 승격 후 폐기되므로
> **이 파일과 `planning/assets_manifest.md`가 유일한 재생산 근거**다.

## 대상 스펙 (출처: BOM §8 — 여기서 고치지 말 것)

| 항목 | 값 |
|---|---|
| bom_id | `sfx_footstep` |
| 종류 | SFX |
| 트리거 | Locomotion 이동중 (도메인 내부 훅) |
| 소리 | 발소리+숨소리(달리기 가중) |
| 길이 | 1.5s |
| dest | `Assets/Audio/SFX/sfx_footstep.wav` |
| 임포트 | Vorbis q70 · Decompress On Load · 2D |

## 톤 근거 (출처: INTENT.md — 동결 · 자동 주입)

`tone: 다크코미디` · `one_emotion: 늦지마!! — 쫓기며 웃픈 하루` · `player_fantasy: 쫓기는 밑바닥 노동자`

## 감독 노트 (사람이 고치는 유일한 칸 — 창작 지시)

<!-- NOTE:BEGIN -->
A single footstep on concrete pavement, worn sneaker, slight grit.
<!-- NOTE:END -->

## 전송 프롬프트 (조립 결과 — 그대로 MCP에 투입)

<!-- PROMPT:BEGIN -->
```
A single footstep on concrete pavement, worn sneaker, slight grit. Duration about 1.5 seconds. Style: retro pixel-art game sound design, dark comedy tone, clean and readable in a busy mix. Single isolated sound effect, dry and close, no background music, no vocals, no long reverb tail.
```
<!-- PROMPT:END -->

금칙어 검사: **통과** (`no background music` · `no vocals`)

## 생성 파라미터

| 파라미터 | 값 |
|---|---|
| MCP 툴 | `text_to_sound_effects` |
| 모델 | `기본` |
| 길이 | 1.5s |
| 출력 포맷 | **PCM 16bit WAV** (mp3 패딩 = 심리스 루프 불가 · 후공정도 WAV 전용) |

## 재생산 절차

```bash
python scripts/audio/prompt_builder.py build --bom-id sfx_footstep
# ↑ 전송 프롬프트로 MCP 생성 → _audio_intake/elevenlabs/ 착지
python scripts/audio/audio_pipeline.py intake    --bom-id sfx_footstep
python scripts/audio/audio_pipeline.py normalize --bom-id sfx_footstep
python scripts/audio/audio_pipeline.py promote   --bom-id sfx_footstep --yes   # ⚠ 별도 승인
```

## 세대 이력 (append-only)

| gen | 일자 | 변경 |
|---|---|---|
| 1 | 2026-07-21 | 최초 조립 |
