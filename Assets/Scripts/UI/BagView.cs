using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 가방(인벤토리) 팝업 (S-064). 기본 5칸 — 좌클릭 선택(들 수 있으면 손에 들기 요청),
    /// 우클릭 컨텍스트(사용/버리기), 드래그 드랍 칸 이동. 데이터는 GameStateSO.bagItems 단일 소유.
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
        [SerializeField] private Button _dropButton;

        private int _selected = -1;
        private bool _inTitle = true; // 게임은 Main(타이틀)에서 시작 — 타이틀에선 가방 금지 (PhoneView 관례)
        private bool _inDialogue; // 대화(최상위 모달) 중 단축키 억제 — 팝업이 대화창 밑에 깔리는 겹침 방지 (S-101 캡처 검수 적발)

        private void Awake()
        {
            Instance = this;
            if (_useButton != null) _useButton.onClick.AddListener(UseSelected);
            if (_dropButton != null) _dropButton.onClick.AddListener(DropSelected);
        }

        private void OnEnable()
        {
            WorldEvents.SceneTransitionCompleted += OnSceneArrived;
            WorldEvents.DialogueStarted += OnDialogueStarted;
            WorldEvents.DialogueEnded += OnDialogueEnded;
        }

        private void OnDisable()
        {
            WorldEvents.SceneTransitionCompleted -= OnSceneArrived;
            WorldEvents.DialogueStarted -= OnDialogueStarted;
            WorldEvents.DialogueEnded -= OnDialogueEnded;
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
            if (item.holdable && WorldEvents.HasBagHoldListener) // 플레이어 없는 씬(집)은 선택만 (S-064 유실 방지)
            {
                // 손에 들기 — Player 도메인이 WorldEvents로 받아 처리 (S-064).
                BagStorage.RemoveOne(_gameState, index);
                _selected = -1;
                WorldEvents.RaiseBagHoldRequested(item);
            }
            Refresh();
        }

        public void OnSlotRightClick(int index)
        {
            if (_gameState == null || index >= _gameState.bagItems.Count) { HideContext(); return; }
            _selected = index;
            Refresh();
            if (_contextMenu != null)
            {
                _contextMenu.SetActive(true);
                _contextMenu.transform.position = _slots[index].transform.position + new Vector3(70f, -40f, 0f);
            }
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

        private void UseSelected()
        {
            if (_gameState == null || _selected < 0 || _selected >= _gameState.bagItems.Count) { HideContext(); return; }
            if (!WorldEvents.HasBagConsumeListener) { Debug.Log("[가방] 여기선 사용할 수 없다"); HideContext(); return; }
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
