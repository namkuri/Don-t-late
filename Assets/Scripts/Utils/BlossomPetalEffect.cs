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
        [SerializeField] private Mesh _petalMesh;
        [SerializeField] private float _rate = 18f;
        [Tooltip("생성 박스 크기 — 실제 blossom_tree 수관 크기에 맞춘다.")]
        [SerializeField] private Vector3 _emitBox = new Vector3(3f, 0.5f, 3f);
        [SerializeField] private float _windSpeed = 1.2f;
        [SerializeField] private float _fallSpeed = 0.15f;
        [SerializeField] private bool _showWindStreaks;
        [SerializeField] private float _petalSizeMultiplier = 1f;
        [SerializeField] private bool _singlePetal;
        [SerializeField] private float _petalFadeInSeconds = 0.5f;
        [SerializeField] private float _petalFadeOutAt = 4f;
        [SerializeField] private float _petalFadeSeconds = 1f;

        private GameObject _singleVisual;
        private Material _singleMaterial;
        private float _singleElapsed;
        private bool _singlePending;
        private bool _singleFinished;
        private float _lastObservedTime;
        private Vector3 _singleBaseScale;

        private void OnEnable()
        {
            _singleFinished = false;
            _lastObservedTime = 0f;
            if (Application.isPlaying)
                EnsureBuilt();
        }

        private void Start()
        {
            EnsureBuilt();
        }

        private void Update()
        {
            if (_singlePetal)
            {
                if (Time.unscaledTime < _lastObservedTime)
                    _singleFinished = false;
                _lastObservedTime = Time.unscaledTime;

                if (_singleVisual == null && !_singlePending && !_singleFinished)
                    EnsureBuilt();
                TickSinglePetal();
                return;
            }

            // Enter Play Mode의 Scene Reload가 꺼져 있으면 이전 플레이에서 런타임으로 만든
            // ParticleSystem만 사라지고 Start가 다시 호출되지 않을 수 있다.
            if (Application.isPlaying && GetComponent<ParticleSystem>() == null)
                EnsureBuilt();
        }

        private void EnsureBuilt()
        {
            if (_singlePetal)
            {
                if (_singleVisual == null && !_singlePending && !_singleFinished)
                {
                    _singlePending = true;
                    StartCoroutine(CreateSingleAfterCameraPlacement());
                }
                return;
            }

            if (GetComponent<ParticleSystem>() != null) return;

            var ps = gameObject.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = !_singlePetal;
            // 나무 모델의 정규화 스케일(예: 0.06)이 꽃잎 크기까지 줄이지 않게 한다.
            // Shape 모드는 방출 영역만 계층 스케일을 따르고, 입자 크기는 월드 단위를 유지한다.
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.prewarm = !_singlePetal;
            float lifetime = Mathf.Max(0.1f, _petalFadeOutAt + _petalFadeSeconds);
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime);
            main.startSpeed = 0f;
            float meshScale = _petalMesh != null
                ? 1f / Mathf.Max(0.001f, _petalMesh.bounds.size.magnitude)
                : 1f;
            main.startSize = new ParticleSystem.MinMaxCurve(
                0.12f * _petalSizeMultiplier * meshScale,
                0.22f * _petalSizeMultiplier * meshScale);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.03f; // 천천히 낙하
            main.maxParticles = _singlePetal ? 1 : 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            if (_singlePetal)
            {
                emission.rateOverTime = 0f;
                emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
            }
            else
            {
                emission.rateOverTime = _rate;
            }

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = _emitBox;

            // 좌우 흔들림 — X 사인 노이즈.
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.35f);
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.3f;

            // 일정한 바람에 실리면서 노이즈로 살랑이는 궤적.
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(_windSpeed * 0.65f, _windSpeed * 1.35f);
            velocity.y = new ParticleSystem.MinMaxCurve(-_fallSpeed * 1.15f, -_fallSpeed * 0.85f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            // 회전 지속.
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
            rotation.y = new ParticleSystem.MinMaxCurve(-1.8f, 1.8f);
            rotation.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

            // 생성·소멸 알파 인아웃.
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            float fadeInEnd = Mathf.Clamp01(_petalFadeInSeconds / lifetime);
            float fadeStart = Mathf.Clamp01(_petalFadeOutAt / lifetime);
            GradientAlphaKey[] alphaKeys = _petalFadeInSeconds > 0.001f
                ? new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, fadeInEnd),
                    new GradientAlphaKey(1f, fadeStart),
                    new GradientAlphaKey(0f, 1f),
                }
                : new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, fadeStart),
                    new GradientAlphaKey(0f, 1f),
                };
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                alphaKeys);
            colorOverLifetime.color = gradient;

            var renderer = GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find(_petalMesh != null
                ? "Universal Render Pipeline/Particles/Lit"
                : "Universal Render Pipeline/Particles/Unlit");
            var material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface", 1f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            material.renderQueue = 3000;
            if (_petalTexture != null)
            {
                material.mainTexture = _petalTexture;
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", _petalTexture);
            }
            renderer.sharedMaterial = material;
            if (_petalMesh != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.mesh = _petalMesh;
            }

            if (_showWindStreaks)
                BuildWindStreaks();

            // AddComponent 직후 ParticleSystem이 먼저 재생되면, 나중에 설정한 0초 Burst는
            // 이미 지난 시점이 되어 첫 꽃잎이 누락된다. 모든 모듈 설정 후 명시적으로 재시작한다.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        private System.Collections.IEnumerator CreateSingleAfterCameraPlacement()
        {
            // DistrictCaptureDemo가 첫 프레임 LateUpdate에서 카메라를 인트로 시작점으로 옮긴 뒤 방출한다.
            yield return new WaitForEndOfFrame();
            _singlePending = false;
            if (!isActiveAndEnabled || _petalMesh == null) yield break;

            _singleVisual = new GameObject("IntroSinglePetal");
            _singleVisual.transform.SetParent(transform.parent, false);
            _singleVisual.transform.localPosition = transform.localPosition;
            _singleVisual.transform.localRotation = Random.rotation;

            MeshFilter filter = _singleVisual.AddComponent<MeshFilter>();
            filter.sharedMesh = _petalMesh;
            MeshRenderer renderer = _singleVisual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateSingleMaterial();
            renderer.sortingOrder = 50;

            float meshSize = Mathf.Max(0.001f, _petalMesh.bounds.size.magnitude);
            float targetSize = 0.17f * _petalSizeMultiplier;
            _singleBaseScale = Vector3.one * (targetSize / meshSize);
            _singleVisual.transform.localScale = _singleBaseScale;
            _singleElapsed = 0f;
        }

        private Material CreateSingleMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            _singleMaterial = new Material(shader);
            _singleMaterial.SetFloat("_Surface", 1f);
            _singleMaterial.SetOverrideTag("RenderType", "Transparent");
            _singleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _singleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _singleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _singleMaterial.SetInt("_ZWrite", 0);
            _singleMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _singleMaterial.renderQueue = 3000;
            if (_petalTexture != null)
            {
                _singleMaterial.mainTexture = _petalTexture;
                if (_singleMaterial.HasProperty("_BaseMap"))
                    _singleMaterial.SetTexture("_BaseMap", _petalTexture);
            }
            return _singleMaterial;
        }

        private void TickSinglePetal()
        {
            if (_singleVisual == null) return;

            float dt = Time.unscaledDeltaTime;
            _singleElapsed += dt;
            Vector3 position = _singleVisual.transform.localPosition;
            position.x += (_windSpeed + Mathf.Sin(_singleElapsed * 3.1f) * 0.35f) * dt;
            position.y -= _fallSpeed * dt;
            // 카메라 쪽으로 조금 다가오며 원근 크기가 변하고, 좌우 깊이 흔들림도 섞는다.
            position.z -= (0.75f + Mathf.Sin(_singleElapsed * 2.2f) * 0.22f) * dt;
            _singleVisual.transform.localPosition = position;
            _singleVisual.transform.Rotate(
                new Vector3(
                    95f + Mathf.Sin(_singleElapsed * 4.3f) * 45f,
                    130f + Mathf.Cos(_singleElapsed * 3.7f) * 55f,
                    165f) * dt, Space.Self);
            float flutterScale = 1f + Mathf.Sin(_singleElapsed * 7.5f) * 0.08f;
            _singleVisual.transform.localScale = _singleBaseScale * flutterScale;

            float alpha = 1f - Mathf.Clamp01(
                (_singleElapsed - _petalFadeOutAt) / Mathf.Max(0.01f, _petalFadeSeconds));
            if (_singleMaterial != null)
            {
                Color color = Color.white;
                color.a = alpha;
                if (_singleMaterial.HasProperty("_BaseColor"))
                    _singleMaterial.SetColor("_BaseColor", color);
                if (_singleMaterial.HasProperty("_Color"))
                    _singleMaterial.SetColor("_Color", color);
            }

            if (_singleElapsed >= _petalFadeOutAt + _petalFadeSeconds)
            {
                Destroy(_singleVisual);
                _singleVisual = null;
                _singleFinished = true;
                if (_singleMaterial != null) Destroy(_singleMaterial);
                _singleMaterial = null;
            }
        }

        private void BuildWindStreaks()
        {
            GameObject go = new GameObject("PetalWindStreaks");
            go.transform.SetParent(transform, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.055f);
            main.startColor = new Color(1f, 1f, 1f, 0.16f);
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(_emitBox.x, Mathf.Max(4f, _emitBox.y), _emitBox.z);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(_windSpeed * 4f, _windSpeed * 6f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.35f;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.16f, 0.2f),
                    new GradientAlphaKey(0f, 1f),
                });
            color.color = gradient;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 3.5f;
            renderer.velocityScale = 0.3f;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Material material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface", 1f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = 3000;
            renderer.sharedMaterial = material;
        }
    }
}
