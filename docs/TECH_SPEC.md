# TECH_SPEC.md — 기술 규격 v3 (2.5D · 계약=동결 🔒 / 튜닝=유동)
```yaml
# === 계약 (동결) ===
engine: "Unity 6.5 (6000.5.3f1) · URP 3D"
unit: "1u = 1m"  character_height: "1.8u (도트 스타일 3D — 도미노 앵커)"
coordinate: "X=진행 · Y=수직 · Z=깊이 레인 (WalkableVolume 한정)"
camera: {type: perspective, fov: "20~25°(망원)", pitch: "8~12° 하향", zoom: 고정,
         rule: "주 레인 1아트픽셀≈3~4스크린픽셀 되는 거리 고정 (밀도 근사)"}
sorting: "Z 깊이 자동 + 알파 컷아웃"
scenes: "Core(상주) + Main·집·캠프·이동·배송지 (SceneFlow 상태기계)"
collision: {layers: [Player, Static, Interactable, Trigger], type: 3D}
anim: {system: Animator(Mecanim), set: [idle,walk,run,jump,pickup,carry],
       facing: "이동방향 회전 · 45° 스냅 옵션(그레이박스 판정)"}
rig: "미결 🔶 — MagicaVoxel(복셀→제네릭) vs Meshy(→Mixamo), D3~4 관통 테스트로 판정"
webgl_budget: {drawcalls: "<150", texture_mb: 96, note: "스프라이트 아틀라스 필수"}
# === 튜닝 (기본값) ===
move: {walk: 2.5, run: 5.0, z_factor: 0.7, jump: "타일 2 (유지 여부 그레이박스 판정)"}
frame_target: 60
ingame_time: {game_min_per_real_sec: 4}
```
## 치수표 (px + u 병기 — 1.8u=48px 기준, 1u≈27px)
| 오브젝트 | 아트 px | 월드 u |
|---|---|---|
| 캐릭터 | 높이 48 | 1.8 |
| 현관문 | 56 | 2.1 |
| 건물 한 층 | 80 | 3.0 |
| 트럭(사이드, 연출 소품) | 176 | 6.5 |
| 택배상자(중) | 20 | 0.75 |
| 박말순 초상(대화 UI) | 256~ (Tier H) | — |
