---
name: deliver
description: 납품 마감 절차 — 셀프검증(컴파일·콘솔0·테스트·재조립·Play 실측·캡처) → 결과 기록(리드 N분) → 납품 커밋·push → 디스코드 항목당 캡처 발신(D-069). 트리거 - 납품, 마감, deliver, 검증하고 커밋.
---

# deliver — 납품 마감 (시공 완료 후 의무 절차)

> CODE_RULES §8·§9 + D-063·D-069의 실행 절차. 이 세션 15회 반복분을 정형화.

## 전제
/order로 [발주] 커밋이 이미 나가 있어야 한다. 없으면 지금이라도 발주 커밋부터(위반 기록 남기고).

## 절차

1. **컴파일**: `unity-cli editor stop`(Play 중이면) → `unity-cli console --clear && unity-cli editor refresh --compile` → sleep 12~15 → `unity-cli console --type error,warning` **0건**.
2. **테스트**: `unity-cli test` → 현재 기준 **32/32** (매니저·정산 로직 변경 시 필수, UI 표시만이면 생략 가능하되 기록에 사유).
3. **재조립**: 씬 구성 변경 시 `unity-cli exec 'UnityEditor.EditorApplication.ExecuteMenuItem("DontLate/Build/★ All Scenes"); return "done";'` (Core만이면 Core Scene 메뉴).
4. **Play 실측**: `unity-cli editor play --wait` → 씬 네비는 정상 경로(Home→Camp→…, 전이 가드 존중) → exec로 상태 수치 검증 → **오버레이 캡처**:
   `unity-cli exec 'UnityEngine.ScreenCapture.CaptureScreenshot("Screenshots/{id}_{항목}.png"); return "ok";'`
   캡처는 **발주 항목당 1장 이상**(D-069) — 화면 무변화 항목만 로그 갈음 명시.
5. **캡처를 Read로 직접 확인** — 찍었다고 끝이 아니라 눈으로 판정(이 세션에서 카메라·라벨 결함 3건을 캡처 확인으로 잡음).
6. **결과 기록**: 대장에 `### 결과 · {date 실행값} (리드 N분)` append — 관찰 위주("~확인" ○표기), 실수·교정도 기록.
7. **납품 커밋**: `git add -A && git reset -- 'Assets/Scenes/*.unity' 'Assets/_Recovery'` (씬 본문 커밋 금지) → `[{ID}] 제목 (via ClaudeCode) [self-tested]` → push.
8. **디스코드**: `python scripts/discord_notify.py "설명" --file Screenshots/x.png` — **항목 수만큼 반복**(--file은 1개만 지원).

## 이 세션에서 배운 함정 (재발 4회+)
- **exec 안 for/foreach = 행잉** — 인덱스 단문으로 풀어 쓴다. 배열 순회가 필요하면 exec 여러 번.
- **파이썬으로 C# 한글/개행 문자열 수정 금지** — `\n`이 실개행으로 들어가 CS1010. Edit 도구를 쓴다.
- **컴파일 후 Play 재시작 없이 검증 금지** — 구 어셈블리 거짓 음성 실사례(S-054).
- 검증 exec가 "no Unity instances" 뱉으면 3~8초 후 1회 재시도(트랜지언트).
- Play 중 컴파일·테스트 불가 — 사람이 플레이 중이면 until-loop 백그라운드로 대기.
