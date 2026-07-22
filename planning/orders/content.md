# orders/content.md — 콘텐츠 발주 대장 (append-only · 시나리오·데이터)

> 형식: [guides/distributed-workflow.md](../guides/distributed-workflow.md) §3. SO 데이터·대사는 커밋 가능 영역.

---

## C-001 · 발주 2026-07-22 19:10 → 정수 (박말순 시나리오 확장)

목표: 대화 콘텐츠 4본 — 현재 인트로가 테스트 문구 수준. 박말순 캐릭터(잔소리 많은 물류소장, "늦지마!"가 입버릇)의 톤을 세워 실플레이 몰입을 만든다.

입력:
- `Assets/Scripts/SO/DialogueScenarioSO.cs`(구조: 화자·텍스트 라인 배열) · `Assets/Data/Dialogue/Scenario_ParkMalsoon_Intro.asset`(현행 — 교체 대상).
- 재생 지점: Home 첫 도착 인트로(WorldDialogueManager._homeIntroScenario) · 진상 전화(PhoneRang — 현재 미배선, 배선 포함 시 WorldMinigameManager와 협의 코멘트).
- 게임 맥락: 빚 10,000원 배달부, 마감 압박, 택배 던지기 가능, 취급주의 파손.

기대: SO 에셋 4본 — ① 첫날 인트로(3~5줄) ② 진상 전화 도입(2~3줄, 미니게임 직전) ③ 배송 개시 잔소리 ④ 지각 질책. 라인은 짧게(모바일 폰겜 호흡), "늦지마" 후렴 활용. LLM 생성 초안 → 정수 감수 → 에셋화.

수용기준: ① 에셋 4본 유효(빈 라인 없음) ② 인트로 교체분 Play 확인 ③ 대사 톤 샘플을 PR 본문에 인용(사람 감수 근거).

실패시: [BLOCKED]. 욕설·실존 상표 금지.
