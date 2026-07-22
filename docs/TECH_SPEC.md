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

## 오디오 (직교 추가 2026-07-22 · S-024 — D-039~043·D-046 확정분 요약 전재)
| 항목 | 규격 | 근거 |
|---|---|---|
| 믹스 기준 | BGM 볼륨 0.5 · SFX 볼륨 0.7 · 전부 2D(spatialBlend 0 — 리스너 위치 무관) | AU-001 · BOM §8 |
| BGM 압축 | Vorbis q30(~118kbps) · Load Type = Compressed In Memory · 스테레오 | D-043 · D-040 |
| SFX 압축 | Vorbis q70 · Load Type = **Decompress On Load**(짧은 원샷 — 지연 최소) · 모노 강제 | D-043 · 임포터 실측 |
| Streaming | **금지** — WebGL(Web Audio API)이 미지원. DecompressOnLoad도 금지(60초 스테레오 1곡 = 생PCM 11.5MB) | D-040 |
| AudioListener | **Core 씬 소유 1개** — 콘텐츠 씬 배치 금지 (D-021 태양 소유와 동형) | D-041 |
| BGM 구동 | 슬롯 3종(Day/Night/Title) · 세션 추첨 = 시작 곡(no-repeat) · 플레이리스트 크로스페이드 3s · 낮→밤 전환 = Evening 진입(17시) | D-039 · D-046 |
| 반입 계약 | `_intake/ElevenLabs/{BGM,SFX}/` → SFX 파일명=bom_id 스왑 · BGM 원제 유지(BgmLibrary SO가 계약) · CREDITS.md 기록 누락 = 반입 차단 | 공장 가이드 §3 |
