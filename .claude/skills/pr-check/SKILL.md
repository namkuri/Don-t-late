---
name: pr-check
description: PR 검수·반입 절차 — 열린 PR 스캔 → 경계·라이선스·코드 규칙 검수 → 충돌 예측 → main 병합·해소 → 검증 → 브랜치 삭제로 닫기 → 디스코드 보고. 트리거 - PR 확인, PR 처리, pr-check.
---

# pr-check — PR 검수·반입 (gh CLI 없이 git만으로)

> 이 세션 6회 반복분(PR#14~19)을 정형화. gh 미설치 환경 전제 — 전부 git 원시 명령.

## 1. 스캔 — 열린 PR 찾기
```bash
git fetch origin && git ls-remote origin 'refs/pull/*/head' | while read sha ref; do
  num=$(echo $ref | cut -d/ -f3)
  git merge-base --is-ancestor $sha origin/main 2>/dev/null || echo "OPEN PR#$num $sha"
done
```
같은 브랜치 연장선 PR들(#16⊂#17⊂#18 같은 스택)은 최상위 하나만 반입하면 전부 커버.

## 2. 검수 (체크리스트)
```bash
git fetch origin +refs/pull/N/head:refs/remotes/origin/prN
git log --format="%h %an %ci %s" origin/main..origin/prN
git diff --stat origin/main...origin/prN
```
- **경계**: 수행자 레인 파일만인가(오디오=Audio/·WorldAudioManager, 아트=_intake·Art/). 씬 본문(.unity) 없나.
- **라이선스**: 신규 바이너리(wav·png·fbx) → CREDITS/manifest 등재 확인. 누락 = 반려(타협 없음).
- **코드 규칙**: Find 계열 금지 · OnEnable/Disable 짝 · 이벤트 저빈도+Log · YAGNI.
- **번호 충돌**: 대장 발주 번호가 관제 선점분과 겹치면 후발(수행자) 재번호 계획 수립.
- 재량 롤백 커밋(A+revert)은 순변화 0인지 diff로 확인.

## 3. 충돌 예측 → 병합
```bash
git merge-tree --write-tree origin/main origin/prN >/dev/null 2>&1 && echo CLEAN || echo CONFLICT
git merge --no-edit origin/prN   # 충돌 시 --no-commit로 열고 해소(양쪽 보존 원칙, 번호 재조정 동반)
```
해소 원칙: 대장(orders/*.md) append 충돌 = 시간순 양쪽 유지 / 코드 배선 인접 충돌 = 양쪽 나란히.

## 4. 검증 → push
컴파일 → 콘솔 0 → 테스트 32/32 → (배선 변경 시 해당 씬 재조립 — 신규 오디오는 임포트 수 분 소요 주의) → push origin main.

## 5. 닫기 — 브랜치 삭제 (gh 없이 PR 자동 close)
```bash
git push origin ":refs/heads/<head브랜치>"
```
⚠ 삭제 직전 `git ls-remote origin refs/pull/N/head`로 **head가 방금 병합한 sha와 같은지 재확인** —
병합 후 수행자가 커밋을 더 올렸으면(실사례 PR#19 발소리 교체) 그것부터 반입하고 지운다.

## 6. 보고
디스코드: 검수 요지(통과/반려 사유)·충돌 처리·수행자 칭찬 or 반려 요청(요청체). 반려면 대장에 반려 기록.
