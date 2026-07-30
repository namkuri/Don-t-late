using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-116 ⑤ — 벚꽃잎 낙하 효과 (District 1, 가이드 §7). 수관 위 박스 영역에서 초당 18개
    /// 꽃잎이 태어나 회전·좌우 흔들림·저속 낙하하고, 생성·소멸 시 알파가 자연스럽게 오간다.
    /// ParticleSystem을 코드로 구성한다 — 텍스처(one_blossom)는 빌더가 주입.
    /// </summary>
    public class BlossomPetalEffect : MonoBehaviour
    {
        [SerializeField] private Texture2D _petalTexture;
        [SerializeField] private float _rate = 18f;
        [Tooltip("생성 박스 크기 — 실제 blossom_tree 수관 크기에 맞춘다.")]
        [SerializeField] private Vector3 _emitBox = new Vector3(3f, 0.5f, 3f);

        private void Start()
        {
            var ps = gameObject.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5.5f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.03f; // 천천히 낙하
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = _rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = _emitBox;

            // 좌우 흔들림 — X 사인 노이즈.
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.35f);
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.3f;

            // 회전 지속.
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

            // 생성·소멸 알파 인아웃.
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f),
                    new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            var renderer = GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface", 1f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = 3000;
            if (_petalTexture != null) material.mainTexture = _petalTexture;
            renderer.sharedMaterial = material;
        }
    }
}
