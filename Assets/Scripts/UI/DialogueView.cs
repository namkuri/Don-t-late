using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 포켓몬식 스피치 박스. 이벤트 구독 + 표시만 한다 — 진행 판정은 WorldDialogueManager 몫.
    /// 타이핑 효과 · 문자 블립 합성음 · 2단 입력(스킵→진행)을 담당한다.
    /// 진행 입력 = 박스 클릭 + Space (E는 월드 상호작용 전용이라 쓰지 않는다).
    /// 블립 오디오 소유권은 P4 WorldAudioManager로 이관 예정 — 지금은 로컬 2D 소스.
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        [Header("루트 (평소 숨김)")]
        [SerializeField] private GameObject _box;

        [Header("텍스트")]
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _bodyLabel;
        [SerializeField] private GameObject _arrow; // "▼" 대기 표시

        [Header("입력")]
        [SerializeField] private Button _advanceButton;

        [Header("블립 (로컬 2D)")]
        [SerializeField] private AudioSource _blipSource;
        [SerializeField] private AudioClip _blipClip;

        [Header("튜닝")]
        [Tooltip("문자 1개당 노출 간격(초).")]
        [SerializeField] private float _charInterval = 0.035f;
        [Tooltip("이 문자 수마다 블립 1회(공백은 스킵).")]
        [SerializeField] private int _blipEveryNChars = 2;
        [SerializeField] private float _arrowBlinkInterval = 0.4f;

        private InputAction _advance;      // Space
        private bool _boxActive;
        private bool _isTyping;
        private string _fullLine = string.Empty;
        private Coroutine _typeRoutine;
        private Coroutine _arrowRoutine;

        private void Awake()
        {
            _advance = new InputAction("DialogueAdvance", InputActionType.Button);
            _advance.AddBinding("<Keyboard>/space");
            _advance.AddBinding("<Keyboard>/enter");      // S-010: 엔터도 진행
            _advance.AddBinding("<Keyboard>/numpadEnter");
            _advance.AddBinding("<Mouse>/leftButton");    // S-010: 화면 아무 데나 좌클릭
        }

        private void OnEnable()
        {
            WorldEvents.DialogueStarted += OnDialogueStarted;
            WorldEvents.DialogueEnded += OnDialogueEnded;
            WorldDialogueManager.LineChanged += OnLineChanged;

            _advance.Enable();
            _advance.performed += OnAdvancePerformed;
            if (_advanceButton != null) _advanceButton.onClick.AddListener(OnAdvanceInput);
        }

        private void OnDisable()
        {
            WorldEvents.DialogueStarted -= OnDialogueStarted;
            WorldEvents.DialogueEnded -= OnDialogueEnded;
            WorldDialogueManager.LineChanged -= OnLineChanged;

            _advance.performed -= OnAdvancePerformed;
            _advance.Disable();
            if (_advanceButton != null) _advanceButton.onClick.RemoveListener(OnAdvanceInput);
        }

        private void OnDestroy() => _advance.Dispose();

        private void Start()
        {
            if (_box != null) _box.SetActive(false);
            if (_arrow != null) _arrow.SetActive(false);
        }

        // ── 표시 토글 ─────────────────────────────────────────
        private void OnDialogueStarted(string scenarioName)
        {
            _boxActive = true;
            if (_box != null)
            {
                _box.SetActive(true);
                _boxOrigin = _box.GetComponent<RectTransform>().anchoredPosition;
            }
        }

        /// <summary>
        /// S-028 ①: 박말순(주인공 아닌 화자) 대사가 타이핑되는 동안 계속 흔든다. 주인공 라인은 무셰이크.
        /// 타이핑이 끝나면(스킵 포함) 원위치로 복귀.
        /// </summary>
        private IEnumerator ShakeWhileTyping()
        {
            RectTransform rect = _box.GetComponent<RectTransform>();
            const float STRENGTH = 12f; // px — 지속형이라 S-027 단발(18px)보다 소폭 절제
            const float FREQUENCY = 14f;
            float t = 0f;
            while (_boxActive && _isTyping)
            {
                t += Time.unscaledDeltaTime * FREQUENCY;
                rect.anchoredPosition = _boxOrigin + new Vector2(
                    (Mathf.PerlinNoise(t, 0.3f) - 0.5f) * 2f * STRENGTH,
                    (Mathf.PerlinNoise(0.7f, t) - 0.5f) * 2f * STRENGTH);
                yield return null;
            }
            rect.anchoredPosition = _boxOrigin;
            _shakeRoutine = null;
        }

        private void OnDialogueEnded(string scenarioName)
        {
            _boxActive = false;
            StopTyping();
            StopShake();
            StopArrow();
            if (_box != null) _box.SetActive(false);
        }

        // ── 라인 전환 ─────────────────────────────────────────
        private void OnLineChanged(DialogueScenarioSO.Line line)
        {
            if (_nameLabel != null) _nameLabel.text = line.speaker;
            _fullLine = line.text ?? string.Empty;
            StopArrow();
            if (_arrow != null) _arrow.SetActive(false);

            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            _typeRoutine = StartCoroutine(TypeRoutine());

            // S-028 ①: 주인공이 아닌 화자(박말순)의 라인은 타이핑 내내 셰이크.
            StopShake();
            if (_box != null && line.speaker != "주인공")
                _shakeRoutine = StartCoroutine(ShakeWhileTyping());
        }

        private Coroutine _shakeRoutine;
        private Vector2 _boxOrigin;

        private void StopShake()
        {
            if (_shakeRoutine != null) { StopCoroutine(_shakeRoutine); _shakeRoutine = null; }
            if (_box != null && _box.activeSelf)
                _box.GetComponent<RectTransform>().anchoredPosition = _boxOrigin;
        }

        private IEnumerator TypeRoutine()
        {
            _isTyping = true;
            if (_bodyLabel != null) _bodyLabel.text = string.Empty;

            for (int i = 0; i < _fullLine.Length; i++)
            {
                char c = _fullLine[i];
                if (_bodyLabel != null) _bodyLabel.text = _fullLine.Substring(0, i + 1);

                if (c != ' ' && c != '\n' && (i % _blipEveryNChars) == 0)
                    PlayBlip();

                yield return new WaitForSeconds(_charInterval);
            }

            FinishTyping();
        }

        private void FinishTyping()
        {
            if (_bodyLabel != null) _bodyLabel.text = _fullLine;
            _isTyping = false;
            _typeRoutine = null;
            if (_arrow != null) _arrow.SetActive(true);
            StartArrow();
        }

        private void StopTyping()
        {
            if (_typeRoutine != null) { StopCoroutine(_typeRoutine); _typeRoutine = null; }
            _isTyping = false;
        }

        // ── 입력 → 2단 진행 ──────────────────────────────────
        private void OnAdvancePerformed(InputAction.CallbackContext _) => OnAdvanceInput();

        private int _lastAdvanceFrame = -1;

        private void OnAdvanceInput()
        {
            if (!_boxActive) return;
            // 박스 클릭(Button)과 좌클릭 바인딩이 같은 프레임에 겹쳐 2줄 스킵되는 것 방지 (S-010).
            if (Time.frameCount == _lastAdvanceFrame) return;
            _lastAdvanceFrame = Time.frameCount;

            if (_isTyping)
            {
                // 타이핑 중 입력 → 남은 문자 전체 즉시 표시 (매니저 무관).
                StopTyping();
                FinishTyping();
                return;
            }

            // 라인 완료 상태 → 매니저에 진행 요청.
            if (WorldDialogueManager.Instance != null)
                WorldDialogueManager.Instance.AdvanceRequested();
        }

        // ── 블립 ─────────────────────────────────────────────
        private void PlayBlip()
        {
            if (_blipSource == null || _blipClip == null) return;
            _blipSource.pitch = Random.Range(0.95f, 1.05f);
            _blipSource.PlayOneShot(_blipClip);
        }

        // ── 대기 화살표 깜빡임 ───────────────────────────────
        private void StartArrow()
        {
            if (_arrow == null) return;
            _arrowRoutine = StartCoroutine(ArrowBlink());
        }

        private void StopArrow()
        {
            if (_arrowRoutine != null) { StopCoroutine(_arrowRoutine); _arrowRoutine = null; }
        }

        private IEnumerator ArrowBlink()
        {
            var wait = new WaitForSeconds(_arrowBlinkInterval);
            while (true)
            {
                _arrow.SetActive(!_arrow.activeSelf);
                yield return wait;
            }
        }
    }
}
