using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-116 ⑤ — 지평선 포그 밴드 (District 1, 가이드 §6). 카메라 자식 쿼드에 상하 그라데이션
    /// 텍스처를 코드 생성해 씌우고, 원경의 검은 경계를 얇게 가린다.
    /// 낮 = 청회색 α0.30 / 밤 = 어두운 남색 α0.38 — 시간대(Phase)에 따라 블렌드.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class HorizonFogBand : MonoBehaviour
    {
        [SerializeField] private Color _dayColor = new Color(0.55f, 0.62f, 0.70f, 0.30f);
        [SerializeField] private Color _nightColor = new Color(0.10f, 0.12f, 0.24f, 0.38f);
        [SerializeField] private float _blendSeconds = 1.5f;

        private Material _material;
        private Color _current;

        private void Awake()
        {
            // 위·아래로 투명해지는 세로 그라데이션 — 중앙만 진한 얇은 띠.
            var texture = new Texture2D(1, 64, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 64; y++)
            {
                float t = y / 63f;
                float band = 1f - Mathf.Abs(t - 0.5f) * 2f;    // 중앙 1 → 가장자리 0
                texture.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, band)));
            }
            texture.Apply();

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            _material = new Material(shader);
            _material.SetFloat("_Surface", 1f); // Transparent
            _material.SetFloat("_Blend", 0f);
            _material.renderQueue = 3000;
            _material.SetOverrideTag("RenderType", "Transparent");
            _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _material.SetInt("_ZWrite", 0);
            _material.mainTexture = texture;
            GetComponent<MeshRenderer>().sharedMaterial = _material;
            _current = _dayColor;
        }

        private void Update()
        {
            Color target = _dayColor;
            if (WorldDayNightManager.Instance != null)
            {
                DayPhase phase = WorldDayNightManager.Instance.Phase;
                target = phase == DayPhase.Night || phase == DayPhase.Evening ? _nightColor : _dayColor;
            }
            _current = Color.Lerp(_current, target, Time.deltaTime / Mathf.Max(0.01f, _blendSeconds));
            _material.SetColor("_BaseColor", _current);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
