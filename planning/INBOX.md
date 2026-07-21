# INBOX.md — 남규 처리함 (사람이 볼 파일은 이거 하나)

> 규칙: 관제가 사람 손이 필요한 모든 것을 여기 모아 유지. 처리하면 답만 주면 됨 — 정리는 관제가.
> 해설: [open-questions.md](open-questions.md) · 갱신: 2026-07-21 19:06 (표 깨짐 수리 + 판정 대반영).

## ① 판정 대기 (review)

| # | 항목 | 상태 | 참조 |
|---|---|---|---|
| R11 | 간판 발광판 | **반려 접수** (발광판이 실제 간판을 가림) → 처방 연구 후 재발주 예정: 간판 분리 익스포트 / 발광 데칼 셰이더그래프 승격 / 블렌드 방식 변경 중 선택 | [iterations.md](iterations.md) 반려 기록 |
| R15 | 하네스 도구 10종 | **사용하며 테스트 중** (님 방침) — 커밋·반입 때마다 훅이 실전 검증됨 | [orders/system.md](orders/system.md) S-001 |
| R4·R5 | 자동 임포트·프리팹 팩토리 | Art 도착 시 자연 검증 (기존 방침 유지 — 이미 반입 4건에서 실전 가동됨) | [BOM.md](BOM.md) §11 |

## ② 결정 대기

**현재 없음** — B-1~B-4 전량 해소. 다음 결정은 **Q1 동결 선언**(관제가 STYLE 개정안 준비 후 요청 예정)과 Q2(BOM 동결)에서 발생.

## ③ 손 작업·외부 대기

| # | 작업 | 누구 | 참조 |
|---|---|---|---|
| H7 | 🔴 **GS25 데시메이트** — 단독 1,499,400 tris (예산 7.5배). 목표 <3,000 | 민지 | scene_stats 실측 |
| A-001 | **한국형 직하 가로등 모델** (신규 발주 — D-c 해소안) | 민지 | [orders/art.md](orders/art.md) |
| H8 | 텍스처 재전송 2건: 가로등·캐릭터 — FBX **Embed Media** 켜기 (캐릭터는 쿠팡 로고 제거 겸) | 민지 | [assets_manifest.md](assets_manifest.md) |
| H9 | 애니 클립: **idle**(급함 — 정지 시에도 걸음) · jump·pickup·carry | 민지(Mixamo) | [BOM.md](BOM.md) §1 |
| H4 | RunPod Trellis 관통 → 소품 1개 실측 | 민지 | [TASKS.md](TASKS.md) M0-04 |
| H1 | `Assets/Art/Building`→`Buildings` 리네임 + GS25 출처 기록 | 님 | [decisions.md](decisions.md) D-002 |
| H3 | 폰에서 https://namkuri.github.io/dontlate-web/ 열기 (1분) | 님 | [TASKS.md](TASKS.md) M0-03 |
| H5 | Greybox 떠돌이 `StreetLampLight`(x=17.5) 삭제 — 광원·광추 9번째의 범인 | 님 | 씬=님 독점 |
| H10 | 정수 투입 개시 신호 — 주면 첫 발주 묶음(P3 스크립트) 발사 | 님 | [guides/distributed-workflow.md](guides/distributed-workflow.md) |

## 처리 완료 (최근)

- ✅ **R6·R7·R8·R9·R10·R12·R13·R14 일괄 승인** (2026-07-21) — 비콘 v2·District 무대·빛기둥·낮밤·HUD·가로등 열·밤하늘 v4(달 토끼 포함)·캐릭터 단일검증 → 전부 done
  - R7 부기: 씬 셋팅 잔여분은 [orders/system.md](orders/system.md) **S-002 구역 자동 배치**로 후속 발주됨
- ✅ **D-b → B-4 확정** (Tripo+Mixamo, [decisions.md](decisions.md) D-034) · **D-c → 한국형 직하 가로등 발주로 해소** (D-035)
- ✅ H6 Main `Core` 삭제 · H2는 GS25 데시메이트(H7)·캐릭터 텍스처(H8)에 흡수
