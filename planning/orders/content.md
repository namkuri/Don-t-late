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

### 결과 · 2026-07-22 20:43 (리드 8분 · 정수 공장)

- **SO 에셋 4본** (`Assets/Data/Dialogue/`): ① `Scenario_ParkMalsoon_Intro` 5줄(교체) ② `Scenario_ParkMalsoon_PhoneNag` 3줄 ③ `Scenario_ParkMalsoon_DeliveryStart` 3줄 ④ `Scenario_ParkMalsoon_LateScold` 4줄. 전량 빈 라인 0 검증(스크립트 검사).
- Play 확인: Home 도착 발화 → `PhoneRang ← 박말순` 이벤트 로그 + 재생 실측 `playing=True scenario=Scenario_ParkMalsoon_Intro line0=[박말순: 여보세— 총각! 자? 지금 자냐고!! 해가 중천이야!]`.
- 톤: "늦지마" 후렴 3회(①⑤줄·③3줄·④1줄) · 짧은 호흡 · 욕설/상표 없음 · 게임 맥락 반영(빚 만 원·던지기·취급주의·벌금).
- **PhoneRang 배선 안 함** — 발주서 "배선 포함 시 WorldMinigameManager와 협의" 조건부라 콘텐츠만 납품. ②~④ 재생 지점 배선은 관제 몫으로 넘김(어느 이벤트에 걸지 = 설계 판단).
- 참고: 콘솔 워닝 1건 "referenced script missing"은 브랜치 전환 잔여(로컬 씬이 S-023 컴포넌트 참조 — PR #5 머지+재조립 시 해소), 본 발주 무관.
