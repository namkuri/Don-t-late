# STYLE.md — 아트 규격 v5 (3D×픽셀화 · 동결됨 🔒)
## 룩: "3D 픽셀아트" — 일반 3D 모델을 셰이더·해상도로 픽셀화 (t3ssel8r 계열)

```yaml
pipeline_levels:               # ⭐ 셰이더 토끼굴 방지 캡
  L1_필수: "월드 카메라 → 저해상 렌더타겟(TECH_SPEC 참조) → 정수 업스케일 + 텍스처 Point 필터"
  L2_목표: "팔레트 양자화/포스터라이즈 포스트 + 약한 디더"
  L3_금지: "아웃라인 엣지검출·픽셀퍼펙트 스냅 — 하지 않는다 (Q3 재예산 통과 시에만 해제)"
tier_rule:
  W_world: "3D 모델 전부 — 저해상 렌더 타겟 안 (픽셀화 대상)"
  H_hires: "UI·폰트·대화창(박말순 초상 포함) — 풀해상 캔버스 오버레이 (선명 유지)"
  particles: "기본 W(픽셀화) — 글로우·별빛만 H 허용"
palette: {base_bg: "#0a0d16", dominant: "#ff9f45", accent: "#35e0c8", danger: "#ff4658"}
color_meaning: {시안: 상호작용가능, 레드: 위험실패, 앰버: 목표보상, 네이비: 중립배경}
asset_rules:                   # AI 생성 전제 (Meshy 등)
  texture: "256px · Point 필터 · PBR 없음 (베이스컬러만)"
  poly: "prop <1500 · 캐릭터 <5000 · 건물 모듈 <3000 (초과 시 데시메이트 — 임포터 자동)"
  silhouette: "픽셀화가 텍스처 결함은 가려도 실루엣은 못 가린다 — 검역은 실루엣 기준"
lighting:
  shadow: "Directional 소프트 섀도 ON (저해상 렌더 안에서 자동 픽셀화 — 공짜 감성)"
  daynight: "Directional 커브 + LUT 2종(낮/밤) — 에셋 불변 원칙"
  points: "가로등=앰버 · 상호작용·간판=시안 · 실패=레드"
postprocess: "L2 양자화 + 블룸(약·H층) + 비네트. CRT·스캔라인=문서 브랜드 전용"
ai_process: "Meshy/Trellis 3D 생성(클라우드) → 검역(실루엣·폴리·원점) → 임포터(데시메이트·Point)
             → 픽셀화 파이프라인이 룩 통일. 후처리가 일관성을 산다 — PS1 철학의 귀환"
readability: {interactable: "시안 미광/림", hierarchy: "목표 밝게·배경 어둡게"}
```
