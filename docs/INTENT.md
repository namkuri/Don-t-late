# INTENT.md — 늦지마 (Don't Late) · v5 (동결됨 🔒)
```yaml
theme: "지각 압박 배달 생존기 — 늦지마!!"
genre: "3D 픽셀아트 사이드뷰 배달 아케이드 (2.5D 앵글)"  # 카드 승계
dimension: 3D                              # 일반 3D 모델 + 픽셀화 렌더 (셰이더·저해상)
camera: "사이드뷰 (+Z 레인 이동)"          # TECH_SPEC이 소비
tone: 다크코미디
aesthetic_primary: 서사                    # 웃픔=씁쓸한 공감 (STYLE·JUICE가 소비)
aesthetic_secondary: 감각                  # 밤거리 무드·조작 질감
one_emotion: "늦지마!! — 쫓기며 웃픈 하루"
priority: 비주얼_임팩트                    # 영상 기준 컨셉 접근 (회의 결정)
player_fantasy: "쫓기는 밑바닥 노동자"
must_have:
  - "하루 사이클 코어루프: 기상→짐싣기→이동→배송→정산"
  - "낮·밤 전환 거리 (에셋 불변·조명 전환 — 2.5D 최대 배당)"
  - "블랙컨슈머 박말순 전화 이벤트 (대화→리듬 미니게임)"
never:
  - 실제 상표명 (택배사·편의점 등 → 가상 브랜드)
  - 런타임 AI 호출 (생성물은 빌드에 굽는다)
  - 씬 6개 초과 (Core+Main+집+캠프+이동+배송지 고정)
  - 멀티플레이어 · 오픈월드
ai_axis:                                   # 빌드타임 공정만. 판별: 빌드에 구워지는가?
  - "3D 에셋 AI 배치 생성(Meshy·클라우드) → 검역 → 픽셀화 셰이더 파이프라인이 룩 통일"
  - "대사·배송 콘텐츠 LLM 생성 → SO/CSV로 굽기 (박말순 시나리오 등)"
  - "로직 자동화 (Claude Code + unity-cli exec/검증)"
  - "검수 자동화 (reviewer + screenshot 채점)"
reference: >
  3D 픽셀아트 (t3ssel8r 계열 — 3D 광원·그림자가 픽셀로 양자화되는 룩)
  + 한국적 거리 (간판·편의점·교회·돌하르방). 생활밀착 다크코미디.
frozen: true
```
