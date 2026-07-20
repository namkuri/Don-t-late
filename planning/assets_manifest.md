# 반입 에셋 매니페스트 (INTAKE 기록)

외부 반입 에셋의 출처·버전·라이선스·용도를 기록한다. 재현 정보는 여기가 단일 소유 —
`_intake/` 원본은 기록 후 삭제한다(원본 보관 아님, 이 표가 재현을 보장).

| 에셋 | 출처 URL | 버전 | 라이선스 | 용도 | 반입일 |
|---|---|---|---|---|---|
| Pretendard-Regular.ttf | https://github.com/orioncactus/pretendard/releases/download/v1.3.9/Pretendard-1.3.9.zip (내부 `public/static/alternative/Pretendard-Regular.ttf`) | v1.3.9 | SIL Open Font License 1.1 | 전 UI 폰트 (TMP 폰트 에셋 `Pretendard-Regular SDF`) | 2026-07-21 |

## Pretendard v1.3.9 — 라이선스 전문 요지
- Copyright (c) 2021, Kil Hyung-jin (https://github.com/orioncactus/pretendard), Reserved Font Name "Pretendard".
- SIL Open Font License, Version 1.1 — https://scripts.sil.org/OFL
- 재배포·임베드·수정 허용, 폰트 자체 판매 금지, 라이선스·저작권 고지 유지 의무.
- dest 경로: `Assets/Art/UI/Fonts/Pretendard-Regular.ttf` + TMP 에셋(Dynamic·아틀라스 4096).

## Tripo INTAKE (민지) — 2026-07-21

> 출처 = Tripo 생성물(민지). **라이선스 = 플랜 미확인 플래그** — Tripo 유료 플랜=산출물 소유권 /
> 무료 플랜=CC-BY(출처 표기 의무). **확인 전 커밋 보류** (라이선스 확정 후 커밋). gen=1.

| bom_id | 원파일명 | dest | gen | 출처 | 라이선스 | 반입일 |
|---|---|---|---|---|---|---|
| moon_pixel | moon.png | `Assets/Art/Backgrounds/moon_pixel.png` (덮어쓰기·GUID 보존) | 1 | Tripo(민지) | ⚠ 플랜 미확인 (유료=소유/무료=CC-BY) — 커밋 보류 | 2026-07-21 |
| prop_streetlamp | 가로등-tripo.fbx | `Assets/Art/Props/prop_streetlamp.fbx` | 1 | Tripo(민지) | ⚠ 플랜 미확인 — 커밋 보류 | 2026-07-21 |
| chr_courier | coupang.fbx | `Assets/Art/Characters/chr_courier.fbx` | 1 | Tripo(민지) | ⚠ 플랜 미확인 — 커밋 보류 | 2026-07-21 |
| A_chr_courier_run | A_coupang_run.fbx | `Assets/Art/Characters/A_chr_courier_run.fbx` | 1 | Tripo(민지) | ⚠ 플랜 미확인 — 커밋 보류 | 2026-07-21 |

### 검역 수치 (INTAKE 2026-07-21)
- **moon_pixel**: png 256×256 RGB24, Point·무압축 자동 적용. ⚠ 알파 채널 없음 → Moon 셰이더 원판 마스크(tex.a)가 무력화되어 밤 컷에서 달 주위 **정사각 헤일로** 발생. 민지 재출력(투명 배경) 필요.
- **prop_streetlamp**: fbx tris=10405 (**Props 상한 1500 초과 → 경고**, 차단 아님). 원본 mesh.bounds 퇴화(≈0) → 컬링 리스크(임포터 RecalculateBounds 권고). 자연 높이 1.0u(Y-up, X270° 임포트 보정), 4.0u로 스케일(×4)해 스왑.
- **chr_courier**: fbx tris=4714 (<5000 통과). animType=Generic(휴머노이드 아님·Mixamo 자동리깅 미적용, 스켈레톤 有 SkinnedMeshRenderer×1). 높이 **1.06u** — 앵커 1.8u ±30%(1.26~2.34u) **미달 → 경고**(≈1.7배 업스케일 필요). 원점=발바닥(min.y≈0). T-포즈. 텍스처 없음(머티리얼 albedo none). 로고 육안: 메시에 "쿠팡/coupang" 글자·데칼 없음 — 단 **원파일명 "coupang"=쿠팡(실상표)** → chr_courier로 개명해 브랜딩 제거.
- **A_chr_courier_run**: fbx animType=Generic, 클립 1개 "mixamo.com" 0.53s @30fps(frames 0-16), loop=False(임포트 시 loop 설정 필요). Mixamo 달리기 모션.

## Character 교체 (late_man) + Walk 애니 — 2026-07-21

> 쿠팡맨(coupang.fbx) → **late_man 캐릭터로 교체**. chr_courier.fbx **내용만 덮어쓰기(GUID·.meta 보존)** —
> 하류 참조(프리팹·컨트롤러·아바타 배선) 무손실. Walk 모션 추가로 Speed 1D 블렌드(Walk/Run) 완성.

| bom_id | 원파일명 | dest | gen | 출처 | 라이선스 | 반입일 |
|---|---|---|---|---|---|---|
| chr_courier | late_man.fbx | `Assets/Art/Characters/chr_courier.fbx` (덮어쓰기·GUID 보존, 구 coupang.fbx 대체) | 2 | Tripo(민지) | ⚠ 과금제=산출물 소유권 (D-029) — 플랜 확인 후 커밋 | 2026-07-21 |
| A_chr_courier_walk | A_late_man_walking.fbx | `Assets/Art/Characters/A_chr_courier_walk.fbx` | 1 | **Mixamo**(Adobe·민지 매개) | Adobe 무료 라이선스(Mixamo) | 2026-07-21 |

### 검역 수치 (교체 INTAKE 2026-07-21)
- **chr_courier(late_man)**: fbx tris=**5432 (Characters 상한 5000 초과 432 → 경고**, 차단 아님·데시메이트 권고), verts=8809. **animType=Human 셋업 성공** — 아바타 isValid=True·isHuman=True (Mixamo 리그 mixamorig: 접두 32본, 계층 chr_courier/Armature/Root/mixamorig:Hips). 원임포트 높이 **1.07u**(앵커 1.8u ±30% 미달·경고) → 빌더가 렌더바운즈 기준 ×1.686 스케일로 **1.800u 정규화**·발끝 y=0 정렬. **텍스처 없음**(임베디드 0·버텍스컬러 없음·FBX 텍스처참조 경로 무효 `E:\dontlate`) → 회색 렌더. **쿠팡 로고: 텍스처 미포함으로 확인 불가**(후속 텍스처 반입 시 자동추출 규칙 처리 예정).
- **A_chr_courier_walk**: Mixamo 걷기, clip "mixamo.com" **0.967s**, animType=Human(아바타 CopyFromOther=chr_courier)·loop=True·isHumanMotion=True. 리타깃 경고(수치 아님)만.
- **A_chr_courier_run**(기존): Human 재셋업, clip 0.533s·loop=True·human=True.
- **AC_chr_courier.controller**: 파라미터 3종(Speed float·IsCarrying bool·IsGrounded bool — PlayerAnimationManager 계약) + 기본 스테이트 **Locomotion = Speed 1D 블렌드트리**(Walk@0·Walk@2.5·Run@4.5 — idle 클립 미납품이라 0 구간은 Walk 대체).
