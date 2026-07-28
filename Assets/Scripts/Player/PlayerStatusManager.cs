using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 스태미나와 캐리 상태. 적재 목록 자체는 GameStateSO가 소유하고,
    /// 여기서는 "지금 손에 든 한 건"만 다룬다.
    /// </summary>
    public class PlayerStatusManager : MonoBehaviour
    {
        /// <summary>이 비율 이상 변했을 때만 경계 밖으로 알린다(프레임 데이터 방지).</summary>
        private const float STAMINA_NOTIFY_STEP = 0.05f;

        [Tooltip("든 물건이 붙는 위치. 플레이어 자식 트랜스폼.")]
        [SerializeField] private Transform _carryAnchor;

        [Tooltip("씬 전이 복원 상자의 비주얼 — 캠프 박스와 동일 프리팹(Prefabs/Auto/prop_box_parcel). 빌더 주입 (S-070 ③).")]
        [SerializeField] private GameObject _parcelVisualPrefab;

        private PlayerManager _hub;
        private float _lastNotifiedStamina = -1f;
        private Transform _carriedVisual;

        public float Stamina { get; private set; }
        public float StaminaNormalized => Mathf.Clamp01(Stamina / _hub.Tuning.staminaMax);
        public DeliveryOrderSO CarriedOrder { get; private set; }
        public bool IsCarrying => CarriedOrder != null;

        // S-055 — 두 개 들기: 누적 배송 성공 5건이면 습득. 2번 슬롯은 머리 위에 쌓인다.
        public DeliveryOrderSO CarriedOrder2 { get; private set; }
        public bool CanDoubleCarry => _hub.GameState != null && _hub.GameState.completedCount >= 5;
        public bool CarryFull => IsCarrying && (CarriedOrder2 != null || !CanDoubleCarry);
        private Transform _carriedVisual2;
        private bool _fillSecondSlot;

        private void Awake() => _hub = GetComponent<PlayerManager>();

        private bool _inHillside; // S-049 — 오르막 스태미나 가중

        private WeatherType _weather; // S-060 — 온도 페널티·음료 보너스 판정

        private void OnEnable()
        {
            WorldEvents.DeliveryFailed += OnDeliveryFailed;
            WorldEvents.SceneTransitionCompleted += OnSceneArrivedStatus;
            WorldEvents.WeatherChanged += OnWeatherChangedStatus;
            WorldEvents.BagHoldRequested += OnBagHoldRequested;   // S-064
            WorldEvents.BagItemConsumed += OnBagItemConsumed;     // S-064
            WorldEvents.SceneTransitionStarted += OnSceneLeaving; // S-066 ② — 든 짐 보존
        }
        private void OnDisable()
        {
            WorldEvents.DeliveryFailed -= OnDeliveryFailed;
            WorldEvents.SceneTransitionCompleted -= OnSceneArrivedStatus;
            WorldEvents.WeatherChanged -= OnWeatherChangedStatus;
            WorldEvents.BagHoldRequested -= OnBagHoldRequested;
            WorldEvents.BagItemConsumed -= OnBagItemConsumed;
            WorldEvents.SceneTransitionStarted -= OnSceneLeaving;
        }

        private void OnWeatherChangedStatus(WeatherType weather) => _weather = weather;

        // ── 가방 연동 (S-064) — 손 들기·즉시 사용 ──
        private void OnBagHoldRequested(BagItem item)
        {
            if (_heldDrink != null) { Debug.Log("[가방] 이미 손에 음료가 있다"); return; }
            TryHoldDrink(CreateDrinkVisual().transform);
            Debug.Log("[가방] " + item.label + " 손에 들었다 — 우클릭 마시기/좌클릭 던지기");
        }

        private void OnBagItemConsumed(BagItem item)
        {
            if (item.id != "drink") return; // S-059 — 사료·장난감 등은 해당 도메인(고양이 등)이 받는다
            // 음료 = 스태미나 회복 + 날씨 보너스.
            float amount = _hub.Tuning.energyDrinkRecover;
            if (_weather == WeatherType.Heat) { amount *= 1.5f; Debug.Log("[가방] 캬 — 시원하다! (폭염 보너스)"); }
            else if (_weather == WeatherType.Snow) { amount *= 1.5f; Debug.Log("[가방] 후 — 따뜻하다! (한파 보너스)"); }
            RecoverStamina(amount);
            WorldAudioManager.Instance?.PlayDrinkSfx();
            Debug.Log("[가방] " + item.label + " 사용 — 스태미나 회복");
        }

        // 가방에서 꺼낸 음료 시각물 — 자판기 캔과 동형 (작은 빨간 캡슐).
        private GameObject CreateDrinkVisual()
        {
            GameObject can = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            can.name = "BagDrink";
            can.transform.localScale = new Vector3(0.18f, 0.14f, 0.18f);
            var renderer = can.GetComponent<Renderer>();
            renderer.material.color = new Color(0.88f, 0.29f, 0.21f);
            can.GetComponent<Collider>().enabled = false;
            return can;
        }

        private void OnSceneArrivedStatus(GameScene scene) => _inHillside = scene == GameScene.Hillside;

        private void Start()
        {
            Stamina = _hub.Tuning.staminaMax;
            NotifyStamina(force: true);
            RestoreCarriedFromState(); // S-066 ② — 엣지 워크로 넘어온 짐 복원
        }

        // ── S-066 ② — 씬 전환 시 든 짐 보존/복원 (씬 오브젝트는 파괴되므로 GameState 경유) ──
        private void OnSceneLeaving(GameScene _)
        {
            if (_hub.GameState == null) return;
            _hub.GameState.carriedOrders.Clear();
            if (CarriedOrder != null) _hub.GameState.carriedOrders.Add(CarriedOrder);
            if (CarriedOrder2 != null) _hub.GameState.carriedOrders.Add(CarriedOrder2);
        }

        private void RestoreCarriedFromState()
        {
            if (_hub.GameState == null || _hub.GameState.carriedOrders.Count == 0) return;
            foreach (DeliveryOrderSO order in _hub.GameState.carriedOrders)
            {
                if (order == null || !TryCarry(order)) continue;
                AttachCarried(CreateBoxVisual(order).transform);
            }
            // 버퍼는 지우지 않는다 — 스포너가 "손에 든 건 스폰 제외" 판정에 참조 (다음 전이 때 재작성).
            Debug.Log("[운반] 들고 온 짐 " + (CarriedOrder2 != null ? 2 : 1) + "건 복원");
        }

        // 복원용 택배 상자 — 캠프 박스(CreateParcelBox)와 동일 룩·동일 장비 (S-070 ③:
        // 주황 큐브로 변형되던 이질감 + 체력바 소실 수리). 프리팹 소켓은 빌더가 주입.
        // S-068 ④: PickupBox를 붙여 두어 버려도(던져도) E로 다시 잡을 수 있다.
        private GameObject CreateBoxVisual(DeliveryOrderSO order)
        {
            GameObject box = new GameObject("CarriedBox");
            BoxCollider collider = box.AddComponent<BoxCollider>();
            collider.size = Vector3.one * 0.7f;
            collider.center = new Vector3(0f, 0.35f, 0f);
            collider.enabled = false; // 손에 있는 동안 잠금 — 드롭 시 DropVisualAsPhysics가 켠다
            Rigidbody body = box.AddComponent<Rigidbody>();
            body.mass = 2f;
            body.isKinematic = true; // 손 위에선 물리 정지 — 드롭 시 해제

            if (_parcelVisualPrefab != null)
            {
                GameObject visual = Instantiate(_parcelVisualPrefab, box.transform);
                visual.name = "Visual";
                Bounds bounds = ComputeVisualBounds(visual);
                if (bounds.size.y > 0.001f)
                {
                    // 캠프 CreateParcelBox와 동일 규칙: 높이 0.7u 정규화 + 바닥을 루트 원점에 정렬.
                    visual.transform.localScale = Vector3.one * (0.7f / bounds.size.y);
                    bounds = ComputeVisualBounds(visual);
                    visual.transform.position += box.transform.position
                        - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                }
            }
            else
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(box.transform, false);
                cube.transform.localScale = Vector3.one * 0.55f;
                cube.transform.localPosition = Vector3.up * 0.35f;
                Destroy(cube.GetComponent<Collider>());
                cube.GetComponent<Renderer>().material.color = new Color(1f, 0.624f, 0.271f);
            }

            BoxDurability durability = box.AddComponent<BoxDurability>();
            durability.Initialize(_hub.Tuning);
            PickupBox pickup = box.AddComponent<PickupBox>();
            pickup.Initialize(order, null, requireInCargo: false, requireScanned: false);
            return box;
        }

        private static Bounds ComputeVisualBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
            Bounds bounds = renderers[0].bounds; // 월드 기준 — 호출부가 루트 원점 대비로 정렬
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private void Update()
        {
            TuningConfigSO tuning = _hub.Tuning;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            bool leftClick = mouse != null && mouse.leftButton.wasPressedThisFrame && !PhoneView.IsOpen;
            bool rightClick = mouse != null && mouse.rightButton.wasPressedThisFrame && !PhoneView.IsOpen;

            // S-032 ④: 우클릭 = 드링크 마시기 · 좌클릭 = 던지기(상자 우선, 없으면 드링크 — 택배와 동일 감각).
            if (rightClick && _heldDrink != null)
                ConsumeHeldDrink();
            if (leftClick && IsCarrying)
                ThrowCarryTowardsMouse(tuning.throwSpeed); // 던지기 (S-016 ⑦)
            else if (leftClick && _heldDrink != null)
                ThrowHeldDrink(tuning.throwSpeed);

            bool moving = _hub.Locomotion.PlanarVelocity.sqrMagnitude > 0.01f;

            if (moving)
            {
                // S-019 ③: 걷기 < 달리기, 든 상자는 무게(kg)만큼 가중.
                float drain = _hub.Input.RunHeld ? tuning.staminaDrainRunPerSecond : tuning.staminaDrainPerSecond;
                if (IsCarrying)
                {
                    drain += CarriedOrder.weight > 0f
                        ? CarriedOrder.weight * tuning.staminaDrainPerKg
                        : drain * (tuning.staminaDrainCarryMultiplier - 1f); // 무게 미지정 주문 폴백
                }
                if (CarriedOrder2 != null) // S-055 — 두 번째 상자 무게 가중
                    drain += CarriedOrder2.weight > 0f
                        ? CarriedOrder2.weight * tuning.staminaDrainPerKg
                        : drain * (tuning.staminaDrainCarryMultiplier - 1f);
                if (_inHillside) drain *= 1.4f; // S-049 — 오르막 동네는 힘들다
                if (_weather == WeatherType.Heat || _weather == WeatherType.Snow) drain *= 1.35f; // S-060 — 덥거나 추우면 더 힘들다
                Stamina -= drain * Time.deltaTime;
            }
            else
            {
                Stamina += tuning.staminaRecoverPerSecond * Time.deltaTime;
            }

            Stamina = Mathf.Clamp(Stamina, 0f, tuning.staminaMax);
            NotifyStamina(force: false);
        }

        public bool TryCarry(DeliveryOrderSO order)
        {
            if (!IsCarrying)
            {
                CarriedOrder = order;
                _fillSecondSlot = false;
                WorldEvents.RaiseCarryStateChanged(true);
                return true;
            }
            if (CanDoubleCarry && CarriedOrder2 == null) // S-055 두 개 들기
            {
                CarriedOrder2 = order;
                _fillSecondSlot = true;
                Debug.Log("[숙련] 두 개 들기 — 상자를 하나 더 얹었다");
                return true;
            }
            return false;
        }

        public DeliveryOrderSO ReleaseCarry(bool dropAsPhysics = false)
        {
            DeliveryOrderSO released = CarriedOrder;
            CarriedOrder = null;

            if (_carriedVisual != null)
            {
                if (dropAsPhysics) DropVisualAsPhysics(_carriedVisual);
                else Destroy(_carriedVisual.gameObject);
                _carriedVisual = null;
            }

            // S-055 — 2번 슬롯 승격: 위에 얹힌 상자가 손으로 내려온다.
            if (CarriedOrder2 != null)
            {
                CarriedOrder = CarriedOrder2;
                CarriedOrder2 = null;
                _carriedVisual = _carriedVisual2;
                _carriedVisual2 = null;
                if (_carriedVisual != null) _carriedVisual.localPosition = Vector3.zero;
            }

            WorldEvents.RaiseCarryStateChanged(IsCarrying);
            return released;
        }

        /// <summary>
        /// 든 물건을 손에서 놓아 물리로 떨어뜨린다. S-017: PickupBox를 살려 두므로 **다시 주울 수 있고**,
        /// 굴러가 비콘 패드에 닿으면 DeliveryPoint 트리거가 배송으로 인증한다(던져 넣기).
        /// </summary>
        private void DropVisualAsPhysics(Transform visual)
        {
            visual.SetParent(null, worldPositionStays: true);

            if (visual.TryGetComponent(out Collider collider))
            {
                collider.enabled = true;
                collider.isTrigger = false;
            }

            if (visual.TryGetComponent(out Rigidbody body)) body.isKinematic = false;
            else visual.gameObject.AddComponent<Rigidbody>();
        }

        /// <summary>든 상자를 마우스가 가리키는 방향으로 던진다 (S-016 ⑦ — 물리 드롭 + 초기 속도).</summary>
        private void ThrowCarryTowardsMouse(float speed)
        {
            Camera camera = Camera.main;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (camera == null || mouse == null || _carriedVisual == null) return;

            // 마우스 레이를 플레이어 Z평면에 투영해 조준점을 얻는다 (2.5D — 깊이는 유지).
            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            Plane plane = new Plane(Vector3.back, new Vector3(0f, 0f, transform.position.z));
            if (!plane.Raycast(ray, out float enter)) return;
            Vector3 aim = ray.GetPoint(enter);

            Transform visual = _carriedVisual;
            Vector3 direction = (aim - visual.position);
            direction.z = 0f;
            direction = direction.sqrMagnitude < 0.01f ? Vector3.up : direction.normalized;

            ReleaseCarry(dropAsPhysics: true);
            if (visual.TryGetComponent(out Rigidbody body))
                body.linearVelocity = direction * speed + Vector3.up * 1.5f; // 살짝 포물선
            WorldAudioManager.Instance?.PlayThrowSfx(); // AU-008 — Instance 명령 (이벤트 없는 지점)
        }

        // ── 드링크 들기·섭취 (S-031 ⑩) ──────────────────────
        private Transform _heldDrink;

        /// <summary>드링크를 손(캐리 앵커 곁)에 붙인다. 이미 들고 있으면 거절.</summary>
        public bool TryHoldDrink(Transform visual)
        {
            if (_heldDrink != null) return false;
            _heldDrink = visual;
            visual.SetParent(_carryAnchor, false);
            visual.localPosition = new Vector3(0.35f, -0.15f, 0f); // 상자와 공존 — 옆손
            visual.localRotation = Quaternion.identity;
            return true;
        }

        private void ConsumeHeldDrink()
        {
            Destroy(_heldDrink.gameObject);
            _heldDrink = null;
            // S-060 — 덥거나 추운 날 음료는 시원함/따뜻함 보너스(회복 1.5배). 종류 분화(찬/뜨거운)는 상점(S-056)에서.
            float amount = _hub.Tuning.energyDrinkRecover;
            if (_weather == WeatherType.Heat) { amount *= 1.5f; Debug.Log("[드링크] 캬 — 시원하다! (폭염 보너스)"); }
            else if (_weather == WeatherType.Snow) { amount *= 1.5f; Debug.Log("[드링크] 후 — 따뜻하다! (한파 보너스)"); }
            RecoverStamina(amount); // 내부에서 힐 이펙트(PlayDrinkEffect)까지 발화
            WorldAudioManager.Instance?.PlayDrinkSfx();     // AU-009
            Debug.Log("[드링크] 섭취 — 스태미나 회복 (우클릭)");
        }

        /// <summary>S-032 ④: 든 드링크를 마우스 방향으로 던진다 — 다시 픽업체가 되어 E로 회수 가능.</summary>
        private void ThrowHeldDrink(float speed)
        {
            Camera camera = Camera.main;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (camera == null || mouse == null) return;

            Transform drink = _heldDrink;
            _heldDrink = null;
            drink.SetParent(null, worldPositionStays: true);

            if (drink.TryGetComponent(out Collider collider)) collider.enabled = true;
            if (!drink.TryGetComponent(out Rigidbody body)) body = drink.gameObject.AddComponent<Rigidbody>();
            body.mass = 0.3f;
            if (drink.GetComponent<EnergyDrinkPickup>() == null) drink.gameObject.AddComponent<EnergyDrinkPickup>();

            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            Plane plane = new Plane(Vector3.back, new Vector3(0f, 0f, transform.position.z));
            Vector3 direction = Vector3.up;
            if (plane.Raycast(ray, out float enter))
            {
                direction = ray.GetPoint(enter) - drink.position;
                direction.z = 0f;
                direction = direction.sqrMagnitude < 0.01f ? Vector3.up : direction.normalized;
            }
            body.linearVelocity = direction * speed + Vector3.up * 1.5f;
            body.angularVelocity = Random.insideUnitSphere * 25f; // S-033 ③ — 캔 팽글팽글
            WorldAudioManager.Instance?.PlayThrowSfx();
            Debug.Log("[드링크] 던짐 (좌클릭) — E로 다시 주울 수 있다");
        }

        /// <summary>든 물건의 겉모습을 캐리 앵커에 붙인다. 내려놓을 때 함께 사라진다.</summary>
        public void AttachCarried(Transform visual)
        {
            visual.SetParent(_carryAnchor, false);
            visual.localRotation = Quaternion.identity;
            if (_fillSecondSlot) // S-055 — 2번 슬롯은 머리 위
            {
                _carriedVisual2 = visual;
                visual.localPosition = new Vector3(0f, 0.62f, 0f);
                _fillSecondSlot = false;
            }
            else
            {
                _carriedVisual = visual;
                visual.localPosition = Vector3.zero;
            }
        }

        /// <summary>지각으로 실패한 건이 지금 든 것이면 손에서 내려놓는다.</summary>
        private void OnDeliveryFailed(DeliveryData data)
        {
            if (CarriedOrder2 != null && CarriedOrder2.orderId == data.OrderId) // S-055
            {
                CarriedOrder2 = null;
                if (_carriedVisual2 != null) { Destroy(_carriedVisual2.gameObject); _carriedVisual2 = null; }
                return;
            }
            if (CarriedOrder == null || CarriedOrder.orderId != data.OrderId) return;
            ReleaseCarry();
        }

        public void RecoverStamina(float amount)
        {
            Stamina = Mathf.Clamp(Stamina + amount, 0f, _hub.Tuning.staminaMax);
            NotifyStamina(force: true);
            if (_hub.Effects != null) _hub.Effects.PlayDrinkEffect(); // S-023 드링크 버스트 (재조립 전 씬 대비 가드)
        }

        private void NotifyStamina(bool force)
        {
            float normalized = StaminaNormalized;
            if (!force && Mathf.Abs(normalized - _lastNotifiedStamina) < STAMINA_NOTIFY_STEP) return;
            _lastNotifiedStamina = normalized;
            WorldEvents.RaiseStaminaChanged(normalized);
        }
    }
}
