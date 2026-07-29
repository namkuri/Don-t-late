using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>설정 팝업 (S-065) — BGM/SFX 볼륨 슬라이더 · 처음 화면(타이틀)으로 · 뒤로가기.</summary>
    public class SettingsView : MonoBehaviour
    {
        public static SettingsView Instance { get; private set; }

        [SerializeField] private GameObject _panel;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Button _titleButton;
        [SerializeField] private Button _closeButton;

        private bool _inDialogue; // 대화(최상위 모달) 중 단축키 억제 — 닫기 버튼이 대화창에 가려짐 (S-101 캡처 검수 적발)

        private void Awake()
        {
            Instance = this;
            if (_bgmSlider != null) _bgmSlider.onValueChanged.AddListener(v => WorldAudioManager.Instance?.SetVolume(v));
            if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(v => WorldAudioManager.Instance?.SetSfxVolume(v));
            if (_titleButton != null) _titleButton.onClick.AddListener(GoTitle);
            if (_closeButton != null) _closeButton.onClick.AddListener(() => _panel.SetActive(false));
        }

        private void OnEnable()
        {
            WorldEvents.DialogueStarted += OnDialogueStarted;
            WorldEvents.DialogueEnded += OnDialogueEnded;
        }

        private void OnDisable()
        {
            WorldEvents.DialogueStarted -= OnDialogueStarted;
            WorldEvents.DialogueEnded -= OnDialogueEnded;
        }

        private void OnDialogueStarted(string _)
        {
            _inDialogue = true;
            if (_panel != null) _panel.SetActive(false); // 대화 개시 = 설정 수납
        }

        private void OnDialogueEnded(string _) => _inDialogue = false;

        // S-101 — ESC 토글. 송장이 같은 키로 닫히는 프레임엔 양보 (실행 순서 무관 방어).
        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame || _inDialogue) return;
            if (InvoiceView.IsOpen || InvoiceView.LastEscCloseFrame == Time.frameCount) return;
            Toggle();
        }

        public void Toggle()
        {
            if (_panel == null) return;
            bool open = !_panel.activeSelf;
            _panel.SetActive(open);
            if (open && WorldAudioManager.Instance != null)
            {
                if (_bgmSlider != null) _bgmSlider.SetValueWithoutNotify(WorldAudioManager.Instance.Volume);
                if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(WorldAudioManager.Instance.SfxVolume);
            }
        }

        private void GoTitle()
        {
            _panel.SetActive(false);
            if (WorldSceneFlowManager.Instance == null || WorldSceneFlowManager.Instance.IsTransitioning) return;
            WorldSceneFlowManager.Instance.Request(GameScene.Main);
        }
    }
}
