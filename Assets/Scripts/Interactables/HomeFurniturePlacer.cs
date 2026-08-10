using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// Home 가구 배치 (S-019 ④ → S-030 ③ → S-031 개편). 세션 데이터(GameState.placedFurniture)로
    /// 배치분을 재생성하고, 폰 가구앱의 배치 대기(PendingPlacementId)를 처리한다.
    /// 블루프린트: 클릭=확정 · R=45° 회전 · ESC=취소 · **0.5u 그리드 스냅**(S-031 ②).
    /// 배치된 가구 클릭 = 집어 들어 재배치(S-031 ①). TV는 벽에도 붙는다(S-031 ⑤).
    /// 침대는 세션당 1회 시드되는 기본 가구다(S-031 ③ — 무대 고정물에서 강등).
    /// </summary>
    public class HomeFurniturePlacer : MonoBehaviour
    {
        private const float GRID = 0.25f; // S-031 ② 스냅 간격 · S-122 ③ 1/2로 촘촘하게 (기존 좌표는 전부 0.5의 배수 = 0.25 격자에도 그대로 얹힌다)

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private FurnitureSO[] _catalog;
        [SerializeField] private Sprite _hintBackground;
        [SerializeField] private Sprite _hintIcon;
        [SerializeField] private Sprite _hintCloseIcon;

        private static readonly Color GhostColor = new Color(0.208f, 0.878f, 0.784f, 0.45f); // 시안 반투명

        // 이 배치기가 만든 배치물 대장 (S-122 ④ 낙하 판정용) — FindObjectsOfType 금지 규약.
        private readonly List<PlacedFurnitureVisual> _visuals = new List<PlacedFurnitureVisual>();

        private GameObject _ghost;
        private string _ghostId;
        private float _ghostYaw;
        private bool _ghostOnWall;

        private void Start()
        {
            // S-189 — 침대 시드 폐지. 종전엔 여기서 플레이 시작에 침대를 만들었는데, 그러면
            // 에디터에서 씬을 열었을 때 방이 비어 아트가 배치를 맞출 수 없었다(민지님이 플레이
            // 중 생긴 `fur_bed(Clone)`을 세트에 담아 온 이유). 침대는 이제 HomeStageBuilder가
            // 무대 고정물로 세운다 — 씬을 열면 바로 거기 있다.
            // S-275 — 씬에 **이미 서 있는** 기본 가구는 다시 스폰하지 않고 그 실물을 옮겨 쓴다.
            // 종전엔 대장 항목마다 새로 스폰해서, 씬 실물과 사본이 **둘 다** 남거나(중복)
            // 빌더가 맞춰 둔 모델 오프셋·스케일이 없는 사본으로 갈려 작고 어긋나 보였다
            // (남규님 관찰: "가구 집으면 조그만해짐").
            var sceneFurniture = new System.Collections.Generic.Dictionary<string, PlacedFurnitureVisual>();
            foreach (PlacedFurnitureVisual visual in Object.FindObjectsByType<PlacedFurnitureVisual>(FindObjectsInactive.Include))
            {
                if (visual == null || string.IsNullOrEmpty(visual.FurnitureId)) continue;
                if (!sceneFurniture.ContainsKey(visual.FurnitureId)) sceneFurniture[visual.FurnitureId] = visual;
            }

            foreach (PlacedFurniture placed in _gameState.placedFurniture)
            {
                if (sceneFurniture.TryGetValue(placed.furnitureId, out PlacedFurnitureVisual existing))
                {
                    existing.transform.SetPositionAndRotation(placed.position, Quaternion.Euler(0f, placed.rotationY, 0f));
                    existing.Bind(placed.furnitureId, placed.position, placed.rotationY);
                    sceneFurniture.Remove(placed.furnitureId);
                    continue;
                }
                SpawnVisual(placed.furnitureId, placed.position, placed.rotationY);
            }

            AdoptSceneFurniture(sceneFurniture); // S-273 — 대장에 없던 씬 가구를 등재
        }

        /// <summary>
        /// S-273 — 씬에 **미리 세워진** 가구(빌더 고정물)를 배치 대장에 등재한다.
        /// 등재되면 클릭 판정(`FindPlacedIndex`)이 찾아내므로 이동·회전·삭제가 구매 가구와
        /// 완전히 같은 경로를 탄다 — 종전엔 대장에 없어 클릭해도 아무 반응이 없었다(남규님 지적).
        /// 이미 대장에 있으면 건너뛴다(재입장 멱등 — 씬을 오갈 때마다 늘면 안 된다).
        /// </summary>
        private void AdoptSceneFurniture(System.Collections.Generic.Dictionary<string, PlacedFurnitureVisual> remaining)
        {
            if (_gameState == null) return;
            foreach (PlacedFurnitureVisual visual in remaining.Values)
            {
                if (visual == null || string.IsNullOrEmpty(visual.FurnitureId)) continue;
                _gameState.placedFurniture.Add(new PlacedFurniture
                {
                    furnitureId = visual.FurnitureId,
                    position = visual.PlacedPosition,
                    rotationY = visual.RotationY,
                });
            }
        }

        private void OnDisable() // 씬 이탈 시 블루프린트·힌트 잔재 방지
        {
            ClearGhost();
            ShowHint(null);
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            Camera camera = Camera.main;
            if (mouse == null || camera == null) return;

            if (string.IsNullOrEmpty(PhoneView.PendingPlacementId))
            {
                ClearGhost();
                ShowHint(_gameState != null && _gameState.placedFurniture.Count > 0
                    ? "좌클릭 = 집어 옮기기 · 우클릭 = 철거(인벤토리로)"
                    : null); // 놓인 가구가 없으면 안내할 것도 없다
                // 하우징 상태: 좌클릭 = 집어 옮기기 · 우클릭 = 철거 (S-031 ① / S-122 ④).
                HandleRepick(mouse, camera);
                return;
            }

            ShowHint("좌클릭 = 배치 · 우클릭 = 배치 취소 · R = 회전");

            // S-127 — 배치 모드의 우클릭은 **배치 취소**다(남규님 지적).
            // 이 상태에선 고스트가 커서에 붙어 다녀 "커서 밑의 기존 가구"를 겨눌 수가 없다 —
            // 그 불가능한 조작(철거)을 여기에 걸어둔 것이 S-124~126 3연속 반려의 뿌리였다.
            // 들고 있는 물건을 우클릭으로 내려놓는 것은 건축 게임의 일반 관례이기도 하다.
            if (mouse.rightButton.wasPressedThisFrame && !PhoneView.IsOpen)
            {
                CancelPlacement("우클릭");
                return;
            }

            Keyboard keyboard = Keyboard.current;

            // ESC = 취소 — 블루프린트 삭제 + 배치 대기 해제 (가구는 인벤토리에 남는다).
            // 폰이 열려 있으면 ESC는 폰 닫기 몫 (S-032 ③ 충돌 회피).
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !PhoneView.IsOpen)
            {
                CancelPlacement("ESC");
                return;
            }

            // R = 45° 회전 (벽 부착 중엔 벽 법선이 방향을 소유).
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame && !_ghostOnWall)
            {
                _ghostYaw = Mathf.Repeat(_ghostYaw + 45f, 360f);
                WorldAudioManager.Instance?.PlayUiTickSfx(); // AU-010
            }

            FurnitureSO so = Find(PhoneView.PendingPlacementId);
            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());

            // S-031 ⑤: 벽 설치 가능 가구는 벽 히트를 우선 시도.
            Vector3 pos;
            _ghostOnWall = false;
            if (so != null && so.wallMountable && TryWallPoint(ray, so, out Vector3 wallPos, out float wallYaw))
            {
                pos = wallPos;
                _ghostYaw = wallYaw;
                _ghostOnWall = true;
            }
            else if (TrySupportPoint(ray, so, out Vector3 supportPos)) // S-122 ② — 기존 가구 위에 올리기
            {
                pos = supportPos;
            }
            else
            {
                Plane floor = new Plane(Vector3.up, Vector3.zero);
                if (!floor.Raycast(ray, out float enter)) return;
                pos = ray.GetPoint(enter);
                pos.x = Mathf.Clamp(pos.x, -3.5f, 3.5f);
                pos.z = Mathf.Clamp(pos.z, -2.4f, 2.4f);
                pos.y = 0f;
                pos.x = Mathf.Round(pos.x / GRID) * GRID; // S-031 ② 그리드 스냅
                pos.z = Mathf.Round(pos.z / GRID) * GRID;
            }

            // 블루프린트 갱신 (대기 id가 바뀌면 재생성).
            if (_ghost == null || _ghostId != PhoneView.PendingPlacementId)
            {
                ClearGhost();
                _ghostId = PhoneView.PendingPlacementId;
                _ghost = BuildGhost(_ghostId);
            }
            // S-201 — 고스트도 배치될 결과와 같은 회전 보정을 받아야 한다. 안 그러면 미리보기는
            // 누워 있는데 놓으면 서는(또는 반대) 어긋남이 생긴다.
            FurnitureSO ghostSo = Find(_ghostId);
            Quaternion ghostFix = ghostSo != null ? Quaternion.Euler(ghostSo.prefabRotation) : Quaternion.identity;
            _ghost.transform.SetPositionAndRotation(
                pos + Vector3.up * 0.001f, Quaternion.Euler(0f, _ghostYaw, 0f) * ghostFix);

            // 클릭 = 확정 (폰이 열려 있으면 폰 조작에 양보).
            if (!mouse.leftButton.wasPressedThisFrame || PhoneView.IsOpen) return;

            string id = PhoneView.PendingPlacementId;
            PhoneView.PendingPlacementId = null;
            _gameState.ownedFurnitureIds.Remove(id);
            _gameState.placedFurniture.Add(new PlacedFurniture { furnitureId = id, position = pos, rotationY = _ghostYaw });
            SpawnVisual(id, pos, _ghostYaw);
            ClearGhost();
            WorldAudioManager.Instance?.PlayFurniturePlaceSfx(); // AU-010
            Debug.Log("[하우징] " + id + " 배치 — " + pos + " yaw " + _ghostYaw);
        }

        /// <summary>S-127 — 배치 취소 (우클릭·ESC 공용). 들고 있던 가구는 인벤토리에 그대로 남는다.</summary>
        private void CancelPlacement(string source)
        {
            string held = PhoneView.PendingPlacementId;
            PhoneView.PendingPlacementId = null;
            ClearGhost();
            WorldAudioManager.Instance?.PlayUiTickSfx(); // AU-010
            Flash("배치 취소 — " + KoreanName(held) + " 인벤토리에 있다");
            Debug.Log("[하우징] 배치 취소 (" + source + ")");
        }

        // ── S-124 조작 힌트 ──────────────────────────────────
        // 기존 안내는 폰 가구앱 라벨에 있었는데 배치를 누르면 폰이 닫혀 보이지 않았다
        // (남규님 "우클릭해도 아무 반응 없음"의 절반은 이 가시성 문제였다).
        private GameObject _hintCanvasGo;
        private TMP_Text _hintLabel;
        private string _hintText;

        // S-126 — 우클릭 결과를 화면에 알린다. 3연속 반려에서 배운 것: 조작이 실패했을 때
        // "아무 일도 안 일어남"이면 사람은 원인을 알 수 없고 관제는 시뮬레이션으로 재현할 수 없다
        // (합성 입력이 wasPressedThisFrame을 못 건드림 — S-100). 실패도 말을 하게 만든다.
        private float _flashUntil;

        private void Flash(string text)
        {
            _flashUntil = Time.time + 2f;
            _hintText = null; // 다음 ShowHint가 복원하도록 캐시 무효화
            ShowHintRaw(text);
        }

        private void ShowHint(string text)
        {
            if (Time.time < _flashUntil) return; // 결과 메시지 표시 중엔 덮지 않는다
            ShowHintRaw(text);
        }

        private void ShowHintRaw(string text)
        {
            if (text == _hintText) return;
            _hintText = text;
            if (string.IsNullOrEmpty(text))
            {
                if (_hintCanvasGo != null) { Destroy(_hintCanvasGo); _hintCanvasGo = null; _hintLabel = null; }
                return;
            }
            if (_hintCanvasGo == null)
            {
                _hintCanvasGo = new GameObject("HousingHintCanvas");
                Canvas canvas = _hintCanvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 6;
                CanvasScaler scaler = _hintCanvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                GameObject banner = new GameObject("TutorialBanner", typeof(RectTransform));
                banner.transform.SetParent(_hintCanvasGo.transform, false);
                RectTransform bannerRect = (RectTransform)banner.transform;
                bannerRect.anchorMin = bannerRect.anchorMax = bannerRect.pivot = new Vector2(0.5f, 1f);
                bannerRect.sizeDelta = new Vector2(900f, 148f);
                bannerRect.anchoredPosition = new Vector2(0f, -102f);

                Image background = new GameObject("BackgroundArt", typeof(RectTransform)).AddComponent<Image>();
                background.transform.SetParent(banner.transform, false);
                background.sprite = _hintBackground;
                background.color = _hintBackground != null ? Color.white : new Color(0.04f, 0.05f, 0.09f, 0.88f);
                background.preserveAspect = _hintBackground != null;
                background.raycastTarget = false;
                RectTransform backgroundRect = background.rectTransform;
                backgroundRect.anchorMin = backgroundRect.anchorMax = backgroundRect.pivot = new Vector2(0.5f, 0.5f);
                backgroundRect.sizeDelta = _hintBackground != null ? new Vector2(1006f, 671f) : bannerRect.sizeDelta;
                backgroundRect.anchoredPosition = _hintBackground != null ? new Vector2(0f, -24f) : Vector2.zero;

                if (_hintIcon != null)
                {
                    Image icon = new GameObject("ParcelIcon", typeof(RectTransform)).AddComponent<Image>();
                    icon.transform.SetParent(banner.transform, false);
                    icon.sprite = _hintIcon;
                    icon.color = Color.white;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    RectTransform iconRect = icon.rectTransform;
                    iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
                    iconRect.sizeDelta = new Vector2(265f, 177f);
                    iconRect.anchoredPosition = new Vector2(-385f, 0f);
                }

                _hintLabel = new GameObject("Hint", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                _hintLabel.transform.SetParent(banner.transform, false);
                if (UiOverlayFont.Korean != null) _hintLabel.font = UiOverlayFont.Korean;
                _hintLabel.fontSize = 34f;
                _hintLabel.fontStyle = FontStyles.Bold;
                _hintLabel.color = new Color(0.039f, 0.051f, 0.086f, 1f);
                _hintLabel.alignment = TextAlignmentOptions.Center;
                _hintLabel.textWrappingMode = TextWrappingModes.NoWrap;
                _hintLabel.raycastTarget = false;
                RectTransform rect = _hintLabel.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(760f, 76f);
                rect.anchoredPosition = new Vector2(45f, 8f);

                if (_hintCloseIcon != null)
                {
                    Image closeImage = new GameObject("CloseButton", typeof(RectTransform)).AddComponent<Image>();
                    closeImage.transform.SetParent(banner.transform, false);
                    closeImage.sprite = _hintCloseIcon;
                    closeImage.color = Color.white;
                    closeImage.preserveAspect = true;
                    RectTransform closeRect = closeImage.rectTransform;
                    closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 1f);
                    closeRect.sizeDelta = new Vector2(47f, 47f);
                    closeRect.anchoredPosition = new Vector2(-20f, -16f);
                    Button closeButton = closeImage.gameObject.AddComponent<Button>();
                    closeButton.targetGraphic = closeImage;
                    closeImage.gameObject.AddComponent<DismissTutorialButton>();
                }
            }
            _hintLabel.text = text;
        }


        private void HandleRepick(Mouse mouse, Camera camera)
        {
            bool pick = mouse.leftButton.wasPressedThisFrame;
            bool demolish = mouse.rightButton.wasPressedThisFrame;
            if (!pick && !demolish) return;
            Recover(mouse, camera, pick);
        }

        /// <summary>배치물을 인벤토리로 회수한다. pick=true면 배치 모드로 재진입(집기), false면 철거.</summary>
        private void Recover(Mouse mouse, Camera camera, bool pick)
        {
            // 좌클릭(집기)은 버튼 위에서 양보한다. 우클릭(철거)은 이 게임의 UI가 쓰지 않는 버튼이라
            // 폰이 열렸을 때만 막는다 — 집 화면은 대화 박스·진행 버튼이 가구 위를 넓게 덮어서,
            // UI 위라고 무조건 막으면 철거가 영영 안 먹는다(남규님 2회 반려의 실원인).
            if (PhoneView.IsOpen)
            {
                if (!pick) Flash("우클릭 감지 — 폰을 닫고 다시 시도");
                return;
            }
            if (pick && PointerOverInteractiveUI()) return;

            // S-126 — 조준 관용: 선 레이가 빗나가도 반경 0.35u 구체로 한 번 더 훑는다
            // (가구가 작거나 커서가 살짝 빗나가도 잡히게).
            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            PlacedFurnitureVisual visual = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                visual = hit.collider.GetComponentInParent<PlacedFurnitureVisual>();
            if (visual == null && Physics.SphereCast(ray, 0.35f, out RaycastHit soft, 100f))
                visual = soft.collider.GetComponentInParent<PlacedFurnitureVisual>();

            if (visual == null)
            {
                if (!pick) Flash("우클릭 감지 — 커서를 치울 가구 위에 올리고 우클릭");
                return;
            }

            int index = FindPlacedIndex(visual);
            if (index < 0)
            {
                if (!pick) Flash("우클릭 감지 — 이 물건은 치울 수 없다(배치물 아님)");
                return;
            }

            PlacedFurniture placed = _gameState.placedFurniture[index];
            bool hasFootprint = TryWorldBounds(visual.transform, out Bounds footprint); // 파괴 전 실측

            _gameState.placedFurniture.RemoveAt(index);
            _gameState.ownedFurnitureIds.Add(placed.furnitureId); // 집기·철거 모두 인벤토리로 회수
            _visuals.Remove(visual);
            Destroy(visual.gameObject);

            // 지지대가 사라졌으니 그 상단면에 얹혀 있던 가구는 지지대가 서 있던 높이로 내려앉는다.
            if (hasFootprint) DropStacked(footprint, placed.position.y);

            if (pick)
            {
                PhoneView.PendingPlacementId = placed.furnitureId;
                _ghostYaw = placed.rotationY; // 집을 때 각도 유지
            }
            WorldAudioManager.Instance?.PlayUiTickSfx(); // AU-010
            if (!pick) Flash("철거 — " + KoreanName(placed.furnitureId) + " 인벤토리로 회수");
            Debug.Log("[하우징] " + placed.furnitureId + (pick ? " 집음 — 재배치 모드" : " 철거 — 인벤토리 회수"));
        }

        // S-125 ① — 우클릭 철거가 계속 무반응이던 진짜 이유.
        // 집(Home)은 화면 하단을 대화 박스·진행 버튼이 넓게 덮는다. 그 위에 가구가 겹쳐 보이는데,
        // 기존 가드가 `IsPointerOverGameObject()`(= 아무 그래픽이나 걸리면 true)여서 가구를 겨눈
        // 클릭이 통째로 삼켜졌다. **실제로 누를 수 있는 UI(Selectable)** 위일 때만 양보한다.
        private static readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> UiHits
            = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();

        private static bool PointerOverInteractiveUI()
        {
            UnityEngine.EventSystems.EventSystem events = UnityEngine.EventSystems.EventSystem.current;
            if (events == null) return false;
            var pointer = new UnityEngine.EventSystems.PointerEventData(events)
            {
                position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero,
            };
            UiHits.Clear();
            events.RaycastAll(pointer, UiHits);
            foreach (UnityEngine.EventSystems.RaycastResult hit in UiHits)
            {
                var selectable = hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>();
                if (selectable != null && selectable.interactable) return true;
            }
            return false;
        }

        private int FindPlacedIndex(PlacedFurnitureVisual visual)
        {
            for (int i = 0; i < _gameState.placedFurniture.Count; i++)
            {
                PlacedFurniture placed = _gameState.placedFurniture[i];
                if (placed.furnitureId != visual.FurnitureId) continue;
                if ((placed.position - visual.PlacedPosition).sqrMagnitude > 0.0001f) continue; // S-122 ② 스택 겹침 구분
                return i;
            }
            return -1;
        }

        // ── 낙하 (S-122 ④) ─────────────────────────────────
        // 배치물은 Rigidbody가 없는 정적 오브젝트라(프리팹·SpawnVisual 모두 미부착) 물리 낙하가 아니라
        // 좌표 재계산으로 내려앉힌다. 착지면 = 치운 지지대가 서 있던 높이.
        private void DropStacked(Bounds supportFootprint, float landingY)
        {
            float supportTopY = supportFootprint.max.y;
            for (int i = _visuals.Count - 1; i >= 0; i--)
            {
                PlacedFurnitureVisual visual = _visuals[i];
                if (visual == null) { _visuals.RemoveAt(i); continue; }
                // 치운 물건의 상단면에 실제로 얹혀 있던 것만 내려앉는다 — "y가 더 높다"로 잡으면
                // 벽걸이 TV·시계(y 1.0~1.6)까지 바닥으로 떨어진다.
                if (Mathf.Abs(visual.PlacedPosition.y - supportTopY) > 0.02f) continue;
                if (!TryWorldBounds(visual.transform, out Bounds bounds)) continue;
                if (bounds.max.x <= supportFootprint.min.x || bounds.min.x >= supportFootprint.max.x) continue;
                if (bounds.max.z <= supportFootprint.min.z || bounds.min.z >= supportFootprint.max.z) continue;

                int index = FindPlacedIndex(visual);
                if (index < 0) continue;
                PlacedFurniture entry = _gameState.placedFurniture[index];
                Vector3 landed = new Vector3(entry.position.x, landingY, entry.position.z);
                _gameState.placedFurniture[index] = new PlacedFurniture
                {
                    furnitureId = entry.furnitureId,
                    position = landed,
                    rotationY = entry.rotationY,
                };
                _visuals.RemoveAt(i);
                Destroy(visual.gameObject);
                SpawnVisual(entry.furnitureId, landed, entry.rotationY); // 새 높이로 재생성 (대장 자동 재등록)
                Debug.Log("[하우징] " + entry.furnitureId + " 낙하 — y "
                    + entry.position.y.ToString("0.00") + " → " + landingY.ToString("0.00"));
            }
        }

        // ── 벽 부착 (S-031 ⑤) — 벽 콜라이더 히트 → 벽면 중심 배치 + 법선 방향 ──
        private bool TryWallPoint(Ray ray, FurnitureSO so, out Vector3 position, out float yaw)
        {
            position = default;
            yaw = 0f;
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return false;
            if (!hit.collider.name.Contains("Wall")) return false;
            if (Mathf.Abs(hit.normal.y) > 0.3f) return false; // 천장·바닥면 제외

            // 부착점: 벽면에서 법선 방향으로 두께 절반 — position 규약은 "바닥 기준"이라 y를 절반 낮춰 저장.
            Vector3 center = hit.point + hit.normal * (so.size.z * 0.5f + 0.01f);
            center.y = Mathf.Max(so.size.y * 0.5f + 0.4f, Mathf.Round(hit.point.y / GRID) * GRID); // 그리드 스냅(높이)
            position = center - Vector3.up * (so.size.y * 0.5f);
            position.x = Mathf.Round(position.x / GRID) * GRID;
            yaw = Quaternion.LookRotation(hit.normal).eulerAngles.y;
            return true;
        }

        // ── 가구 상단 얹기 (S-122 ②) — 배치물 콜라이더를 맞으면 그 결합 바운즈 상단면이 새 바닥이 된다.
        // 카메라가 거의 수평(하향 8°)이라 상판만 허용하면 사실상 못 올린다 → 법선을 가리지 않고
        // "가구를 맞추면 위에 올린다"로 판정해 조준 난이도를 낮춘다.
        private bool TrySupportPoint(Ray ray, FurnitureSO so, out Vector3 position)
        {
            position = default;
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return false;
            PlacedFurnitureVisual support = hit.collider.GetComponentInParent<PlacedFurnitureVisual>();
            if (support == null) return false;
            if (!TryWorldBounds(support.transform, out Bounds top)) return false;

            Vector3 half = (so != null ? so.size : Vector3.one * 0.8f) * 0.5f;
            position = new Vector3(
                Mathf.Round(hit.point.x / GRID) * GRID,
                top.max.y,                                  // 지지대 상단면 = 새 가구의 바닥
                Mathf.Round(hit.point.z / GRID) * GRID);
            // 상판을 벗어나 공중에 뜨지 않게 지지면 안으로 당긴다 (지지면보다 큰 가구는 중앙 정렬).
            position.x = half.x * 2f >= top.size.x ? top.center.x
                : Mathf.Clamp(position.x, top.min.x + half.x, top.max.x - half.x);
            position.z = half.z * 2f >= top.size.z ? top.center.z
                : Mathf.Clamp(position.z, top.min.z + half.z, top.max.z - half.z);
            return true;
        }

        /// <summary>배치물의 월드 결합 바운즈 — 콜라이더 우선(회전 반영 AABB), 없으면 렌더러.</summary>
        private static bool TryWorldBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool has = false;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled) continue;
                if (has) bounds.Encapsulate(collider.bounds); else { bounds = collider.bounds; has = true; }
            }
            if (has) return true;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (has) bounds.Encapsulate(renderer.bounds); else { bounds = renderer.bounds; has = true; }
            }
            return has;
        }

        private string KoreanName(string furnitureId)
        {
            FurnitureSO so = Find(furnitureId);
            return so != null && !string.IsNullOrEmpty(so.displayName) ? so.displayName : furnitureId;
        }

        private FurnitureSO Find(string furnitureId)
        {
            if (_catalog != null)
                foreach (FurnitureSO item in _catalog)
                    if (item != null && item.furnitureId == furnitureId) return item;
            return null;
        }

        private void SpawnVisual(string furnitureId, Vector3 position, float rotationY)
        {
            FurnitureSO so = Find(furnitureId);
            Quaternion rotation = Quaternion.Euler(0f, rotationY, 0f);
            GameObject visual;

            if (so != null && so.prefab != null)
            {
                // S-201 — 모델 회전 보정을 **배치 회전 뒤에** 곱한다(로컬 기준). 그래야 플레이어가
                // 돌린 방향(rotationY)은 그대로 살고, 누워 나오는 모델만 제자리에서 세워진다.
                visual = Instantiate(so.prefab, position, rotation * Quaternion.Euler(so.prefabRotation));
                // S-173 ② — 모델 제작 스케일이 제각각이라 방 안에서 크기가 안 맞는다(침대가 작다).
                // 프리팹 원본을 건드리지 않고 SO에서 배율만 준다 — 다른 씬의 같은 프리팹은 그대로.
                if (!Mathf.Approximately(so.prefabScale, 1f))
                    visual.transform.localScale *= so.prefabScale;
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube); // 콜라이더 보존 — 클릭 재배치 판정용 (S-031 ①)
                Vector3 size = so != null ? so.size : Vector3.one * 0.8f;
                visual.transform.SetPositionAndRotation(position + Vector3.up * (size.y * 0.5f), rotation);
                visual.transform.localScale = size;
                visual.GetComponent<Renderer>().material.color = so != null ? so.color : Color.gray;
            }

            visual.name = "Furniture_" + furnitureId;
            if (visual.GetComponentInChildren<Collider>() == null)
                visual.AddComponent<BoxCollider>(); // 프리팹에 콜라이더가 없으면 클릭 판정용 부여
            PlacedFurnitureVisual marker = visual.AddComponent<PlacedFurnitureVisual>();
            marker.Bind(furnitureId, position, rotationY);
            _visuals.Add(marker); // S-122 ④ — 낙하 판정 대장
        }

        // ── 블루프린트 (반투명 시안 고스트) ──────────────────
        private GameObject BuildGhost(string furnitureId)
        {
            FurnitureSO so = Find(furnitureId);
            GameObject ghost;

            if (so != null && so.prefab != null)
            {
                ghost = Instantiate(so.prefab);
                foreach (Collider collider in ghost.GetComponentsInChildren<Collider>())
                    collider.enabled = false;
            }
            else
            {
                ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(ghost.GetComponent<Collider>());
                Vector3 size = so != null ? so.size : Vector3.one * 0.8f;
                ghost.transform.localScale = size;
                // 큐브 폴백은 피벗이 중심이라 바닥 기준으로 올린다.
                GameObject root = new GameObject("Ghost_" + furnitureId);
                ghost.transform.SetParent(root.transform, false);
                ghost.transform.localPosition = Vector3.up * (size.y * 0.5f);
                ghost = root;
            }

            ghost.name = "Ghost_" + furnitureId;
            foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>())
            {
                Material material = renderer.material; // 인스턴스 — 공유 머티리얼 무오염
                material.color = GhostColor;
                // URP Lit 투명 전환 (고스트 반투명).
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            return ghost;
        }

        private void ClearGhost()
        {
            if (_ghost == null) return;
            Destroy(_ghost);
            _ghost = null;
            _ghostId = null;
            _ghostOnWall = false;
        }
    }
}
