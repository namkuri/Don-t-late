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
        /// <summary>UI 층 내 직접 참조용 (S-072 ② — 송장 바코드 조준. BagView와 같은 선례).</summary>
        public static PhoneView Instance { get; private set; }

        private enum Screen { Home, Delivery, Music, Invest, Bank, Furniture, Call, Map, Shop, Social, Weather }

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
        [Tooltip("NPC 프로필 카탈로그 (S-079 ④ — 소셜앱). 빌더 주입.")]
        [SerializeField] private NpcSO[] _npcCatalog;

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
            new MapPin { label = "아파트단지", district = DeliveryOrderSO.DISTRICT_APARTMENT,
                         locked = false, far = true, pos = new Vector2(0.74f, 0.78f) }, // S-038 활성화
            new MapPin { label = "언덕주택가", district = DeliveryOrderSO.DISTRICT_HILLSIDE,
                         locked = false, far = false, pos = new Vector2(0.24f, 0.20f) }, // S-049 활성화
        };
        private static readonly Vector2 MapOriginPos = new Vector2(0.5f, 0.07f); // 출발 마커 위치

        // S-054 — 개척 잠금: unlockedDistricts에 없으면 잠김 (목록이 비면 전체 해금 취급 — 그레이박스 호환).
        private bool IsPinLocked(MapPin pin)
        {
            if (pin.locked) return true;
            if (_gameState == null || _gameState.unlockedDistricts.Count == 0) return false;
            return !_gameState.unlockedDistricts.Contains(pin.district);
        }

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
            Instance = this;
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
            WorldEvents.MinigameEnded += OnMinigameEnded; // S-037 — 부재중(타임아웃) 시 폰 접힘
            WorldEvents.SceneTransitionCompleted += OnSceneChanged; // S-032 ①
            WorldEvents.DebtSettled += OnSettled; // S-034 ① — 정산 후 상차 리스트 초기화
            WorldEvents.WeatherChanged += OnWeatherChangedPhone; // S-068 ⑦ — 날씨앱 실시간 갱신
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
            WorldEvents.MinigameEnded -= OnMinigameEnded;
            WorldEvents.SceneTransitionCompleted -= OnSceneChanged;
            WorldEvents.DebtSettled -= OnSettled;
            WorldEvents.WeatherChanged -= OnWeatherChangedPhone;
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
                if (!_open) TogglePanel();   // 지도 앱 자동 오픈
                ShowScreen(Screen.Map);
            }
            else if (wasTravel)
            {
                ApplyPanelLayout();              // 패널 원복
                if (_open) TogglePanel();    // Travel 이탈 = 지도 수납
                else if (_slide == null && _panel != null)
                    _panel.anchoredPosition = new Vector2(_panel.anchoredPosition.x, HiddenY);
                if (_screen == Screen.Map) ShowScreen(Screen.Home);
            }

            if (_inTitle && _open) TogglePanel(); // 타이틀 복귀 시 강제 수납
            _prevScene = scene;
        }

        // S-034 ①: 정산 = 하루 마감 — 상차 리스트를 비운다 (히스토리는 GameState 소유라 유지).
        private void OnSettled(DebtSettlement _)
        {
            _scanned.Clear();
            _status.Clear();
            RefreshCurrent();
        }

        // S-078 ① — 중복 가드: 사고(스캔 기록만 리셋) 후 재스캔 등으로 같은 주문이 두 번 오면
        // 기존 행을 갱신한다 (리스트에 같은 운송장이 두 줄 찍히던 원흉).
        private void OnBarcodeScanned(DeliveryData data)
        {
            int existing = _scanned.FindIndex(d => d.OrderId == data.OrderId);
            if (existing >= 0) _scanned[existing] = data;
            else _scanned.Add(data);
            _status.Remove(data.OrderId); // 재스캔 = 재도전 — 사고 실패 표기를 진행으로 되돌린다
            // S-164 ④ — 방금 찍은 항목을 잠깐 초록으로 물들인다(남규님 지시).
            // 목록이 길어지면 "내가 방금 찍은 게 어느 줄인지" 못 찾는다 — 시선을 그 줄로 데려간다.
            _justScannedId = data.OrderId;
            if (_scanFlash != null) StopCoroutine(_scanFlash);
            _scanFlash = StartCoroutine(ClearScanFlash());
            RefreshCurrent();
        }

        private int _justScannedId = -1;
        private Coroutine _scanFlash;

        private IEnumerator ClearScanFlash()
        {
            yield return new WaitForSecondsRealtime(1.4f); // 정산창 timeScale=0에서도 흐른다
            _justScannedId = -1;
            _scanFlash = null;
            RefreshCurrent();
        }
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
            if (_open) TogglePanel(); // 열려 있을 때만 닫는다 (S-032 ③)
        }

        private void OnToggle(InputAction.CallbackContext _)
        {
            if (_inTitle && !_open) return; // S-032 ① — 타이틀에선 열지 않는다

            // S-072 ③ — 계층 내비: 앱 화면에서 Tab = 홈으로, 홈에서 Tab = 폰 닫기.
            // (Travel은 지도가 홈 역할이라 제외 — 기존처럼 바로 수납.)
            if (_open && !_inTravel && _screen != Screen.Home)
            {
                ShowScreen(Screen.Home);
                return;
            }

            TogglePanel();
        }

        // 실제 개폐 — 내부 자동 오픈/수납(Travel 진입·이탈 등)은 계층 내비를 거치지 않고 이걸 부른다.
        /// <summary>
        /// S-153 — 밖에서 폰을 닫는다(튜토리얼이 "확인했으면 닫자"를 대신 해주는 용도).
        /// 이미 닫혀 있으면 아무 것도 하지 않는다 — 토글이 아니라 **닫기**여야 오작동이 없다.
        /// </summary>
        public void ClosePanel()
        {
            if (!_open) return;
            TogglePanel();
        }

        private void TogglePanel()
        {
            _open = !_open;
            IsOpen = _open;
            WorldAudioManager.Instance?.PlayPhoneToggleSfx(); // AU-008 — 개폐 공용
            if (_slide != null) StopCoroutine(_slide);
            _slide = StartCoroutine(Slide(_open ? _shownY : HiddenY));
            if (_open)
            {
                ShowScreen(_inTravel ? Screen.Map : Screen.Home); // S-036 — Travel 기본 앱 = 지도
                WorldEvents.RaisePhoneOpened();                   // S-146 — 튜토리얼 진행 판정용
            }
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
                    Screen.Shop => "쇼핑",
                    Screen.Social => "소셜",
                    Screen.Weather => "날씨",
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
                case Screen.Weather: RefreshWeather(); break;
                case Screen.Shop: RefreshShop(); break;
                case Screen.Social: RefreshSocial(); break; // S-079 ④
            }
        }


        // ── 쇼핑 앱 (S-056) — 구루마·음료·고양이 용품 ──
        private Transform _shopList;

        private struct ShopItem
        {
            public string id; public string label; public int price;
            public bool stackable; public bool holdable;
        }

        private static readonly ShopItem[] ShopItems =
        {
            // S-122 ⑥ — 라벨 축약(줄바꿈 해소). id는 그대로 = 게임 로직 무영향(판정은 id 기준).
            new ShopItem { id = "cart", label = "구루마", price = 8000 },
            new ShopItem { id = "drink", label = "에너지드링크", price = 1500, stackable = true, holdable = true },
            new ShopItem { id = "water", label = "생수 (더위↓)", price = 800, stackable = true },       // S-088 ④
            new ShopItem { id = "hot_drink", label = "코코아 (추위↓)", price = 1200, stackable = true }, // S-088 ④
            new ShopItem { id = "cat_food", label = "고양이 사료", price = 2000, stackable = true },
            new ShopItem { id = "cat_toy", label = "고양이 장난감", price = 3000, stackable = true },
            new ShopItem { id = "cat_tower", label = "캣타워", price = 10000 },
            // S-123 ⑤ — 선물용 꽃. holdable을 켜지 않는 이유: 손에 든 물건은 id를 저장하지 않아
            // 우클릭이 "꽃을 마신다"가 된다. 가방에 두고 E로 건네는 경로만 쓴다.
            new ShopItem { id = "flower", label = "꽃 한 다발", price = 5000, stackable = true },
        };

        private void BuildShopScreen()
        {
            GameObject screen = NewScreen(Screen.Shop);

            // S-124 ⑥ — 스크롤 영역 (가구앱 InvViewport와 같은 패턴). 품목이 늘어도 개구 밖으로
            // 떨어지지 않는다 — 행을 직접 화면에 깔던 구조가 하위 품목 구매 불가의 원인이었다.
            GameObject viewport = new GameObject("ShopViewport", typeof(RectTransform));
            viewport.transform.SetParent(screen.transform, false);
            RectTransform vpRect = (RectTransform)viewport.transform;
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(0f, 4f);
            vpRect.offsetMax = new Vector2(0f, -4f);
            Image vpBg = viewport.AddComponent<Image>(); // 드래그 타겟 (거의 투명)
            vpBg.color = new Color(1f, 1f, 1f, 0.03f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("ShopContent", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 10f);
            _shopList = contentRect;

            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = vpRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 24f;
        }

        private void RefreshShop()
        {
            if (_shopList == null) return;
            foreach (Transform child in _shopList) Destroy(child.gameObject); // 재구축 (품목 적음)

            for (int i = 0; i < ShopItems.Length; i++)
            {
                ShopItem item = ShopItems[i];
                bool cartOwned = item.id == "cart" && _gameState != null && _gameState.ownsCart;

                GameObject row = new GameObject("Shop_" + item.id, typeof(RectTransform));
                row.transform.SetParent(_shopList, false);
                Image rowBg = row.AddComponent<Image>();
                rowBg.color = new Color(0.12f, 0.15f, 0.22f, 0.95f);
                RectTransform rowRect = (RectTransform)row.transform;
                rowRect.anchorMin = new Vector2(0f, 1f); rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                // S-122 ⑥ 개구 정합 + S-124 ⑥ 스크롤: 이제 개구를 넘어가도 스크롤로 닿으므로
                // 행을 다시 편하게(높이 56·피치 60) 되돌린다.
                rowRect.sizeDelta = new Vector2(-16f, 56f);
                rowRect.anchoredPosition = new Vector2(0f, -6f - i * 60f);

                TMP_Text label = MakeText(row.transform, "Label",
                    item.label + "\n<size=70%>₩" + item.price.ToString("N0") + "</size>",
                    20f, Color.white, TextAlignmentOptions.Left);
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                // 가용폭 250-14-84 = 152px — 최장 라벨 "코코아 (추위↓)"가 한 줄에 들어간다.
                labelRect.offsetMin = new Vector2(14f, 2f); labelRect.offsetMax = new Vector2(-84f, -2f);

                Button buy = MakeButton(row.transform, "Buy", cartOwned ? "보유" : "구매", () => BuyShopItem(item));
                RectTransform buyRect = (RectTransform)buy.transform;
                buyRect.anchorMin = buyRect.anchorMax = buyRect.pivot = new Vector2(1f, 0.5f);
                buyRect.sizeDelta = new Vector2(72f, 40f); // 우여백 8 → 버튼 x 170~242, 라벨 끝 166
                buyRect.anchoredPosition = new Vector2(-8f, 0f);
                buy.interactable = !cartOwned;
            }

            // S-124 ⑥ — 스크롤 내용 높이 = 행 총합 (없으면 ScrollRect가 못 움직인다).
            if (_shopList is RectTransform contentRect)
                contentRect.sizeDelta = new Vector2(0f, 6f + ShopItems.Length * 60f + 6f);
        }

        private void BuyShopItem(ShopItem item)
        {
            if (WorldDebtManager.Instance == null || _gameState == null) return;
            if (item.id == "cart" && _gameState.ownsCart) return;

            if (item.id != "cart" && _gameState.bagItems.Count >= BagStorage.CAPACITY
                && !(_gameState.bagItems.Exists(b => b.id == item.id && b.stackable)))
            {
                Debug.Log("[쇼핑] 가방이 가득 찼다");
                return;
            }

            if (!WorldDebtManager.Instance.TrySpend(item.price))
            {
                Debug.Log("[쇼핑] 잔액 부족");
                return;
            }

            if (item.id == "cart")
            {
                _gameState.ownsCart = true;
                Debug.Log("[쇼핑] 구루마 구매 — 캠프에 준비된다 (트럭 생기면 배송지 자동 스폰)");
            }
            else
            {
                BagStorage.TryAdd(_gameState, item.id, item.label, item.stackable, item.holdable);
                if (BagView.Instance != null) BagView.Instance.Refresh();
            }
            RefreshShop();
        }

        // ── 날씨 앱 (S-058) — 오늘·내일 날씨와 기온 ──
        private TMP_Text _weatherBody;

        private void BuildWeatherScreen()
        {
            GameObject screen = NewScreen(Screen.Weather);
            _weatherBody = MakeText(screen.transform, "Body", string.Empty, 30f, Color.white, TextAlignmentOptions.Top);
            RectTransform rect = _weatherBody.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 12f); rect.offsetMax = new Vector2(-18f, -24f);
        }

        // S-068 ⑦ — 이모지 글리프가 폰트에 없어 네모로 깨짐 → 한글만 (아이콘은 A-007 실아트로 교체 예정).
        private static string WeatherName(WeatherType weather) => weather switch
        {
            WeatherType.Clear => "맑음", WeatherType.Cloudy => "흐림", WeatherType.Rain => "비",
            WeatherType.Snow => "눈", WeatherType.Fog => "안개", WeatherType.Heat => "폭염", _ => "?"
        };

        private void OnWeatherChangedPhone(WeatherType _)
        {
            if (_screen == Screen.Weather) RefreshWeather(); // 열려 있으면 즉시 갱신
        }

        private void RefreshWeather()
        {
            if (_weatherBody == null || WorldWeatherManager.Instance == null) return;
            WeatherType today = WorldWeatherManager.Instance.Weather;
            WeatherType tomorrow = WorldWeatherManager.Instance.TomorrowWeather;
            _weatherBody.text = "<size=130%><b>오늘</b></size>" + "\n"
                + WeatherName(today) + "  " + WorldWeatherManager.TemperatureFor(today) + "°C" + "\n" + "\n"
                + "<size=130%><b>내일</b></size>" + "\n"
                + WeatherName(tomorrow) + "  " + WorldWeatherManager.TemperatureFor(tomorrow) + "°C" + "\n" + "\n"
                + "<size=70%>비 오면 길이 미끄럽고, 덥거나 추우면 몸이 빨리 지친다.</size>";
        }

        // S-088 ② — 앱 아이콘 클릭: 커졌다 작아지는 펀치 후 화면 전환.
        private System.Collections.IEnumerator PunchThenOpen(Transform tile, Screen target)
        {
            float t = 0f;
            const float DURATION = 0.16f;
            while (t < DURATION && tile != null)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / DURATION) * Mathf.PI); // 0→1→0
                tile.localScale = Vector3.one * (1f + 0.18f * k);
                yield return null;
            }
            if (tile != null) tile.localScale = Vector3.one;
            ShowScreen(target);
        }

        // ── S-079 ④ — 소셜앱: 만난 NPC 리스트(프로필+호감도 게이지), 휠/드래그 스크롤 ──

        private RectTransform _socialContent;

        private void BuildSocialScreen()
        {
            GameObject screen = NewScreen(Screen.Social);

            GameObject viewport = new GameObject("SocialViewport", typeof(RectTransform));
            viewport.transform.SetParent(screen.transform, false);
            RectTransform vpRect = (RectTransform)viewport.transform;
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(4f, 4f);
            vpRect.offsetMax = new Vector2(-4f, -4f);
            Image vpBg = viewport.AddComponent<Image>(); // 스크롤 드래그 타겟
            vpBg.color = new Color(1f, 1f, 1f, 0.02f);
            viewport.AddComponent<RectMask2D>();

            _socialContent = new GameObject("List", typeof(RectTransform)).GetComponent<RectTransform>();
            _socialContent.SetParent(viewport.transform, false);
            _socialContent.anchorMin = new Vector2(0f, 1f);
            _socialContent.anchorMax = new Vector2(1f, 1f);
            _socialContent.pivot = new Vector2(0.5f, 1f);
            _socialContent.sizeDelta = new Vector2(0f, 100f);

            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = vpRect;
            scroll.content = _socialContent;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 24f;
        }

        private void RefreshSocial()
        {
            if (_socialContent == null || _gameState == null) return;
            for (int i = _socialContent.childCount - 1; i >= 0; i--)
                Destroy(_socialContent.GetChild(i).gameObject);

            const float ROW_H = 84f;
            float y = 0f;

            // S-124 — "호감도를 어떻게 올리는지 모르겠다"(남규님). 올리는 법과 응원 임계를 맨 위에 적는다.
            TMP_Text guide = MakeText(_socialContent, "Guide",
                "<color=#8fe3d5>인사 +20 · 꽃 +25 · 심부름 +10\n상자로 맞히면 −15</color>\n"
                + "<color=#ffd45e>" + PedestrianNpc.CheerAffinity + " 이상이면 길에서 응원</color>",
                17f, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(guide.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -4f), 74f);
            y -= 80f; // 3줄(17f × 3 ≈ 60) + 여백

            foreach (NpcAffinity entry in _gameState.npcAffinities)
            {
                NpcSO npc = FindNpc(entry.npcId);
                BuildSocialRow(entry, npc, y);
                y -= ROW_H;
            }
            if (_gameState.npcAffinities.Count == 0)
            {
                TMP_Text empty = MakeText(_socialContent, "Empty",
                    "아직 아는 사람이 없다\n<size=70%><color=#8a93a8>동네 사람들과 이야기해 보자</color></size>",
                    24f, new Color(0.75f, 0.80f, 0.90f), TextAlignmentOptions.Center);
                Anchor(empty.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -30f), 80f);
            }
            _socialContent.sizeDelta = new Vector2(0f, Mathf.Max(100f, -y + 8f));
        }

        private NpcSO FindNpc(string npcId)
        {
            if (_npcCatalog == null) return null;
            foreach (NpcSO npc in _npcCatalog)
                if (npc != null && npc.npcId == npcId) return npc;
            return null;
        }

        private void BuildSocialRow(NpcAffinity entry, NpcSO npc, float y)
        {
            GameObject row = new GameObject("Row_" + entry.npcId, typeof(RectTransform));
            row.transform.SetParent(_socialContent, false);
            RectTransform rowRect = (RectTransform)row.transform;
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(0f, 78f);
            rowRect.anchoredPosition = new Vector2(0f, y - 4f);
            Image rowBg = row.AddComponent<Image>();
            rowBg.sprite = RoundedSprite();
            rowBg.type = Image.Type.Sliced;
            rowBg.color = new Color(1f, 1f, 1f, 0.06f);
            rowBg.raycastTarget = false;

            // 프로필 — 실아트 초상 소켓, 비면 색 블록+이니셜.
            Image portrait = new GameObject("Portrait", typeof(RectTransform)).AddComponent<Image>();
            portrait.transform.SetParent(row.transform, false);
            RectTransform pRect = portrait.rectTransform;
            pRect.anchorMin = pRect.anchorMax = new Vector2(0f, 0.5f);
            pRect.pivot = new Vector2(0f, 0.5f);
            pRect.sizeDelta = new Vector2(58f, 58f);
            pRect.anchoredPosition = new Vector2(10f, 0f);
            portrait.raycastTarget = false;
            if (npc != null && npc.portrait != null) portrait.sprite = npc.portrait;
            else
            {
                portrait.sprite = RoundedSprite();
                portrait.type = Image.Type.Sliced;
                portrait.color = npc != null ? npc.placeholderColor : new Color(0.45f, 0.52f, 0.62f);
                string initial = npc != null && !string.IsNullOrEmpty(npc.displayName)
                    ? npc.displayName.Substring(0, 1) : "?";
                TMP_Text mono = MakeText(portrait.transform, "Initial", initial, 30f, Color.white, TextAlignmentOptions.Center);
                RectTransform mRect = mono.rectTransform;
                mRect.anchorMin = Vector2.zero; mRect.anchorMax = Vector2.one;
                mRect.offsetMin = mRect.offsetMax = Vector2.zero;
            }

            TMP_Text name = MakeText(row.transform, "Name",
                (npc != null ? npc.displayName : entry.npcId), 26f, Color.white, TextAlignmentOptions.TopLeft);
            RectTransform nRect = name.rectTransform;
            nRect.anchorMin = new Vector2(0f, 1f); nRect.anchorMax = new Vector2(1f, 1f);
            nRect.pivot = new Vector2(0.5f, 1f);
            // S-124 — 이름 상자 폭 122 → 178: "회색 코트"(≈130px)가 두 줄로 접혀 아래 게이지를
            // 침범했다(남규님 호감도 가시성 건과 같은 화면). 값 라벨은 하단 우측이라 겹치지 않는다.
            nRect.offsetMin = new Vector2(80f, -40f); nRect.offsetMax = new Vector2(-8f, -8f);
            name.textWrappingMode = TextWrappingModes.NoWrap;

            // 호감도 게이지 — 배경 위 fill을 width로 (sprite 없는 fillAmount 함정 회피 — S-068 교훈).
            Image gaugeBg = new GameObject("GaugeBg", typeof(RectTransform)).AddComponent<Image>();
            gaugeBg.transform.SetParent(row.transform, false);
            gaugeBg.color = new Color(0.10f, 0.12f, 0.16f, 0.9f);
            gaugeBg.raycastTarget = false;
            RectTransform gbRect = gaugeBg.rectTransform;
            gbRect.anchorMin = new Vector2(0f, 0f); gbRect.anchorMax = new Vector2(1f, 0f);
            gbRect.pivot = new Vector2(0.5f, 0f);
            gbRect.offsetMin = new Vector2(80f, 14f); gbRect.offsetMax = new Vector2(-64f, 28f);

            Image gaugeFill = new GameObject("GaugeFill", typeof(RectTransform)).AddComponent<Image>();
            gaugeFill.transform.SetParent(gaugeBg.transform, false);
            gaugeFill.color = new Color(1f, 0.55f, 0.65f); // 핑크 — 호감
            gaugeFill.raycastTarget = false;
            RectTransform gfRect = gaugeFill.rectTransform;
            float t = Mathf.Clamp01(entry.affinity / (float)NpcAffinityLedger.MAX);
            gfRect.anchorMin = new Vector2(0f, 0f);
            gfRect.anchorMax = new Vector2(t, 1f); // 왼쪽 기점 — 비율만큼
            gfRect.offsetMin = new Vector2(2f, 2f); gfRect.offsetMax = new Vector2(-2f, -2f);

            TMP_Text value = MakeText(row.transform, "Value", entry.affinity + "%", 20f,
                new Color(1f, 0.55f, 0.65f), TextAlignmentOptions.MidlineRight);
            RectTransform vRect = value.rectTransform;
            vRect.anchorMin = new Vector2(1f, 0f); vRect.anchorMax = new Vector2(1f, 0f);
            vRect.pivot = new Vector2(1f, 0f);
            vRect.sizeDelta = new Vector2(54f, 26f);
            vRect.anchoredPosition = new Vector2(-8f, 10f);
        }

        // S-062 ⑦ — 쇼핑(S-056)·소셜(S-061) 자리 표시 화면. 정식 시공 때 교체.
        private void BuildPlaceholderScreen(Screen target, string title, string body)
        {
            GameObject screen = NewScreen(target);
            TMP_Text label = MakeText(screen.transform, "ComingSoon", title + "\n\n<size=60%>" + body + "</size>",
                34f, new Color(0.75f, 0.80f, 0.90f), TextAlignmentOptions.Center);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
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
            // S-164 ⑦ — 개구 폭(298)에서 "LateTel LTE 100%"가 **줄바꿈돼 내려와** 홈 버튼과 겹쳤다
            // (남규님 캡처). 문구를 줄이고 줄바꿈을 막는다 — 상태바는 한 줄이어야 상태바다.
            TMP_Text carrier = MakeText(screenBg, "StatusRight", "LTE 100%", 18f, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.TopRight);
            carrier.textWrappingMode = TextWrappingModes.NoWrap;
            Anchor(carrier.rectTransform, new Vector2(0.42f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -8f), 26f);

            _titleLabel = MakeText(screenBg, "Title", "홈", 34f, Color.white, TextAlignmentOptions.Top);
            Anchor(_titleLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), 44f);

            Button home = MakeButton(screenBg, "HomeBtn", "홈", () => ShowScreen(Screen.Home));
            RectTransform homeRect = (RectTransform)home.transform;
            homeRect.anchorMin = homeRect.anchorMax = homeRect.pivot = new Vector2(1f, 1f);
            homeRect.sizeDelta = new Vector2(54f, 40f);
            // S-117 — 새 프레임 개구(폭 298)에선 -38이 상태바 "100%"를 가린다(캡처 게이트 적발) — 한 줄 아래로.
            // S-164 ⑦ — 상태바가 한 줄로 고정됐으므로 홈 버튼도 그 아래 고정 위치로 내린다.
            homeRect.anchoredPosition = new Vector2(-10f, -40f);

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
            BuildShopScreen();     // S-056
            BuildWeatherScreen();  // S-058
            BuildSocialScreen(); // S-079 ④ — 만난 NPC 리스트+호감도 게이지 (플레이스홀더 은퇴)
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
                ("지", "지도", Screen.Map, new Color(0.30f, 0.62f, 0.85f)),      // S-062 ⑦
                ("쇼", "쇼핑", Screen.Shop, new Color(0.90f, 0.42f, 0.45f)),     // S-062 ⑦ — S-056 상점 예정지
                ("소", "소셜", Screen.Social, new Color(0.95f, 0.75f, 0.30f)),   // S-062 ⑦ — S-061 SNS 예정지
                ("날", "날씨", Screen.Weather, new Color(0.45f, 0.72f, 0.95f)),   // S-058
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
                Transform tileTransform = tile.transform;
                button.onClick.AddListener(() => StartCoroutine(PunchThenOpen(tileTransform, target))); // S-088 ②

                RectTransform rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(80f, 80f); // 남규 수정
                // S-122 ⑦ — S-117 개구(298) 정합: 화면루트 266 = 좌여백 5 + 피치 88×2 + 타일 80 + 우여백 5.
                // 구 값(14/112)은 개구 354 시절 상수로, 우측을 52px 관통하고 있었다.
                rect.anchoredPosition = new Vector2(5f + (i % 3) * 88f, -22f - (i / 3) * 138f);

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
            // S-164 ⑦ — 경고문이 길어 줄바꿈되면 아래 목록을 덮었다(남규님 캡처).
            // 폰트를 줄이고 **오토사이징**으로 한 줄에 욱여넣는다 — 경고는 한 줄이어야 목록을 안 가린다.
            _deliveryWarn = MakeText(screen.transform, "Warn", "", 19f, new Color(1f, 0.45f, 0.35f), TextAlignmentOptions.Top);
            _deliveryWarn.textWrappingMode = TextWrappingModes.NoWrap;
            _deliveryWarn.enableAutoSizing = true;
            _deliveryWarn.fontSizeMin = 13f;
            _deliveryWarn.fontSizeMax = 19f;
            Anchor(_deliveryWarn.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -40f), 26f);
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

            BuildBarcodeAimPanel(screen.transform); // S-072 ② — 송장 바코드 카메라 뷰
        }

        // ── S-072 ② — 바코드 스캔 카메라 뷰 (택배앱 상단 오버레이) ──
        // 송장 바코드 호버 중에만 뜬다. 마우스 오프셋을 받아 뷰 속 바코드가 반대로 흐르고
        // (카메라 조준감), 중앙에 맞으면 가이드가 민트로 바뀐다 — 그때 클릭이 촬영.

        private GameObject _aimPanel;
        private RectTransform _aimBarcode;
        private Image _aimGuide;
        private TMP_Text _aimHint;
        private bool _aimCentered;
        private const float AIM_CENTER_THRESHOLD = 0.35f;

        private void BuildBarcodeAimPanel(Transform parent)
        {
            _aimPanel = new GameObject("BarcodeAimPanel", typeof(RectTransform));
            _aimPanel.transform.SetParent(parent, false);
            RectTransform panelRect = (RectTransform)_aimPanel.transform;
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.offsetMin = new Vector2(6f, -170f);
            panelRect.offsetMax = new Vector2(-6f, -6f);
            Image bg = _aimPanel.AddComponent<Image>(); // 카메라 파인더 — 검은 유리
            bg.color = new Color(0.03f, 0.04f, 0.06f, 0.97f);
            _aimPanel.AddComponent<RectMask2D>();       // 흐르는 바코드가 파인더 밖으로 안 샌다

            // 중앙 조준 가이드 (색 = 조준 판정 피드백).
            _aimGuide = new GameObject("Guide", typeof(RectTransform)).AddComponent<Image>();
            _aimGuide.transform.SetParent(_aimPanel.transform, false);
            _aimGuide.color = new Color(1f, 0.624f, 0.271f, 0.28f);
            _aimGuide.raycastTarget = false;
            RectTransform guideRect = _aimGuide.rectTransform;
            guideRect.anchorMin = guideRect.anchorMax = guideRect.pivot = new Vector2(0.5f, 0.5f);
            guideRect.sizeDelta = new Vector2(150f, 78f);

            // 뷰 속 바코드 (줄무늬 컨테이너 — 마우스 오프셋의 반대로 움직인다).
            _aimBarcode = new GameObject("AimBarcode", typeof(RectTransform)).GetComponent<RectTransform>();
            _aimBarcode.SetParent(_aimPanel.transform, false);
            _aimBarcode.anchorMin = _aimBarcode.anchorMax = _aimBarcode.pivot = new Vector2(0.5f, 0.5f);
            _aimBarcode.sizeDelta = new Vector2(130f, 58f);

            _aimHint = MakeText(_aimPanel.transform, "AimHint", "바코드를 중앙에 맞춰 클릭", 20f,
                new Color(1f, 0.624f, 0.271f), TextAlignmentOptions.Bottom);
            Anchor(_aimHint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 6f), 28f);
            ((RectTransform)_aimHint.transform).anchoredPosition = new Vector2(0f, 6f);

            _aimPanel.SetActive(false);
        }

        public void OpenBarcodeAim(DeliveryOrderSO order)
        {
            if (order == null || _aimPanel == null) return;
            if (!_open && !_inTitle) TogglePanel();     // 폰 자동 오픈
            if (_screen != Screen.Delivery) ShowScreen(Screen.Delivery);
            _aimPanel.SetActive(true);
            _aimCentered = false;
            RebuildAimBarcode(order.orderId);
        }

        public void CloseBarcodeAim()
        {
            _aimCentered = false; // 조준 종료 = 판정 리셋 (구값 잔존으로 빗나간 촬영이 통과되는 것 방지)
            if (_aimPanel != null) _aimPanel.SetActive(false);
        }

        public void UpdateBarcodeAim(Vector2 offset)
        {
            if (_aimBarcode == null || _aimPanel == null || !_aimPanel.activeSelf) return;
            // 카메라를 움직이는 감각 — 뷰 속 바코드는 마우스 오프셋의 반대로 흐른다.
            _aimBarcode.anchoredPosition = new Vector2(-offset.x * 90f, -offset.y * 34f);
            _aimCentered = offset.magnitude < AIM_CENTER_THRESHOLD;
            if (_aimGuide != null)
                _aimGuide.color = _aimCentered
                    ? new Color(0.208f, 0.878f, 0.784f, 0.34f)  // 민트 — 찍을 수 있다
                    : new Color(1f, 0.624f, 0.271f, 0.28f);     // 앰버 — 더 중앙으로
            if (_aimHint != null)
                _aimHint.text = _aimCentered ? "고정! 자동 촬영 중…" : "바코드를 중앙에 맞춰라";
        }

        /// <summary>조준이 중앙에 맞았는가 — 송장(InvoiceView)의 자동 촬영 타이머가 참조 (S-073 ②).</summary>
        public bool IsAimCentered => _aimCentered;

        /// <summary>조준 촬영 (송장 좌클릭). 중앙 판정 성공 시 운송장 등록까지 — true면 송장을 접는다.</summary>
        public bool TryShootBarcode(DeliveryOrderSO order)
        {
            if (order == null) return false;
            if (_aimPanel == null || !_aimPanel.activeSelf) return false; // 조준 중이 아니면 촬영 없음
            if (!_aimCentered)
            {
                ShowWarn("⚠ 흔들렸다 — 바코드를 중앙에 맞춰라");
                return false;
            }
            if (WorldDeliveryManager.Instance == null) return false;
            if (!WorldDeliveryManager.Instance.RegisterBarcode(order))
                ShowWarn("⚠ " + Invoice(order.orderId) + " 이미 등록됨"); // S-164 ⑦ 축약
            CloseBarcodeAim();
            return true; // 중복이어도 촬영은 성립 — 송장을 접는다
        }

        // 뷰 속 바코드 줄무늬 — 송장과 같은 orderId 결정적 패턴(작게 16바).
        private void RebuildAimBarcode(int orderId)
        {
            if (_aimBarcode == null) return;
            const int bars = 16;
            float width = _aimBarcode.sizeDelta.x;
            var widths = new float[bars];
            float total = 0f;
            uint seed = (uint)(orderId * 2654435761u + 12345u);
            for (int i = 0; i < bars; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                widths[i] = 3f + (seed >> 8) % 7;
                total += widths[i] + 2f;
            }
            float scale = width / total;
            float x = 0f;
            for (int i = 0; i < bars; i++)
            {
                Image bar;
                if (i < _aimBarcode.childCount) bar = _aimBarcode.GetChild(i).GetComponent<Image>();
                else
                {
                    bar = new GameObject("Bar", typeof(RectTransform)).AddComponent<Image>();
                    bar.transform.SetParent(_aimBarcode, false);
                    bar.color = new Color(0.92f, 0.94f, 0.97f, 1f); // 파인더 속 밝은 줄무늬
                    bar.raycastTarget = false;
                }
                var rect = bar.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(x, 0f);
                rect.sizeDelta = new Vector2(widths[i] * scale, 0f);
                x += (widths[i] + 2f) * scale;
            }
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

            // S-028 ④ 테스트 전용 입금 버튼 — S-067 ⑦: 에디터/개발빌드에서만 생성 (릴리스 자동 제외).
            if (Application.isEditor || Debug.isDebugBuild)
            {
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
            // S-124 — 구매 그리드 1행(y 160~226)이 뷰포트 하단(0.46 → y 197.8)을 28px 침범했다
            // (남규님 관찰: "소파·책상 버튼이 보유 가구 박스 쪽을 침범"). 0.55 → y 236.5로 올려
            // 그리드 위에 10px 여백을 남긴다. 화면 430 배분: 헤더 70 / 인벤 236~360 / 구매 84~226 / 벽지·바닥 8~74.
            // 비율(0.46) 대신 절대 px — 구매 버튼이 절대 좌표라 좌표계가 섞이면 개구 높이가 바뀔 때마다
            // 다시 침범한다(남규님 재관찰). 하단 236px = 구매 그리드 상단(226) + 여백 10.
            vpRect.anchorMin = new Vector2(0f, 0f);
            vpRect.anchorMax = new Vector2(1f, 1f);
            vpRect.offsetMin = new Vector2(4f, 236f);
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
                // S-122 ⑧ — 화면루트 266 정합: 좌여백 6 + 폭 122 + 간격 8 + 폭 122 + 우여백 8
                // (구 184/196은 우측을 120px 관통 — 개구 축소 전에도 이탈이었다).
                rect.sizeDelta = new Vector2(122f, 66f);
                rect.anchoredPosition = new Vector2(6f + (i % 2) * 130f, 160f - (i / 2) * 76f);
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
            wallRect.sizeDelta = new Vector2(122f, 66f); // S-122 ⑧
            wallRect.anchoredPosition = new Vector2(6f, 8f);

            _floorButton = MakeButton(screen.transform, "Floor", "", () =>
            {
                _gameState.floorIndex = (_gameState.floorIndex + 1) % HomeDecorator.FloorPalette.Length;
                WorldAudioManager.Instance?.PlayUiTickSfx(); // AU-010
                RefreshDecorButtons();
            });
            RectTransform floorRect = (RectTransform)_floorButton.transform;
            floorRect.anchorMin = floorRect.anchorMax = floorRect.pivot = new Vector2(0f, 0f);
            floorRect.sizeDelta = new Vector2(122f, 66f); // S-122 ⑧
            floorRect.anchoredPosition = new Vector2(138f, 8f); // 6 + 122 + 간격 10

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
            if (_open) TogglePanel(); // 폰 닫고 배치 모드
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

            // S-038: 아파트 건이 잡혀 있으면 공동현관 비번을 여기서 알려준다 (키패드는 폰을 보란 콘셉트).
            foreach (DeliveryData d in _scanned)
                if (d.District == DeliveryOrderSO.DISTRICT_APARTMENT)
                {
                    sb.Append("공동현관 비번  <color=#35e0c8><b>").Append(_gameState.apartmentGatePassword).Append("</b></color>\n");
                    break;
                }

            // S-161 — 스캔한 건이 없으면 **여기서 스캔 방법을 알려준다**(남규님 지시).
            // 빈 목록만 띄우면 "아직 아무것도 없다"만 알 뿐 무엇을 해야 하는지 모른다.
            if (_scanned.Count == 0)
            {
                sb.Append("<color=#8a93a8>아직 스캔한 짐이 없다.</color>\n\n")
                  .Append("<color=#ff9f45><b>스캔하는 법</b></color>\n")
                  .Append("<size=88%>1. 상자 가까이 가서 <b>상자를 클릭</b>\n")
                  .Append("2. 뜬 <b>송장의 바코드에 마우스</b>를 갖다 댄다\n")
                  .Append("3. 폰이 올라오면 <b>카메라 한가운데</b>에 맞춰 잠깐 유지</size>\n");
                _deliveryList.text = sb.ToString();
                return;
            }

            sb.Append("<color=#8a93a8>No 운송장     순번 목적지</color>\n");
            var byDeadline = new List<DeliveryData>(_scanned);
            byDeadline.Sort((a, b) => a.DeadlineMinuteOfDay.CompareTo(b.DeadlineMinuteOfDay));
            for (int i = 0; i < _scanned.Count; i++)
            {
                DeliveryData d = _scanned[i];
                int rank = byDeadline.FindIndex(x => x.OrderId == d.OrderId) + 1;
                // S-084 ④ — 한 줄 유지: 주소는 6자 컷(초과 시 …).
                string shortAddress = d.Address != null && d.Address.Length > 6
                    ? d.Address.Substring(0, 6) + "…" : d.Address;
                string row = (i + 1) + " " + Invoice(d.OrderId) + "  " + rank + "  " + shortAddress;
                int status = _status.TryGetValue(d.OrderId, out int s) ? s : 0;
                // S-164 ④ — 방금 스캔한 줄은 다른 상태 표기보다 **먼저** 초록으로 잡는다.
                if (d.OrderId == _justScannedId)
                {
                    sb.Append("<color=#3ddc84><b>").Append(row).Append("  스캔완료</b></color>\n");
                    continue;
                }
                if (status == 1) sb.Append("<color=#8a93a8>").Append(row).Append(" ✓</color>\n");
                // S-075 ⑤ — 파손 상태: HP 소진으로 깨진 건은 배송앱에도 '파손'으로.
                else if (_gameState.destroyedOrderIds.Contains(d.OrderId))
                    sb.Append("<color=#8a93a8><s>").Append(row).Append("</s> 파손</color>\n");
                // S-084 ④ — 지각이어도 배달(배치)했으면 "배치됨"(빨간색 유지 — 벌점은 이미 받았다).
                else if (status == 2 && placedIds.Contains(d.OrderId))
                    sb.Append("<color=#ff7359>").Append(row).Append("  배치됨</color>\n");
                // S-122 ⑩ — 취소선 제거: 빨강(#ff7359)만으로 지각 표시 (배치됨 행과 표기 통일).
                else if (status == 2) sb.Append("<color=#ff7359>").Append(row).Append("  지각</color>\n");
                else if (!loadedIds.Contains(d.OrderId))
                {
                    sb.Append(row).Append("  <color=#ff9f45>미상차</color>\n"); // 캠프에서 실어야 스폰된다
                    // S-072 ⑨ — 구역 표시 회귀 수리: 미상차 행에도 목적지 구역을 보여준다
                    // (상차완료 행에만 있던 └ 서브라인이 미상차에선 빠져 있었다).
                    if (!string.IsNullOrEmpty(d.District))
                        sb.Append("<size=78%><color=#8a93a8>  └ ").Append(d.District).Append("</color></size>\n");
                }
                else
                {
                    // S-074 ⑤ — 도보 시대엔 "배송중" (상차는 트럭 해금 후에야 실체가 있다).
                    sb.Append(row).Append(placedIds.Contains(d.OrderId)
                        ? "  <color=#35e0c8>배치됨</color>"
                        : _gameState.hasTruck ? "  <color=#35e0c8><b>상차완료</b></color>"
                                              : "  <color=#35e0c8><b>배송중</b></color>").Append('\n');
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
            if (!_open) TogglePanel();
            ShowScreen(Screen.Call);
        }

        // S-037: 수신 화면을 띄운 채 통화가 종결(부재중 타임아웃 포함)되면 폰을 접는다.
        // 받기(Screen.Home 전환)·거절(수동 수납) 경로는 이미 Call 화면을 벗어나 있어 무해.
        private void OnMinigameEnded(MinigameResult _)
        {
            if (_screen != Screen.Call) return;
            if (_open) TogglePanel();
            ShowScreen(Screen.Home); // 재오픈 시 묵은 수신 화면이 남지 않게
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
                if (_open) TogglePanel();
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
                // ☎ 글리프는 Pretendard SDF에 없음 — TMP 폴백 워닝 유발이라 텍스트로 대체 (S-037 부수).
                _callLabel.text = "수신 중\n\n<size=140%><b>박말순</b></size>\n<color=#8a93a8>진상 기류의 냄새가 난다…</color>";
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
                    IsPinLocked(pin) ? pin.label + "\n<size=70%>잠김 — 개척 필요</size>" : pin.label,
                    () => OnPinTapped(index));
                RectTransform rect = (RectTransform)b.transform;
                // S-122 ⑨ — S-117 개구(298) 정합: 지도폭 266에 정규화 좌표 중앙배치라 반폭 58까지만 안 뚫린다
                // (구 158은 최좌 0.24 핀이 좌측 15px, 최우 0.74 핀이 우측 10px 이탈).
                PlaceOnMap(rect, pin.pos, new Vector2(116f, IsPinLocked(pin) ? 62f : 46f));
                b.GetComponentInChildren<TMP_Text>().fontSize = 18f; // 116px에 "아파트단지" 한 줄
                if (IsPinLocked(pin))
                {
                    b.image.color = new Color(0.25f, 0.27f, 0.32f, 0.9f);
                    b.GetComponentInChildren<TMP_Text>().color = new Color(0.65f, 0.68f, 0.75f);
                }
            }

            _mapInfoLabel = MakeText(screen.transform, "Info", "목적지 핀을 탭해라", 22f, Color.white, TextAlignmentOptions.BottomLeft); // S-122 ⑨ 26→22 (2줄이 62px 상자에 들어가게)
            RectTransform infoRect = _mapInfoLabel.rectTransform;
            infoRect.anchorMin = new Vector2(0f, 0f);
            infoRect.anchorMax = new Vector2(1f, 0f);
            infoRect.pivot = new Vector2(0.5f, 0f);
            infoRect.anchoredPosition = new Vector2(0f, 86f);
            infoRect.sizeDelta = new Vector2(0f, 62f);

            _departButton = MakeButton(screen.transform, "Depart",
                IsWalkingEra ? "트럭 없음 — 걸어서 개척" : "목적지로 출발", DepartSelected); // S-054b
            RectTransform departRect = (RectTransform)_departButton.transform;
            departRect.anchorMin = departRect.anchorMax = departRect.pivot = new Vector2(0.5f, 0f);
            departRect.sizeDelta = new Vector2(240f, 64f); // S-122 ⑨ — 화면루트 266 → 좌우 13px 여백 (구 320은 좌우 27px 이탈)
            departRect.anchoredPosition = new Vector2(0f, 6f);
            _departButton.GetComponentInChildren<TMP_Text>().fontSize = 20f; // "트럭 없음 — 걸어서 개척" 한 줄
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
            // AU-011: 핀 탭 = map_pin · 활성 핀은 경로가 그려지므로 map_route 동반 (잠금 핀은 pin만 — 막다른 감).
            WorldAudioManager.Instance?.PlayMapPinSfx();
            if (!IsPinLocked(Pins[index])) WorldAudioManager.Instance?.PlayMapRouteSfx();
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
            _departButton.interactable = (_inTravel || HasTruck) && !IsWalkingEra; // S-062 ⑥ — 트럭 시대 어디서나
        }

        // 출발 — 로직은 매니저 위임(시간=DayNight·목적지=Delivery·전이=SceneFlow). 구 TravelMapView.Depart 승계.
        // S-054b — 트럭 전에는 지도 출발 불가 (이동은 씬 가장자리 도보 게이트).
        private bool IsWalkingEra => _gameState != null && !_gameState.hasTruck && _gameState.unlockedDistricts.Count > 0;

        private bool HasTruck => _gameState != null && _gameState.hasTruck;

        private void DepartSelected()
        {
            if (_selectedPin < 0 || IsPinLocked(Pins[_selectedPin])) return;
            if (IsWalkingEra) { Debug.Log("[지도] 트럭이 없다 — 동네 가장자리로 걸어가자."); return; }
            if (!_inTravel && !HasTruck) return; // S-062 ⑥ — 트럭이 있으면 어디서든 지도 출발
            if (WorldSceneFlowManager.Instance == null || WorldDayNightManager.Instance == null
                || WorldDeliveryManager.Instance == null) return;
            if (WorldSceneFlowManager.Instance.IsTransitioning) return;

            MapPin pin = Pins[_selectedPin];
            WorldAudioManager.Instance?.PlayMapDepartSfx(); // AU-011
            WorldDayNightManager.Instance.AdvanceMinutes(pin.far ? _tuning.travelFarMinutes : _tuning.travelNearMinutes);
            WorldDeliveryManager.Instance.SetDestination(pin.district);
            // S-038·S-049: 아파트·언덕은 별도 씬(D-067) — 나머지는 공용 District.
            GameScene target = pin.district == DeliveryOrderSO.DISTRICT_APARTMENT ? GameScene.Apartment
                             : pin.district == DeliveryOrderSO.DISTRICT_HILLSIDE ? GameScene.Hillside
                             : GameScene.District;
            WorldSceneFlowManager.Instance.Request(target);
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

            // S-165 ① — **여기서는 등록하지 않는다.** 이 경로는 폰이 열린 채 상자를 좌클릭하면
            // 조준·유지 없이 곧바로 등록해 버렸다(남규님 지적: "바코드 안찍고 클릭만해도 등록됨").
            // 튜토리얼이 가르치는 절차(송장 → 바코드 조준 → 중앙 유지)와도 어긋난다.
            // 등록은 **조준 완료 경로 하나**로만 일어난다 — 여기는 어느 상자를 겨눴는지 표시만 한다.
            if (box == null || box.Order == null || !mouse.leftButton.wasPressedThisFrame) return;
            ShowWarn("상자를 클릭해 송장을 열고, 바코드를 카메라 중앙에 맞춰라");
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
