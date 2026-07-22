using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// 취급주의 상자 내구도 (S-019 ①). 낙하·투척 충돌 속도가 안전치를 넘으면 HP가 닳고,
    /// 0이 되면 파편 이펙트와 함께 터진다(주문은 cargo에 남아 구역 재진입 시 재스폰 — 파손 페널티는 시간).
    /// HP바는 풀해상 오버레이 캔버스(S-030 ① — 월드 쿼드는 480×270 픽셀화에 뭉개짐. S-021 주소 라벨과 동일 처방).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoxDurability : MonoBehaviour
    {
        [SerializeField] private TuningConfigSO _tuning;

        /// <summary>런타임 스폰 초기화 (스포너 — 에디터 API 없이 튜닝 주입).</summary>
        public void Initialize(TuningConfigSO tuning) => _tuning = tuning;

        private float _hp;
        private GameObject _barCanvas;
        private RectTransform _barRoot;
        private Image _barFill;
        private const float BAR_W = 96f;
        private const float FILL_W = 88f;

        private void Start()
        {
            _hp = _tuning != null ? _tuning.boxMaxHp : 100f;
            BuildBar();
        }

        // 상자 머리 위 월드점을 스크린 좌표로 투영해 오버레이 바를 따라붙인다 (S-030 ①).
        private void LateUpdate()
        {
            if (_barCanvas == null || !_barCanvas.activeSelf) return;
            Camera camera = Camera.main;
            if (camera == null) return;
            Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 0.95f);
            if (screen.z < 0f) { _barRoot.gameObject.SetActive(false); return; }
            _barRoot.gameObject.SetActive(true);
            _barRoot.position = new Vector3(screen.x, screen.y, 0f);
        }

        private void OnDestroy()
        {
            if (_barCanvas != null) Destroy(_barCanvas);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_tuning == null) return;
            float speed = collision.relativeVelocity.magnitude;
            if (speed <= _tuning.boxSafeImpactSpeed) return;

            _hp -= (speed - _tuning.boxSafeImpactSpeed) * _tuning.boxDamagePerSpeed;
            Debug.Log("[취급주의] 충격 " + speed.ToString("0.0") + "m/s → HP " + Mathf.Max(0f, _hp).ToString("0"));
            RefreshBar();

            if (_hp <= 0f) Explode();
        }

        private void Explode()
        {
            // 파편 — 작은 큐브 6개가 사방으로 튀고 잠시 후 사라진다 (그레이박스 폭발).
            for (int i = 0; i < 6; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.transform.position = transform.position + Vector3.up * 0.3f;
                shard.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);
                var renderer = GetComponentInChildren<Renderer>();
                if (renderer != null) shard.GetComponent<Renderer>().sharedMaterial = renderer.sharedMaterial;
                Rigidbody rb = shard.AddComponent<Rigidbody>();
                rb.linearVelocity = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(2f, 5f), Random.Range(-1.5f, 1.5f));
                rb.angularVelocity = Random.insideUnitSphere * 8f;
                Destroy(shard, 1.6f);
            }
            Debug.Log("[취급주의] 상자 파손! 주문은 남는다 — 구역을 다시 오면 재스폰.");
            WorldEvents.RaisePackageDestroyed(); // AU-008 — 파손 SFX·연출 구독 지점
            Destroy(gameObject);
        }

        // ── HP바 (풀해상 오버레이 — 상자당 소형 캔버스, HUD(sort 10)보다 아래) ──

        private void BuildBar()
        {
            _barCanvas = new GameObject("HpBarCanvas");
            Canvas canvas = _barCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;

            _barRoot = new GameObject("HpBar", typeof(RectTransform)).GetComponent<RectTransform>();
            _barRoot.SetParent(_barCanvas.transform, false);
            _barRoot.sizeDelta = new Vector2(BAR_W, 12f);

            Image bg = new GameObject("Bg", typeof(RectTransform)).AddComponent<Image>();
            bg.transform.SetParent(_barRoot, false);
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);
            bg.raycastTarget = false;
            RectTransform bgRect = bg.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

            _barFill = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
            _barFill.transform.SetParent(_barRoot, false);
            _barFill.color = new Color(0.35f, 0.9f, 0.45f, 1f);
            _barFill.raycastTarget = false;
            RectTransform fillRect = _barFill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = new Vector2(FILL_W, 6f);
            fillRect.anchoredPosition = new Vector2((BAR_W - FILL_W) * 0.5f, 0f);

            _barCanvas.SetActive(false); // 무손상일 땐 숨김
        }

        private void RefreshBar()
        {
            if (_barCanvas == null) return;
            float ratio = Mathf.Clamp01(_hp / (_tuning != null ? _tuning.boxMaxHp : 100f));
            _barCanvas.SetActive(true);
            _barFill.rectTransform.sizeDelta = new Vector2(FILL_W * ratio, 6f);
            _barFill.color = ratio > 0.5f
                ? new Color(0.35f, 0.9f, 0.45f) : ratio > 0.25f
                    ? new Color(1f, 0.62f, 0.27f) : new Color(1f, 0.35f, 0.3f);
        }
    }
}
