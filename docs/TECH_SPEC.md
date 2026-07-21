# TECH_SPEC.md — 기술 규격 v5 (3D×픽셀화 · 계약=동결 🔒 / 튜닝=유동)
```yaml
frozen: true   # Q1 동결 2026-07-21 (D-037)
# === 계약 (동결) ===
engine: "Unity 6.5 (6000.5.3f1) · URP 3D"
unit: "1u = 1m"  character_height: "1.8u"        # 3D 규격 회귀 — u가 전부, 픽셀은 렌더가 만든다
render_target: "월드 카메라 480×270 ⭐ 도미노 앵커 — 정수 업스케일 (1080p=×4)"
ui_layer: "풀해상 캔버스 오버레이 (저해상 밖 — 한글 가독성)"
coordinate: "X=진행 · Y=수직 · Z=깊이 레인 (WalkableVolume 한정)"
camera: {type: perspective, fov: "20~25°(망원)", pitch: "8~12° 하향", zoom: 고정, view: "사이드 2.5D 앵글"}
shadow: "Directional 소프트 ON — 저해상 렌더가 그림자도 픽셀화"
scenes: "Core(상주) + Main·집·캠프·이동·배송지"
collision: {layers: [Player, Static, Interactable, Trigger], type: 3D}
anim: {system: Animator(Mecanim), set: [idle,walk,run,jump,pickup,carry],
       facing: "이동방향 회전 · 45° 스냅 옵션(그레이박스 판정)"}
rig: "🔒 확정 — Tripo 모델 + Mixamo 리깅·애니 (단일검증 실측: Humanoid 아바타·Walk/Run 리타깃·본 구동 — D-034)"
asset_import: "Meshy 생성물: 스케일 검증(1.8u 앵커) · 원점=바닥중심 · 데시메이트 자동"
webgl_budget: {tris_total: "<200k", drawcalls: "<150", texture_mb: 96}
# === 튜닝 (기본값) ===
move: {walk: 2.5, run: 5.0, z_factor: 0.7, jump: "0.6u (그레이박스 판정)"}
frame_target: 60
ingame_time: {game_min_per_real_sec: 4}
```
## 치수표 (u — 3D 월드 단위, 스케일 시트로 프롬프트에 첨부)
| 오브젝트 | 크기 (u) |
|---|---|
| 캐릭터 | 높이 1.8 |
| 현관문 | 높이 2.1 |
| 건물 한 층 | 3.0 |
| 트럭 (사이드 연출 소품) | 길이 6.5 |
| 택배상자 (중) | 0.4~0.75 |
| 가로등 | 4.0 |
| 박말순 초상 (대화 UI) | Tier H — 2D 일러스트 유지 |
