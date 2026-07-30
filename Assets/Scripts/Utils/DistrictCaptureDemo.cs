using System.Collections.Generic;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// District 영상 촬영용 자동 연출.
    /// 플레이어가 화면 좌우를 달려 왕복하고, 끝에 닿을 때마다 T/Y와 같은 시간·날씨 전환을 실행한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DistrictCaptureDemo : MonoBehaviour
    {
        public static bool SuppressTrafficAccidents { get; private set; }

        [SerializeField] private PlayerManager _player;
        [SerializeField] private Camera _camera;
        [SerializeField] private float _leftX = -15.44f;
        [SerializeField] private float _rightX = 15.44f;
        [SerializeField] private float _turnPauseSeconds = 0.35f;
        [SerializeField] private Vector3 _cameraPosition = new Vector3(0f, 38.1f, -40.4f);
        [SerializeField] private Vector3 _cameraEuler = new Vector3(10f, 0f, 0f);
        [SerializeField] private float _cameraEndY = 11f;
        [SerializeField] private float _cameraStartDelaySeconds = 2f;
        [SerializeField] private float _cameraDescentSeconds = 10f;
        [SerializeField] private float _runSpeedMultiplier = 2f;
        [SerializeField] private Transform _carryAnchor;
        [SerializeField] private GameObject _boxPropPrefab;
        [SerializeField] private float _boxLocalYOffset = -2f;

        private PlayerInputHandler _input;
        private CameraFollowX _cameraFollow;
        private int _direction = 1;
        private float _pauseTimer;
        private float _cameraElapsed;
        private bool _initialWeatherApplied;
        private bool _runStarted;
        private GameObject _demoBox;
        private readonly List<Variation> _variationDeck = new List<Variation>();
        private int _variationIndex;
        private Variation _lastVariation;

        private readonly struct Variation
        {
            public readonly WeatherType Weather;
            public readonly int Hour;

            public Variation(WeatherType weather, int hour)
            {
                Weather = weather;
                Hour = hour;
            }
        }

        private static readonly WeatherType[] CaptureWeathers =
        {
            WeatherType.Clear,
            WeatherType.Rain,
            WeatherType.Heat,
            WeatherType.Snow,
            WeatherType.Cloudy
        };

        private static readonly int[] CapturePhaseHours = { 6, 10, 17, 20 };

        private void Start()
        {
            SuppressTrafficAccidents = true;
            if (_player == null) _player = FindAnyObjectByType<PlayerManager>();
            if (_camera == null) _camera = Camera.main;

            if (_player == null)
            {
                Debug.LogError("[DistrictCaptureDemo] PlayerManager를 찾지 못했습니다.");
                enabled = false;
                return;
            }

            _input = _player.Input != null
                ? _player.Input
                : _player.GetComponent<PlayerInputHandler>();

            if (_camera != null)
            {
                _cameraFollow = _camera.GetComponent<CameraFollowX>();
                if (_cameraFollow != null) _cameraFollow.enabled = false;
                LockCamera();
            }

            CharacterController controller = _player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            Vector3 start = _player.transform.position;
            start.x = _leftX;
            _player.transform.position = start;
            if (controller != null) controller.enabled = true;

            _direction = 1;
            _input?.SetDemoInput(Vector2.zero, false, _runSpeedMultiplier);
            CreateDemoCarryBox();
        }

        private void Update()
        {
            if (!_initialWeatherApplied && WorldWeatherManager.Instance != null)
            {
                WorldWeatherManager.Instance.SetWeather(WeatherType.Clear);
                if (WorldDayNightManager.Instance != null)
                {
                    WorldDayNightManager.Instance.SetTime(12, 0);
                    _lastVariation = new Variation(WeatherType.Clear, 10);
                    BuildVariationDeck(excludeInitialClearDay: true);
                    _initialWeatherApplied = true;
                }
            }

            if (_input == null) return;

            float introDuration = _cameraStartDelaySeconds + _cameraDescentSeconds;
            if (!_runStarted)
            {
                _input.SetDemoInput(Vector2.zero, false, _runSpeedMultiplier);
                if (_cameraElapsed < introDuration) return;

                _runStarted = true;
                _input.SetDemoInput(Vector2.right, true, _runSpeedMultiplier);
            }

            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                _input.SetDemoInput(Vector2.zero, false, _runSpeedMultiplier);
                if (_pauseTimer <= 0f)
                    _input.SetDemoInput(
                        new Vector2(_direction, 0f), true, _runSpeedMultiplier);
                return;
            }

            float x = _player.transform.position.x;
            bool reachedEnd = _direction > 0 ? x >= _rightX : x <= _leftX;
            if (!reachedEnd) return;

            _direction = -_direction;
            _pauseTimer = _turnPauseSeconds;
            _input.SetDemoInput(Vector2.zero, false, _runSpeedMultiplier);

            // 왼쪽 → 오른쪽 → 왼쪽의 완전한 왕복을 마친 순간에만 T/Y 연출을 실행한다.
            if (_direction > 0)
                ApplyNextVariation();
        }

        private void LateUpdate()
        {
            LockCamera();
        }

        private void LockCamera()
        {
            if (_camera == null) return;
            _cameraElapsed += Time.unscaledDeltaTime;
            float duration = Mathf.Max(0.01f, _cameraDescentSeconds);
            float descentElapsed = Mathf.Max(0f, _cameraElapsed - _cameraStartDelaySeconds);
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(descentElapsed / duration));
            Vector3 position = _cameraPosition;
            position.y = Mathf.Lerp(_cameraPosition.y, _cameraEndY, progress);
            _camera.transform.SetPositionAndRotation(
                position, Quaternion.Euler(_cameraEuler));
        }

        private void OnDisable()
        {
            SuppressTrafficAccidents = false;
            _input?.ClearDemoInput();
            if (_player != null && _player.Animation != null)
                _player.Animation.DemoCarrying = false;
            if (_demoBox != null) Destroy(_demoBox);
            if (_cameraFollow != null) _cameraFollow.enabled = true;
        }

        private void CreateDemoCarryBox()
        {
            if (_carryAnchor == null)
            {
                Transform[] children = _player.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child.name != "__gb_CarryAnchor") continue;
                    _carryAnchor = child;
                    break;
                }
            }
            if (_carryAnchor == null || _boxPropPrefab == null) return;

            _demoBox = new GameObject("DemoCarriedBox");
            _demoBox.transform.SetParent(_carryAnchor, false);
            _demoBox.transform.localPosition = new Vector3(0f, _boxLocalYOffset, 0f);
            GameObject visual = Instantiate(_boxPropPrefab, _demoBox.transform);
            visual.name = "BoxProp";

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                if (bounds.size.y > 0.001f)
                {
                    visual.transform.localScale *= 0.7f / bounds.size.y;
                    bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                        bounds.Encapsulate(renderers[i].bounds);
                    visual.transform.position += _carryAnchor.position -
                        new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                }
            }

            if (_player.Animation != null)
                _player.Animation.DemoCarrying = true;
        }

        private void ApplyNextVariation()
        {
            if (_variationIndex >= _variationDeck.Count)
                BuildVariationDeck(excludeInitialClearDay: false);
            if (_variationDeck.Count == 0) return;

            Variation variation = _variationDeck[_variationIndex++];
            _lastVariation = variation;
            WorldDayNightManager.Instance?.SetTime(variation.Hour, 0);
            WorldWeatherManager.Instance?.SetWeather(variation.Weather);
        }

        private void BuildVariationDeck(bool excludeInitialClearDay)
        {
            _variationDeck.Clear();
            foreach (WeatherType weather in CaptureWeathers)
            {
                foreach (int hour in CapturePhaseHours)
                {
                    if (excludeInitialClearDay &&
                        weather == WeatherType.Clear &&
                        hour == 10)
                        continue;
                    _variationDeck.Add(new Variation(weather, hour));
                }
            }

            for (int i = _variationDeck.Count - 1; i > 0; i--)
            {
                int swap = Random.Range(0, i + 1);
                Variation temp = _variationDeck[i];
                _variationDeck[i] = _variationDeck[swap];
                _variationDeck[swap] = temp;
            }

            if (_variationDeck.Count > 1 &&
                _variationDeck[0].Weather == _lastVariation.Weather &&
                _variationDeck[0].Hour == _lastVariation.Hour)
            {
                Variation temp = _variationDeck[0];
                _variationDeck[0] = _variationDeck[1];
                _variationDeck[1] = temp;
            }
            _variationIndex = 0;
        }
    }
}
