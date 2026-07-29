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
5.5. **캡처 검수 게이트 (S-099 신설 — 시각 납품 의무)**: UI·연출·씬 룩 캡처는 디스코드 발신 전에
   `capture-reviewer` 서브에이전트에 태운다 — 입력 = 캡처 경로 + **기대 명세 한 줄**(무엇이 보여야
   하는가). FAIL(경계 침범·겹침·글리프 깨짐 등 차단급)이면 재시공 후 재캡처. 근거: 관제 자체 확인은
   "기능 존재"만 보고 레이아웃 결함을 반복 통과시켰다(S-098 버프 fill 돌출 반려 — iterations M3).
6. **결과 기록**: 대장에 `### 결과 · {date 실행값} (리드 N분)` append — 관찰 위주("~확인" ○표기), 실수·교정도 기록.
7. **납품 커밋**: `git add -A && git reset -- 'Assets/Scenes/*.unity' 'Assets/_Recovery'` (씬 본문 커밋 금지) → `[{ID}] 제목 (via ClaudeCode) [self-tested]` → push.
8. **재배포는 기본 생략 (D-072)** — 유니티 검증으로 갈음. 배포는 남규님 요청·웹 전용 검증 필요
   건·마일스톤 단위 묶음에만 (빌드+배포가 리드타임을 지배하던 것 폐지).
9. **디스코드**: `python scripts/discord_notify.py "설명" --file Screenshots/x.png` — **항목 수만큼 반복**(--file은 1개만 지원).

## 이 세션에서 배운 함정 (재발 4회+)
- **exec 안 for/foreach = 행잉** — 인덱스 단문으로 풀어 쓴다. 배열 순회가 필요하면 exec 여러 번.
- **파이썬으로 C# 한글/개행 문자열 수정 금지** — `\n`이 실개행으로 들어가 CS1010. Edit 도구를 쓴다.
- **컴파일 후 Play 재시작 없이 검증 금지** — 구 어셈블리 거짓 음성 실사례(S-054).
- 검증 exec가 "no Unity instances" 뱉으면 3~8초 후 1회 재시도(트랜지언트).
- Play 중 컴파일·테스트 불가 — 사람이 플레이 중이면 until-loop 백그라운드로 대기.
- **왕복 사이클까지 검증** — 픽업만 확인하고 끝내지 않는다: 버리고→다시 잡고→씬 넘어갔다 돌아오는 왕복이
  진짜 검증 (S-068 재픽업·주문 리롤 버그가 편도 검증의 구멍).
- **게이지·바 UI는 중간값 시료로 캡처** — 0%나 100% 캡처는 렌더 고장(sprite 없는 Image의 fillAmount 무시 등)을
  못 잡는다 (S-068 게이지 사례).
- **볼륨·설정류 시공은 채널 전수 대조** — BGM·SFX·앰비언스·자체 AudioSource(블립) 등 재생 채널 인벤토리와 대조
  (S-065 블립·S-068 앰비언스 — 같은 구멍 2회).
- **결과 헤더 시각은 date 출력을 눈으로 본 뒤 기입** — 명령을 미리 써두면 손기입 추정치가 들어간다 (S-073·S-075·S-076 3회 재발 → 커밋 후 정정 소요).
- **refresh 거부(Play 중)는 exit 0 — && 체인이 안 멈춘다**: 검증 체인에 씬 전이 exec를 묶기 전 status가 ready인지 별도 확인. 위반 시 사람 플레이 세션에 전이 명령이 주입된다 (S-094 실사고 — 남규님 세션에 Home·Camp 전이 주입).
- **InputSystem 가상 디바이스 정리는 `native==false` 판별로만** — deviceId 짐작으로 지우면 실물 마우스가 날아가 물리 입력이 전멸한다(S-100 실사고 — 시작 버튼 불능·에디터 재시작으로 복구). 검증 종료 시 `InputSystem.devices` 전수 조회로 native 키보드·마우스 생존 확인. InputSystem.Reset 리플렉션 호출 금지(상태 오염 — 초기화 예외).
- **Play 중 에디터에 상태 변화 exec(onClick.Invoke·씬 전이류) 금지** — 사람 테스트 세션일 수 있다(S-094에 이어 S-100 진단 중 2차 침범). 진단은 읽기 전용 조회로만, 발화가 필요하면 정지 후 관제 소유 세션에서.
- **납품 커밋 전 `git status --short` 잔량 확인** — 의도한 파일 외 잔량(특히 대량 M = EOL 재정규화류)이 있으면 기능 커밋에 섞지 말고 원인 확정 후 별도 chore 커밋으로 격리하거나 소거한다 (감사 v3 — 263파일 일시 폭풍 실측, S-106).
