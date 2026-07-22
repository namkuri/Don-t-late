# 공장 모드 (정수) — 코드·오디오 레인

> 확정: [decisions](../decisions.md) D-055 (2026-07-22). 세션 분기는 CLAUDE.md — 정수 PC의 Claude Code는
> 이 문서가 곧 운영 규칙이다. 산출물은 **브랜치→PR로만** main에 들어온다 (main 직접 push는 훅이 차단).

## 0. 최초 셋업 (1회)

1. clone 후 `git config core.hooksPath hooks` (컴파일 게이트·main 차단 훅 활성)
2. Unity 6000.5.3f1 + 씬 재현 메뉴: `DontLate/Build Core Scene` → `Build Scene Flow UI` → `Build District Stage` → `Build Camp Stage` → `Build Home Stage`
3. Core.unity Play → 타이틀부터 하루 사이클 완주 확인
- ⚠ `dontlate.role`은 설정하지 않는다 — 미설정이 곧 "공장"이며, main push가 자동 차단된다.

## 1. 공통 절차 (레인 무관)

```
git pull → 브랜치 생성(git switch -c feature/jjs-<주제>) → 발주서 낭독 → 수행
→ 셀프검증 → 대장에 "### 결과 · 시각 (리드 N분)" append → push → PR
```
- 발주서가 곧 수용기준. 모호하면 **구현 전에 관제에 되묻기**.
- PR 본문은 관찰 기록(판정어 금지). 씬·프리팹·Settings 커밋 금지(훅 차단).
- 자발 작업은 4조건(스코프 내·기록·착수 예고·감각 판정은 명시 위임만) — [distributed-workflow](distributed-workflow.md) §5.

## 2. 코드 레인

- 대장: [orders/system.md](../orders/system.md) · 셀프검증 3종 = `unity-cli editor refresh --compile` → `console` 0건 → `editor play --wait` 기대동작
- ⚠ 장시간 Play 관측 전 `unity-cli console --clear` (버퍼 포화 시 최신 로그 누락 — 실측)

## 3. 오디오 레인 (별도 모드 아님 — 차이 3가지만)

- 대장: [orders/audio.md](../orders/audio.md)
- 검증 = 인게임 **청취 도구**(Play 중 N=다음곡·B=슬롯전환·T=시각점프) + 귀. 콘솔 검증은 임포트 에러 확인용.
- 반입 = `Assets/_intake/ElevenLabs/{BGM,SFX}/` + **라이선스 기록 즉시**(`Assets/Audio/CREDITS.md` — 누락=반입 차단, 실격 사유 영역). SFX 파일명=bom_id(스왑 계약) · BGM은 원제 유지(BgmLibrary SO가 계약).
- WebGL 제약: Streaming 금지(Vorbis·Compressed In Memory — 임포터가 자동 적용).

## 4. 예비검수 (PR 전 권장 — 반려 왕복 절약)

관제 2축 체크리스트로 자가 리허설:
- [품질] 컴파일·콘솔 0·금지 패턴(FindObjectOfType 등)·통신 2층·Play 기대동작
- [절차] 발주 존재(자발이면 D-기록)·매니페스트 정합(새 파일=직교 기록)·반입 경로·커밋 경계·주체 표기
