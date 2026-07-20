# STYLE.md — 아트 규격 v3 (2.5D · 동결됨 🔒)
## 룩: "픽셀 월드 × 사실적 광원" — 2D 도트 에셋을 3D 무대에 세운다

```yaml
tier_rule:
  P_dot_world:                 # 캐릭터(도트3D)·건물·소품·바닥 — 도트 에셋
    density: "밀도 근사 — 주 플레이 레인(Z=0)에서 1아트픽셀 ≈ 3~4스크린픽셀"
    note: "퍼스펙티브라 정수 스케일은 없다(포기 선언). 근/원경 밀도차=깊이감"
    alpha: "컷아웃(hard edge) — 반투명 금지 (소팅 버그 원천 차단)"
  H_hires:                     # UI·폰트·파티클·라이트·글로우·LUT
    resolution: 자유
palette: {base_bg: "#0a0d16", dominant: "#ff9f45", accent: "#35e0c8", danger: "#ff4658"}
color_meaning: {시안: 상호작용가능, 레드: 위험실패, 앰버: 목표보상, 네이비: 중립배경}
character_3d:                  # 도트 스타일 3D (ARCHITECTURE §5.7)
  texel_rule: "텍셀 크기 ≈ 주변 스프라이트 아트픽셀 (저해상 텍스처+Point)"
  lighting: "월드와 동일 URP Lit(플랫)"  shadow: "블롭 섀도"
signs: "간판=베이스+이미시브 2레이어 (밤 발광은 이미시브만 점등)"
lighting:
  daynight: "Directional 커브 + LUT 2종(낮/밤) 블렌드 — 에셋 불변이 원칙"
  points: "가로등=앰버 · 상호작용·간판=시안 · 실패=레드"
postprocess: "블룸(약, 광원·간판만) + 비네트(약). CRT·스캔라인은 문서 브랜드 전용(게임 미사용)"
ai_process: "ComfyUI 등 고해상 생성 → 다운스케일 → 팔레트 양자화 → Art/ 폴더 투입(자동 임포트)"
readability: {interactable: "시안 미광/림", hierarchy: "목표 밝게·배경 어둡게"}
```
