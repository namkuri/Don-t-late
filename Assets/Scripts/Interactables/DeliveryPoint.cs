using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 배송지 문 앞. 들고 온 건이 이 주소와 맞으면 인증되고 완료 처리된다.
    /// 하이라이트는 두 갈래 — 근접 포커스(센서)와 목적지 표시(픽업 이후) 중 하나라도 켜지면 켠다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    // S-133 ② — IFocusGate 제거(정수님 QA "위치 좀만 어긋나도 실패"). 패드 사각형 안에 정확히
    // 서야만 포커스되던 게이트를 걷어내, 근처에서 상호작용하면 성공하게 한다.
    // ⚠ `_padSize` 필드와 `PadSize` 프로퍼티는 **남긴다** — 빌더가 SetVector2로 주입하고(널체크
    // 없어 지우면 씬 재조립이 NRE로 죽는다) 프로퍼티를 지우면 write-only 필드가 되어 CS0414 경고.
    public class DeliveryPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private DeliveryOrderSO _expectedOrder;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;
        [Tooltip("목적지 표시색 (S-133 ① — 들고 있는 상자의 목적지). 비면 하이라이트색으로 폴백.")]
        [SerializeField] private Material _targetMaterial;
        [SerializeField] private Vector2 _padSize = new Vector2(1f, 1f);
        [SerializeField] private GameObject _riseEffect;
        [Tooltip("패드 위 포커스 시 나타나는 주소 라벨(월드 텍스트) — S-016 ②.")]
        [SerializeField] private TMPro.TMP_Text _addressLabel;
        [SerializeField] private float _idleAlpha = 1f;
        [SerializeField] private float _focusedAlpha = 0.3f;

        public Vector2 PadSize => _padSize;
        /// <summary>HUD 풀해상 표시용 주소 (S-021 ② — 월드 텍스트는 픽셀레이트에 뭉개져 폐지).</summary>
        public string Address => _expectedOrder != null ? _expectedOrder.address : string.Empty;

        /// <summary>런타임 스폰 초기화 (S-015 — 비콘 프리팹 인스턴스에 주문 배정).</summary>
        public void SetOrder(DeliveryOrderSO order)
        {
            _expectedOrder = order;
            if (_addressLabel != null)
            {
                _addressLabel.text = order != null ? order.address : string.Empty;
                _addressLabel.gameObject.SetActive(false); // 포커스 시에만 (S-016 ②)
            }
        }

        private bool _focused;
        private bool _isDestination;
        private MaterialPropertyBlock _riseMpb;

        private void OnEnable()
        {
            WorldEvents.PackagePickedUp += OnPackagePickedUp;
            WorldEvents.PackageReleased += OnPackageReleased; // S-133 ①
            WorldEvents.DeliveryCompleted += OnDeliverySettled;
            WorldEvents.DeliveryFailed += OnDeliverySettled;
        }

        private void OnDisable()
        {
            WorldEvents.PackagePickedUp -= OnPackagePickedUp;
            WorldEvents.PackageReleased -= OnPackageReleased;
            WorldEvents.DeliveryCompleted -= OnDeliverySettled;
            WorldEvents.DeliveryFailed -= OnDeliverySettled;
            if (_nameLabel != null) _nameLabel.gameObject.SetActive(false); // 패드 소멸 시 라벨 잔존 방지
        }

        // S-034 ④: 비콘에 놓기 = 내려놓기일 뿐 — 완료·보상 없음. 주소가 달라도 놓인다(오배치 = 정산 때 실패).
        public void Interact(PlayerContext ctx)
        {
            DeliveryOrderSO carried = ctx.Player.Status.CarriedOrder;
            if (carried == null) { Debug.Log("[DeliveryPoint] 빈손 — 상자를 들고 와야 내려놓는다."); return; }
            if (!WorldDeliveryManager.Instance.CanHandle(carried)) // S-075 ② — 지각 건도 놓을 수 있다
            {
                Debug.Log("[DeliveryPoint] #" + carried.orderId + " 은 오늘 물량이 아니다 — 내려놓기 불가.");
                return;
            }

            ctx.Player.Status.ReleaseCarry(dropAsPhysics: true);
            WorldDeliveryManager.Instance.PlaceDelivery(carried, Address);
            if (carried.address == Address) { ShowSuccessFloat(); HideBeacon(); } // S-073 ⑥ + S-097 ① 비콘 소등
        }

        /// <summary>
        /// 던져 넣기 (S-017 ② → S-034 배치화) — 물리로 굴러온 상자가 패드에 닿으면 배치 기록.
        /// 상자는 파괴하지 않는다 — 다시 들어 옮길 수 있다.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out PickupBox box) || box.Order == null) return;
            if (WorldDeliveryManager.Instance == null || !WorldDeliveryManager.Instance.CanHandle(box.Order)) return; // S-075 ②
            // S-098 ① — 라이브 배치는 물리 유지(던져 놓고 밀치는 손맛). 즉시 스냅(S-097)은 폐지 —
            // 고정은 씬 재입장 스포너의 frozen 스폰이 맡는다.
            if (WorldDeliveryManager.Instance.IsPlaced(box.Order.orderId)) return; // 재스폰·낙하 재진입 중복 방지 (S-097 ①)
            WorldDeliveryManager.Instance.PlaceDelivery(box.Order, Address);
            if (box.Order.address == Address) { ShowSuccessFloat(); HideBeacon(); } // S-073 ⑥ — 던져 넣기도 연출
        }

        /// <summary>S-097 ① — 배송성공 시 비콘 빛기둥 제거 (재방문 스폰 시에도 Start에서 호출).</summary>
        private void HideBeacon()
        {
            if (_riseEffect != null) _riseEffect.SetActive(false);
        }

        private void Start()
        {
            // S-097 ① — 이미 이 패드에 배치 완료된 건이면 비콘 없이 선다 (재방문).
            if (_expectedOrder != null && WorldDeliveryManager.Instance != null
                && WorldDeliveryManager.Instance.IsPlacedAt(_expectedOrder.orderId, Address))
                HideBeacon();

            // S-164 — **씬 진입 시 이미 들고 있는 짐을 인식한다.**
            // 종전엔 `PackagePickedUp` 이벤트만 들었는데, 배송지 비콘은 씬 진입 때 스폰되므로
            // 픽업은 그 **전에** 이미 끝나 있다 — 그래서 두 번째 짐부터 목적지 표시(파랑)가
            // 아예 안 떴다(남규님 실관찰: "2번째 짐 가져오니까 파란색 비콘이 없어").
            // 이벤트를 놓친 게 아니라 **들을 때 이미 지나간 사건**이라 상태를 직접 조회해야 한다.
            RefreshDestinationFromCarried();
        }

        /// <summary>지금 들고 있는 짐 중에 이 패드 목적지가 있으면 목적지 표시를 켠다.</summary>
        private void RefreshDestinationFromCarried()
        {
            if (_expectedOrder == null || WorldDeliveryManager.Instance == null) return;
            if (!WorldDeliveryManager.Instance.IsCarried(_expectedOrder.orderId)) return;
            _isDestination = true;
            ApplyHighlight();
            ApplyRiseAlpha(_focused);
        }

        // S-156 — **패드 이탈 철회를 없앴다**(남규님 난이도 조절 지시).
        // 종전엔 `OnTriggerExit`에서 배치를 철회했다. 그래서 E로 제대로 내려놨어도 상자가 살짝
        // 굴러 나가면 정산에서 실패로 잡혔다 — 플레이어가 한 행동을 물리가 나중에 뒤집는 구조라
        // "제대로 했는데 실패"라는 억울함이 남는다. 판정 기준은 **E를 누른 그 순간의 의사**다.
        // 정산은 원래부터 기록만 본다(`placedDeliveries[i].beaconAddress == order.address`,
        // 물리 검사 없음) — 즉 이 철회만 없애면 기록이 그대로 살아 성공으로 잡힌다.
        // 재픽업 철회(`PickupBox`)는 그대로 둔다: 다시 집는 건 플레이어의 명시적 의사다.

        /// <summary>지금 들고 있는 상자의 목적지인가 (S-133 ①④) — 패드 색과 E키 우선순위가 이걸 본다.</summary>
        public bool IsCarriedDestination => _isDestination;

        public void SetHighlight(bool on)
        {
            _focused = on;
            ApplyHighlight();
            ApplyRiseAlpha(on);
            if (_addressLabel != null) _addressLabel.gameObject.SetActive(on); // 패드 위 = 주소 표시 (S-016 ②)
        }

        private void OnPackagePickedUp(DeliveryData data)
        {
            if (_expectedOrder == null || data.OrderId != _expectedOrder.orderId) return;
            _isDestination = true;
            ApplyHighlight();
            ApplyRiseAlpha(_focused); // S-161 — 목적지 전환 시 빛기둥 색도 함께 갱신
        }

        // S-133 ① — 상자를 내려놓으면 목적지 표시를 끈다. 종전엔 켜지기만 하고 꺼지지 않아
        // 배송을 마치기 전까지 패드가 계속 빛났다(어디로 갈지 헷갈리는 원인).
        private void OnPackageReleased(DeliveryData data)
        {
            if (_expectedOrder == null || data.OrderId != _expectedOrder.orderId) return;
            _isDestination = false;
            ApplyHighlight();
            ApplyRiseAlpha(_focused); // S-161 — 목적지 해제 시 빛기둥 색 복귀
        }

        // ── S-073 ⑤⑥ — 패드 위 풀해상 오버레이: 목적지 건물이름 상시 + "배송성공" 플로팅 ──
        // (월드 텍스트(_addressLabel)는 픽셀레이트에 뭉개지는 한계가 있어(S-021 ②) 오버레이 병행.
        //  BoxDurability HP바와 같은 패턴 — 자체 소형 캔버스 + WorldToScreenPoint 추적.)

        private GameObject _overlayCanvasGo;
        private TMPro.TMP_Text _nameLabel;

        private void OnDestroy()
        {
            if (_overlayCanvasGo != null) Destroy(_overlayCanvasGo);
        }

        private void LateUpdate()
        {
            bool show = _isDestination && _expectedOrder != null;
            if (!show)
            {
                if (_nameLabel != null) _nameLabel.gameObject.SetActive(false);
                return;
            }

            Camera camera = Camera.main;
            if (camera == null) return;
            if (_nameLabel == null) BuildOverlay();

            Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 1.6f);
            if (screen.z < 0f) { _nameLabel.gameObject.SetActive(false); return; }
            _nameLabel.gameObject.SetActive(true);
            _nameLabel.rectTransform.position = new Vector3(screen.x, screen.y, 0f);
            _nameLabel.text = _expectedOrder.address;
        }

        private void BuildOverlay()
        {
            _overlayCanvasGo = new GameObject("PadLabelCanvas");
            Canvas canvas = _overlayCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;

            _nameLabel = MakeOverlayText("PadName", 24f, new Color(0.208f, 0.878f, 0.784f));
        }

        private TMPro.TMP_Text MakeOverlayText(string name, float size, Color color)
        {
            var text = new GameObject(name, typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
            text.transform.SetParent(_overlayCanvasGo.transform, false);
            // S-073 — 한글 글리프 보장 폰트 (비콘 월드 라벨 폰트는 한글이 없어 네모 — 실캡처 적발).
            if (UiOverlayFont.Korean != null) text.font = UiOverlayFont.Korean;
            else if (_addressLabel != null) text.font = _addressLabel.font;
            text.fontSize = size;
            text.fontStyle = TMPro.FontStyles.Bold;
            text.color = color;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(360f, 34f);
            return text;
        }

        private void ShowSuccessFloat()
        {
            if (_nameLabel == null) BuildOverlay();
            TMPro.TMP_Text label = MakeOverlayText("SuccessFloat", 30f, new Color(0.208f, 0.878f, 0.784f));
            label.text = "배송성공";
            StartCoroutine(FloatAndFade(label));
        }

        private System.Collections.IEnumerator FloatAndFade(TMPro.TMP_Text label)
        {
            const float DURATION = 1.3f;
            Camera camera = Camera.main;
            Color baseColor = label.color;
            float t = 0f;
            while (t < 1f && camera != null)
            {
                t += Time.deltaTime / DURATION;
                Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 1.9f);
                label.rectTransform.position = new Vector3(screen.x, screen.y + 70f * t, 0f);
                label.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t * t);
                yield return null;
            }
            if (label != null) Destroy(label.gameObject);
        }

        private void OnDeliverySettled(DeliveryData data)
        {
            if (_expectedOrder == null || data.OrderId != _expectedOrder.orderId) return;

            // S-165 ② — **아직 손에 들고 있으면 패드를 남긴다.**
            // 지각(DeliveryFailed)이 나면 여기서 패드를 통째로 꺼 버려, 짐을 들고 있는데도
            // 목적지 표시가 사라졌다(남규님 지적). 지각은 점수 문제일 뿐 **여전히 배달해야 할
            // 짐**이다 — 갈 곳을 감추면 플레이어는 길을 잃는다.
            if (WorldDeliveryManager.Instance != null
                && WorldDeliveryManager.Instance.IsCarried(_expectedOrder.orderId))
            {
                _isDestination = true;
                ApplyHighlight();
                ApplyRiseAlpha(_focused);
                return;
            }

            _isDestination = false;
            // 처리된 배송지는 패드째 완전 소멸 (S-009) — 서 있어도 다시 빛나지 않는다.
            gameObject.SetActive(false);
        }

        // S-133 ① — 3단 색: 포커스(시안) > 목적지(앰버=상자색) > 평상.
        // 목적지를 포커스와 같은 색으로 두면 "지금 겨눈 패드"인지 "내 짐의 목적지"인지 구분이 안 된다.
        // 앰버는 택배상자 색과 같아 "이 상자가 갈 자리"로 읽힌다.
        private void ApplyHighlight()
        {
            if (_renderer == null) return;
            Material material = _focused ? _highlightMaterial
                : _isDestination && _targetMaterial != null ? _targetMaterial
                : _isDestination ? _highlightMaterial
                : _normalMaterial;
            if (material != null) _renderer.sharedMaterial = material;
        }

        // S-161 — 들고 있는 상자의 목적지 빛기둥 색(파랑). S-160에서 **패드** 머티리얼만 바꿨는데
        // 정작 눈에 띄는 건 빛기둥이고 그건 `BeaconRise.shader`의 `_Color`(기본 초록)로 그려져
        // 여전히 초록이었다(남규님 지적). 알파와 같은 방식으로 MPB에 실어 보낸다 —
        // 공유 머티리얼을 건드리지 않으므로 다른 비콘은 초록 그대로다.
        private static readonly Color RISE_DESTINATION = new Color(0.227f, 0.627f, 1f, 1f);
        private static readonly Color RISE_NORMAL = new Color(0.247f, 0.878f, 0.353f, 1f);

        /// <summary>빛기둥 알파·색을 MaterialPropertyBlock으로 전환한다 — 공유 머티리얼을 오염시키지 않는다.</summary>
        private void ApplyRiseAlpha(bool focused)
        {
            if (_riseEffect == null) return;
            float alpha = focused ? _focusedAlpha : _idleAlpha;
            Color color = _isDestination ? RISE_DESTINATION : RISE_NORMAL;
            _riseMpb ??= new MaterialPropertyBlock();
            foreach (Renderer r in _riseEffect.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(_riseMpb);
                _riseMpb.SetFloat("_Alpha", alpha);
                _riseMpb.SetColor("_Color", color);
                r.SetPropertyBlock(_riseMpb);
            }
        }
    }
}
