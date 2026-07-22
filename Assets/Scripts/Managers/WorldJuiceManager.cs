using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 이벤트 → 화면 전역 연출 매핑 (JUICE.md 표 시공 — S-023).
    /// 담당 행: 배송 완료(플래시+체크팝+히트스톱)·시간초과 실패(레드 비네트 펄스+셰이크+히트스톱).
    /// HUD 펄스·리듬 노트·컷인 등은 각 View 소유 — 여기는 화면 전역 연출만.
    /// 오버레이 캔버스는 런타임에 스스로 조립한다(씬 재조립만으로 완성 — PhoneView 방식).
    /// 카메라 셰이크는 Y축만 — CameraFollowX가 X만 쓰고 Y·Z는 보존하므로 충돌이 구조적으로 없다.
    /// 연출 애니메이션은 전부 unscaled 시간 — 히트스톱(timeScale=0)과 겹쳐도 멈추지 않는다.
    /// </summary>
    public class WorldJuiceManager : MonoBehaviour
    {
        public static WorldJuiceManager Instance { get; private set; }

        [Header("폰트 (빌더 주입 — 체크팝 텍스트)")]
        [SerializeField] private TMP_FontAsset _font;

        [Header("배송 완료 — 플래시·체크팝·히트스톱·미세 셰이크")]
        [SerializeField, Range(0f, 1f)] private float _completeFlashAlpha = 0.35f;
        [SerializeField] private float _completeFlashSeconds = 0.18f;
        [SerializeField] private float _completePopScale = 1.45f;
        [SerializeField] private float _completePopSeconds = 0.7f;
        [SerializeField] private float _completeHitStopSeconds = 0.05f;
        [SerializeField] private float _completeShakeAmplitude = 0.04f;
        [SerializeField] private float _completeShakeSeconds = 0.15f;

        [Header("시간초과 실패 — 레드 비네트 펄스·히트스톱·셰이크(소)")]
        [SerializeField, Range(0f, 1f)] private float _failVignetteAlpha = 0.4f;
        [SerializeField] private int _failVignettePulses = 2;
        [SerializeField] private float _failVignetteSeconds = 0.7f;
        [SerializeField] private float _failHitStopSeconds = 0.1f;
        [SerializeField] private float _failShakeAmplitude = 0.12f;
        [SerializeField] private float _failShakeSeconds = 0.35f;

        private static readonly Color CYAN = new Color(0.208f, 0.878f, 0.784f, 1f);   // #35e0c8
        private static readonly Color RED = new Color(0.9f, 0.15f, 0.15f, 0f);

        private Image _flash;
        private Image _vignette;
        private TMP_Text _popLabel;

        private Coroutine _flashRoutine;
        private Coroutine _vignetteRoutine;
        private Coroutine _popRoutine;
        private Coroutine _hitStopRoutine;
        private Coroutine _shakeRoutine;

        // 셰이크가 지난 프레임에 얹은 Y 오프셋 — 중단·종료 시 정확히 되돌린다.
        private Transform _shakeTarget;
        private float _shakeApplied;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            BuildOverlay();
        }

        private void OnEnable()
        {
            WorldEvents.DeliveryCompleted += OnDeliveryCompleted;
            WorldEvents.DeliveryFailed += OnDeliveryFailed;
        }

        private void OnDisable()
        {
            WorldEvents.DeliveryCompleted -= OnDeliveryCompleted;
            WorldEvents.DeliveryFailed -= OnDeliveryFailed;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── 이벤트 → 연출 ────────────────────────────────────

        private void OnDeliveryCompleted(DeliveryData data)
        {
            Restart(ref _flashRoutine, FlashRoutine());
            Restart(ref _popRoutine, PopRoutine("✓ +₩" + data.Reward.ToString("N0")));
            Restart(ref _hitStopRoutine, HitStopRoutine(_completeHitStopSeconds));
            StartShake(_completeShakeAmplitude, _completeShakeSeconds);
        }

        private void OnDeliveryFailed(DeliveryData data)
        {
            Restart(ref _vignetteRoutine, VignetteRoutine());
            Restart(ref _hitStopRoutine, HitStopRoutine(_failHitStopSeconds));
            StartShake(_failShakeAmplitude, _failShakeSeconds);
        }

        // ── 오버레이 조립 (런타임) ───────────────────────────

        private void BuildOverlay()
        {
            GameObject canvasGo = new GameObject("JuiceCanvas");
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80; // HUD(10) 위 · 폰(85)·대화(90)·페이드(100) 아래
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _flash = CreateFullscreenImage(canvasGo.transform, "Flash", new Color(1f, 1f, 1f, 0f));
            _vignette = CreateFullscreenImage(canvasGo.transform, "Vignette", RED);

            GameObject popGo = new GameObject("CheckPop", typeof(RectTransform));
            popGo.transform.SetParent(canvasGo.transform, false);
            _popLabel = popGo.AddComponent<TextMeshProUGUI>();
            if (_font != null) _popLabel.font = _font;
            _popLabel.fontSize = 96f;
            _popLabel.color = CYAN;
            _popLabel.alignment = TextAlignmentOptions.Center;
            _popLabel.raycastTarget = false;
            RectTransform popRect = _popLabel.rectTransform;
            popRect.anchorMin = popRect.anchorMax = new Vector2(0.5f, 0.55f);
            popRect.pivot = new Vector2(0.5f, 0.5f);
            popRect.sizeDelta = new Vector2(900f, 140f);
            popGo.SetActive(false);
        }

        private static Image CreateFullscreenImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return image;
        }

        // ── 연출 루틴 (전부 unscaled) ────────────────────────

        private void Restart(ref Coroutine slot, IEnumerator routine)
        {
            if (slot != null) StopCoroutine(slot);
            slot = StartCoroutine(routine);
        }

        private IEnumerator FlashRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _completeFlashSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(elapsed / _completeFlashSeconds);
                SetAlpha(_flash, _completeFlashAlpha * k);
                yield return null;
            }
            SetAlpha(_flash, 0f);
            _flashRoutine = null;
        }

        private IEnumerator VignetteRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _failVignetteSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / _failVignetteSeconds);
                // 펄스 N회 — 사인 반주기 반복, 끝은 0으로 수렴.
                float pulse = Mathf.Abs(Mathf.Sin(k * Mathf.PI * _failVignettePulses)) * (1f - k);
                SetAlpha(_vignette, _failVignetteAlpha * pulse);
                yield return null;
            }
            SetAlpha(_vignette, 0f);
            _vignetteRoutine = null;
        }

        private IEnumerator PopRoutine(string text)
        {
            _popLabel.text = text;
            _popLabel.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < _completePopSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / _completePopSeconds);
                float ease = 1f - (1f - k) * (1f - k); // ease-out
                _popLabel.rectTransform.localScale =
                    Vector3.one * Mathf.LerpUnclamped(_completePopScale, 1f, ease);
                // 뒤 30%에서 페이드아웃.
                float alpha = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                _popLabel.color = new Color(CYAN.r, CYAN.g, CYAN.b, alpha);
                yield return null;
            }
            _popLabel.gameObject.SetActive(false);
            _popRoutine = null;
        }

        private IEnumerator HitStopRoutine(float seconds)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(seconds);
            Time.timeScale = 1f;
            _hitStopRoutine = null;
        }

        // ── 카메라 셰이크 (Y 전용) ───────────────────────────

        private void StartShake(float amplitude, float seconds)
        {
            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
                UndoShake();
            }
            _shakeRoutine = StartCoroutine(ShakeRoutine(amplitude, seconds));
        }

        private IEnumerator ShakeRoutine(float amplitude, float seconds)
        {
            Camera camera = Camera.main; // 콘텐츠 씬 소유 — 셰이크 시작 시점에 잡는다
            if (camera == null) { _shakeRoutine = null; yield break; }
            _shakeTarget = camera.transform;
            _shakeApplied = 0f;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_shakeTarget == null) { _shakeRoutine = null; yield break; } // 씬 전환으로 카메라 소멸
                float damp = 1f - Mathf.Clamp01(elapsed / seconds);
                float y = (Mathf.PerlinNoise(0.37f, elapsed * 35f) * 2f - 1f) * amplitude * damp;
                _shakeTarget.position += new Vector3(0f, y - _shakeApplied, 0f);
                _shakeApplied = y;
                yield return null;
            }
            UndoShake();
            _shakeRoutine = null;
        }

        private void UndoShake()
        {
            if (_shakeTarget != null)
                _shakeTarget.position -= new Vector3(0f, _shakeApplied, 0f);
            _shakeTarget = null;
            _shakeApplied = 0f;
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
