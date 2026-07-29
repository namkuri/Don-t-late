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
        private const float STAMINA_NOTIFY_STEP = 0.01f; // S-074 ⑦ — 5% 스텝이 게이지 계단의 원흉 (로그는 원래 없음 — 무비용)

        [Tooltip("든 물건이 붙는 위치. 플레이어 자식 트랜스폼.")]
        [SerializeField] private Transform _carryAnchor;

        [Tooltip("씬 전이 복원 상자의 비주얼 — 캠프 박스와 동일 프리팹(Prefabs/Auto/prop_box_parcel). 빌더 주입 (S-070 ③).")]
        [SerializeField] private GameObject _parcelVisualPrefab;

        [Tooltip("오버레이 라벨 폰트(한글) — 든 상자 마감 카운트다운용. 빌더 주입 (S-073 ④).")]
        [SerializeField] private TMPro.TMP_FontAsset _overlayFont;

        private PlayerManager _hub;
        private float _lastNotifiedStamina = -1f;
        private Transform _carriedVisual;

        public float Stamina { get; private set; }
        /// <summary>기본 최대 대비 비율 — 드링크 버프 중엔 1.0을 넘는다(초과분 = HUD 파란 fill, S-097 ③).</summary>
        public float StaminaNormalized => Mathf.Max(0f, Stamina / _hub.Tuning.staminaMax);
        public DeliveryOrderSO CarriedOrder { get; private set; }
        public bool IsCarrying => CarriedOrder != null;
        /// <summary>손에 든 음료 여부 (S-071 ② — 송장 좌클릭이 음료 던지기와 충돌하지 않게 센서가 참조).</summary>
        public bool IsHoldingDrink => _heldDrink != null;

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
            // S-088 ④ — 생수=더움 해소 · 따뜻한 음료=추움 해소 (회복 없음, 해소 전용).
            if (item.id == "water" || item.id == "hot_drink")
            {
                ApplyRelief(item.id);
                WorldAudioManager.Instance?.PlayDrinkSfx();
                Debug.Log("[가방] " + item.label + " — 패널티 해소 (" + _hub.Tuning.staminaPenaltyReliefSeconds + "초)");
                return;
            }
            if (item.id != "drink") return; // S-059 — 사료·장난감 등은 해당 도메인(고양이 등)이 받는다
            // 음료 = 스태미나 회복 + 날씨 보너스.
            float amount = _hub.Tuning.energyDrinkRecover;
            if (_weather == WeatherType.Heat) { amount *= 1.5f; Debug.Log("[가방] 캬 — 시원하다! (폭염 보너스)"); }
            else if (_weather == WeatherType.Snow) { amount *= 1.5f; Debug.Log("[가방] 후 — 따뜻하다! (한파 보너스)"); }
            RecoverStamina(amount);
            ApplyRelief("drink"); // S-088 ④ — 에너지드링크 = 더움 해소도
            ApplyDrinkBuff(); // S-074 ⑧ — 가방에서 바로 마셔도 같은 버프
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
            // S-082 ⑤ — 날씨 드레인 배율도 씬 진입 즉시 현재 날씨로 (플레이어 재탄생 구멍).
            if (WorldWeatherManager.Instance != null) _weather = WorldWeatherManager.Instance.Weather;
            // S-081 ① — 씬 전환마다 풀 충전되던 것: GameState 영속값 복원 (음수=하루 첫 진입 → 풀).
            Stamina = _hub.GameState != null && _hub.GameState.stamina >= 0f
                ? _hub.GameState.stamina : _hub.Tuning.staminaMax;
            NotifyStamina(force: true);
            RestoreCarriedFromState(); // S-066 ② — 엣지 워크로 넘어온 짐 복원
        }

        // ── S-066 ② — 씬 전환 시 든 짐 보존/복원 (씬 오브젝트는 파괴되므로 GameState 경유) ──
        private void OnSceneLeaving(GameScene _)
        {
            if (_hub.GameState == null) return;
            _hub.GameState.stamina = Stamina; // S-081 ① — 씬 간 영속
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
            // S-072 ⑧ — UI 위 클릭(가방 뒤로가기 등)이 던지기로 새던 버그: 포인터가 UI에 있으면 무시.
            // S-074 ⑥ — 폰이 열려 있어도 클릭 지점이 폰 UI 밖(월드)이면 던지기·마시기 허용.
            bool overUI = UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            bool leftClick = mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI;
            bool rightClick = mouse != null && mouse.rightButton.wasPressedThisFrame && !overUI;

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
                // S-088 ④ — 구 온도 드레인 배율(×1.35)은 상한 차감 모델로 대체.
                if (DrinkBuffActive) drain *= 0.85f; // S-074 ⑧ — 드링크 버프
                Stamina -= drain * Time.deltaTime;
            }
            else
            {
                Stamina += tuning.staminaRecoverPerSecond * Time.deltaTime;
            }

            // S-088 ④ — 패널티 상한: 활성 패널티 합만큼 사용 가능 최대가 깎인다.
            TickPenalties();
            Stamina = Mathf.Clamp(Stamina, 0f, EffectiveStaminaMax);
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
            // S-073 ④ — 마감 라벨은 '들고 있을 때'만: 손을 떠나면 라벨째 제거.
            if (visual.TryGetComponent(out CarryDeadlineLabel label)) Destroy(label);

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
            ApplyRelief("drink");   // S-088 ④ — 손 음료도 더움 해소
            ApplyDrinkBuff();       // S-074 ⑧
            WorldAudioManager.Instance?.PlayDrinkSfx();     // AU-009
            Debug.Log("[드링크] 섭취 — 스태미나 회복 (우클릭)");
        }

        // ── S-088 ④ — 스태미나 패널티 구간: 상한 차감 + 음료 해소 타이머 ──

        private float _heatReliefUntil = -1f;  // 생수·에너지드링크
        private float _coldReliefUntil = -1f;  // 따뜻한 음료
        private StaminaPenalties _lastPenalties;

        public StaminaPenalties CurrentPenalties { get; private set; }
        public float EffectiveStaminaMax
            => Mathf.Max(10f, _hub.Tuning.staminaMax + DrinkBuffBonus - CurrentPenalties.Total);

        private void TickPenalties()
        {
            TuningConfigSO tuning = _hub.Tuning;
            var penalties = new StaminaPenalties
            {
                Heat = _weather == WeatherType.Heat && Time.time >= _heatReliefUntil ? tuning.staminaPenaltyHeat : 0f,
                Cold = _weather == WeatherType.Snow && Time.time >= _coldReliefUntil ? tuning.staminaPenaltyCold : 0f,
                Carry = ((CarriedOrder != null ? 1 : 0) + (CarriedOrder2 != null ? 1 : 0)) * tuning.staminaPenaltyCarryPerBox,
                Storm = _weather == WeatherType.Storm ? tuning.staminaPenaltyStorm : 0f,
            };
            CurrentPenalties = penalties;
            if (!Mathf.Approximately(penalties.Heat, _lastPenalties.Heat)
                || !Mathf.Approximately(penalties.Cold, _lastPenalties.Cold)
                || !Mathf.Approximately(penalties.Carry, _lastPenalties.Carry)
                || !Mathf.Approximately(penalties.Storm, _lastPenalties.Storm))
            {
                _lastPenalties = penalties;
                WorldEvents.RaiseStaminaPenaltyChanged(penalties);
            }
        }

        /// <summary>음료별 패널티 해소 (S-088 ④ — 지속은 튜닝 staminaPenaltyReliefSeconds).</summary>
        private void ApplyRelief(string itemId)
        {
            float until = Time.time + _hub.Tuning.staminaPenaltyReliefSeconds;
            if (itemId == "water" || itemId == "drink") _heatReliefUntil = until;   // 시원한 것 = 더움 해소
            if (itemId == "hot_drink") _coldReliefUntil = until;                    // 따뜻한 것 = 추움 해소
        }

        // ── S-074 ⑧ — 드링크 버프: 이동 +30% · 드레인 -15% (실시간 45초 = 게임 90분) ──
        private const float DRINK_BUFF_SECONDS = 45f;
        private const float DRINK_BUFF_STAMINA_RATIO = 0.1f; // S-097 ③ — 버프 중 총량 +10%
        private float _drinkBuffUntil = -1f;

        private bool DrinkBuffActive => Time.time < _drinkBuffUntil;
        /// <summary>이동속도 배율 — Locomotion이 매 프레임 읽는다.</summary>
        public float SpeedMultiplier => DrinkBuffActive ? 1.3f : 1f;
        /// <summary>버프 중 늘어난 총량 (S-097 ③). 버프가 끝나면 Update의 클램프가 초과분을 걷어낸다.</summary>
        private float DrinkBuffBonus => DrinkBuffActive ? _hub.Tuning.staminaMax * DRINK_BUFF_STAMINA_RATIO : 0f;

        private void ApplyDrinkBuff()
        {
            _drinkBuffUntil = Time.time + DRINK_BUFF_SECONDS;
            // S-097 ③ — 늘어난 총량만큼 즉시 충전: 이 초과분이 HUD에서 파란 fill로 보인다.
            Stamina = Mathf.Clamp(Stamina + _hub.Tuning.staminaMax * DRINK_BUFF_STAMINA_RATIO, 0f, EffectiveStaminaMax);
            NotifyStamina(force: true);
            Debug.Log("[드링크] 버프 — 이동 +30% · 소모 -15% · 총량 +10% (" + DRINK_BUFF_SECONDS + "초)");
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
            DeliveryOrderSO labelOrder; // S-073 ④ — 이 비주얼이 표현하는 주문 (직전 TryCarry 결과)
            if (_fillSecondSlot) // S-055 — 2번 슬롯은 머리 위
            {
                _carriedVisual2 = visual;
                visual.localPosition = new Vector3(0f, 0.62f, 0f);
                _fillSecondSlot = false;
                labelOrder = CarriedOrder2;
            }
            else
            {
                _carriedVisual = visual;
                visual.localPosition = Vector3.zero;
                labelOrder = CarriedOrder;
            }

            // S-073 ④ — 든 상자 위 마감 카운트다운. 드롭 시 함께 제거된다.
            if (labelOrder != null && !visual.TryGetComponent(out CarryDeadlineLabel _))
                visual.gameObject.AddComponent<CarryDeadlineLabel>()
                    .Init(labelOrder, _hub.GameState, _overlayFont);
        }

        // S-082 ② — 지각 강제 하차 폐지: 지각해도 손의 짐은 유지 — 끝까지 배달할 수 있다
        // (S-075 ② 지각 배달 완화의 완성. 구 동작: 실패 이벤트가 손에서 짐을 앗아갔다).
        private void OnDeliveryFailed(DeliveryData data) { }

        public void RecoverStamina(float amount)
        {
            Stamina = Mathf.Clamp(Stamina + amount, 0f, EffectiveStaminaMax); // S-088 ④ — 패널티 상한
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
