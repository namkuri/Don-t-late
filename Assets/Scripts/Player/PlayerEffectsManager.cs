using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 로컬 이펙트 — 이동 먼지·드링크 회복 버스트 (S-023 · JUICE "계단 오르기/에너지드링크" 행의 그레이박스 수준).
    /// 파티클은 코드로 조립한다(프리팹 없음 — 씬 재조립만으로 완성). 허브 경유로만 형제를 본다.
    /// </summary>
    public class PlayerEffectsManager : MonoBehaviour
    {
        [Header("이동 먼지")]
        [SerializeField] private float _dustRate = 8f;
        [SerializeField] private float _dustSize = 0.12f;
        [SerializeField] private float _dustLifetime = 0.45f;
        [SerializeField] private Color _dustColor = new Color(0.75f, 0.72f, 0.68f, 0.6f);

        [Header("드링크 버스트")]
        [SerializeField] private int _drinkBurstCount = 32; // S-031 ⑨ — 18로는 힐이 안 읽힘, 강화
        [SerializeField] private Color _drinkColor = new Color(0.28f, 0.9f, 0.55f, 0.9f);

        private PlayerManager _hub;
        private ParticleSystem _dust;
        private ParticleSystem _drinkBurst;

        private void Awake()
        {
            _hub = GetComponent<PlayerManager>();
            _dust = BuildDust();
            _drinkBurst = BuildDrinkBurst();
        }

        private void Update()
        {
            bool moving = _hub.Locomotion.PlanarVelocity.sqrMagnitude > 0.01f
                       && _hub.Locomotion.IsGrounded;
            ParticleSystem.EmissionModule emission = _dust.emission;
            emission.rateOverTime = moving ? _dustRate : 0f;
        }

        /// <summary>드링크 음용 순간 1회 버스트. PlayerStatusManager가 허브 경유로 호출한다.</summary>
        public void PlayDrinkEffect() => _drinkBurst.Emit(_drinkBurstCount);

        // ── 눈 발자국 (S-045 ⑤) — 눈이 쌓인 동안 이동 궤적에 밟은 자국 ──
        private const float FOOTPRINT_STRIDE = 0.55f;
        private const float FOOTPRINT_LIFETIME = 30f;
        private const float FOOTPRINT_ALPHA = 0.55f;       // 눌린 눈 그늘 기준 알파
        private const float FOOTPRINT_MELT_SECONDS = 1.2f; // S-122 ⑮ — 날씨 전환 시 급속 소멸

        private bool _snowing;
        private Vector3 _lastFootprintPos;
        private bool _leftFoot;
        private Material _footprintMaterial;
        private readonly System.Collections.Generic.List<GameObject> _footprints
            = new System.Collections.Generic.List<GameObject>();
        private Coroutine _footprintMelt;

        private void OnEnable() => WorldEvents.WeatherChanged += OnWeatherChanged;
        private void OnDisable() => WorldEvents.WeatherChanged -= OnWeatherChanged;

        private void OnWeatherChanged(WeatherType weather)
        {
            bool wasSnowing = _snowing;
            _snowing = weather == WeatherType.Snow;
            // S-122 ⑮ — 눈 → 다른 날씨: 남은 자국을 1.2초에 걷는다
            // (눈은 다 녹았는데 자국만 최대 30초 남던 창을 막는다).
            if (wasSnowing && !_snowing && _footprintMelt == null && _footprints.Count > 0)
                _footprintMelt = StartCoroutine(MeltFootprints());
        }

        // 전 자국이 머티리얼 1개를 공유한다 — 알파를 한 번만 낮추면 전부 같이 옅어진다(비용 1).
        private System.Collections.IEnumerator MeltFootprints()
        {
            float elapsed = 0f;
            while (elapsed < FOOTPRINT_MELT_SECONDS)
            {
                elapsed += Time.deltaTime;
                SetFootprintAlpha(Mathf.Lerp(FOOTPRINT_ALPHA, 0f, elapsed / FOOTPRINT_MELT_SECONDS));
                yield return null;
            }
            foreach (GameObject print in _footprints) if (print != null) Destroy(print);
            _footprints.Clear();
            SetFootprintAlpha(FOOTPRINT_ALPHA); // 다음 눈에 재사용 — 기준 알파 복구
            _footprintMelt = null;
        }

        private void SetFootprintAlpha(float alpha)
        {
            if (_footprintMaterial == null) return;
            _footprintMaterial.SetColor("_BaseColor", new Color(0.52f, 0.58f, 0.72f, alpha));
        }

        private void LateUpdate()
        {
            if (!_snowing || !_hub.Locomotion.IsGrounded) return;
            // 쌓임이 어느 정도 자란 뒤부터 자국이 남는다 (World 상태 읽기 — Instance 조회만).
            if (WorldWeatherManager.Instance == null || !WorldWeatherManager.Instance.HasSnowCover) return;
            if ((transform.position - _lastFootprintPos).sqrMagnitude < FOOTPRINT_STRIDE * FOOTPRINT_STRIDE) return;

            _lastFootprintPos = transform.position;
            _leftFoot = !_leftFoot;
            SpawnFootprint();
        }

        private void SpawnFootprint()
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Footprint";
            Destroy(quad.GetComponent<Collider>());
            float yaw = transform.eulerAngles.y;
            Vector3 side = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * (_leftFoot ? 0.12f : -0.12f);
            quad.transform.SetPositionAndRotation(
                transform.position + side + Vector3.up * 0.045f, // 눈막(0.03) 위
                Quaternion.Euler(90f, yaw + 90f, 0f));
            quad.transform.localScale = new Vector3(0.16f, 0.30f, 1f);

            if (_footprintMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                _footprintMaterial = new Material(shader);
                _footprintMaterial.SetFloat("_Surface", 1f);
                _footprintMaterial.SetOverrideTag("RenderType", "Transparent");
                _footprintMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                _footprintMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _footprintMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _footprintMaterial.SetInt("_ZWrite", 0);
                _footprintMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                SetFootprintAlpha(FOOTPRINT_ALPHA); // 눌린 눈 그늘
            }
            quad.GetComponent<Renderer>().sharedMaterial = _footprintMaterial;
            _footprints.RemoveAll(print => print == null); // 30초 만료분 정리
            _footprints.Add(quad);
            Destroy(quad, FOOTPRINT_LIFETIME);
        }

        // ── 파티클 조립 (코드 — 그레이박스) ──────────────────

        private ParticleSystem BuildDust()
        {
            ParticleSystem system = CreateSystem("DustEmitter", new Vector3(0f, 0.08f, 0f), _dustColor);

            ParticleSystem.MainModule main = system.main;
            main.startLifetime = _dustLifetime;
            main.startSpeed = 0.4f;
            main.startSize = _dustSize;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.15f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f; // Update가 이동 여부로 켠다
            system.Play();
            return system;
        }

        private ParticleSystem BuildDrinkBurst()
        {
            ParticleSystem system = CreateSystem("DrinkBurst", new Vector3(0f, 1.4f, 0f), _drinkColor);

            ParticleSystem.MainModule main = system.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 1.2f;
            main.startSize = 0.1f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.05f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f; // Emit 전용
            system.Play();
            return system;
        }

        private ParticleSystem CreateSystem(string name, Vector3 localPosition, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;

            ParticleSystem system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = color;
            main.gravityModifier = 0f;
            main.playOnAwake = false;
            main.loop = true;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default"); // 폴백
            renderer.material = new Material(shader);
            return system;
        }
    }
}
