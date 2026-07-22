using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 취급주의 상자 내구도 (S-019 ①). 낙하·투척 충돌 속도가 안전치를 넘으면 HP가 닳고,
    /// 0이 되면 파편 이펙트와 함께 터진다(주문은 cargo에 남아 구역 재진입 시 재스폰 — 파손 페널티는 시간).
    /// 머리 위 HP바(월드 쿼드 2장)는 피해를 입은 뒤에만 보인다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoxDurability : MonoBehaviour
    {
        [SerializeField] private TuningConfigSO _tuning;

        /// <summary>런타임 스폰 초기화 (스포너 — 에디터 API 없이 튜닝 주입).</summary>
        public void Initialize(TuningConfigSO tuning) => _tuning = tuning;

        private float _hp;
        private Transform _barRoot;
        private Transform _barFill;

        private void Start()
        {
            _hp = _tuning != null ? _tuning.boxMaxHp : 100f;
            BuildBar();
        }

        // 상자가 물리로 구르면 자식인 바도 같이 기운다 — 매 프레임 세계 기준으로 세운다 (S-021 ①).
        private void LateUpdate()
        {
            if (_barRoot == null || !_barRoot.gameObject.activeSelf) return;
            _barRoot.position = transform.position + Vector3.up * 0.95f;
            _barRoot.rotation = Quaternion.identity; // 카메라(-Z 고정 시선) 정면
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
            Destroy(gameObject);
        }

        // ── HP바 (월드 쿼드 — 카메라 -Z 고정이라 빌보드 불필요) ──

        private void BuildBar()
        {
            _barRoot = new GameObject("HpBar").transform;
            _barRoot.SetParent(transform, false);
            _barRoot.localPosition = new Vector3(0f, 0.95f, 0f);

            Transform bg = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            Object.Destroy(bg.GetComponent<Collider>());
            bg.SetParent(_barRoot, false);
            bg.localScale = new Vector3(0.8f, 0.1f, 1f);
            bg.GetComponent<Renderer>().material.color = new Color(0.05f, 0.05f, 0.08f, 1f);

            _barFill = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            Object.Destroy(_barFill.GetComponent<Collider>());
            _barFill.SetParent(_barRoot, false);
            _barFill.localPosition = new Vector3(0f, 0f, -0.005f);
            _barFill.localScale = new Vector3(0.76f, 0.07f, 1f);
            _barFill.GetComponent<Renderer>().material.color = new Color(0.35f, 0.9f, 0.45f, 1f);

            _barRoot.gameObject.SetActive(false); // 무손상일 땐 숨김
        }

        private void RefreshBar()
        {
            if (_barRoot == null) return;
            float ratio = Mathf.Clamp01(_hp / (_tuning != null ? _tuning.boxMaxHp : 100f));
            _barRoot.gameObject.SetActive(true);
            _barFill.localScale = new Vector3(0.76f * ratio, 0.07f, 1f);
            _barFill.localPosition = new Vector3(-0.38f * (1f - ratio), 0f, -0.005f);
            var renderer = _barFill.GetComponent<Renderer>();
            renderer.material.color = ratio > 0.5f
                ? new Color(0.35f, 0.9f, 0.45f) : ratio > 0.25f
                    ? new Color(1f, 0.62f, 0.27f) : new Color(1f, 0.35f, 0.3f);
        }
    }
}
