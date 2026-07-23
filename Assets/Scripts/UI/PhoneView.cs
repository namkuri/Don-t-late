using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 스마트폰 OS(View) — S-019 ⑥ 전면 개편. Tab으로 우하단 슬라이드, 홈 화면(앱 그리드)에서
    /// 앱 진입: 택배(바코드 상차·히스토리·수익)/음악/금융(투자)/은행/가구(하우징).
    /// 화면 위젯은 런타임 생성(폰 내부 UI는 씬 조립 대상이 아님 — 빌더는 본체 패널만).
    /// 상태 변경은 전부 매니저 Instance 명령으로 위임 — 여기는 표시·입력 라우팅만.
    /// </summary>
    public class PhoneView : MonoBehaviour
    {
        private const float SLIDE_SECONDS = 0.22f;

        public static bool IsOpen { get; private set; }
        /// <summary>가구 배치 대기 id — Home 씬 HomeFurniturePlacer가 소비 (S-019 ④).</summary>
        public static string PendingPlacementId;

        private enum Screen { Home, Delivery, Music, Invest, Bank, Furniture, Call, Map }

        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TuningConfigSO _tuning;
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private FurnitureSO[] _furnitureCatalog;
        [SerializeField] private float _hiddenY = -640f;
        [SerializeField] private float _shownY = 24f;

        [Header("아트 스왑 슬롯 (S-020 — 플레이스홀더 계약: 비면 코드 생성 폴백)")]
        [Tooltip("배경화면. 실아트가 오면 여기 꽂는다 — bom_id: ui_phone_wallpaper")]
        [SerializeField] private Sprite _wallpaper;
        [Tooltip("앱 아이콘 5종 (택배·음악·금융·은행·가구 순) — bom_id: ui_phone_icon_*")]
        [SerializeField] private Sprite[] _appIcons;
        [Tooltip("Travel 지도 앱 배경 일러 (S-036). 실아트(A-004)가 오면 여기 꽂는다 — bom_id: ui_map_town")]
        [SerializeField] private Sprite _mapSprite;

        private readonly List<DeliveryData> _scanned = new List<DeliveryData>();
        private readonly Dictionary<int, int> _status = new Dictionary<int, int>(); // 0진행 1완료 2지각
        private InputAction _toggle;
        private InputAction _close;   // S-032 ③ ESC·백스페이스
        private bool _inTitle = true; // S-032 ① 타이틀에선 폰 금지 (게임은 Main에서 시작)
        private bool _open;
        private Screen _screen = Screen.Home;
        private Coroutine _slide;
        private int _lastClockMinute = -1;

        private Transform _screenRoot;     // 화면 컨테이너들의 부모
        private readonly Dictionary<Screen, GameObject> _screens = new Dictionary<Screen, GameObject>();
        private TMP_Text _titleLabel;
        private TMP_Text _deliveryHover;
        private TMP_Text _deliveryList;
        private TMP_Text _deliveryWarn;
        private TMP_Text _musicLabel;
        private TMP_Text _investLabel;
        private RawImage _chartImage;   // S-032 ⑤ 시세 차트
        private Texture2D _chartTexture;
        private const int CHART_W = 200;
        private const int CHART_H = 64;
        private TMP_Text _bankLabel;
        private TMP_Text _furnitureLabel;
        private RectTransform _furnitureListContent; // S-030 ③ 인벤토리 스크롤 내용
        private TMP_Text _callLabel;                 // S-031 ⑧ 전화 수신 화면
        private Button _wallpaperButton;             // S-031 ④ 벽지 순환
        private Button _floorButton;                 // S-031 ④ 바닥 순환
        private string _furnitureInvSignature;       // 행 재구축 판정 (스크롤 리셋 방지)

        // ── S-036 다이제틱 폰 지도 (Travel 전용 앱 — 노드 버튼 UI 은퇴 대체) ──

        private struct MapPin
        {
            public string label;
            public string district;
            public bool locked;
            public bool far;
            public Vector2 pos; // 지도 영역 정규화 좌표
        }

        // 4구역 핀 — 활성 2(S-035 구역) + 잠금 2("준비 중"). 활성화 시 DeliveryOrderSO 상수로 승격.
        private static readonly MapPin[] Pins =
        {
            new MapPin { label = "빌라촌", district = DeliveryOrderSO.DISTRICT_VILLATOWN,
                         locked = false, far = false, pos = new Vector2(0.26f, 0.64f) },
            new MapPin { label = "먹자골목", district = DeliveryOrderSO.DISTRICT_FOODALLEY,
                         locked = false, far = true, pos = new Vector2(0.70f, 0.40f) },
            new MapPin { label = "아파트단지", district = "아파트단지",
                         locked = true, far = true, pos = new Vector2(0.74f, 0.78f) },
            new MapPin { label = "언덕주택가", district = "언덕주택가",
                         locked = true, far = false, pos = new Vector2(0.24f, 0.20f) },
        };
        private static readonly Vector2 MapOriginPos = new Vector2(0.5f, 0.07f); // 출발 마커 위치

        private const float TRAVEL_PANEL_W = 700f;   // 세로 풀스크린(1080 기준) 패널 규격
        private const float TRAVEL_PANEL_H = 1010f;
        private const float TRAVEL_HIDDEN_Y = -1080f;

        private RectTransform _mapArea;
        private RectTransform _routeLine;
        private TMP_Text _mapOriginLabel;
        private TMP_Text _mapInfoLabel;
        private Button _departButton;
        private int _selectedPin = -1;
        private bool _inTravel;
        private GameScene _prevScene = GameScene.Main;
        private string _travelOrigin = "물류캠프";
        private Vector2 _panelBaseSize;
        private float _panelBaseX;

        private float HiddenY => _inTravel ? TRAVEL_HIDDEN_Y : _hiddenY;

        // ── 수명주기 ─────────────────────────────────────────

        private void Awake()
        {
            _toggle = new InputAction("PhoneToggle", InputActionType.Button);
            _toggle.AddBinding("<Keyboard>/tab");
            _close = new InputAction("PhoneClose", InputActionType.Button); // S-032 ③
            _close.AddBinding("<Keyboard>/escape");
            _close.AddBinding("<Keyboard>/backspace");
        }

        private void OnEnable()
        {
            _toggle.Enable();
            _toggle.performed += OnToggle;
            _close.Enable();
            _close.performed += OnClosePressed;
            WorldEvents.BarcodeScanned += OnBarcodeScanned;
            WorldEvents.DeliveryCompleted += OnDeliveryCompleted;
            WorldEvents.DeliveryFailed += OnDeliveryFailed;
            WorldEvents.ClockTicked += OnClockTicked;
            WorldEvents.PhoneRang += OnPhoneRang; // S-031 ⑧ — 진상 전화 수신 화면
            WorldEvents.SceneTransitionCompleted += OnSceneChanged; // S-032 ①
            WorldEvents.DebtSettled += OnSettled; // S-034 ① — 정산 후 상차 리스트 초기화
        }

        private void OnDisable()
        {
            _toggle.performed -= OnToggle;
            _toggle.Disable();
            _close.performed -= OnClosePressed;
            _close.Disable();
            WorldEvents.BarcodeScanned -= OnBarcodeScanned;
            WorldEvents.DeliveryCompleted -= OnDeliveryCompleted;
            WorldEvents.DeliveryFailed -= OnDeliveryFailed;
            WorldEvents.ClockTicked -= OnClockTicked;
            WorldEvents.PhoneRang -= OnPhoneRang;
            WorldEvents.SceneTransitionCompleted -= OnSceneChanged;
            WorldEvents.DebtSettled -= OnSettled;
        }

        private void OnDestroy() { _toggle.Dispose(); _close.Dispose(); }

        private void Start()
        {
            if (_panel != null)
            {
                _panelBaseSize = _panel.sizeDelta;       // S-036 — Travel 확대 후 원복 기준
                _panelBaseX = _panel.anchoredPosition.x;
                _panel.anchoredPosition = new Vector2(_panel.anchoredPosition.x, _hiddenY);
            }
            BuildUI();
            ShowScreen(Screen.Home);
        }

        private void Update()
        {
            if (!_open) return;
            if (_screen == Screen.Delivery) ScanPointer();
        }

        // ── 이벤트 핸들러 ────────────────────────────────────

        private void OnSceneChanged(GameScene scene)
        {
            _inTitle = scene == GameScene.Main;
            bool wasTravel = _inTravel;
            _inTravel = scene == GameScene.Travel;

            if (_inTravel)
            {
                // S-036: 출발지 = 직전 위치 자동 라벨 (District에서 오면 마지막 구역, 그 외 물류캠프).
                _travelOrigin = _prevScene == GameScene.District && _gameState != null
                    && !string.IsNullOrEmpty(_gameState.currentDistrict)
                    ? _gameState.currentDistrict : "물류캠프";
                _selectedPin = -1;
                ApplyPanelLayout();
                if (!_open) OnToggle(default);   // 지도 앱 자동 오픈
                ShowScreen(Screen.Map);
            }
            else if (wasTravel)
            {
                ApplyPanelLayout();              // 패널 원복
                if (_open) OnToggle(default);    // Travel 이탈 = 지도 수납
                else if (_slide == null && _panel != null)
                    _panel.anchoredPosition = new Vector2(_panel.anchoredPosition.x, HiddenY);
                if (_screen == Screen.Map) ShowScreen(Screen.Home);
            }

            if (_inTitle && _open) OnToggle(default); // 타이틀 복귀 시 강제 수납
            _prevScene = scene;
        }

        // S-034 ①: 정산 = 하루 마감 — 상차 리스트를 비운다 (히스토리는 GameState 소유라 유지).
        private void OnSettled(DebtSettlement _)
        {
            _scanned.Clear();
            _status.Clear();
            RefreshCurrent();
        }

        private void OnBarcodeScanned(DeliveryData data) { _scanned.Add(data); RefreshCurrent(); }
        private void OnDeliveryCompleted(DeliveryData data) { _status[data.OrderId] = 1; RefreshCurrent(); }
        private void OnDeliveryFailed(DeliveryData data) { _status[data.OrderId] = 2; RefreshCurrent(); }

        private void OnClockTicked(GameClock clock)
        {
            _lastClockMinute = clock.MinuteOfDay;
            if (_statusClock != null)
                _statusClock.text = clock.Hour.ToString("00") + ":" + clock.Minute.ToString("00");
            if (_open) RefreshCurrent();
        }

        private void OnClosePressed(InputAction.CallbackContext _)
        {
            if (_open) OnToggle(default); // 열려 있을 때만 닫는다 (S-032 ③)
        }

        private void OnToggle(InputAction.CallbackContext _)
        {
            if (_inTitle && !_open) return; // S-032 ① — 타이틀에선 열지 않는다
            _open = !_open;
            IsOpen = _open;
            WorldAudioManager.Instance?.PlayPhoneToggleSfx(); // AU-008 — 개폐 공용
            if (_slide != null) StopCoroutine(_slide);
            _slide = StartCoroutine(Slide(_open ? _shownY : HiddenY));
            if (_open) { ShowScreen(_inTravel ? Screen.Map : Screen.Home); } // S-036 — Travel 기본 앱 = 지도
        }

        private IEnumerator Slide(float targetY)
        {
            float startY = _panel.anchoredPosition.y;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / SLIDE_SECONDS;
                _panel.anchoredPosition = new Vector2(
                    _panel.anchoredPosition.x, Mathf.Lerp(startY, targetY, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }
            _slide = null;
        }

        // ── 화면 전환 ────────────────────────────────────────

        private void ShowScreen(Screen screen)
        {
            _screen = screen;
            foreach (var pair in _screens) pair.Value.SetActive(pair.Key == screen);
            if (_titleLabel != null)
                _titleLabel.text = screen switch
                {
                    Screen.Delivery => "배송상차",
                    Screen.Music => "음악",
                    Screen.Invest => "늦코인",
                    Screen.Bank => "은행",
                    Screen.Call => "전화",
                    Screen.Furniture => "가구",
                    Screen.Map => "지도",
                    _ => "홈"
                };
            RefreshCurrent();
        }

        private void RefreshCurrent()
        {
            switch (_screen)
            {
                case Screen.Delivery: RefreshDelivery(); break;
                case Screen.Music: RefreshMusic(); break;
                case Screen.Invest: RefreshInvest(); break;
                case Screen.Bank: RefreshBank(); break;
                case Screen.Call: RefreshCall(); break;
                case Screen.Furniture: RefreshFurniture(); break;
                case Screen.Map: RefreshMap(); break;
            }
        }

        // S-036: Travel에선 폰이 세로 풀스크린 지도 앱 — 패널 중앙 확대, 이탈 시 원복.
        private void ApplyPanelLayout()
        {
            if (_panel == null) return;
            if (_inTravel)
            {
                float canvasWidth = ((RectTransform)_panel.parent).rect.width;
                _panel.sizeDelta = new Vector2(TRAVEL_PANEL_W, TRAVEL_PANEL_H);
                _panel.anchoredPosition = new Vector2(
                    -(canvasWidth - TRAVEL_PANEL_W) * 0.5f, _panel.anchoredPosition.y);
            }
            else
            {
                _panel.sizeDelta = _panelBaseSize;
                _panel.anchoredPosition = new Vector2(_panelBaseX, _panel.anchoredPosition.y);
            }
        }

        // ── UI 구축 (런타임) ─────────────────────────────────

        private TMP_Text _statusClock;

        private void BuildUI()
        {
            Transform screenBg = _panel.Find("Screen");
            if (screenBg == null) return;

            // 기존 빌더 산출물(구 배송상차 위젯)이 남아 있으면 청소 — v2는 전부 런타임 생성.
            for (int i = screenBg.childCount - 1; i >= 0; i--) Destroy(screenBg.GetChild(i).gameObject);

            // 배경화면 (S-020) — 실아트 스왑 슬롯, 폴백 = 남보라 세로 그라디언트.
            GameObject wall = new GameObject("Wallpaper", typeof(RectTransform));
            wall.transform.SetParent(screenBg, false);
            Image wallImage = wall.AddComponent<Image>();
            wallImage.sprite = _wallpaper != null ? _wallpaper : GradientSprite();
            wallImage.color = Color.white;
            wallImage.raycastTarget = false;
            RectTransform wallRect = (RectTransform)wall.transform;
            wallRect.anchorMin = Vector2.zero;
            wallRect.anchorMax = Vector2.one;
            wallRect.offsetMin = wallRect.offsetMax = Vector2.zero;

            // 상태바 — 시계·통신사·배터리 (실사 폰 감각).
            _statusClock = MakeText(screenBg, "StatusClock", "--:--", 22f, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(_statusClock.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(18f, -8f), 28f);
            TMP_Text carrier = MakeText(screenBg, "StatusRight", "LateTel LTE 100%", 20f, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.TopRight);
            Anchor(carrier.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -8f), 28f);

            _titleLabel = MakeText(screenBg, "Title", "홈", 34f, Color.white, TextAlignmentOptions.Top);
            Anchor(_titleLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), 44f);

            Button home = MakeButton(screenBg, "HomeBtn", "홈", () => ShowScreen(Screen.Home));
            RectTransform homeRect = (RectTransform)home.transform;
            homeRect.anchorMin = homeRect.anchorMax = homeRect.pivot = new Vector2(1f, 1f);
            homeRect.sizeDelta = new Vector2(54f, 44f);
            homeRect.anchoredPosition = new Vector2(-10f, -38f);

            _screenRoot = new GameObject("Screens", typeof(RectTransform)).transform;
            _screenRoot.SetParent(screenBg, false);
            RectTransform rootRect = (RectTransform)_screenRoot;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = new Vector2(16f, 14f);
            rootRect.offsetMax = new Vector2(-16f, -88f); // 상태바+타이틀 아래부터

            BuildHomeScreen();
            BuildDeliveryScreen();
            BuildMusicScreen();
            BuildInvestScreen();
            BuildBankScreen();
            BuildCallScreen(); // S-031 ⑧
            BuildFurnitureScreen();
            BuildMapScreen();  // S-036
        }

        private GameObject NewScreen(Screen key)
        {
            GameObject go = new GameObject(key.ToString(), typeof(RectTransform));
            go.transform.SetParent(_screenRoot, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _screens[key] = go;
            return go;
        }

        private void BuildHomeScreen()
        {
            GameObject screen = NewScreen(Screen.Home);
            (string emoji, string label, Screen target, Color tint)[] apps =
            {
                ("택", "택배", Screen.Delivery, new Color(0.95f, 0.62f, 0.25f)),
                ("음", "음악", Screen.Music, new Color(0.62f, 0.45f, 0.95f)),
                ("금", "금융", Screen.Invest, new Color(0.30f, 0.78f, 0.50f)),
                ("은", "은행", Screen.Bank, new Color(0.32f, 0.56f, 0.92f)),
                ("가", "가구", Screen.Furniture, new Color(0.75f, 0.52f, 0.35f)),
            };
            for (int i = 0; i < apps.Length; i++)
            {
                Screen target = apps[i].target;

                // 아이콘 타일 — 라운드 사각 + 앱 색 (실아트 스왑 슬롯 _appIcons[i] 우선).
                GameObject tile = new GameObject("App_" + target, typeof(RectTransform));
                tile.transform.SetParent(screen.transform, false);
                Image tileImage = tile.AddComponent<Image>();
                if (_appIcons != null && i < _appIcons.Length && _appIcons[i] != null)
                {
                    tileImage.sprite = _appIcons[i];
                    tileImage.color = Color.white;
                }
                else
                {
                    tileImage.sprite = RoundedSprite();
                    tileImage.type = Image.Type.Sliced;
                    tileImage.color = apps[i].tint;
                }
                Button button = tile.AddComponent<Button>();
                button.targetGraphic = tileImage;
                button.onClick.AddListener(() => ShowScreen(target));

                RectTransform rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(96f, 96f);
                rect.anchoredPosition = new Vector2(22f + (i % 3) * 124f, -22f - (i / 3) * 138f);

                TMP_Text emoji = MakeText(tile.transform, "Emoji", apps[i].emoji, 44f, Color.white, TextAlignmentOptions.Center);
                RectTransform emojiRect = emoji.rectTransform;
                emojiRect.anchorMin = Vector2.zero;
                emojiRect.anchorMax = Vector2.one;
                emojiRect.offsetMin = emojiRect.offsetMax = Vector2.zero;

                TMP_Text name = MakeText(tile.transform, "Name", apps[i].label, 20f, Color.white, TextAlignmentOptions.Center);
                RectTransform nameRect = name.rectTransform;
                nameRect.anchorMin = new Vector2(0f, 0f);
                nameRect.anchorMax = new Vector2(1f, 0f);
                nameRect.pivot = new Vector2(0.5f, 1f);
                nameRect.anchoredPosition = new Vector2(0f, -6f);
                nameRect.sizeDelta = new Vector2(0f, 26f);
            }
        }

        // ── 코드 생성 폴백 스프라이트 (S-020 — 실아트 오면 인스펙터 슬롯이 대체) ──

        private static Sprite _roundedCache;
        private static Sprite _gradientCache;

        private static Sprite RoundedSprite()
        {
            if (_roundedCache != null) return _roundedCache;
            const int size = 64, radius = 18;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    float dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    bool inside = dx * dx + dy * dy <= radius * radius;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            tex.Apply();
            _roundedCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return _roundedCache;
        }

        private static Sprite GradientSprite()
        {
            if (_gradientCache != null) return _gradientCache;
            const int h = 128;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false);
            Color bottom = new Color(0.05f, 0.06f, 0.12f);
            Color top = new Color(0.14f, 0.10f, 0.24f);
            for (int y = 0; y < h; y++) tex.SetPixel(0, y, Color.Lerp(bottom, top, (float)y / h));
            tex.Apply();
            _gradientCache = Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f));
            return _gradientCache;
        }

        private void BuildDeliveryScreen()
        {
            GameObject screen = NewScreen(Screen.Delivery);
            _deliveryHover = MakeText(screen.transform, "Hover", "-", 28f, new Color(0.208f, 0.878f, 0.784f), TextAlignmentOptions.Top);
            Anchor(_deliveryHover.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), 38f);
            _deliveryWarn = MakeText(screen.transform, "Warn", "", 22f, new Color(1f, 0.45f, 0.35f), TextAlignmentOptions.Top);
            Anchor(_deliveryWarn.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -40f), 30f);
            // S-034 ③: 리스트가 폰을 뚫고 내려가던 것 — 스크롤 영역으로 격리.
            GameObject viewport = new GameObject("ListViewport", typeof(RectTransform));
            viewport.transform.SetParent(screen.transform, false);
            RectTransform vpRect = (RectTransform)viewport.transform;
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(4f, 4f);
            vpRect.offsetMax = new Vector2(-4f, -76f);
            Image vpBg = viewport.AddComponent<Image>(); // 스크롤 드래그 타겟
            vpBg.color = new Color(1f, 1f, 1f, 0.02f);
            viewport.AddComponent<RectMask2D>();

            _deliveryList = MakeText(viewport.transform, "List", "", 22f, Color.white, TextAlignmentOptions.TopLeft);
            RectTransform listRect = _deliveryList.rectTransform;
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.sizeDelta = new Vector2(0f, 100f); // 높이는 ContentSizeFitter가 갱신
            listRect.anchoredPosition = Vector2.zero;
            _deliveryList.gameObject.AddComponent<ContentSizeFitter>().verticalFit
                = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = vpRect;
            scroll.content = listRect;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 24f;
        }

        private void BuildMusicScreen()
        {
            GameObject screen = NewScreen(Screen.Music);
            _musicLabel = MakeText(screen.transform, "Info", "", 24f, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(_musicLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), 250f); // S-032 ② 플레이리스트 영역 확보

            (string label, System.Action act)[] controls =
            {
                ("재생/정지", () => WorldAudioManager.Instance?.TogglePause()),
                ("다음곡", () => WorldAudioManager.Instance?.NextTrack()),
                ("볼륨-", () => WorldAudioManager.Instance?.SetVolume(WorldAudioManager.Instance.Volume - 0.1f)),
                ("볼륨+", () => WorldAudioManager.Instance?.SetVolume(WorldAudioManager.Instance.Volume + 0.1f)),
            };
            for (int i = 0; i < controls.Length; i++)
            {
                System.Action act = controls[i].act;
                Button b = MakeButton(screen.transform, "Ctl" + i, controls[i].label, () => { act(); RefreshMusic(); });
                RectTransform rect = (RectTransform)b.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(84f, 60f);
                rect.anchoredPosition = new Vector2(6f + i * 96f, -260f); // S-032 ② 하향 — 위 250px는 플레이리스트 몫
            }
            // 곡선택 — 현재 슬롯 풀 1~4번
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                Button b = MakeButton(screen.transform, "Track" + i, (i + 1) + "번", () =>
                {
                    WorldAudioManager.Instance?.PlayTrackAt(index);
                    RefreshMusic();
                });
                RectTransform rect = (RectTransform)b.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(84f, 48f);
                rect.anchoredPosition = new Vector2(6f + i * 96f, -334f); // S-032 ②
            }
        }

        private void BuildInvestScreen()
        {
            GameObject screen = NewScreen(Screen.Invest);
            _investLabel = MakeText(screen.transform, "Info", "", 24f, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(_investLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), 210f);

            // S-032 5: 시세 차트 - 과거 240게임분을 결정론 시세식으로 재계산해 그린다.
            GameObject chartGo = new GameObject("Chart", typeof(RectTransform));
            chartGo.transform.SetParent(screen.transform, false);
            _chartImage = chartGo.AddComponent<RawImage>();
            _chartTexture = new Texture2D(CHART_W, CHART_H, TextureFormat.RGBA32, false);
            _chartTexture.filterMode = FilterMode.Point;
            _chartImage.texture = _chartTexture;
            RectTransform chartRect = (RectTransform)chartGo.transform;
            chartRect.anchorMin = new Vector2(0f, 1f);
            chartRect.anchorMax = new Vector2(1f, 1f);
            chartRect.pivot = new Vector2(0.5f, 1f);
            chartRect.offsetMin = new Vector2(6f, 0f);
            chartRect.offsetMax = new Vector2(-6f, 0f);
            chartRect.sizeDelta = new Vector2(chartRect.sizeDelta.x, 130f);
            chartRect.anchoredPosition = new Vector2(0f, -215f);

            Button buy = MakeButton(screen.transform, "Buy", "1개 매수", () =>
            {
                if (WorldDebtManager.Instance != null && !WorldDebtManager.Instance.BuyOneCoin())
                    _investLabel.text += "\n<color=#ff7359>잔액 부족</color>";
                RefreshInvest(); // 매수 성공 SFX·플로팅은 MoneySpent 이벤트가 처리 (S-030)
            });
            RectTransform buyRect = (RectTransform)buy.transform;
            buyRect.anchorMin = buyRect.anchorMax = buyRect.pivot = new Vector2(0f, 0f);
            buyRect.sizeDelta = new Vector2(180f, 62f);
            buyRect.anchoredPosition = new Vector2(6f, 78f);

            Button sellOne = MakeButton(screen.transform, "SellOne", "1개 매도", () =>
            {
                int gained = WorldDebtManager.Instance != null ? WorldDebtManager.Instance.SellOneCoin() : 0;
                RefreshInvest();
                if (gained > 0) WorldAudioManager.Instance?.PlayCoinSfx();
            });
            RectTransform sellOneRect = (RectTransform)sellOne.transform;
            sellOneRect.anchorMin = sellOneRect.anchorMax = sellOneRect.pivot = new Vector2(1f, 0f);
            sellOneRect.sizeDelta = new Vector2(180f, 62f);
            sellOneRect.anchoredPosition = new Vector2(-6f, 78f);

            Button sellAll = MakeButton(screen.transform, "SellAll", "전량 매도", () =>
            {
                int gained = WorldDebtManager.Instance != null ? WorldDebtManager.Instance.SellAllCoin() : 0;
                RefreshInvest();
                if (gained > 0)
                {
                    _investLabel.text += "\n<color=#35e0c8>+₩" + gained.ToString("N0") + " 회수</color>";
                    WorldAudioManager.Instance?.PlayCoinSfx();
                }
            });
            RectTransform sellAllRect = (RectTransform)sellAll.transform;
            sellAllRect.anchorMin = sellAllRect.anchorMax = sellAllRect.pivot = new Vector2(0.5f, 0f);
            sellAllRect.sizeDelta = new Vector2(180f, 56f);
            sellAllRect.anchoredPosition = new Vector2(0f, 10f);
        }

        // 차트 그리기 (S-032 ⑤ → S-033 ② 캔들 개편) — 15게임분 캔들 16개 + 평단가 수평 점선.
        private void RedrawChart()
        {
            if (_chartTexture == null || WorldDebtManager.Instance == null) return;
            var debt = WorldDebtManager.Instance;
            float now = _gameState.day * 1440f + _gameState.minuteOfDay;
            const int CANDLES = 16;
            const float CANDLE_MIN = 15f; // 캔들당 15게임분 (총 4게임시간)

            // OHLC 수집 — 결정론 시세식이라 분 단위 재계산으로 고가·저가를 얻는다.
            var open = new int[CANDLES]; var close = new int[CANDLES];
            var high = new int[CANDLES]; var low = new int[CANDLES];
            int min = int.MaxValue, max = int.MinValue;
            for (int c = 0; c < CANDLES; c++)
            {
                float start = now - CANDLE_MIN * (CANDLES - c);
                open[c] = debt.CoinPriceAt(start);
                close[c] = debt.CoinPriceAt(start + CANDLE_MIN);
                high[c] = int.MinValue; low[c] = int.MaxValue;
                for (int m = 0; m <= 15; m++)
                {
                    int p = debt.CoinPriceAt(start + m);
                    if (p > high[c]) high[c] = p;
                    if (p < low[c]) low[c] = p;
                }
                if (low[c] < min) min = low[c];
                if (high[c] > max) max = high[c];
            }

            // 평단가 — 보유가 있으면 범위에 포함해 선이 화면 안에 오게 한다.
            float avgCost = _gameState.coinUnits > 0f ? _gameState.coinCostBasis / _gameState.coinUnits : -1f;
            if (avgCost > 0f)
            {
                if (avgCost < min) min = Mathf.FloorToInt(avgCost);
                if (avgCost > max) max = Mathf.CeilToInt(avgCost);
            }
            if (max == min) max = min + 1;

            Color bg = new Color(0.05f, 0.07f, 0.11f, 0.95f);
            Color up = new Color(1f, 0.35f, 0.35f);   // 양봉 = 빨강 (국장)
            Color down = new Color(0.35f, 0.55f, 1f); // 음봉 = 파랑
            var pixels = new Color[CHART_W * CHART_H];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

            int Y(float price) => 3 + Mathf.RoundToInt((price - min) / (max - min) * (CHART_H - 7));

            int slot = CHART_W / CANDLES; // 12px — 몸통 8 + 여백
            for (int c = 0; c < CANDLES; c++)
            {
                Color color = close[c] >= open[c] ? up : down;
                int x0 = c * slot + 2;
                int xMid = x0 + slot / 2 - 1;

                for (int y = Y(low[c]); y <= Y(high[c]); y++)             // 꼬리 (1px)
                    pixels[y * CHART_W + xMid] = color;
                int bodyTop = Y(Mathf.Max(open[c], close[c]));
                int bodyBottom = Y(Mathf.Min(open[c], close[c]));
                for (int y = bodyBottom; y <= bodyTop; y++)               // 몸통 (slot-4 px)
                    for (int x = x0; x < x0 + slot - 4; x++)
                        pixels[y * CHART_W + x] = color;
            }

            // S-033 ②: 평단가 수평 점선 (시안 — 4px on / 3px off).
            if (avgCost > 0f)
            {
                int ay = Y(avgCost);
                Color avgLine = new Color(0.208f, 0.878f, 0.784f);
                for (int x = 0; x < CHART_W; x++)
                    if (x % 7 < 4) pixels[ay * CHART_W + x] = avgLine;
            }

            _chartTexture.SetPixels(pixels);
            _chartTexture.Apply();
        }

        private void BuildBankScreen()
        {
            GameObject screen = NewScreen(Screen.Bank);
            _bankLabel = MakeText(screen.transform, "Info", "", 26f, Color.white, TextAlignmentOptions.TopLeft);
            RectTransform rect = _bankLabel.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            // S-028 ④ 테스트 전용 — 튜닝·시연용 입금 버튼. 릴리스 전 삭제 예정 (님 지시).
            Button cheat = MakeButton(screen.transform, "TestDeposit", "[테스트] +₩1,000", () =>
            {
                _gameState.money += 1000;
                RefreshBank();
            });
            RectTransform cheatRect = (RectTransform)cheat.transform;
            cheatRect.anchorMin = cheatRect.anchorMax = cheatRect.pivot = new Vector2(0.5f, 0f);
            cheatRect.sizeDelta = new Vector2(300f, 56f);
            cheatRect.anchoredPosition = new Vector2(0f, 8f);
        }

        private void BuildFurnitureScreen()
        {
            GameObject screen = NewScreen(Screen.Furniture);
            _furnitureLabel = MakeText(screen.transform, "Info", "", 22f, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(_furnitureLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), 64f);

            // S-030 ③: 보유 인벤토리 스크롤 영역 — 늘어나도 하단 구매 버튼과 안 겹친다.
            GameObject viewport = new GameObject("InvViewport", typeof(RectTransform));
            viewport.transform.SetParent(screen.transform, false);
            RectTransform vpRect = (RectTransform)viewport.transform;
            vpRect.anchorMin = new Vector2(0f, 0.46f);
            vpRect.anchorMax = new Vector2(1f, 1f);
            vpRect.offsetMin = new Vector2(4f, 0f);
            vpRect.offsetMax = new Vector2(-4f, -70f);
            Image vpBg = viewport.AddComponent<Image>(); // 스크롤 드래그 타겟
            vpBg.color = new Color(1f, 1f, 1f, 0.03f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("InvContent", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            _furnitureListContent = (RectTransform)content.transform;
            _furnitureListContent.anchorMin = new Vector2(0f, 1f);
            _furnitureListContent.anchorMax = new Vector2(1f, 1f);
            _furnitureListContent.pivot = new Vector2(0.5f, 1f);
            _furnitureListContent.sizeDelta = new Vector2(0f, 10f);

            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = vpRect;
            scroll.content = _furnitureListContent;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 24f;

            // 구매 버튼 — 하단 고정 2×2 (스크롤 영역 밖).
            if (_furnitureCatalog == null) return;
            for (int i = 0; i < _furnitureCatalog.Length && i < 4; i++)
            {
                FurnitureSO item = _furnitureCatalog[i];
                if (item == null) continue;
                Button buy = MakeButton(screen.transform, "Buy_" + item.furnitureId,
                    item.displayName + "\n₩" + item.price.ToString("N0"), () => BuyFurniture(item));
                RectTransform rect = (RectTransform)buy.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0f);
                rect.sizeDelta = new Vector2(184f, 66f);
                rect.anchoredPosition = new Vector2(6f + (i % 2) * 196f, 160f - (i / 2) * 76f);
            }

            // S-031 ④: 벽지·바닥 팔레트 순환 (무료 — 팔레트는 HomeDecorator와 공유).
            _wallpaperButton = MakeButton(screen.transform, "Wallpaper", "", () =>
            {
                _gameState.wallpaperIndex = (_gameState.wallpaperIndex + 1) % HomeDecorator.WallPalette.Length;
                WorldAudioManager.Instance?.PlayUiTickSfx(); // AU-010
                RefreshDecorButtons();
            });
            RectTransform wallRect = (RectTransform)_wallpaperButton.transform;
            wallRect.anchorMin = wallRect.anchorMax = wallRect.pivot = new Vector2(0f, 0f);
            wallRect.sizeDelta = new Vector2(184f, 66f);
            wallRect.anchoredPosition = new Vector2(6f, 8f);

            _floorButton = MakeButton(screen.transform, "Floor", "", () =>
            {
                _gameState.floorIndex = (_gameState.floorIndex + 1) % HomeDecorator.FloorPalette.Length;
                WorldAudioManager.Instance?.PlayUiTickSfx(); // AU-010
                RefreshDecorButtons();
            });
            RectTransform floorRect = (RectTransform)_floorButton.transform;
            floorRect.anchorMin = floorRect.anchorMax = floorRect.pivot = new Vector2(0f, 0f);
            floorRect.sizeDelta = new Vector2(184f, 66f);
            floorRect.anchoredPosition = new Vector2(202f, 8f);

            RefreshDecorButtons();
        }

        private void RefreshDecorButtons()
        {
            if (_wallpaperButton != null)
                _wallpaperButton.GetComponentInChildren<TMP_Text>().text =
                    "벽지 ▶ " + HomeDecorator.WallPalette[_gameState.wallpaperIndex % HomeDecorator.WallPalette.Length].name;
            if (_floorButton != null)
                _floorButton.GetComponentInChildren<TMP_Text>().text =
                    "바닥 ▶ " + HomeDecorator.FloorPalette[_gameState.floorIndex % HomeDecorator.FloorPalette.Length].name;
        }

        // ── 앱 로직 (표시 + 매니저 위임) ─────────────────────

        private void BuyFurniture(FurnitureSO item)
        {
            if (WorldDebtManager.Instance == null) return;
            if (!WorldDebtManager.Instance.TrySpend(item.price))
            {
                _furnitureLabel.text = "<color=#ff7359>잔액 부족 — " + item.displayName + "</color>";
                return;
            }
            _gameState.ownedFurnitureIds.Add(item.furnitureId);
            RefreshFurniture();
        }

        // S-030 ③: 인벤토리에서 고른 가구로 배치 개시 — 고스트·R회전·ESC취소는 HomeFurniturePlacer 몫.
        private void BeginPlacement(string furnitureId)
        {
            PendingPlacementId = furnitureId;
            _furnitureLabel.text = "<color=#35e0c8>집 바닥 클릭=배치 · R=회전 · ESC=취소 — "
                + KoreanName(furnitureId) + "</color>";
            if (_open) OnToggle(default); // 폰 닫고 배치 모드
        }

        private string KoreanName(string furnitureId)
        {
            if (_furnitureCatalog != null)
                foreach (FurnitureSO item in _furnitureCatalog)
                    if (item != null && item.furnitureId == furnitureId) return item.displayName;
            return furnitureId;
        }

        private void RefreshDelivery()
        {
            if (_deliveryList == null) return;
            var sb = new System.Text.StringBuilder();

            // S-034 ①: 상차 여부 — cargo(트럭 적재분)가 정본. 스캔만 한 건은 '미상차' 경고.
            var loadedIds = new HashSet<int>();
            foreach (DeliveryOrderSO order in _gameState.cargo)
                if (order != null) loadedIds.Add(order.orderId);
            var placedIds = new HashSet<int>();
            foreach (PlacedDelivery placed in _gameState.placedDeliveries) placedIds.Add(placed.orderId);

            DeliveryData? urgent = null;
            foreach (DeliveryData d in _scanned)
            {
                if (_status.TryGetValue(d.OrderId, out int st) && st != 0) continue;
                if (!loadedIds.Contains(d.OrderId)) continue; // S-034 — 실은 것만이 갈 곳
                if (urgent == null || d.DeadlineMinuteOfDay < urgent.Value.DeadlineMinuteOfDay) urgent = d;
            }
            if (urgent != null && !string.IsNullOrEmpty(urgent.Value.District))
                sb.Append("가야 할 구역  <color=#ff9f45><b>").Append(urgent.Value.District).Append("</b></color>\n");

            sb.Append("<color=#8a93a8>No 운송장     순번 목적지</color>\n");
            var byDeadline = new List<DeliveryData>(_scanned);
            byDeadline.Sort((a, b) => a.DeadlineMinuteOfDay.CompareTo(b.DeadlineMinuteOfDay));
            for (int i = 0; i < _scanned.Count; i++)
            {
                DeliveryData d = _scanned[i];
                int rank = byDeadline.FindIndex(x => x.OrderId == d.OrderId) + 1;
                string row = (i + 1) + " " + Invoice(d.OrderId) + "  " + rank + "  " + d.Address;
                int status = _status.TryGetValue(d.OrderId, out int s) ? s : 0;
                if (status == 1) sb.Append("<color=#8a93a8>").Append(row).Append(" ✓</color>\n");
                else if (status == 2) sb.Append("<color=#ff7359><s>").Append(row).Append("</s> 지각</color>\n");
                else if (!loadedIds.Contains(d.OrderId))
                {
                    sb.Append(row).Append("  <color=#ff9f45>미상차</color>\n"); // 캠프에서 실어야 스폰된다
                }
                else
                {
                    sb.Append(row).Append(placedIds.Contains(d.OrderId)
                        ? "  <color=#35e0c8>배치됨</color>" : "  <color=#35e0c8><b>상차완료</b></color>").Append('\n');
                    if (_lastClockMinute >= 0)
                    {
                        int remain = Mathf.RoundToInt(d.DeadlineMinuteOfDay) - _lastClockMinute;
                        sb.Append("<size=78%><color=#8a93a8>  └ ").Append(d.District).Append(" · ")
                          .Append(remain >= 0 ? "남은 " + remain + "분" : "마감 지남").Append("</color></size>\n");
                    }
                }
            }
            if (_scanned.Count == 0) sb.Append("<color=#8a93a8>박스를 클릭해 송장을 찍어라</color>\n");

            // 히스토리·수익 (S-019 ⑥)
            sb.Append("\n<color=#8a93a8>── 히스토리 (최근 4) ──</color>\n");
            int from = Mathf.Max(0, _gameState.deliveryHistory.Count - 4);
            for (int i = _gameState.deliveryHistory.Count - 1; i >= from; i--)
            {
                DeliveryRecord r = _gameState.deliveryHistory[i];
                sb.Append("<size=82%>").Append(r.day).Append("일 ")
                  .Append(r.minuteOfDay / 60).Append(':').Append((r.minuteOfDay % 60).ToString("00"))
                  .Append("  ").Append(r.address).Append("  <color=#35e0c8>+₩").Append(r.reward.ToString("N0"))
                  .Append("</color></size>\n");
            }
            if (_gameState.deliveryHistory.Count == 0) sb.Append("<size=82%><color=#8a93a8>아직 없음</color></size>\n");
            sb.Append("누적 수익  <color=#35e0c8>₩").Append(_gameState.totalEarned.ToString("N0")).Append("</color>");

            _deliveryList.text = sb.ToString();
        }

        private void RefreshMusic()
        {
            if (_musicLabel == null) return;
            var audio = WorldAudioManager.Instance;
            if (audio == null) { _musicLabel.text = "오디오 매니저 없음"; return; }

            var sb = new System.Text.StringBuilder();
            sb.Append("현재 곡\n<color=#35e0c8>")
              .Append(audio.CurrentClip != null ? Shorten(audio.CurrentClip.name) : "(무음)")
              .Append("</color>\n상태 ").Append(audio.IsPaused ? "⏸ 정지" : "▶ 재생")
              .Append("   볼륨 ").Append(Mathf.RoundToInt(audio.Volume * 100)).Append("%\n\n곡 목록 (")
              .Append(audio.CurrentSlot).Append(")\n");
            List<string> names = audio.TrackNames();
            for (int i = 0; i < names.Count; i++)
                sb.Append("<size=80%>").Append(i + 1).Append(". ").Append(Shorten(names[i])).Append("</size>\n");
            _musicLabel.text = sb.ToString();
        }

        private void RefreshInvest()
        {
            if (_investLabel == null || WorldDebtManager.Instance == null) return;
            int price = WorldDebtManager.Instance.CoinPrice();
            float held = _gameState.coinUnits;
            int valuation = Mathf.RoundToInt(held * price);
            int profit = valuation - Mathf.RoundToInt(_gameState.coinCostBasis);

            // S-032 5: 차익 = 평가액 - 매수원가. + = 빨강, - = 파랑 (국장 색).
            string profitLine = held > 0f
                ? "차익  " + (profit >= 0
                    ? "<color=#ff5a5a>+₩" + profit.ToString("N0") + "</color>"
                    : "<color=#5a8cff>-₩" + Mathf.Abs(profit).ToString("N0") + "</color>")
                  + "  <size=76%>(매수원가 ₩" + Mathf.RoundToInt(_gameState.coinCostBasis).ToString("N0") + ")</size>"
                : "<color=#8a93a8>보유 없음 - 1개 매수로 시작</color>";

            _investLabel.text =
                "늦코인 시세  <color=#ff9f45>₩" + price.ToString("N0") + "</color>  <size=70%>(1개 단위 거래)</size>\n" +
                "보유  " + held.ToString("0") + "개  평가 ₩" + valuation.ToString("N0") + "\n" +
                profitLine + "\n" +
                "잔액  ₩" + _gameState.money.ToString("N0");
            RedrawChart();
        }

        // ── 전화 수신 (S-031 ⑧) — PhoneRang(진상 전화)이 오면 폰이 열리며 이 화면이 뜬다 ──

        private void OnPhoneRang(PhoneCall call)
        {
            if (call.ScenarioId != "phone_grumpy") return; // Home 인트로 전화(자동 대화)는 제외
            if (!_open) OnToggle(default);
            ShowScreen(Screen.Call);
        }

        private void BuildCallScreen()
        {
            GameObject screen = NewScreen(Screen.Call);

            TMP_Text caller = MakeText(screen.transform, "Caller", "", 30f, Color.white, TextAlignmentOptions.Center);
            Anchor(caller.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), 140f);
            _callLabel = caller;

            Button accept = MakeButton(screen.transform, "Accept", "받기", () =>
            {
                WorldAudioManager.Instance?.PlayPhoneToggleSfx(); // AU-010 — 거절은 OnToggle 폐음이 커버
                ShowScreen(Screen.Home);
                WorldMinigameManager.Instance?.AcceptCall(); // 미니게임 패널이 폰 위로 뜬다
            });
            RectTransform acceptRect = (RectTransform)accept.transform;
            acceptRect.anchorMin = acceptRect.anchorMax = acceptRect.pivot = new Vector2(0.5f, 0f);
            acceptRect.sizeDelta = new Vector2(170f, 70f);
            acceptRect.anchoredPosition = new Vector2(-95f, 60f);
            accept.GetComponentInChildren<TMP_Text>().color = new Color(0.3f, 0.95f, 0.5f);

            Button decline = MakeButton(screen.transform, "Decline", "거절", () =>
            {
                if (_open) OnToggle(default);
                WorldMinigameManager.Instance?.DeclineCall();
            });
            RectTransform declineRect = (RectTransform)decline.transform;
            declineRect.anchorMin = declineRect.anchorMax = declineRect.pivot = new Vector2(0.5f, 0f);
            declineRect.sizeDelta = new Vector2(170f, 70f);
            declineRect.anchoredPosition = new Vector2(95f, 60f);
            decline.GetComponentInChildren<TMP_Text>().color = new Color(1f, 0.45f, 0.35f);
        }

        private void RefreshCall()
        {
            if (_callLabel != null)
                _callLabel.text = "☎ 수신 중\n\n<size=140%><b>박말순</b></size>\n<color=#8a93a8>진상 기류의 냄새가 난다…</color>";
        }

        private void RefreshBank()
        {
            if (_bankLabel == null) return;
            _bankLabel.text =
                "잔고      <color=#35e0c8>₩" + _gameState.money.ToString("N0") + "</color>\n" +
                "빚        <color=#ff7359>₩" + _gameState.debt.ToString("N0") + "</color>\n" +
                "누적 수익  ₩" + _gameState.totalEarned.ToString("N0") + "\n\n" +
                "오늘 완료  " + _gameState.completedCount + "건\n" +
                "오늘 지각  " + _gameState.lateCount + "건\n" +
                "코인 보유  " + _gameState.coinUnits.ToString("0.###") + "개";
        }

        private void RefreshFurniture()
        {
            if (_furnitureLabel == null) return;
            _furnitureLabel.text = "보유 인벤토리 <color=#8a93a8>(클릭=배치)</color>\n배치됨 "
                + _gameState.placedFurniture.Count + "개  잔액 <color=#35e0c8>₩"
                + _gameState.money.ToString("N0") + "</color>";

            // S-030 ③: 보유분을 종류별 묶음(한글명 ×개수) 행으로 재구축 — 클릭하면 그 가구로 배치 개시.
            // 시계 틱마다 RefreshCurrent가 도는데 매번 재구축하면 스크롤이 리셋된다 — 변화 시에만.
            if (_furnitureListContent == null) return;
            string signature = string.Join(",", _gameState.ownedFurnitureIds);
            if (signature == _furnitureInvSignature) return;
            _furnitureInvSignature = signature;
            for (int i = _furnitureListContent.childCount - 1; i >= 0; i--)
                Destroy(_furnitureListContent.GetChild(i).gameObject);

            var counts = new System.Collections.Generic.Dictionary<string, int>();
            foreach (string id in _gameState.ownedFurnitureIds)
                counts[id] = counts.TryGetValue(id, out int c) ? c + 1 : 1;

            int row = 0;
            foreach (var pair in counts)
            {
                string id = pair.Key;
                Button rowButton = MakeButton(_furnitureListContent, "Inv_" + id,
                    KoreanName(id) + " ×" + pair.Value + "   —   배치", () => BeginPlacement(id));
                RectTransform rect = (RectTransform)rowButton.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(-8f, 48f);
                rect.anchoredPosition = new Vector2(0f, -4f - row * 54f);
                row++;
            }
            if (row == 0)
            {
                TMP_Text empty = MakeText(_furnitureListContent, "Empty",
                    "<color=#8a93a8>보유 가구 없음 — 아래에서 구매</color>", 22f, Color.white, TextAlignmentOptions.TopLeft);
                RectTransform rect = empty.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(-16f, 40f);
                rect.anchoredPosition = new Vector2(0f, -8f);
            }
            _furnitureListContent.sizeDelta = new Vector2(0f, 12f + row * 54f);
        }

        // ── 바코드 스캔 (택배앱 화면에서만) ──────────────────

        // ── S-036 지도 앱 화면 ───────────────────────────────

        private void BuildMapScreen()
        {
            GameObject screen = NewScreen(Screen.Map);

            _mapOriginLabel = MakeText(screen.transform, "Origin", "출발: 물류캠프", 26f, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(_mapOriginLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, 32f);

            // 지도 영역 — 실아트 스왑 소켓(_mapSprite · bom_id: ui_map_town), 폴백 = 색 블록.
            GameObject map = new GameObject("MapArea", typeof(RectTransform));
            map.transform.SetParent(screen.transform, false);
            _mapArea = (RectTransform)map.transform;
            _mapArea.anchorMin = Vector2.zero;
            _mapArea.anchorMax = Vector2.one;
            _mapArea.offsetMin = new Vector2(0f, 152f);
            _mapArea.offsetMax = new Vector2(0f, -36f);
            Image mapImage = map.AddComponent<Image>();
            mapImage.sprite = _mapSprite != null ? _mapSprite : MapFallbackSprite();
            mapImage.color = Color.white;
            mapImage.raycastTarget = false;

            // 추천 경로선 — 선택 시 출발 마커→핀 (좌하단 원점 픽셀 배치 · 회전).
            GameObject line = new GameObject("Route", typeof(RectTransform));
            line.transform.SetParent(map.transform, false);
            Image lineImage = line.AddComponent<Image>();
            lineImage.color = new Color(0.208f, 0.878f, 0.784f, 0.9f);
            lineImage.raycastTarget = false;
            _routeLine = (RectTransform)line.transform;
            _routeLine.gameObject.SetActive(false);

            TMP_Text origin = MakeText(map.transform, "OriginMarker", "▲", 30f, new Color(1f, 0.62f, 0.27f), TextAlignmentOptions.Center);
            origin.raycastTarget = false;
            PlaceOnMap(origin.rectTransform, MapOriginPos, new Vector2(48f, 40f));

            for (int i = 0; i < Pins.Length; i++)
            {
                int index = i;
                MapPin pin = Pins[i];
                Button b = MakeButton(map.transform, "Pin_" + pin.label,
                    pin.locked ? pin.label + "\n<size=70%>준비 중</size>" : pin.label,
                    () => OnPinTapped(index));
                RectTransform rect = (RectTransform)b.transform;
                PlaceOnMap(rect, pin.pos, new Vector2(158f, pin.locked ? 78f : 58f));
                if (pin.locked)
                {
                    b.image.color = new Color(0.25f, 0.27f, 0.32f, 0.9f);
                    b.GetComponentInChildren<TMP_Text>().color = new Color(0.65f, 0.68f, 0.75f);
                }
            }

            _mapInfoLabel = MakeText(screen.transform, "Info", "목적지 핀을 탭해라", 26f, Color.white, TextAlignmentOptions.BottomLeft);
            RectTransform infoRect = _mapInfoLabel.rectTransform;
            infoRect.anchorMin = new Vector2(0f, 0f);
            infoRect.anchorMax = new Vector2(1f, 0f);
            infoRect.pivot = new Vector2(0.5f, 0f);
            infoRect.anchoredPosition = new Vector2(0f, 86f);
            infoRect.sizeDelta = new Vector2(0f, 62f);

            _departButton = MakeButton(screen.transform, "Depart", "목적지로 출발", DepartSelected);
            RectTransform departRect = (RectTransform)_departButton.transform;
            departRect.anchorMin = departRect.anchorMax = departRect.pivot = new Vector2(0.5f, 0f);
            departRect.sizeDelta = new Vector2(320f, 72f);
            departRect.anchoredPosition = new Vector2(0f, 6f);
            _departButton.interactable = false;
        }

        private static void PlaceOnMap(RectTransform rect, Vector2 normalized, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = normalized;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        private void LayoutRoute(Vector2 fromNorm, Vector2 toNorm)
        {
            Vector2 size = _mapArea.rect.size;
            Vector2 a = Vector2.Scale(fromNorm, size);
            Vector2 b = Vector2.Scale(toNorm, size);
            Vector2 d = b - a;
            _routeLine.anchorMin = _routeLine.anchorMax = Vector2.zero;
            _routeLine.pivot = new Vector2(0f, 0.5f);
            _routeLine.anchoredPosition = a;
            _routeLine.sizeDelta = new Vector2(d.magnitude, 5f);
            _routeLine.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        private void OnPinTapped(int index)
        {
            WorldAudioManager.Instance?.PlayUiTickSfx(); // AU-011 sfx_map_pin 도착 시 교체
            _selectedPin = index;
            RefreshMap();
        }

        private void RefreshMap()
        {
            if (_mapOriginLabel == null) return;
            _mapOriginLabel.text = "출발: " + _travelOrigin;

            if (_selectedPin < 0)
            {
                _mapInfoLabel.text = _inTravel ? "목적지 핀을 탭해라"
                    : "<color=#8a93a8>출발은 이동(Travel)에서만 가능</color>";
                _routeLine.gameObject.SetActive(false);
                if (_departButton != null) _departButton.interactable = false;
                return;
            }

            MapPin pin = Pins[_selectedPin];
            if (pin.locked)
            {
                // 잠금 구역 — 진입 불가 (S-036 수용기준).
                _mapInfoLabel.text = pin.label + " — <color=#ff9f45>준비 중</color> · 진입 불가";
                _routeLine.gameObject.SetActive(false);
                _departButton.interactable = false;
                return;
            }

            float minutes = pin.far ? _tuning.travelFarMinutes : _tuning.travelNearMinutes;
            _mapInfoLabel.text = "<b>" + pin.label + "</b> · 추천 경로 " + (pin.far ? "간선도로" : "골목길")
                + " · 예상 <color=#35e0c8>" + Mathf.RoundToInt(minutes) + "분</color>";
            _routeLine.gameObject.SetActive(true);
            LayoutRoute(MapOriginPos, pin.pos);
            _departButton.interactable = _inTravel;
        }

        // 출발 — 로직은 매니저 위임(시간=DayNight·목적지=Delivery·전이=SceneFlow). 구 TravelMapView.Depart 승계.
        private void DepartSelected()
        {
            if (_selectedPin < 0 || !_inTravel || Pins[_selectedPin].locked) return;
            if (WorldSceneFlowManager.Instance == null || WorldDayNightManager.Instance == null
                || WorldDeliveryManager.Instance == null) return;
            if (WorldSceneFlowManager.Instance.IsTransitioning) return;

            MapPin pin = Pins[_selectedPin];
            WorldAudioManager.Instance?.PlayUiTickSfx(); // AU-011 sfx_map_depart 도착 시 교체
            WorldDayNightManager.Instance.AdvanceMinutes(pin.far ? _tuning.travelFarMinutes : _tuning.travelNearMinutes);
            WorldDeliveryManager.Instance.SetDestination(pin.district);
            WorldSceneFlowManager.Instance.Request(GameScene.District);
        }

        private static Sprite _mapFallbackCache;

        // 색 블록 폴백 지도 (S-036) — 실일러(A-004 · ui_map_town) 도착 전까지. 구역 대역 4블록 + 길.
        private static Sprite MapFallbackSprite()
        {
            if (_mapFallbackCache != null) return _mapFallbackCache;
            const int W = 128, H = 160;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[W * H];
            Color ground = new Color(0.09f, 0.12f, 0.20f);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = ground;

            void Block(float nx, float ny, float nw, float nh, Color c)
            {
                int x0 = (int)(nx * W), y0 = (int)(ny * H);
                int x1 = x0 + (int)(nw * W), y1 = y0 + (int)(nh * H);
                for (int y = y0; y < y1 && y < H; y++)
                    for (int x = x0; x < x1 && x < W; x++)
                        pixels[y * W + x] = c;
            }
            Block(0.10f, 0.52f, 0.34f, 0.28f, new Color(0.30f, 0.26f, 0.22f)); // 빌라촌 — 웜브라운
            Block(0.56f, 0.28f, 0.32f, 0.24f, new Color(0.16f, 0.24f, 0.30f)); // 먹자골목 — 딥블루
            Block(0.60f, 0.68f, 0.30f, 0.22f, new Color(0.16f, 0.18f, 0.24f)); // 아파트단지 (잠금 — 저채도)
            Block(0.08f, 0.10f, 0.30f, 0.20f, new Color(0.15f, 0.17f, 0.22f)); // 언덕주택가 (잠금)
            Block(0.47f, 0f, 0.06f, 1f, new Color(0.22f, 0.24f, 0.30f));       // 세로 간선
            Block(0f, 0.44f, 1f, 0.05f, new Color(0.22f, 0.24f, 0.30f));       // 가로 골목

            tex.SetPixels(pixels);
            tex.Apply();
            _mapFallbackCache = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f));
            return _mapFallbackCache;
        }

        private void ScanPointer()
        {
            Camera camera = Camera.main;
            Mouse mouse = Mouse.current;
            if (camera == null || mouse == null) return;

            PickupBox box = null;
            float nearest = float.MaxValue;
            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            foreach (RaycastHit hit in Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Collide))
            {
                if (!hit.collider.TryGetComponent(out PickupBox candidate)) continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                box = candidate;
            }

            if (_deliveryHover != null)
                _deliveryHover.text = box != null && box.Order != null ? "송장 " + Invoice(box.Order.orderId) : "-";

            if (box == null || box.Order == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (WorldDeliveryManager.Instance == null) return;
            if (!WorldDeliveryManager.Instance.RegisterBarcode(box.Order))
                ShowWarn("⚠ " + Invoice(box.Order.orderId) + " — 이미 등록된 운송장");
        }

        private Coroutine _warnFade;

        private void ShowWarn(string message)
        {
            if (_deliveryWarn == null) return;
            if (_warnFade != null) StopCoroutine(_warnFade);
            _deliveryWarn.text = message;
            _warnFade = StartCoroutine(ClearWarn());
        }

        private IEnumerator ClearWarn()
        {
            yield return new WaitForSecondsRealtime(1.6f);
            _deliveryWarn.text = string.Empty;
            _warnFade = null;
        }

        // ── 위젯 헬퍼 ────────────────────────────────────────

        private static string Invoice(int orderId) => "DL-" + orderId.ToString("0000");

        private static string Shorten(string clipName)
        {
            int cut = clipName.IndexOf("_2026");
            return cut > 0 ? clipName.Substring(0, cut).Replace('_', ' ') : clipName;
        }

        private TMP_Text MakeText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) label.font = _font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private Button MakeButton(Transform parent, string name, string label, System.Action onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image bg = go.AddComponent<Image>();
            bg.sprite = RoundedSprite();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.13f, 0.18f, 0.26f, 0.95f);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => onClick());
            TMP_Text text = MakeText(go.transform, "Label", label, 24f, new Color(0.208f, 0.878f, 0.784f), TextAlignmentOptions.Center);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return button;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pos, float height)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(0f, height);
        }
    }
}
