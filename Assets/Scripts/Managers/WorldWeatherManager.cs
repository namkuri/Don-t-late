using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DontLate
{
    /// <summary>
    /// 날씨 (S-042) — 하루 1회 추첨(맑음·흐림·비·눈·안개·폭염), 파티클(비·눈·아지랑이)·구름·
    /// 컬러 그레이드("LUT" — 날씨×시간대×구역 합성, 수 초 러프)를 소유한다. Core 상주.
    /// 안개 밀도는 WorldDayNightManager가 WeatherChanged를 구독해 협조(조명과 한 몸).
    /// 연출물은 전부 코드 조립 — 씬 재조립만으로 완성(그레이박스 원칙).
    /// </summary>
    public class WorldWeatherManager : MonoBehaviour
    {
        public static WorldWeatherManager Instance { get; private set; }

        [SerializeField] private GameStateSO _gameState;
        [Tooltip("그레이드 전이 속도 (1/초) — 낮을수록 느긋한 트랜지션.")]
        [SerializeField] private float _gradeLerpSpeed = 0.5f;

        public WeatherType Weather { get; private set; } = WeatherType.Clear;

        private int _lastRolledDay = -1;
        private ParticleSystem _rain;
        private ParticleSystem _snow;
        private GameObject _hazeRoot; // S-044 ③ — 일렁 셰이더 쿼드 (파티클 박스룩 폐지)
        private Transform _cloudRoot;
        private Renderer _snowCover;   // S-045 ⑤ — 지면 쌓임(알파 성장)
        private float _snowAmount;
        private SpriteRenderer[] _clouds;
        private DayPhase _phase = DayPhase.Morning;

        // 런타임 글로벌 볼륨 (그레이드 소유 — 씬 볼륨(블룸)과 별개, 우선순위 높음).
        private ColorAdjustments _colorAdjust;
        private WhiteBalance _whiteBalance;
        private Bloom _bloom; // S-043 — 밤/낮 강도
        private float _targetExposure, _targetSaturation, _targetTemperature, _targetBloom = 0.3f;
        private Color _targetFilter = Color.white;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            WorldEvents.ClockTicked += OnClockTicked;
            WorldEvents.DayPhaseChanged += OnDayPhaseChanged;
            WorldEvents.SceneTransitionCompleted += OnSceneChanged;
        }

        private void OnDisable()
        {
            WorldEvents.ClockTicked -= OnClockTicked;
            WorldEvents.DayPhaseChanged -= OnDayPhaseChanged;
            WorldEvents.SceneTransitionCompleted -= OnSceneChanged;
        }

        private void Start()
        {
            BuildEffects();
            BuildGradeVolume();
            Reroll();
        }

        private void Update()
        {
            // S-045 ③: Y키 = 날씨 순환 (검증·튜닝용 — 심사 전 제거 후보).
            if (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame)
                SetWeather((WeatherType)(((int)Weather + 1) % 6));

            DriftClouds();
            LerpGrade();
            UpdateSnowCover();
        }

        // S-044 ①: 실내 씬(Home)에선 강수·아지랑이를 창밖 원경(z+)으로 민다.
        private float _sceneZOffset;

        private void LateUpdate()
        {
            // 연출 리그는 카메라 X를 따라간다 (씬·구역 무관).
            Camera camera = Camera.main;
            if (camera == null) return;
            Vector3 cam = camera.transform.position;
            transform.position = new Vector3(cam.x, 0f, _sceneZOffset);
        }

        // ── 추첨 ─────────────────────────────────────────────
        private void OnClockTicked(GameClock clock)
        {
            if (clock.Day == _lastRolledDay) return;
            Reroll();
        }

        private static readonly (WeatherType type, int weight)[] Weights =
        {
            (WeatherType.Clear, 28), (WeatherType.Cloudy, 22), (WeatherType.Rain, 16),
            (WeatherType.Snow, 10), (WeatherType.Fog, 12), (WeatherType.Heat, 12),
        };

        private void Reroll()
        {
            _lastRolledDay = _gameState != null ? _gameState.day : 0;
            int total = 0;
            foreach (var w in Weights) total += w.weight;
            int roll = Random.Range(0, total);
            WeatherType picked = WeatherType.Clear;
            foreach (var w in Weights)
            {
                roll -= w.weight;
                if (roll < 0) { picked = w.type; break; }
            }
            SetWeather(picked);
        }

        /// <summary>디버그·튜닝용 강제 설정 (unity-cli exec 검증 포함).</summary>
        public void SetWeather(WeatherType weather)
        {
            Weather = weather;
            ApplyWeatherVisuals();
            RefreshGradeTarget();
            WorldEvents.RaiseWeatherChanged(weather);
        }

        // ── 파티클·구름 ──────────────────────────────────────
        private void ApplyWeatherVisuals()
        {
            Toggle(_rain, Weather == WeatherType.Rain);
            Toggle(_snow, Weather == WeatherType.Snow);
            if (_hazeRoot != null) _hazeRoot.SetActive(Weather == WeatherType.Heat);

            int cloudCount = Weather switch
            {
                WeatherType.Clear => 1,
                WeatherType.Heat => 0,
                WeatherType.Cloudy => 7,
                WeatherType.Rain => 8,
                WeatherType.Snow => 6,
                WeatherType.Fog => 4,
                _ => 3
            };
            Color cloudColor = Weather == WeatherType.Rain
                ? new Color(0.30f, 0.31f, 0.36f, 0.92f)   // 먹구름
                : new Color(0.92f, 0.93f, 0.96f, 0.82f);
            for (int i = 0; i < _clouds.Length; i++)
            {
                _clouds[i].gameObject.SetActive(i < cloudCount);
                _clouds[i].color = cloudColor * new Color(1f, 1f, 1f, 0.6f + 0.4f * ((i * 37) % 10) / 10f);
            }
        }

        private static void Toggle(ParticleSystem system, bool on)
        {
            if (system == null) return;
            if (on && !system.isPlaying) system.Play();
            else if (!on && system.isPlaying) system.Stop();
        }

        private void DriftClouds()
        {
            if (_cloudRoot == null) return;
            for (int i = 0; i < _clouds.Length; i++)
            {
                if (!_clouds[i].gameObject.activeSelf) continue;
                Transform cloud = _clouds[i].transform;
                float speed = 0.35f + 0.1f * (i % 3);
                Vector3 pos = cloud.localPosition + Vector3.right * (speed * Time.deltaTime);
                if (pos.x > 45f) pos.x = -45f; // 랩
                cloud.localPosition = pos;
            }
        }

        // ── 그레이드 ("LUT" — 날씨×시간대×구역) ─────────────
        private void OnDayPhaseChanged(DayPhase phase)
        {
            _phase = phase;
            RefreshGradeTarget();
        }

        private void OnSceneChanged(GameScene scene)
        {
            _sceneZOffset = scene == GameScene.Home ? 10f : 0f; // S-044 ① — 방 뒷벽(z3) 너머 창밖
            RefreshGradeTarget();
        }

        private void RefreshGradeTarget()
        {
            // 시간대 베이스 (조명은 DayNight 몫 — 여기는 필름 톤만 살짝).
            float exposure = 0f, saturation = 0f, temperature = 0f;
            Color filter = Color.white;
            switch (_phase)
            {
                case DayPhase.Morning: temperature = 4f; break;
                case DayPhase.Day: exposure = 0.05f; break;
                case DayPhase.Evening: temperature = 14f; saturation = 6f; break;
                case DayPhase.Night: temperature = -10f; saturation = -6f; exposure = -0.05f; break;
            }

            // 날씨 모디파이어.
            switch (Weather)
            {
                case WeatherType.Rain: exposure -= 0.28f; saturation -= 18f; temperature -= 10f; filter = new Color(0.88f, 0.92f, 1f); break;
                case WeatherType.Snow: exposure += 0.08f; saturation -= 12f; temperature -= 18f; break;
                case WeatherType.Fog: exposure -= 0.18f; saturation -= 14f; break;
                case WeatherType.Cloudy: exposure -= 0.12f; saturation -= 8f; break;
                case WeatherType.Heat: temperature += 22f; saturation += 6f; exposure += 0.06f; filter = new Color(1f, 0.97f, 0.90f); break;
            }

            // 구역 분위기.
            string district = _gameState != null ? _gameState.currentDistrict : null;
            if (district == DeliveryOrderSO.DISTRICT_VILLATOWN) temperature += 6f;                      // 웜그레이 골목
            else if (district == DeliveryOrderSO.DISTRICT_FOODALLEY) { saturation += 8f; filter *= new Color(1f, 0.96f, 0.99f); } // 네온끼
            else if (district == DeliveryOrderSO.DISTRICT_APARTMENT) saturation -= 4f;                  // 무채 단지

            // S-043: Bloom 밤/낮 강도 — 밤에 전광판 HDR이 크게 번지고 낮엔 절제.
            float bloom = _phase switch
            {
                DayPhase.Night => 0.85f,
                DayPhase.Evening => 0.6f,
                DayPhase.Morning => 0.3f,
                _ => 0.2f
            };
            if (Weather == WeatherType.Rain) bloom += 0.1f; // 젖은 밤거리 번짐

            _targetExposure = exposure;
            _targetSaturation = saturation;
            _targetTemperature = temperature;
            _targetFilter = filter;
            _targetBloom = bloom;
        }

        private void LerpGrade()
        {
            if (_colorAdjust == null) return;
            float t = Time.deltaTime * _gradeLerpSpeed;
            _colorAdjust.postExposure.value = Mathf.Lerp(_colorAdjust.postExposure.value, _targetExposure, t);
            _colorAdjust.saturation.value = Mathf.Lerp(_colorAdjust.saturation.value, _targetSaturation, t);
            _colorAdjust.colorFilter.value = Color.Lerp(_colorAdjust.colorFilter.value, _targetFilter, t);
            _whiteBalance.temperature.value = Mathf.Lerp(_whiteBalance.temperature.value, _targetTemperature, t);
            _bloom.intensity.value = Mathf.Lerp(_bloom.intensity.value, _targetBloom, t);
        }

        private void BuildGradeVolume()
        {
            GameObject go = new GameObject("WeatherGradeVolume");
            go.transform.SetParent(transform, false);
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 50f; // 씬 블룸 볼륨보다 위

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>(); // 런타임 전용 — 에셋 무오염
            _colorAdjust = profile.Add<ColorAdjustments>();
            _colorAdjust.postExposure.overrideState = true;
            _colorAdjust.saturation.overrideState = true;
            _colorAdjust.colorFilter.overrideState = true;
            _whiteBalance = profile.Add<WhiteBalance>();
            _whiteBalance.temperature.overrideState = true;
            _bloom = profile.Add<Bloom>(); // S-043 — 밤 간판 발광 증폭, 낮 절제
            _bloom.intensity.overrideState = true;
            _bloom.threshold.overrideState = true;
            _bloom.threshold.value = 0.9f;
            volume.profile = profile;
        }

        // ── 연출물 조립 (코드 — 그레이박스) ──────────────────
        private void BuildEffects()
        {
            _rain = BuildFallSystem("RainEmitter", new Color(0.62f, 0.72f, 0.92f, 0.55f),
                startSpeed: 26f, size: 0.05f, lengthScale: 6f, rate: 340f, gravity: 1.2f,
                tiltDegrees: 15f); // 아트 피드백 (2026-07-24) — 수직 낙하는 부자연, 15° 사선
            ConfigureRainSplash(_rain); // S-044 ② — 충돌 지점 물 튀김
            _snow = BuildFallSystem("SnowEmitter", new Color(0.98f, 0.98f, 1f, 0.9f),
                startSpeed: 1.6f, size: 0.09f, lengthScale: 1f, rate: 120f, gravity: 0.06f, noise: true,
                lifetime: 12f); // S-046 ① — 14u 상공에서 지면까지 (2.2s는 공중 소멸)
            ConfigureSnowPile(_snow);   // S-046 ③ — 낙하 지점 실누적 (반드시 _snow 생성 후)
            _hazeRoot = BuildHazeQuads();
            BuildSnowCover();
            BuildClouds();
        }

        private ParticleSystem BuildFallSystem(string name, Color color, float startSpeed,
            float size, float lengthScale, float rate, float gravity, bool noise = false, float tiltDegrees = 0f, float lifetime = 2.2f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 14f, 1.5f);
            // 낙하 방향 — 기울기(도)만큼 X로 사선 (스트레치 렌더가 속도 정렬이라 빗줄기도 같이 기운다).
            float tilt = tiltDegrees * Mathf.Deg2Rad;
            go.transform.localRotation = Quaternion.LookRotation(
                new Vector3(Mathf.Sin(tilt), -Mathf.Cos(tilt), 0f));

            ParticleSystem system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.startSpeed = startSpeed;
            main.startSize = size;
            main.startLifetime = lifetime;
            main.startColor = color;
            main.maxParticles = 3200; // S-046 ② 영역 확대분(눈 12s 체공 포함)
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(70f, 10f, 70f); // S-046 ② — 70×70 전역

            var emission = system.emission;
            emission.rateOverTime = rate;

            if (noise)
            {
                var noiseModule = system.noise;
                noiseModule.enabled = true;
                noiseModule.strength = 0.55f;
                noiseModule.frequency = 0.4f;
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = MakeParticleMaterial(color);
            if (lengthScale > 1.01f)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = lengthScale; // 빗줄기
            }
            system.Stop();
            return system;
        }

        // 눈 실누적 (S-046 ③) — 눈송이가 닿은 지점에 퇴적 입자가 남는다 (균일 커버는 보조 톤으로 강등).
        private void ConfigureSnowPile(ParticleSystem snow)
        {
            var collision = snow.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.bounce = 0f;
            collision.lifetimeLoss = 1f;
            collision.quality = ParticleSystemCollisionQuality.Medium;

            GameObject pileGo = new GameObject("SnowPile");
            pileGo.transform.SetParent(snow.transform, false);
            ParticleSystem pile = pileGo.AddComponent<ParticleSystem>();
            var main = pile.main;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
            main.startLifetime = 50f; // 퇴적 잔류 — 이후 서서히 소멸(녹음)
            main.startColor = new Color(0.96f, 0.97f, 1f, 0.85f);
            main.gravityModifier = 0f;
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = pile.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var fade = pile.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.85f, 0.8f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            pileGo.GetComponent<ParticleSystemRenderer>().material
                = MakeParticleMaterial(new Color(0.96f, 0.97f, 1f, 0.85f));
            pile.Stop();

            var subEmitters = snow.subEmitters;
            subEmitters.enabled = true;
            subEmitters.AddSubEmitter(pile, ParticleSystemSubEmitterType.Collision,
                ParticleSystemSubEmitterProperties.InheritNothing);
        }

        // 눈 쌓임 보조 톤 (S-045 ⑤ → S-046 ③ 강등) — 실누적 아래 옅은 바탕.
        private void BuildSnowCover()
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "SnowCover";
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(110f, 9f, 1f);
            _snowCover = quad.GetComponent<Renderer>();
            _snowCover.material = MakeParticleMaterial(new Color(0.96f, 0.97f, 1f, 0f));
            quad.SetActive(false);
        }

        private void UpdateSnowCover()
        {
            if (_snowCover == null) return;
            float target = Weather == WeatherType.Snow ? 0.30f : 0f; // S-046 ③ — 보조 톤으로 강등
            float rate = Weather == WeatherType.Snow ? 0.030f : 0.018f; // 쌓임은 느긋(~24s), 녹음은 더 느긋
            _snowAmount = Mathf.MoveTowards(_snowAmount, target, rate * Time.deltaTime);
            bool visible = _snowAmount > 0.01f;
            if (_snowCover.gameObject.activeSelf != visible) _snowCover.gameObject.SetActive(visible);
            if (visible && _snowCover.material.HasProperty("_BaseColor"))
            {
                Color color = _snowCover.material.GetColor("_BaseColor");
                color.a = _snowAmount;
                _snowCover.material.SetColor("_BaseColor", color);
            }
        }

        /// <summary>플레이어 발자국용 — 지금 눈이 쌓여 있는가 (PlayerEffects가 WeatherChanged와 함께 사용).</summary>
        public bool HasSnowCover => _snowAmount > 0.25f;

        // 비 스플래시 (S-044 ②) — 빗방울이 월드 콜라이더에 닿으면 소멸 + 자잘한 물방울 튐.
        private void ConfigureRainSplash(ParticleSystem rain)
        {
            var collision = rain.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.bounce = 0f;
            collision.lifetimeLoss = 1f; // 닿는 순간 소멸 → 스플래시로 교대
            collision.quality = ParticleSystemCollisionQuality.Medium;

            // 스플래시 서브 시스템 — 위로 톡 튀는 물방울 3~4개.
            GameObject splashGo = new GameObject("RainSplash");
            splashGo.transform.SetParent(rain.transform, false);
            ParticleSystem splash = splashGo.AddComponent<ParticleSystem>();
            var main = splash.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.2f); // S-046 ④ — 더 높이
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.03f); // S-045 ② — 절반
            main.startLifetime = 2f; // S-046 ④ — 남규님 지정
            main.startColor = new Color(0.75f, 0.84f, 1f, 0.75f);
            main.gravityModifier = 1.6f;
            main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = splash.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3, 4, 1, 0.01f) });

            var shape = splash.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.04f;

            var splashFade = splash.colorOverLifetime;
            splashFade.enabled = true;
            Gradient splashGradient = new Gradient();
            splashGradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.5f, 0.4f), new GradientAlphaKey(0f, 0.75f) });
            splashFade.color = splashGradient;

            splashGo.GetComponent<ParticleSystemRenderer>().material
                = MakeParticleMaterial(new Color(0.75f, 0.84f, 1f, 0.75f));
            splash.Stop();

            var subEmitters = rain.subEmitters;
            subEmitters.enabled = true;
            subEmitters.AddSubEmitter(splash, ParticleSystemSubEmitterType.Collision,
                ParticleSystemSubEmitterProperties.InheritNothing);
        }

        // 아지랑이 (S-044 ③) — HeatHaze 셰이더 쿼드 2겹: 정점 일렁임+상승 노이즈.
        private GameObject BuildHazeQuads()
        {
            GameObject root = new GameObject("HeatHaze");
            root.transform.SetParent(transform, false);

            Shader shader = Shader.Find("DontLate/HeatHaze");
            Material material = shader != null ? new Material(shader) : null;

            for (int i = 0; i < 2; i++)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "HazeQuad_" + i;
                Object.Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(root.transform, false);
                quad.transform.localPosition = new Vector3(0f, 1.15f + i * 0.4f, 0.6f + i * 1.2f);
                quad.transform.localScale = new Vector3(44f, 2.3f, 1f);
                if (material != null) quad.GetComponent<Renderer>().sharedMaterial = material;
            }
            root.SetActive(false);
            return root;
        }

        private void BuildClouds()
        {
            _cloudRoot = new GameObject("Clouds").transform;
            _cloudRoot.SetParent(transform, false);

            Sprite blob = MakeCloudSprite();
            _clouds = new SpriteRenderer[8];
            for (int i = 0; i < _clouds.Length; i++)
            {
                GameObject cloud = new GameObject("Cloud_" + i);
                cloud.transform.SetParent(_cloudRoot, false);
                cloud.transform.localPosition = new Vector3(-40f + i * 11f, 21f + (i * 13 % 7), 64f + (i % 3) * 3f);
                cloud.transform.localScale = new Vector3(9f + (i * 7 % 5), 3.2f + (i * 5 % 3), 1f);
                SpriteRenderer renderer = cloud.AddComponent<SpriteRenderer>();
                renderer.sprite = blob;
                cloud.SetActive(false);
                _clouds[i] = renderer;
            }
        }

        private static Material MakeParticleMaterial(Color tint)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.SetFloat("_Surface", 1f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            return material;
        }

        private static Sprite _cloudSpriteCache;

        private static Sprite MakeCloudSprite()
        {
            if (_cloudSpriteCache != null) return _cloudSpriteCache;
            const int W = 64, H = 32;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    // 타원 3개 겹친 소프트 블롭.
                    float a = BlobAlpha(x, y, 20f, 18f, 16f, 9f)
                            + BlobAlpha(x, y, 36f, 14f, 14f, 8f)
                            + BlobAlpha(x, y, 46f, 19f, 12f, 7f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            tex.Apply();
            _cloudSpriteCache = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 16f);
            return _cloudSpriteCache;
        }

        private static float BlobAlpha(int x, int y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx;
            float dy = (y - cy) / ry;
            float d = dx * dx + dy * dy;
            return Mathf.Clamp01(1.15f - d);
        }
    }
}
