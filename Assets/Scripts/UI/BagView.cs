using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 가방(인벤토리) 팝업 (S-064). 기본 4칸 — **좌클릭 = 즉시 사용**(S-205),
    /// 우클릭 컨텍스트(손에 들기/버리기), 드래그 드랍 칸 이동. 데이터는 GameStateSO.bagItems 단일 소유.
    /// 손 들기·소비는 도메인 경계 규칙대로 WorldEvents로 Player에 통지한다.
    /// </summary>
    public class BagView : MonoBehaviour
    {
        public static BagView Instance { get; private set; }

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private GameObject _panel;
        [SerializeField] private BagSlot[] _slots;
        [SerializeField] private GameObject _contextMenu;
        [SerializeField] private Button _useButton;
        [Tooltip("S-243 — 우클릭 메뉴의 '사용'. 빌더 주입, 없으면 조용히 생략(좌클릭 사용은 그대로).")]
        [SerializeField] private Button _consumeButton;
        [SerializeField] private Button _dropButton;
        [Tooltip("S-205 — 슬롯 호버 안내('[좌클릭] 사용'). 빌더 주입, 없으면 조용히 생략.")]
        [SerializeField] private TMP_Text _hoverHint;

        private int _selected = -1;
        private bool _inTitle = true; // 게임은 Main(타이틀)에서 시작 — 타이틀에선 가방 금지 (PhoneView 관례)
        private bool _inDialogue; // 대화(최상위 모달) 중 단축키 억제 — 팝업이 대화창 밑에 깔리는 겹침 방지 (S-101 캡처 검수 적발)

        private void Awake()
        {
            Instance = this;
            if (_useButton != null) _useButton.onClick.AddListener(UseSelected);
            if (_consumeButton != null) _consumeButton.onClick.AddListener(ConsumeSelected);
            if (_dropButton != null) _dropButton.onClick.AddListener(DropSelected);
            CaptureContextSlots();
        }

        private void OnEnable()
        {
            WorldEvents.SceneTransitionCompleted += OnSceneArrived;
            WorldEvents.DialogueStarted += OnDialogueStarted;
            WorldEvents.DialogueEnded += OnDialogueEnded;
            WorldEvents.BagChanged += Refresh;   // S-243 — 밖에서 바뀐 가방을 화면이 따라온다
        }

        private void OnDisable()
        {
            WorldEvents.SceneTransitionCompleted -= OnSceneArrived;
            WorldEvents.DialogueStarted -= OnDialogueStarted;
            WorldEvents.DialogueEnded -= OnDialogueEnded;
            WorldEvents.BagChanged -= Refresh;
        }

        private void OnDialogueStarted(string _) { _inDialogue = true; Close(); }
        private void OnDialogueEnded(string _) => _inDialogue = false;

        private void OnSceneArrived(GameScene scene)
        {
            _inTitle = scene == GameScene.Main;
            if (_inTitle) Close(); // 타이틀 복귀 시 강제 수납
        }

        // S-101 — I키 토글 (버튼 경로와 병행).
        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.iKey.wasPressedThisFrame && !_inTitle && !_inDialogue) Toggle();
        }

        public void Toggle()
        {
            if (_panel == null) return;
            bool open = !_panel.activeSelf;
            _panel.SetActive(open);
            _selected = -1;
            HideContext();
            if (open) { Refresh(); WorldEvents.RaiseBagOpened(); } // S-146 — 튜토리얼 진행 판정용
        }

        public void Close()
        {
            if (_panel != null) _panel.SetActive(false);
            HideContext();
        }

        public void Refresh()
        {
            if (_slots == null || _gameState == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                BagItem? item = i < _gameState.bagItems.Count ? _gameState.bagItems[i] : (BagItem?)null;
                _slots[i].Render(item, i == _selected);
            }
        }

        public void OnSlotLeftClick(int index)
        {
            HideContext();
            if (_gameState == null || index >= _gameState.bagItems.Count) { _selected = -1; Refresh(); return; }

            _selected = index;
            BagItem item = _gameState.bagItems[index];

            // S-205 — **좌클릭 한 번에 바로 쓴다**(남규님 지시). 종전엔 우클릭 → '사용' 2단계라
            // 스태미나가 바닥났을 때 드링크를 꺼내 먹는 데 두 번이 걸렸다 — 급할 때 쓰라고 만든
            // 물건이 급할 때 못 쓰이는 구조였다.
            // '손에 들기'(S-064 — 들고 던지기)는 우클릭 메뉴로 옮긴다. 자주 쓰는 쪽이 한 번,
            // 가끔 쓰는 쪽이 두 번이 맞다.
            if (WorldEvents.HasBagConsumeListener)
            {
                BagStorage.RemoveOne(_gameState, index);
                _selected = -1;
                WorldEvents.RaiseBagItemConsumed(item); // 사용 효과는 아이템별 — Player 도메인 몫
                Refresh();
                return;
            }

            // 소비를 받을 사람이 없는 씬(집 등)에선 종전대로 — 들 수 있으면 손에.
            if (item.holdable && WorldEvents.HasBagHoldListener)
            {
                BagStorage.RemoveOne(_gameState, index);
                _selected = -1;
                WorldEvents.RaiseBagHoldRequested(item);
            }
            Refresh();
        }

        /// <summary>
        /// S-205 — 슬롯 호버 안내. 좌클릭이 실제로 무엇을 하는지는 씬에 따라 갈린다
        /// (플레이어가 있으면 사용 / 집처럼 없으면 들기·버리기뿐) — 안내도 그대로 갈라 준다.
        /// "사용"이라 써 놓고 아무 일도 안 일어나면 그게 제일 나쁘다.
        /// </summary>
        public void OnSlotHover(int index, bool entered)
        {
            if (_hoverHint == null) return;

            if (!entered || _gameState == null || index < 0 || index >= _gameState.bagItems.Count)
            {
                _hoverHint.gameObject.SetActive(false);
                return;
            }

            BagItem item = _gameState.bagItems[index];
            bool canUse = WorldEvents.HasBagConsumeListener;
            bool canHold = item.holdable && WorldEvents.HasBagHoldListener;
            // S-243 — 우클릭은 이제 '사용'도 들어 있는 메뉴다. 특정 항목 이름을 박아 두면
            // 메뉴 구성이 바뀔 때마다 안내가 거짓말을 한다.
            _hoverHint.text = canUse
                ? "[좌클릭] 사용   ·   [우클릭] 메뉴"
                : (canHold ? "[좌클릭] 손에 들기   ·   [우클릭] 메뉴" : "[우클릭] 메뉴");
            _hoverHint.gameObject.SetActive(true);
        }

        public void OnSlotRightClick(int index)
        {
            if (_gameState == null || index >= _gameState.bagItems.Count) { HideContext(); return; }
            _selected = index;
            Refresh();

            // S-243 — 여기서 못 쓰는 항목은 아예 안 보여 준다. 눌렀는데 아무 일도 안 일어나는
            // 버튼이 "아이템이 그냥 사라졌다"는 인상의 절반이었다.
            BagItem item = _gameState.bagItems[index];
            if (_consumeButton != null) _consumeButton.gameObject.SetActive(WorldEvents.HasBagConsumeListener);
            if (_useButton != null) _useButton.gameObject.SetActive(item.holdable && WorldEvents.HasBagHoldListener);
            LayoutContext();

            if (_contextMenu != null)
            {
                _contextMenu.SetActive(true);
                _contextMenu.transform.position = _slots[index].transform.position + new Vector3(70f, -40f, 0f);
            }
        }

        /// <summary>
        /// 숨긴 항목이 빈 칸으로 남지 않게 남은 버튼을 위에서부터 다시 쌓는다 (S-243).
        /// 칸 위치는 조립 시점의 것을 그대로 재사용한다 — 가방 UI는 아트 프리팹으로도,
        /// 코드로도 조립되고 둘의 간격·여백이 다르다. 숫자를 박으면 한쪽이 깨진다.
        /// </summary>
        private void LayoutContext()
        {
            if (_contextSlots == null || _contextSlots.Length == 0) return;

            Button[] buttons = { _consumeButton, _useButton, _dropButton };
            int visible = 0;
            foreach (Button button in buttons)
            {
                if (button == null || !button.gameObject.activeSelf) continue;
                if (visible < _contextSlots.Length)
                    ((RectTransform)button.transform).anchoredPosition = _contextSlots[visible];
                visible++;
            }

            if (_contextMenu == null) return;
            float gap = _contextSlots.Length > 1 ? _contextSlots[0].y - _contextSlots[1].y : 0f;
            RectTransform rect = (RectTransform)_contextMenu.transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x,
                _contextFullHeight - (_contextSlots.Length - visible) * gap);
        }

        // 조립 시점의 칸 위치·메뉴 높이 (S-243) — 위에서 아래 순.
        private Vector2[] _contextSlots;
        private float _contextFullHeight;

        private void CaptureContextSlots()
        {
            var slots = new System.Collections.Generic.List<Vector2>();
            foreach (Button button in new[] { _consumeButton, _useButton, _dropButton })
                if (button != null) slots.Add(((RectTransform)button.transform).anchoredPosition);
            slots.Sort((a, b) => b.y.CompareTo(a.y));
            _contextSlots = slots.ToArray();
            if (_contextMenu != null) _contextFullHeight = ((RectTransform)_contextMenu.transform).sizeDelta.y;
        }

        public void OnSlotDropped(int from, int to)
        {
            if (_gameState == null || from < 0 || from >= _gameState.bagItems.Count) return;
            HideContext();
            _selected = -1;

            if (to < _gameState.bagItems.Count)
            {
                (BagItem a, BagItem b) = (_gameState.bagItems[from], _gameState.bagItems[to]);
                _gameState.bagItems[from] = b;
                _gameState.bagItems[to] = a;
            }
            else
            {
                BagItem moved = _gameState.bagItems[from];
                _gameState.bagItems.RemoveAt(from);
                _gameState.bagItems.Add(moved); // 빈 칸으로 끌기 = 맨 끝으로
            }
            Refresh();
        }

        /// <summary>
        /// S-205 — 우클릭 메뉴의 첫 항목은 이제 **'손에 들기'**다(사용은 좌클릭으로 옮겼다).
        /// 들 수 없는 물건이면 종전대로 사용으로 떨어진다 — 메뉴가 아무 일도 안 하면 그게 더 나쁘다.
        /// </summary>
        private void UseSelected()
        {
            if (_gameState == null || _selected < 0 || _selected >= _gameState.bagItems.Count) { HideContext(); return; }
            BagItem item = _gameState.bagItems[_selected];

            if (item.holdable && WorldEvents.HasBagHoldListener)
            {
                BagStorage.RemoveOne(_gameState, _selected);
                _selected = -1;
                HideContext();
                WorldEvents.RaiseBagHoldRequested(item);
                Refresh();
                return;
            }

            if (!WorldEvents.HasBagConsumeListener) { Debug.Log("[가방] 여기선 쓸 수 없다"); HideContext(); return; }
            BagStorage.RemoveOne(_gameState, _selected);
            _selected = -1;
            HideContext();
            WorldEvents.RaiseBagItemConsumed(item); // 사용 효과는 아이템별 — Player 도메인 몫
            Refresh();
        }

        /// <summary>
        /// S-243 — 우클릭 메뉴의 '사용'. 좌클릭 사용(S-205)과 같은 일을 하지만, 남규님을 포함해
        /// 우클릭으로 쓰려는 손이 실제로 있다. 우클릭 메뉴에 '사용'이 없어서 '손에 들기'가
        /// 눌렸고, 그게 거절되면 아이템만 사라졌다.
        /// </summary>
        private void ConsumeSelected()
        {
            if (_gameState == null || _selected < 0 || _selected >= _gameState.bagItems.Count) { HideContext(); return; }
            if (!WorldEvents.HasBagConsumeListener) { Debug.Log("[가방] 여기선 쓸 수 없다"); HideContext(); return; }

            BagItem item = _gameState.bagItems[_selected];
            BagStorage.RemoveOne(_gameState, _selected);
            _selected = -1;
            HideContext();
            WorldEvents.RaiseBagItemConsumed(item); // 사용 효과는 아이템별 — Player 도메인 몫
            Refresh();
        }

        private void DropSelected()
        {
            if (_gameState == null || _selected < 0 || _selected >= _gameState.bagItems.Count) { HideContext(); return; }
            Debug.Log("[가방] " + _gameState.bagItems[_selected].label + " 버림");
            BagStorage.RemoveOne(_gameState, _selected);
            _selected = -1;
            HideContext();
            Refresh();
        }

        private void HideContext()
        {
            if (_contextMenu != null) _contextMenu.SetActive(false);
        }
    }
}
