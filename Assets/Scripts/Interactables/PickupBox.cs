using UnityEngine;

namespace DontLate
{
    /// <summary>택배 상자. 상호작용하면 플레이어가 손에 들고 월드에서 사라진다.</summary>
    [RequireComponent(typeof(Collider))]
    public class PickupBox : MonoBehaviour, IInteractable
    {
        [SerializeField] private DeliveryOrderSO _order;
        [Tooltip("켜면 오늘 적재 목록(cargo)에 있는 건만 픽업 가능 — 배송지(District) 상자용. Camp 상자는 끔.")]
        [SerializeField] private bool _requireInCargo;
        [Tooltip("켜면 폰으로 바코드 스캔한 건만 픽업 가능 — Camp 상차용 (S-011).")]
        [SerializeField] private bool _requireScanned;
        [SerializeField] private Material _highlightMaterial;

        // 수제 박스는 렌더러·머티리얼 슬롯이 여럿(본체+테이프) — 전 슬롯을 통째로 바꿨다 되돌린다 (S-013).
        private Renderer[] _renderers;
        private Material[][] _originalMaterials;

        public DeliveryOrderSO Order => _order;

        /// <summary>
        /// S-169 — 지금 이 상자에 필요한 다음 동작 안내(필요 없으면 null).
        /// **판정을 여기 두는 이유**: 픽업을 막는 조건(`_requireScanned` + 미스캔)이 `Interact`
        /// 안에 있다. 안내 문구를 센서 쪽에서 따로 판정하면 조건이 두 곳으로 갈라져
        /// "안내는 뜨는데 집히거나 / 집히지도 않는데 안내가 없는" 어긋남이 생긴다.
        /// </summary>
        public string FocusHint
        {
            get
            {
                if (!_requireScanned || _order == null) return null;
                if (WorldDeliveryManager.Instance == null) return null;
                return WorldDeliveryManager.Instance.IsScanned(_order) ? null : "[상자 클릭] 바코드 스캔";
            }
        }

        private void Awake() => CacheRenderers();

        // S-119 ① — 정지 스폰 강체는 첫 프레임부터 잠들 수 있다(스택 위 상자 공중부양 실관찰).
        // 기상시켜 스폰 갭만큼 낙하·안착시킨다.
        private void Start()
        {
            if (TryGetComponent(out Rigidbody body) && !body.isKinematic) body.WakeUp();
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _originalMaterials = new Material[_renderers.Length][];
            for (int i = 0; i < _renderers.Length; i++)
                _originalMaterials[i] = _renderers[i].sharedMaterials;
        }

        /// <summary>주문 교체 (S-021 ③ — 캠프 주문 갱신. 소진된 건을 새 목적지로).</summary>
        public void SetOrder(DeliveryOrderSO order)
        {
            _order = order;
            GetComponent<Collider>().enabled = true; // 소진으로 꺼졌던 픽업도 재활성
        }

        /// <summary>런타임 스폰 초기화 (S-015 — DistrictCargoSpawner). AddComponent 직후 호출.</summary>
        public void Initialize(DeliveryOrderSO order, Material highlight, bool requireInCargo, bool requireScanned)
        {
            _order = order;
            _highlightMaterial = highlight;
            _requireInCargo = requireInCargo;
            _requireScanned = requireScanned;
            CacheRenderers(); // 비주얼이 Initialize 전에 붙었을 수 있어 재캐시
        }

        // S-089 ③ — 태풍 바람에 상자가 조금씩 흔들린다: 비주얼 자식만 z축 기울기 진동(물리 비침습).
        private Transform _swayVisual;
        private float _swayPhase;

        private void Update()
        {
            float windX = WorldWeatherManager.Instance != null ? WorldWeatherManager.Instance.WindX : 0f;
            if (windX == 0f)
            {
                if (_swayVisual != null) _swayVisual.localRotation = Quaternion.identity;
                return;
            }
            if (_swayVisual == null)
            {
                if (transform.childCount == 0) return;
                _swayVisual = transform.GetChild(0);
                _swayPhase = (Mathf.Abs(GetEntityId().GetHashCode()) % 628) * 0.01f; // 상자마다 위상 분산
            }
            float lean = -windX * (2.2f + 1.6f * Mathf.Sin(Time.time * 2.4f + _swayPhase));
            _swayVisual.localRotation = Quaternion.Euler(0f, 0f, lean);
        }

        public void Interact(PlayerContext ctx)
        {
            if (_order == null) return;
            // 씬 단독 Play 직후 프레임 등 매니저 부재 시 조용히 무시 (S-013 — EnsureCoreLoaded가 로드 중).
            if (WorldDeliveryManager.Instance == null)
            {
                Debug.LogWarning("[PickupBox] WorldDeliveryManager 없음 — Core 로드 대기 중이거나 씬 구성 오류.");
                return;
            }
            // S-010: 캠프에서 싣지 않은(또는 지각 실패한) 건은 배송 불가 — 침묵 무반응 대신 사유를 남긴다.
            if (_requireInCargo && !WorldDeliveryManager.Instance.CanHandle(_order)) // S-075 ② — 지각 건도 집을 수 있다
            {
                Debug.Log("[PickupBox] #" + _order.orderId + " 은 오늘 적재 목록에 없다 — 캠프에서 싣지 않았거나 지각 실패한 건.");
                return;
            }
            if (_requireScanned && !WorldDeliveryManager.Instance.IsScanned(_order))
            {
                Debug.Log("[PickupBox] #" + _order.orderId + " 은 바코드 미스캔 — Tab으로 폰을 열고 박스를 클릭해 송장을 찍어라.");
                // S-270 ② — 콘솔로만 거절하면 화면에는 아무 일도 안 일어난 것과 같다.
                // 스캔 대상 상자는 캠프 물량뿐이라(`_requireScanned`) 이 독백도 캠프에만 뜬다.
                WorldEvents.RaisePlayerRemarked("먼저 마우스로 클릭해서 송장을 찍어야되.");
                return;
            }
            // S-066 ① — 캠프 상자(스캔 대상)는 픽업 상한 = 적재 상한 (등록 안 되면 정산도 안 되므로 정직하게 거부).
            if (_requireScanned && !WorldDeliveryManager.Instance.IsInCargo(_order)
                && ctx.Player.GameState.cargo.Count >= ctx.Player.Tuning.maxCargo)
            {
                Debug.Log("[PickupBox] 오늘 적재 상한(" + ctx.Player.Tuning.maxCargo + ") — 더 받을 수 없다.");
                return;
            }

            if (!ctx.Player.Status.TryCarry(_order)) return;

            // S-066 ① — 픽업 시점에 오늘 배송 목록(cargo) 등록: 트럭 없이 들고 걸어가도 정산이 인정된다.
            // (트럭 상차 시 AcceptOrder는 중복 가드가 있어 안전.)
            if (_requireScanned) WorldDeliveryManager.Instance.AcceptOrder(_order);

            WorldDeliveryManager.Instance.NotifyPickedUp(_order);
            WorldDeliveryManager.Instance.UnplaceDelivery(_order.orderId); // S-034 ④ — 다시 들면 배치 철회
            SetHighlight(false);

            // 손에 든 동안은 센서에 다시 잡히면 안 된다. 물리 상자면 손 안에서 잠근다 (S-016 ⑥).
            // S-116 ① — 콜라이더를 끄기 전에 위에 얹힌 상자를 깨운다: 잠든(sleep) Rigidbody는
            // 받침 콜라이더가 꺼져도 안 깨어나 공중에 떠 있다 (캠프 스택 위 2개 실증상).
            Collider myCollider = GetComponent<Collider>();
            Vector3 top = myCollider.bounds.center + Vector3.up * myCollider.bounds.extents.y;
            foreach (Collider above in Physics.OverlapBox(
                top + Vector3.up * 0.2f,
                new Vector3(myCollider.bounds.extents.x, 0.3f, myCollider.bounds.extents.z)))
            {
                if (above.attachedRigidbody != null && above.attachedRigidbody.gameObject != gameObject)
                    above.attachedRigidbody.WakeUp();
            }
            myCollider.enabled = false;
            if (TryGetComponent(out Rigidbody body)) body.isKinematic = true;
            ctx.Player.Status.AttachCarried(transform);
        }

        public void SetHighlight(bool on)
        {
            // S-154 — 캐시가 반쪽만 남았으면 다시 잡는다.
            // `Material[][]`(지그재그 배열)은 **Unity 직렬화 미지원 타입**이라, 플레이 중
            // 스크립트를 고쳐 도메인 리로드가 일어나면 `_renderers`(지원 타입)만 복원되고
            // `_originalMaterials`는 null로 돌아온다. `Awake`는 다시 돌지 않으므로 아래 가드를
            // 통과해 `_originalMaterials[i]`에서 NRE가 났다(실측 재현: 강제 리컴파일 직후
            // 상자 전부 `렌더러=3 · 원본=null`). 빌드엔 없는 현상이지만 에디터에선 매번 겪는다.
            if (_renderers == null || _originalMaterials == null
                || _originalMaterials.Length != _renderers.Length)
                CacheRenderers();
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                if (on && _highlightMaterial != null)
                {
                    var materials = new Material[_originalMaterials[i].Length];
                    for (int s = 0; s < materials.Length; s++) materials[s] = _highlightMaterial;
                    _renderers[i].sharedMaterials = materials;
                }
                else
                {
                    _renderers[i].sharedMaterials = _originalMaterials[i];
                }
            }
        }
    }
}
