using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-144 — 타이틀 진입 연출. 카메라가 **하늘에서 수직으로** 제자리까지 천천히 내려앉는다.
    ///
    /// 착지점은 씬에 저장된 카메라 위치 그 자체다 — 인스펙터에 목표를 따로 적지 않는다.
    /// 빌더가 카메라 위치를 바꾸면 착지점도 따라 바뀌므로 두 값이 어긋날 일이 없다.
    ///
    /// 수평 성분은 건드리지 않는다(요구: "수직으로"). X·Z는 착지점 값을 그대로 유지하고
    /// Y만 위에서 아래로 움직인다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class TitleCameraDrop : MonoBehaviour
    {
        [Tooltip("착지점 기준 시작 고도(u). 이 높이만큼 위에서 출발한다.")]
        [SerializeField] private float _dropHeight = 42f;

        [Tooltip("하강을 시작하기 전 시작 고도에서 머무는 시간(초).")]
        [SerializeField] private float _startDelay = 2f;

        [Tooltip("내려앉는 데 걸리는 시간(초).")]
        [SerializeField] private float _duration = 4.5f;

        [Tooltip("착지 후 다음 연출까지의 여유(초) — 지금은 로그용 표식.")]
        [SerializeField] private float _settlePause = 0.4f;

        [Header("인트로 하늘 구름")]
        [SerializeField] private Texture2D[] _introCloudTextures;
        [SerializeField] private float _cloudDriftX = 1.2f;
        [SerializeField] private float _cloudBobY = 0.15f;

        private Vector3 _landing;
        private float _elapsed;
        private bool _done;
        private SpriteRenderer[] _introClouds;
        private Sprite[] _runtimeCloudSprites;
        private Vector3[] _cloudHomes;
        private float[] _cloudBaseAlpha;

        private void Awake()
        {
            // 착지점 = 씬에 저장된 위치. Awake에서 잡아둔다(다른 컴포넌트가 옮기기 전에).
            _landing = transform.position;
            transform.position = _landing + Vector3.up * _dropHeight;

            BuildIntroClouds();
            if (_introClouds == null || _introClouds.Length == 0) return;
            _cloudHomes = new Vector3[_introClouds.Length];
            _cloudBaseAlpha = new float[_introClouds.Length];
            for (int i = 0; i < _introClouds.Length; i++)
            {
                if (_introClouds[i] == null) continue;
                _cloudHomes[i] = _introClouds[i].transform.localPosition;
                _cloudBaseAlpha[i] = _introClouds[i].color.a;
            }
        }

        private void OnDestroy()
        {
            if (_runtimeCloudSprites == null) return;
            for (int i = 0; i < _runtimeCloudSprites.Length; i++)
                if (_runtimeCloudSprites[i] != null) Destroy(_runtimeCloudSprites[i]);
        }

        private void BuildIntroClouds()
        {
            if (_introCloudTextures == null || _introCloudTextures.Length == 0) return;
            Camera camera = GetComponent<Camera>();
            if (camera == null) return;

            GameObject root = new GameObject("IntroClouds");
            root.transform.SetParent(transform, false);

            _introClouds = new SpriteRenderer[_introCloudTextures.Length];
            _runtimeCloudSprites = new Sprite[_introCloudTextures.Length];
            for (int i = 0; i < _introCloudTextures.Length; i++)
            {
                Texture2D texture = _introCloudTextures[i];
                if (texture == null) continue;

                Sprite sprite = Sprite.Create(texture,
                    new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                _runtimeCloudSprites[i] = sprite;

                bool first = i == 0;
                _introClouds[i] = CreateIntroCloud(root.transform, camera, sprite,
                    first ? "IntroCloud_A" : "IntroCloud_B",
                    first ? new Vector2(0.24f, 0.76f) : new Vector2(0.76f, 0.82f),
                    first ? 0.78f : 0.88f,
                    first ? 42f : 48f,
                    first ? 0.92f : 0.86f);
            }
        }

        private static SpriteRenderer CreateIntroCloud(Transform parent, Camera camera, Sprite sprite,
            string name, Vector2 viewportPosition, float viewportWidth, float depth, float alpha)
        {
            GameObject cloud = new GameObject(name);
            cloud.transform.SetParent(parent, false);

            float halfHeight = camera.orthographic
                ? camera.orthographicSize
                : depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfWidth = halfHeight * camera.aspect;
            cloud.transform.localPosition = new Vector3(
                (viewportPosition.x - 0.5f) * halfWidth * 2f,
                (viewportPosition.y - 0.5f) * halfHeight * 2f,
                depth);

            float spriteWidth = Mathf.Max(0.01f, sprite.bounds.size.x);
            float scale = halfWidth * 2f * viewportWidth / spriteWidth;
            cloud.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer renderer = cloud.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 1f, 1f, alpha);
            renderer.sortingOrder = -900;
            return renderer;
        }

        private void Update()
        {
            if (_done) return;

            _elapsed += Time.deltaTime;
            float dropElapsed = Mathf.Max(0f, _elapsed - _startDelay);
            float t = _duration > 0.01f ? Mathf.Clamp01(dropElapsed / _duration) : 1f;
            UpdateIntroClouds(t);
            if (_elapsed <= _startDelay) return;

            // 감속 착지 — 처음엔 빠르게 떨어지고 끝에서 부드럽게 멈춘다.
            // 등속으로 두면 착지 순간이 뚝 끊겨 "떨어졌다"가 아니라 "순간이동"으로 읽힌다.
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            Vector3 position = transform.position;
            position.y = Mathf.Lerp(_landing.y + _dropHeight, _landing.y, eased);
            transform.position = position;

            if (t < 1f) return;

            transform.position = _landing; // 부동소수 오차 제거 — 정확히 착지점에 앉힌다.
            _done = true;
        }

        private void UpdateIntroClouds(float dropProgress)
        {
            if (_introClouds == null || _cloudHomes == null) return;

            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.72f, dropProgress));
            for (int i = 0; i < _introClouds.Length; i++)
            {
                SpriteRenderer cloud = _introClouds[i];
                if (cloud == null) continue;

                float phase = i * 1.7f;
                float speed = 0.28f + i * 0.09f;
                cloud.transform.localPosition = _cloudHomes[i] + new Vector3(
                    Mathf.Sin(_elapsed * speed + phase) * _cloudDriftX,
                    Mathf.Sin(_elapsed * speed * 1.6f + phase) * _cloudBobY,
                    0f);

                Color color = cloud.color;
                color.a = _cloudBaseAlpha[i] * fade;
                cloud.color = color;
                if (dropProgress >= 1f) cloud.gameObject.SetActive(false);
            }
        }

        /// <summary>강하가 끝났는지. 다른 연출이 착지 뒤에 붙고 싶을 때 본다.</summary>
        public bool Landed => _done;

        /// <summary>착지 후 여유 시간(초).</summary>
        public float SettlePause => _settlePause;
    }
}
