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
        public float StaminaNormalized => Mathf.Clamp01(Stamina / _hub.Tuning.staminaMax);
        public DeliveryOrderSO CarriedOrder { get; private set; }
        public bool IsCarrying => CarriedOrder != null;

        /// <summary>
        /// S-147 — 좌클릭을 던지기로 소비한 프레임 번호. InteractionSensor가 같은 프레임에
        /// 송장을 띄우지 않도록 보는 표식이다(둘이 같은 마우스 입력을 각자 읽는 구조라 필요).
        /// </summary>
        public int LeftClickConsumedFrame { get; private set; } = -1;
        /// <summary>손에 든 음료 여부 (S-071 ② — 송장 좌클릭이 음료 던지기와 충돌하지 않게 센서가 참조).</summary>
        public bool IsHoldingDrink => _heldDrink != null;

        // S-055 — 두 개 들기: 2번 슬롯은 머리 위에 쌓인다.
        // S-134 ② — 상한 기준을 **레벨**로 교체(종전 누적 성공 5건). 표는 LevelPerks 한 곳.
        public DeliveryOrderSO CarriedOrder2 { get; private set; }
        public DeliveryOrderSO CarriedOrder3 { get; private set; }

        /// <summary>지금 들 수 있는 최대 개수 (Lv1=1 · Lv2=2 · Lv3+=3).</summary>
        public int CarryCapacity =>
            _hub.GameState != null ? LevelPerks.CarryCapacity(_hub.GameState.playerLevel) : 1;

        /// <summary>지금 손에 든 개수.</summary>
        public int CarryCount =>
            (CarriedOrder != null ? 1 : 0) + (CarriedOrder2 != null ? 1 : 0) + (CarriedOrder3 != null ? 1 : 0);

        public bool CanDoubleCarry => CarryCapacity >= 2;
        public bool CarryFull => CarryCount >= CarryCapacity;
        private Transform _carriedVisual2;
        private Transform _carriedVisual3;
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
            WorldEvents.DialogueStarted += OnDialogueStartedStatus; // S-123 ③
            WorldEvents.DialogueEnded += OnDialogueEndedStatus;
        }
        private void OnDisable()
        {
            WorldEvents.DeliveryFailed -= OnDeliveryFailed;
            WorldEvents.SceneTransitionCompleted -= OnSceneArrivedStatus;
            WorldEvents.WeatherChanged -= OnWeatherChangedStatus;
            WorldEvents.BagHoldRequested -= OnBagHoldRequested;
            WorldEvents.BagItemConsumed -= OnBagItemConsumed;
            WorldEvents.SceneTransitionStarted -= OnSceneLeaving;
            WorldEvents.DialogueStarted -= OnDialogueStartedStatus;
            WorldEvents.DialogueEnded -= OnDialogueEndedStatus;
        }

        private void OnWeatherChangedStatus(WeatherType weather) => _weather = weather;

        // S-123 ③ — 대사 진행 좌클릭이 상자 던지기로 새지 않게 (대화 중 손 조작 정지).
        private bool _inDialogue;
        private float _dialogueEndedAt = -99f;
        private void OnDialogueStartedStatus(string _) => _inDialogue = true;

        private void OnDialogueEndedStatus(string _)
        {
            _inDialogue = false;
            // S-153 — 대화를 **끝낸 그 클릭**이 월드 클릭으로 새는 것을 막는다.
            // 마지막 클릭에 DialogueEnded가 먼저 발화하면 `_inDialogue`가 이미 false라,
            // 같은 프레임의 `wasPressedThisFrame`이 그대로 통과해 상자를 던져 버렸다
            // (남규님 지적 "대화 마지막 끝낼때 클릭하면 상자를 바닥에 던져버려").
            // 이벤트 발화 순서가 컴포넌트마다 달라 프레임 비교만으론 불안정하므로 짧은 시간창을 쓴다.
            _dialogueEndedAt = Time.unscaledTime;
        }

        /// <summary>
        /// S-153 — 대화가 좌/우클릭을 먹고 있는 동안인가. 대화 중이거나 방금 끝난 직후를 포함한다.
        /// 던지기(여기)와 송장(InteractionSensor)이 같은 판정을 봐야 새는 구멍이 안 생긴다.
        /// </summary>
        public bool DialogueBlocksClick =>
            _inDialogue || Time.unscaledTime - _dialogueEndedAt < DIALOGUE_CLICK_GRACE;

        private const float DIALOGUE_CLICK_GRACE = 0.18f;

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
            if (CarriedOrder3 != null) _hub.GameState.carriedOrders.Add(CarriedOrder3); // S-134 ②
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
            Debug.Log("[운반] 들고 온 짐 " + CarryCount + "건 복원");
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
            TickCarryShake(); // S-133 ⑥

            var mouse = UnityEngine.InputSystem.Mouse.current;
            // S-072 ⑧ — UI 위 클릭(가방 뒤로가기 등)이 던지기로 새던 버그: 포인터가 UI에 있으면 무시.
            // S-074 ⑥ — 폰이 열려 있어도 클릭 지점이 폰 UI 밖(월드)이면 던지기·마시기 허용.
            bool overUI = UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            // S-123 ③ — 대화 중엔 손 조작 정지: 대사 진행용 좌클릭이 상자 던지기로 새면 안 된다.
            // S-153 — `!_inDialogue`가 아니라 `!DialogueBlocksClick`을 본다(대화를 끝낸 클릭까지 차단).
            bool leftClick = mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI && !DialogueBlocksClick;
            bool rightClick = mouse != null && mouse.rightButton.wasPressedThisFrame && !overUI && !DialogueBlocksClick;

            // S-032 ④: 우클릭 = 드링크 마시기 · 좌클릭 = 던지기(상자 우선, 없으면 드링크 — 택배와 동일 감각).
            if (rightClick && _heldDrink != null)
                ConsumeHeldDrink();
            // S-147 — 던지기가 좌클릭을 **소비했다는 사실을 프레임에 남긴다**.
            // 종전엔 이 블록이 IsCarrying을 끈 뒤 InteractionSensor가 같은 프레임에 `!IsCarrying`을
            // 보고 송장을 띄웠다 — 상자를 던졌는데 송장까지 뜨는 것(남규님 지적). 두 컴포넌트가
            // 같은 입력을 각자 읽는 구조라 조건만으론 못 막는다. 센서는 이 표식을 보고 물러난다.
            if (leftClick && IsCarrying)
            {
                ThrowCarryTowardsMouse(tuning.throwSpeed); // 던지기 (S-016 ⑦)
                LeftClickConsumedFrame = Time.frameCount;
            }
            else if (leftClick && _heldDrink != null)
            {
                ThrowHeldDrink(tuning.throwSpeed);
                LeftClickConsumedFrame = Time.frameCount;
            }

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
                Stamina -= DrainBuffPoolFirst(drain * Time.deltaTime); // S-098 ② — 파란 풀부터 소모
            }
            else
            {
                Stamina += tuning.staminaRecoverPerSecond * Time.deltaTime; // 회복은 본 스태미나만 (버프 풀은 음용 전용)
            }

            if (!DrinkBuffActive && _drinkBuffPool > 0f) // S-098 ② — 버프 만료 시 잔여 풀 소멸
            {
                _drinkBuffPool = 0f;
                NotifyBuffStamina(force: true);
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
            if (CarryCapacity >= 2 && CarriedOrder2 == null) // S-055 두 개 들기
            {
                CarriedOrder2 = order;
                _fillSecondSlot = true;
                Debug.Log("[숙련] 두 개 들기 — 상자를 하나 더 얹었다");
                return true;
            }
            // S-134 ② / S-133 ⑥ — 3번째는 Lv3부터. 맨 위라 흔들리고 떨어진다.
            if (CarryCapacity >= 3 && CarriedOrder3 == null)
            {
                CarriedOrder3 = order;
                _fillSecondSlot = false;
                Debug.Log("[숙련] 세 개 들기 — 맨 위가 위태롭다");
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

                // S-134 ② — 3번 슬롯도 한 칸 내려온다.
                if (CarriedOrder3 != null)
                {
                    CarriedOrder2 = CarriedOrder3;
                    CarriedOrder3 = null;
                    _carriedVisual2 = _carriedVisual3;
                    _carriedVisual3 = null;
                    if (_carriedVisual2 != null) _carriedVisual2.localPosition = new Vector3(0f, 0.62f, 0f);
                }
            }

            WorldEvents.RaiseCarryStateChanged(IsCarrying);
            // S-133 ① — 손에서 놓았음을 알린다. 목적지 패드가 이걸로 하이라이트를 끈다.
            if (released != null) WorldEvents.RaisePackageReleased(DeliveryData.From(released));
            return released;
        }

        /// <summary>
        /// 든 물건을 손에서 놓아 물리로 떨어뜨린다. S-017: PickupBox를 살려 두므로 **다시 주울 수 있고**,
        /// 굴러가 비콘 패드에 닿으면 DeliveryPoint 트리거가 배송으로 인증한다(던져 넣기).
        /// </summary>
        // ── S-133 ⑥ 캐리 흔들림 (남규님: 1~2개는 효과만 · 3번째는 실제로 떨어진다) ──
        // 설계 의도: Lv3 해금이 **순수 이득이 아니라 거래**가 된다 — 많이 들고 조심히 걷거나,
        // 적게 들고 뛰거나. 마감 압박이 있는 게임이라 이 선택이 매 배송마다 살아난다.
        private const float SWAY_BASE = 0.035f;      // 1~2번 슬롯 기본 흔들림(시각 전용)
        private const float SWAY_SPEED = 8.5f;
        private const float TOP_DROP_AT = 1f;        // 불안정도가 여기 닿으면 떨어진다
        private const float TOP_GAIN_RUN = 0.62f;    // 달리면 초당 — 약 1.6초면 떨어진다
        private const float TOP_GAIN_WALK = 0.10f;   // 걸으면 초당 — 10초는 버틴다
        private const float TOP_GAIN_LAND = 0.5f;    // 착지 한 방 — 점프 두 번이면 위험
        private const float TOP_SETTLE = 0.9f;       // 멈춰 서면 초당 가라앉는다
        private float _topInstability;
        private bool _wasGrounded = true;

        private void TickCarryShake()
        {
            float t = Time.time;
            // 1·2번 슬롯 — 시각 효과만. 떨어지지 않는다.
            if (_carriedVisual2 != null)
                _carriedVisual2.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * SWAY_SPEED) * 3.5f);

            // 판정 기준은 **슬롯**이지 비주얼이 아니다. 비주얼로 가드하면 비주얼 생성이 실패했을 때
            // 3번째 상자가 영영 안 떨어지고 불안정도가 계속 0으로 리셋된다(실측에서 드러남).
            if (CarriedOrder3 == null)
            {
                _topInstability = 0f;
                _wasGrounded = true;
                return;
            }

            PlayerLocomotionManager loco = _hub.Locomotion;
            float speed = loco != null ? loco.PlanarVelocity.magnitude : 0f;
            bool grounded = loco == null || loco.IsGrounded;

            float gain = speed > _hub.Tuning.moveSpeed * 1.05f ? TOP_GAIN_RUN
                : speed > 0.1f ? TOP_GAIN_WALK
                : -TOP_SETTLE;
            _topInstability = Mathf.Max(0f, _topInstability + gain * Time.deltaTime);
            if (grounded && !_wasGrounded) _topInstability += TOP_GAIN_LAND; // 착지 충격
            _wasGrounded = grounded;

            if (_carriedVisual3 != null)
            {
                float sway = SWAY_BASE * (0.5f + _topInstability * 1.8f);
                _carriedVisual3.localPosition = new Vector3(
                    Mathf.Sin(t * SWAY_SPEED) * sway, 1.24f, Mathf.Cos(t * SWAY_SPEED * 0.7f) * sway * 0.6f);
                _carriedVisual3.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * SWAY_SPEED) * sway * 220f);
            }

            if (_topInstability >= TOP_DROP_AT) DropTopBox();
        }

        /// <summary>맨 위(3번째) 상자만 떨어뜨린다. 낙하 상자는 기존 파손 판정을 그대로 탄다.</summary>
        private void DropTopBox()
        {
            DeliveryOrderSO dropped = CarriedOrder3;
            CarriedOrder3 = null;
            _topInstability = 0f;
            if (_carriedVisual3 != null)
            {
                DropVisualAsPhysics(_carriedVisual3);
                _carriedVisual3 = null;
            }
            Debug.Log("[운반] 맨 위 상자가 떨어졌다 — " + (dropped != null ? dropped.address : "?"));
            WorldEvents.RaiseCarryStateChanged(IsCarrying);
            if (dropped != null) WorldEvents.RaisePackageReleased(DeliveryData.From(dropped));
        }

        private void DropVisualAsPhysics(Transform visual)
        {
            // S-073 ④ — 마감 라벨은 '들고 있을 때'만: 손을 떠나면 라벨째 제거.
            if (visual.TryGetComponent(out CarryDeadlineLabel label)) Destroy(label);

            DetachKeepingWorldPose(visual);

            if (visual.TryGetComponent(out Collider collider))
            {
                collider.enabled = true;
                collider.isTrigger = false;
            }

            if (visual.TryGetComponent(out Rigidbody body)) body.isKinematic = false;
            else visual.gameObject.AddComponent<Rigidbody>();
        }

        /// <summary>
        /// S-200 — 손에서 뗄 때 <b>월드 포즈만</b> 유지하고 로컬 스케일은 손대지 않는다.
        ///
        /// 종전의 `SetParent(null, worldPositionStays: true)`는 월드 스케일을 지키려고
        /// 앵커 스케일(GreyboxStageBuilder.CarryAnchorScale = 0.5/0.6/0.6)을
        /// 로컬 스케일에 눌러 담았다. 그 상자를 다시 잡으면 앵커 스케일이 한 번 더 곱해지므로
        /// 던지고 잡기를 반복할수록 계속 작아졌다(2회 = 0.25/0.36/0.36배).
        /// 로컬 스케일을 그대로 두면 바닥 상자는 언제나 원래 크기, 든 상자는 언제나 앵커 배율이다.
        /// </summary>
        private static void DetachKeepingWorldPose(Transform visual)
        {
            visual.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
            visual.SetParent(null, worldPositionStays: false);
            visual.SetPositionAndRotation(position, rotation);
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
        // S-134 ② — Lv6 스태미나 해금 (+20%). 페널티는 그 위에서 깎인다.
        public float EffectiveStaminaMax => Mathf.Max(10f,
            _hub.Tuning.staminaMax * (_hub.GameState != null
                ? LevelPerks.StaminaMaxMultiplier(_hub.GameState.playerLevel) : 1f)
            - CurrentPenalties.Total);

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
        // S-098 ② — 버프 스태미나는 별도 풀: 음용 즉시 10% 만충으로 생기고(스태미나 잔량 무관),
        //           소모 시 풀부터 줄어든다. S-097의 "총량 초과분" 모델은 만충 근처가 아니면
        //           파란 fill이 안 보여 폐기.
        private const float DRINK_BUFF_SECONDS = 45f;
        private const float DRINK_BUFF_STAMINA_RATIO = 0.1f;
        private float _drinkBuffUntil = -1f;
        private float _drinkBuffPool;
        private float _lastNotifiedBuff = -1f;

        private bool DrinkBuffActive => Time.time < _drinkBuffUntil;
        /// <summary>이동속도 배율 — Locomotion이 매 프레임 읽는다.</summary>
        public float SpeedMultiplier => DrinkBuffActive ? 1.3f : 1f;

        private void ApplyDrinkBuff()
        {
            _drinkBuffUntil = Time.time + DRINK_BUFF_SECONDS;
            _drinkBuffPool = _hub.Tuning.staminaMax * DRINK_BUFF_STAMINA_RATIO;
            NotifyBuffStamina(force: true);
            Debug.Log("[드링크] 버프 — 이동 +30% · 소모 -15% · 버프 스태미나 +10% (" + DRINK_BUFF_SECONDS + "초)");
        }

        /// <summary>버프 풀부터 깎고 남은 소모량을 돌려준다 (S-098 ②).</summary>
        private float DrainBuffPoolFirst(float amount)
        {
            if (_drinkBuffPool <= 0f) return amount;
            float fromPool = Mathf.Min(_drinkBuffPool, amount);
            _drinkBuffPool -= fromPool;
            NotifyBuffStamina(force: false);
            return amount - fromPool;
        }

        private void NotifyBuffStamina(bool force)
        {
            float normalized = Mathf.Max(0f, _drinkBuffPool / _hub.Tuning.staminaMax);
            if (!force && Mathf.Abs(normalized - _lastNotifiedBuff) < STAMINA_NOTIFY_STEP) return;
            _lastNotifiedBuff = normalized;
            WorldEvents.RaiseBuffStaminaChanged(normalized);
        }

        /// <summary>S-032 ④: 든 드링크를 마우스 방향으로 던진다 — 다시 픽업체가 되어 E로 회수 가능.</summary>
        private void ThrowHeldDrink(float speed)
        {
            Camera camera = Camera.main;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (camera == null || mouse == null) return;

            Transform drink = _heldDrink;
            _heldDrink = null;
            DetachKeepingWorldPose(drink); // S-200 — 드링크도 같은 결함(던지고 줍기를 반복하면 작아졌다)

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

        /// <summary>
        /// S-196 — 든 상자를 앵커보다 살짝 **위로** 띄운다.
        ///
        /// 남규님이 플레이 중 맞춘 값을 옮긴 것이다. 인스펙터에선 상자 안쪽 아트 노드를
        /// (`prop_box_parcel(Clone)`) y 0.274 → 0.462로 올렸는데, 그 노드는 팩토리가 만드는
        /// `Prefabs/Auto` 프리팹 안에 있어 재임포트로 덮인다. 게다가 상자 조립 코드가 매번
        /// **바닥을 루트 원점에 정렬**하므로 프리팹 쪽 y를 고쳐도 그 자리에서 상쇄된다.
        /// 그래서 같은 결과를 상자 루트를 띄우는 것으로 낸다 —
        /// 0.1882(안쪽 노드 이동량) × 1.1667(Visual 정규화 배율) = 0.2196u, 월드 결과가 동일하다.
        /// </summary>
        private const float CARRY_ART_LIFT = 0.2196f;

        /// <summary>든 물건의 겉모습을 캐리 앵커에 붙인다. 내려놓을 때 함께 사라진다.</summary>
        public void AttachCarried(Transform visual)
        {
            visual.SetParent(_carryAnchor, false);
            visual.localRotation = Quaternion.identity;
            DeliveryOrderSO labelOrder; // S-073 ④ — 이 비주얼이 표현하는 주문 (직전 TryCarry 결과)
            if (_fillSecondSlot) // S-055 — 2번 슬롯은 머리 위
            {
                _carriedVisual2 = visual;
                visual.localPosition = new Vector3(0f, CARRY_ART_LIFT + 0.62f, 0f);
                _fillSecondSlot = false;
                labelOrder = CarriedOrder2;
            }
            else if (CarriedOrder3 != null && _carriedVisual3 == null) // S-134 ② — 3번 슬롯은 그 위
            {
                _carriedVisual3 = visual;
                visual.localPosition = new Vector3(0f, CARRY_ART_LIFT + 1.24f, 0f);
                labelOrder = CarriedOrder3;
            }
            else
            {
                _carriedVisual = visual;
                visual.localPosition = new Vector3(0f, CARRY_ART_LIFT, 0f);
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
