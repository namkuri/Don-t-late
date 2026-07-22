using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// UI 시머 스윕(View 연출) — S-027 ⑦. 사선 광 스트립이 좌→우로 흘러가는 반짝임.
    /// 자기 Image 알파 모양대로 클립(Mask 스텐실)하므로 광은 로고 픽셀 위로만 지나간다.
    /// 스트립 스프라이트·자식 오브젝트는 런타임 생성 — 씬에는 이 컴포넌트만 있으면 된다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIShine : MonoBehaviour
    {
        [SerializeField] private float _sweepDuration = 0.9f;
        [Tooltip("스윕과 스윕 사이 쉬는 시간(초).")]
        [SerializeField] private float _interval = 1.6f;
        [SerializeField] private float _stripWidth = 150f;
        [Tooltip("사선 기울기(도). 양수 = 위가 오른쪽으로 기운 사선.")]
        [SerializeField] private float _angleDegrees = 18f;
        [SerializeField] private float _maxAlpha = 0.8f;

        private RectTransform _strip;
        private float _timer;
        private float _travelHalf;

        private void Awake()
        {
            // 로고 알파 모양 클립 — Mask가 UI 알파 클립 스텐실을 켠다 (투명 픽셀엔 광 없음).
            Mask mask = GetComponent<Mask>();
            if (mask == null) mask = gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            RectTransform self = (RectTransform)transform;
            GameObject go = new GameObject("Shine", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            Image image = go.AddComponent<Image>();
            image.sprite = BuildGradientSprite();
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, _maxAlpha);

            _strip = (RectTransform)go.transform;
            float coverHeight = self.rect.height * 2f
                + self.rect.width * Mathf.Tan(_angleDegrees * Mathf.Deg2Rad);
            _strip.sizeDelta = new Vector2(_stripWidth, coverHeight);
            _strip.localRotation = Quaternion.Euler(0f, 0f, _angleDegrees);
            _travelHalf = self.rect.width * 0.5f + _stripWidth;
            _strip.anchoredPosition = new Vector2(-_travelHalf, 0f);
        }

        private void OnEnable() => _timer = 0f;

        private void Update()
        {
            if (_strip == null) return;
            _timer += Time.unscaledDeltaTime;
            float cycle = _sweepDuration + _interval;
            float t = Mathf.Repeat(_timer, cycle) / _sweepDuration;
            _strip.gameObject.SetActive(t <= 1f);
            if (t <= 1f)
                _strip.anchoredPosition = new Vector2(Mathf.Lerp(-_travelHalf, _travelHalf, t), 0f);
        }

        // 가로 64px 부드러운 알파 산(sin) 그라디언트 — 흰 광 스트립.
        private static Sprite BuildGradientSprite()
        {
            const int W = 64;
            Texture2D texture = new Texture2D(W, 4, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int x = 0; x < W; x++)
            {
                float alpha = Mathf.Sin(Mathf.PI * x / (W - 1f));
                for (int y = 0; y < 4; y++)
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, W, 4), new Vector2(0.5f, 0.5f));
        }
    }
}
