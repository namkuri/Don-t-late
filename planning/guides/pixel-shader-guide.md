# 가이드 — 네이티브 해상도 픽셀화 셰이더 (D-011 · 남규 작업분)

> 목표: 렌더타겟 축소 없이, 풀해상 화면을 "가상 480×270 그리드"로 스냅해 픽셀 룩을 낸다.
> 코드 0줄 — Shader Graph + Renderer Feature. 전부 에디터 작업(남규 영역).

## 순서 (약 20분)

1. **Fullscreen Shader Graph 생성**
   Project 창 → Create → Shader Graph → URP → **Fullscreen Shader Graph** → 이름 `SG_Pixelate`
   (위치 제안: `Assets/Art/Shaders/` — 폴더 없으면 생성)
2. **프로퍼티**: Blackboard의 `+` → **Vector2**, 이름 `PixelGrid`, Default = **(480, 270)**
3. **노드 배선** (좌→우):
   ```
   Screen Position(Default) ─ Multiply(× PixelGrid) ─ Floor ─ Add(+0.5,+0.5) ─ Divide(÷ PixelGrid)
                                                                                    │
   URP Sample Buffer (Source: Blit Source) ◄─ UV ────────────────────────────────────┘
        └─ 출력 → Fragment의 Base Color
   ```
   (+0.5는 텍셀 중심 샘플링 — 빼면 경계에서 지글거림)
4. **Save Asset** → SG_Pixelate 우클릭 → Create → **Material** → `M_Pixelate`
5. **Renderer Feature 장착**
   `Assets/Settings/`에서 현재 Quality가 쓰는 **URP Renderer Data** 선택
   → Inspector 맨 아래 **Add Renderer Feature → Full Screen Pass Renderer Feature**
   - Pass Material = `M_Pixelate`
   - Injection Point = **Before Rendering Post Processing**
     (블룸·비네트가 픽셀 위에 부드럽게 얹힘 — STYLE의 "블룸=H층" 의도와 일치)
6. **확인 포인트**
   - UI 캔버스는 **Screen Space - Overlay**면 자동으로 픽셀화 제외(풀해상 유지 = Tier H).
     Camera-Space로 월드 카메라에 붙이면 같이 픽셀화되니 주의.
   - URP Asset의 **MSAA = Disabled** (켜면 블록 경계가 뭉개짐).
   - 끄기 = Renderer Feature 체크박스 → 나중에 진짜 L1(저해상 RT)과 A/B 비교용.
7. **L2 맛보기 (선택)**: URP Sample Buffer 출력 → **Posterize**(Steps 6~8) → Base Color.
   팔레트 양자화 근사. 과하면 스킵 — L2는 목표지 필수가 아니다.

## 정직한 한계 (동결 판단 재료)

- 1080p에서 480×270 스냅 = 4×4 블록이라 **룩은 L1과 사실상 동일**. 단 GPU는 풀해상
  렌더 비용을 그대로 낸다 → **WebGL 성능은 L1이 유리**. 관통 빌드 프레임 보고 판단.
- 이 방식은 STYLE의 L1 문구("저해상 렌더타겟")와 **다른 구현** — 룩이 확정되면
  동결 전에 STYLE 문구를 실구현에 맞게 개정한다 (개정안은 관제가 준비).
- 스냅 그리드는 화면 고정이라 카메라 이동 시 픽셀이 화면에 붙어 보인다 — L1도 동일 특성.
