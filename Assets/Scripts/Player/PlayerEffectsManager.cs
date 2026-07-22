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
