# INBOX.md — 남규 처리함 (사람이 볼 파일은 이거 하나)

> 규칙: 관제가 사람 손이 필요한 모든 것을 여기 모아 유지. 처리하면 답만 주면 됨 — 정리는 관제가.
> 해설: [open-questions.md](open-questions.md) · 갱신: 2026-07-22 22:35 (중간점검 2차 — [retrospective-2026-07-22.md](retrospective-2026-07-22.md) · R12 묶음 검증 메뉴 신설).

## ① 판정 대기 (review)

| # | 항목 | 상태 | 참조 |
|---|---|---|---|
| R16 | **SFX 6세대(동물의 숲 토이 톤) 19종** — ①② 완료 (2026-07-22 23:20): ① Director 청취 판정 **19종 통과**("검증결과 괜찮네" — 체크리스트 청취) ② `Assets/Audio/SFX/` 6세대 승격 커밋(PR #11, main .meta 보존). **잔여 = 관제 ③④**: ③ BOM §8 신규 행 추가+JUICE 대응 — AU-008 7종 + **PR#12 4종 + PR#13 5종(amb_villatown/foodalley·map_pin/route/depart) 합류** ④ GAME-SFX-RULES §1 앵커 개정("retro pixel-art" → 토이 톤). 부기: 미배선 8종(deadline_warn·phone_ring·rhythm_hit/miss·scene_whoosh·footstep·drink·amb_night) 배선은 AU-007 카드1대로 관제 몫 | [orders/audio.md](orders/audio.md) AU-007/008 결과 7 · PR #11 |
| R17 | **PR#13 사람 판정** — 잔여 ①만: 구역감(빌라촌 웜그레이 저층 · 먹자골목 간판 스트립 그레이박스 톤). ~~② 지도 조작감~~→폴리싱 백로그 (님: "추후 폴리싱") · ~~③ 앰비언스~~→**5초 루프 기각** — [orders/audio.md](orders/audio.md) AU-012 재생성(30s+×3종) | 부분 처리 (2026-07-24) | PR #13 |
| R11 | 간판 발광판 | **재발주됨** (2026-07-22) — 처방 확정 [decisions.md](decisions.md) D-051: 발광판 폐지 → 간판 머티리얼 이미시브 스왑(S-004, 시스템) + 간판 분리 익스포트(아트 공통 규격 승격) | [orders/system.md](orders/system.md) S-004 |

## ② 결정 대기

**현재 없음** — B-7·B-8은 D-064~066으로 해소 (2026-07-23). 다음 결정은 Q2(BOM 동결). — B-6까지 전량 해소. 다음 결정은 Q2(BOM 동결)에서 발생.

## ③ 손 작업·외부 대기

| # | 작업 | 누구 | 참조 |
|---|---|---|---|
| H12 | **Trellis2 반입물 2종 마감질** — 편의점 store_2(485,891→<3,000 tris)·가로등(95,724→<1,500) 데시메이트 + 텍스처 포함 재출력 (현재 회백색) | 민지 | [orders/art.md](orders/art.md) A-001 결과 |
| H8 | 텍스처 재전송 2건: ~~가로등~~(Trellis2로 대체됨)·캐릭터 — FBX **Embed Media** 켜기 (캐릭터는 쿠팡 로고 제거 겸) | 민지 | [assets_manifest.md](assets_manifest.md) |
| H9 | 애니 클립: **idle**(급함 — 정지 시에도 걸음) · jump·pickup·carry | 민지(Mixamo) | [BOM.md](BOM.md) §1 |
| H4 | RunPod Trellis 관통 → 소품 1개 실측 | 민지 | [TASKS.md](TASKS.md) M0-04 |
| H14 | **Mixamo 애니 1차 3종** — idle(급함·H9 승계)·짐 들고 걷기(carry walk)·침대 기상(getting up). Humanoid·`A_chr_courier_<동작>.fbx` 명명, 반입은 _intake/art/Mixamo/ | **남규** | [art-roadmap.md](art-roadmap.md) §4 P0 |
| ~~H10~~ | ~~정수 투입 개시~~ → **자발 개시됨** (오디오 PR merge 완료) — 다음 발주 묶음은 P3 잔여 스크립트로 협의 | — | [orders/audio.md](orders/audio.md) |

## 처리 완료 (최근)

- ✅ **R12 묶음 플레이 검증 통과** (2026-07-23 님 실플레이) — 하우징·늦코인·미니게임·셰이크/시머 전부 통과. 잔여 결함 1건(전화 15초 미응답 방치) = [orders/system.md](orders/system.md) S-037로 이관

- ✅ **H13 GitHub 브랜치 보호 적용** (2026-07-22 님) — main은 PR+승인 1 필수, 관리자 직접 push 유지. 로컬 pre-push 훅과 이중 방벽 완성 (D-055)

- ✅ **B-6 Home 존치 확정 + 무대 시공** (2026-07-22 님 결정, [decisions.md](decisions.md) D-052) — 방 그레이박스(침대·창문·문) `DontLate/Build Home Stage`
- ✅ **S-005~007 직접 납품 완료** (님 지시 D-053 — 정수→관제) — 매니페스트 31/34, 관찰 기록은 [orders/system.md](orders/system.md) 결과 블록
- ✅ **B-5 발주 진행** (2026-07-22 님 "전부 발주") — 정수 3건([orders/system.md](orders/system.md) S-005 Camp정산 · S-006 Travel · S-007 미니게임) push 완료 + 관제 S-008 Camp 무대 즉시 납품

- ✅ **H1 리네임 완료** (2026-07-22 관제 대행) — `Assets/Art/Buildings`로 계약 경로 정합. 내용물은 GS25(지에스.fbx)뿐이라 출처 기록은 생략 — D-050 폐기 예정이므로 폐기 시 폴더째 정리
- ✅ **H3 폰 확인 완료** (2026-07-22 님 관찰: "되긴 됨, 컨트롤 안 됨") — 모바일 렌더·로드는 정상, **터치 입력은 미구현**(키보드 전제). 심사·시연은 데스크톱 전제라 스코프 밖 — 본선에서 필요해지면 그때 발주 ([open-questions.md](open-questions.md) 백로그 기재)
- ✅ **H5 소멸 확인** (2026-07-22 님: "떠돌이 없는 것 같은데") — 이후 Greybox 재빌드 과정에서 자연 정리된 것으로 판단, 종결
- ✅ **H11 BGM 컷 판정 종결** (2026-07-22 님: "꽤 괜찮음") — 정수 위임 채택 5곡을 님이 최종 승인. **bom_id 리네임은 불필요** — D-046(슬롯당 다곡 플레이리스트)으로 파일명 1:1 계약이 소멸했고, 스왑 계약은 `BgmLibrary.asset`(SO) 참조로 성립 ([assets_manifest.md](assets_manifest.md) 참조)
- ✅ **R15 하네스 도구 10종 승인** (2026-07-22 님 "패스") — 커밋·반입 실전에서 훅 4종·채점기 가동 확인됨 → done
- ✅ **R4·R5 자동 임포트·프리팹 팩토리 완료** (2026-07-22 님 승인) — 반입 4건(가로등×2·달·캐릭터)에서 실전 가동: URP Lit 리맵·스케일 정규화·프리팹 자동 생성 동작, 가동 중 발견된 결함(애니 차단·텍스처 자동추출)도 규칙으로 회수됨 → done
- ✅ **H7 철회 — GS25 폐기 예정** (2026-07-22 님 결정, [decisions.md](decisions.md) D-050) — 민지 데시메이트 불필요

- ✅ **R6·R7·R8·R9·R10·R12·R13·R14 일괄 승인** (2026-07-21) — 비콘 v2·District 무대·빛기둥·낮밤·HUD·가로등 열·밤하늘 v4(달 토끼 포함)·캐릭터 단일검증 → 전부 done
  - R7 부기: 씬 셋팅 잔여분은 [orders/system.md](orders/system.md) **S-002 구역 자동 배치**로 후속 발주됨
- ✅ **D-b → B-4 확정** (Tripo+Mixamo, [decisions.md](decisions.md) D-034) · **D-c → 한국형 직하 가로등 발주로 해소** (D-035)
- ✅ H6 Main `Core` 삭제 · H2는 GS25 데시메이트(H7)·캐릭터 텍스처(H8)에 흡수
