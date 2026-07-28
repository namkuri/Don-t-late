using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// UI 콘페티 분출 (S-086) — 개척 해금·트럭 지급 정산 라인에서 터진다.
    /// 자기 완결: 오버레이 캔버스를 스스로 만들고 색조각을 코드로 스폰·애니메이트한다.
    /// 정산 중 timeScale=0에서도 흐르도록 전부 unscaled, 최상위 sortingOrder라 정산 패널 위에 보인다.
    /// 씬·프리팹 배선이 필요 없다(SettlementView가 Create로 띄운다) — 팩토리 경계 준수.
    /// </summary>
    public class ConfettiBurst : MonoBehaviour
    {
        private const int PIECE_COUNT = 50;
        private const float GRAVITY = 2600f;      // px/s^2 — 낙하 가속
        private const float LIFETIME = 1.8f;      // s — 조각 수명
        private const float FADE_START = 1.0f;    // s — 이후 알파 감쇠 시작
        private const int SORTING_ORDER = 32000;  // 정산 패널 위

        // 축제 팔레트 — 상호작용 민트(#35e0c8) 중심 + 앰버·코랄·하늘·노랑·흰색.
        private static readonly Color[] PALETTE =
        {
            new Color(0.208f, 0.878f, 0.784f),
            new Color(1f, 0.624f, 0.271f),
            new Color(1f, 0.451f, 0.349f),
            new Color(0.549f, 0.804f, 1f),
            new Color(1f, 0.918f, 0.400f),
            Color.white,
        };

        private RectTransform _root;

        /// <summary>오버레이 캔버스를 띄운 콘페티 인스턴스를 만든다. 수명은 현재 씬을 따라간다.</summary>
        public static ConfettiBurst Create()
        {
            var go = new GameObject("ConfettiBurst");
            var burst = go.AddComponent<ConfettiBurst>();
            burst.InitCanvas();
            return burst;
        }

        private void InitCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SORTING_ORDER;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 타겟 해상도 고정(D-003)
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            // GraphicRaycaster 불요 — 콘페티는 입력을 받지 않는다(렌더에는 캔버스만 필요).
            _root = (RectTransform)transform;
        }

        /// <summary>화면 중앙 약간 위에서 색조각 한 다발을 분출한다.</summary>
        public void Burst()
        {
            if (_root == null) return;
            for (int i = 0; i < PIECE_COUNT; i++) SpawnPiece();
        }

        private void SpawnPiece()
        {
            var go = new GameObject("piece", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_root, false);

            float w = Random.Range(10f, 20f);
            rt.sizeDelta = new Vector2(w, w * Random.Range(0.5f, 1f)); // 직사각 조각
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.55f);    // 화면 중앙 약간 위
            rt.anchoredPosition = new Vector2(Random.Range(-40f, 40f), Random.Range(-20f, 20f));

            var img = go.GetComponent<Image>();
            img.color = PALETTE[Random.Range(0, PALETTE.Length)];
            img.raycastTarget = false; // 확인 버튼 등 아래 UI 입력을 가리지 않게

            // 위로 편향된 방사 분출 — 콘 형태로 터진다.
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float speed = Random.Range(500f, 1100f);
            var vel = new Vector2(Mathf.Cos(ang) * speed * 0.7f, Mathf.Abs(Mathf.Sin(ang)) * speed + 300f);

            StartCoroutine(Animate(rt, img, vel, Random.Range(-360f, 360f)));
        }

        private IEnumerator Animate(RectTransform rt, Image img, Vector2 vel, float spinDegPerSec)
        {
            float t = 0f;
            Color baseColor = img.color;
            while (t < LIFETIME && rt != null)
            {
                float dt = Time.unscaledDeltaTime; // 정산 중 timeScale=0 — unscaled로 흐른다
                t += dt;
                vel.y -= GRAVITY * dt;
                rt.anchoredPosition += vel * dt;
                rt.Rotate(0f, 0f, spinDegPerSec * dt);
                if (t > FADE_START)
                {
                    float k = 1f - (t - FADE_START) / (LIFETIME - FADE_START);
                    img.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(k));
                }
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }
    }
}
