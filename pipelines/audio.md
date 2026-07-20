# 파이프라인: 오디오 (audio)
담당: orchestrator 위임 + 🖐 사람 매개 가능 (검증: reviewer+사람 청취) · 연결: #8 Suno · #9 ElevenLabs · #10 Freesound
원칙: **시간당 체감 품질 1위 영역** — 국면 3의 최우선. BGM 루프 1곡 + 핵심 SFX 소수면 충분.

## 1. 기동 점검
- [ ] Suno/ElevenLabs/Freesound 중 최소 1개 사용 가능 — 전부 미연결 → CONNECT_REQUEST(높음)
- [ ] JUICE.md 이벤트 목록 로드 (SFX 목록은 JUICE 이벤트와 짝)
- [ ] 폴백 순서: Suno→Freesound(BGM) / ElevenLabs→Freesound(SFX) / 전부 불가→무음+최소 신디

## 2. 공정 단계
1. SFX 목록 확정 = JUICE 이벤트에서 도출 (임의 추가 금지)
2. 확보 (생성 or 라이브러리) — 착지는 `_intake/suno/`·`_intake/freesound/` 등 (HARNESS §9),
   **확보 즉시 manifest 라이선스 기록 = 입장권** (누락 = 반입 차단)
3. 볼륨 정규화 + TECH_SPEC 믹스 비율 적용
4. 임포트 → 이벤트 훅 연결 (unity-dev)

## 3. 자동교정 루프 (cap 2)
| 게이트 실패 | 자가 교정 |
|---|---|
| 루프 이음새 튐 | 크로스페이드 처리 시도 → 실패 시 재생성/교체 |
| 라이선스 불명 | **반입 차단**(교정 불가) → 출처 재확인 or 폐기·교체 |
| 볼륨 편차 | 정규화 재적용 |
| 무드 이탈 | 자가 판정 불가 → 후보 2개를 사람 청취로 |

## 4. 실수 → 규칙 (append-only)
- [시드] 라이선스 확인 전 임포트 → 확보와 기록은 한 동작
- [시드] SFX 욕심으로 목록 팽창 → JUICE 이벤트 밖 SFX는 만들지 않는다
