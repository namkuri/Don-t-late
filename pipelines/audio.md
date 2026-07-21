# 파이프라인: 오디오 (audio)
담당: orchestrator 위임 + 🖐 사람 매개 가능 (검증: reviewer+사람 청취) · 연결: **#9 ElevenLabs(1순위)** · #10 Freesound · 🖐#8 Suno(웹 수동)
원칙: **시간당 체감 품질 1위 영역** — 국면 3의 최우선. BGM 루프 1곡 + 핵심 SFX 소수면 충분.

> **분리 원칙 (2026-07-21)**: 음원 제작은 게임 제작과 물리적으로 분리한다. 생성·검역·후공정은
> `Assets/` **밖**(`_audio_intake/`, gitignore)에서 끝내고, `Assets/Audio/`로 들어가는 승격 한 걸음만
> 별도 승인·별도 커밋으로 뗀다. 유니티 작업자와 타이밍이 충돌하지 않게 하기 위한 것이다.

## 0. MCP 연결 (#9 — 1회 설정)

공식 서버 `elevenlabs/elevenlabs-mcp`. 툴 27종 중 이 공정이 쓰는 것:
`compose_music`(BGM) · `create_composition_plan` · `text_to_sound_effects`(SFX · **0.5~5초**).

```bash
# ① API 키를 환경변수로 (❗ 설정 파일에 키 원문을 넣지 않는다 — 커밋되면 폐기·재발급뿐)
setx ELEVENLABS_API_KEY "<발급받은 키>"

# ② MCP 로컬 스코프 등록 (리포에 흔적 0 — 팀원 환경 무영향)
claude mcp add --scope local ElevenLabs -- uvx elevenlabs-mcp

# ③ 착지 경로 고정 (기본값은 ~/Desktop — 그대로 두면 HARNESS §9 착지 격리가 깨진다)
#    ~/.claude.json 의 해당 서버 env 에 추가:
#      "ELEVENLABS_MCP_BASE_PATH": "C:/Works/Game/Don-t-late/_audio_intake/elevenlabs"
#      "ELEVENLABS_MCP_OUTPUT_MODE": "files"
```
설정 반영에는 **Claude Code 재시작**이 필요하다. 등록 확인은 `claude mcp list`.

## 1. 기동 점검
- [ ] `ELEVENLABS_API_KEY` 설정됨 · `claude mcp list` 에 ElevenLabs 보임 — 아니면 CONNECT_REQUEST(높음)
- [ ] `python scripts/audio/audio_pipeline.py status` — 착지 경로·예산 확인
- [ ] `python scripts/audio/prompt_builder.py check` — 프롬프트 금칙어 전건 통과
- [ ] JUICE.md 이벤트 목록 로드 (SFX 목록은 JUICE 이벤트와 짝)
- [ ] 폴백 순서: #9 → #10 Freesound → 🖐#8 Suno 웹 수동 → 전부 불가 시 무음+최소 신디

## 2. 공정 단계
1. **대상 확정** — `audio_pipeline.py list` 가 BOM §8을 파싱해 발주 가능 항목을 보여준다.
   JUICE 근거 ❌(J-1 미승인) 항목은 **코드가 차단**한다 — 임의 추가 금지 원칙의 집행 장치.
2. **프롬프트 조립 (품질이 갈리는 지점)** — `prompt_builder.py build --bom-id <id> [--note "..."]`
   · 규격(길이·루프·톤·금칙어)은 **코드가 BOM §8 + INTENT에서 결정론적으로** 박는다 — 사람이 기억하지 않는다.
   · **감독 노트**(악기·장르·무드)만 사람이 쓴다. 노트는 프롬프트 문서 안에 보존돼 재조립 때 재사용된다.
   · **금칙어 검사**: `no ending fade`·`no intro`·`loopable`·`no vocals`가 빠지면 **차단**한다 —
     페이드가 붙으면 4번의 루프 처리가 통째로 무의미해지기 때문이다.
   · 한국어 규격값은 `PHRASE_EN` 매핑으로 영문화한다(매핑 없으면 경고 — 조용한 한글 유출 방지).
3. **생성 (MCP)** — 조립된 프롬프트를 그대로 투입. **출력은 PCM 16bit WAV**
   (mp3는 인코더 패딩 때문에 심리스 루프가 원리적으로 불가).
   BGM은 `create_composition_plan`으로 **구조를 먼저 받아 확인**한 뒤 `compose_music`에 넘긴다 —
   마음에 안 들 때 곡 전체를 재생성하지 않아도 되어 크레딧을 아낀다. SFX(≤5초)는 이 단계를 건너뛴다.
4. **반입** — `audio_pipeline.py intake --bom-id <id>`
   확장자 화이트리스트 검역 + `assets_manifest.md` 기록(**= 입장권**) + `<bom_id>.wav` 리네임.
   **프롬프트 원본이 없으면 차단**한다 — 착지 원본은 나중에 폐기되므로 그때 재생산 근거가 사라진다.
5. **후공정** — `audio_pipeline.py normalize --bom-id <id> [--loop-crossfade-ms 500]`
   꼬리를 머리에 겹쳐 루프 이음매 제거 + 피크 정규화(기본 −1.0 dBFS).
6. **사람 청취** — 무드 이탈은 자가 판정 불가 영역. 통과 못 하면 **감독 노트를 고쳐 2번으로**
   (규격은 건드리지 않는다 — 코드가 다시 박아준다).
7. **승격 (⚠ 유일한 Unity 접점)** — `audio_pipeline.py promote --bom-id <id> --yes`
   `--yes` 없이는 계획만 출력한다. 예산 10MB 초과 시 차단. 착지 원본은 여기서 폐기(HARNESS §9 ⑤).
8. **임포트 설정·이벤트 훅** — BGM=Vorbis·Streaming / SFX=Vorbis q70·Decompress On Load·2D.
   소비자는 `WorldAudioManager`(P4). 이건 unity-dev 몫으로 넘긴다.

## 3. 자동교정 루프 (cap 2)
| 게이트 실패 | 자가 교정 |
|---|---|
| 루프 이음새 튐 | `--loop-crossfade-ms` 값을 키워 재시도(500→1000) → 실패 시 재생성/교체 |
| 라이선스 불명 | **반입 차단**(교정 불가) → 출처 재확인 or 폐기·교체 |
| 볼륨 편차 | `normalize` 재적용 |
| 예산 초과 | 길이 단축 → 모노 강등 → 그래도 넘치면 sacrifice 후보로 |
| 무드 이탈 | 자가 판정 불가 → **감독 노트만 바꿔** 후보 2개 재조립 → 사람 청취로 승자 채택 |
| 금칙어 누락 | `prompt_builder check` 가 검출 → `build` 로 재조립(손편집 되돌림) |

## 4. 실수 → 규칙 (append-only)
- [시드] 라이선스 확인 전 임포트 → 확보와 기록은 한 동작
- [시드] SFX 욕심으로 목록 팽창 → JUICE 이벤트 밖 SFX는 만들지 않는다
- 2026-07-21: **대장의 연결 정보를 사실 확인 없이 신뢰하지 않는다.** #8 Suno가 "웹/API"로 등재돼 있었으나
  공식 API가 존재하지 않았다(파트너 베타뿐). 유통되는 래퍼는 전부 비공식 = ToS 위반 리스크.
  → 연결을 처음 가동할 때는 **공식 문서로 API 실재 여부를 먼저 확인**하고 대장을 정정한다.
- 2026-07-21: **mp3로 받으면 심리스 루프가 불가능하다** — 인코더 지연·패딩이 샘플 정확도를 깨뜨린다.
  루프가 필요한 음원은 생성 단계에서 WAV/PCM으로 받는다(후공정에서 되돌릴 수 없는 손실).
- 2026-07-21: 착지 경로를 `Assets/` 안에 두면 유니티 임포트 규칙에 의존하게 된다.
  **`Assets/` 밖 + gitignore** 로 두면 규칙에 기대지 않고 분리가 성립한다.
