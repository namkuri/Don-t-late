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

        private void Awake()
        {
            Instance = this;
            if (_bgmSlider != null) _bgmSlider.onValueChanged.AddListener(v => WorldAudioManager.Instance?.SetVolume(v));
            if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(v => WorldAudioManager.Instance?.SetSfxVolume(v));
            if (_titleButton != null) _titleButton.onClick.AddListener(GoTitle);
            if (_closeButton != null) _closeButton.onClick.AddListener(() => _panel.SetActive(false));
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
