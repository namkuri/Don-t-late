using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace DontLate
{
    /// <summary>
    /// BGM 재생 — 슬롯(Day/Night/Title)별 풀에서 세션 시작 시 1곡씩 추첨해 고정하고,
    /// 시간대·씬 변화에 따라 크로스페이드로 갈아탄다 (D-039).
    /// Core 씬 상주. SFX·믹서는 이 매니저의 책임이 아니다(음원 확보 후 별도 — YAGNI).
    /// </summary>
    public class WorldAudioManager : MonoBehaviour
    {
        public static WorldAudioManager Instance { get; private set; }

        [Header("데이터")]
        [SerializeField] private BgmLibrarySO _library;

        [Header("SFX — 실음원이 오면 같은 파일명으로 교체된다(BOM §8 스왑 계약)")]
        [SerializeField] private AudioClip _sfxPickup;
        [SerializeField] private AudioClip _sfxDeliveryOk;
        [SerializeField] private AudioClip _sfxLateBuzzer;

        [Header("SFX — 신기능 7종 (AU-008)")]
        [SerializeField] private AudioClip _sfxBoxBreak;
        [SerializeField] private AudioClip _sfxBarcode;
        [SerializeField] private AudioClip _sfxPenalty;
        [SerializeField] private AudioClip _sfxVending;
        [SerializeField] private AudioClip _sfxThrow;
        [SerializeField] private AudioClip _sfxCoin;
        [SerializeField] private AudioClip _sfxPhone;
        // AU-025 (S-162) — 튜토리얼 한 단계 성공. 9단계 내내 반복되므로 짧고 절제된 톤.
        [SerializeField] private AudioClip _sfxTutorialStep;
        [Tooltip("AU-027 — 레벨업. 비면 무음 폴백(파일 도착 전에도 안전).")]
        [SerializeField] private AudioClip _sfxLevelUp;

        [Header("SFX — 잔여 배선 8종 (AU-009)")]
        [SerializeField] private AudioClip _sfxDeadlineWarn;
        [SerializeField] private AudioClip _sfxPhoneRing;
        [SerializeField] private AudioClip _sfxRhythmHit;
        [SerializeField] private AudioClip _sfxRhythmMiss;
        [SerializeField] private AudioClip _sfxSceneWhoosh;
        [SerializeField] private AudioClip _sfxFootstep;
        [SerializeField] private AudioClip _sfxDrink;
        [SerializeField] private AudioClip _ambNight;
        [SerializeField, Range(0f, 1f)] private float _ambVolume = 0.35f; // 배경 앰비언스 — SFX보다 낮게

        [Header("SFX — 신규 기능 갭 4종 (AU-010)")]
        [SerializeField] private AudioClip _sfxSettleOk;
        [SerializeField] private AudioClip _sfxFanfare;   // S-086 — bom_id: sfx_fanfare (AU-021)
        [SerializeField] private AudioClip _sfxThunder;   // S-088 ⑥ — bom_id: sfx_thunder (AU-022)
        [SerializeField] private AudioClip _sfxSettleBad;
        [SerializeField] private AudioClip _sfxFurniturePlace;
        [SerializeField] private AudioClip _sfxUiTick;

        [Header("구역 앰비언스 2종 + 지도 앱 3종 (AU-011)")]
        [SerializeField] private AudioClip _ambVillatown;
        [SerializeField] private AudioClip _ambFoodalley;
        [SerializeField] private AudioClip _sfxMapPin;
        [SerializeField] private AudioClip _sfxMapRoute;
        [SerializeField] private AudioClip _sfxMapDepart;
        [Tooltip("구역 앰비언스 선택용 — currentDistrict 조회 (AU-011).")]
        [SerializeField] private GameStateSO _gameState;

        [Header("SFX — 배송지 도착 차임 (AU-018 ④)")]
        [SerializeField] private AudioClip _sfxArrive;

        [Header("SFX — 교통사고 (S-066 ③ · AU-020)")]
        [SerializeField] private AudioClip _sfxCarCrash; // 끼익!! 쿵! — 클립 도착 전엔 무음 소켓

        [Header("SFX — 액션 4종 (AU-018 ③)")]
        [SerializeField] private AudioClip _sfxBoxDamage;   // 상자 HP 닳음(미파손)
        [SerializeField] private AudioClip _sfxJump;
        [SerializeField] private AudioClip _sfxLand;
        [SerializeField] private AudioClip _sfxFootstepSnow; // 적설 시 발소리 스왑

        [Header("날씨 앰비언스 3종 (AU-018 ①) — Rain·Snow·Heat 루프 베드")]
        [SerializeField] private AudioClip _ambWeatherRain;
        [SerializeField] private AudioClip _ambWeatherSnow;
        [SerializeField] private AudioClip _ambWeatherHeat;

        [Header("날씨 BGM 4종 (AU-018 ②) — 시간대 슬롯을 덮어쓰는 무드 곡")]
        [SerializeField] private AudioClip _bgmRain;
        [SerializeField] private AudioClip _bgmSnow;
        [SerializeField] private AudioClip _bgmHeat;
        [SerializeField] private AudioClip _bgmFog;

        [Header("믹스")]
        [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.7f;
        // Evening 진입 시 낮→밤 전환에 쓰는 교차 시간(초).
        [SerializeField] private float _crossfadeSeconds = 3f;

        // 직전 세션에 뽑힌 곡을 기억해 연속 중복을 피한다(no-repeat).
        private const string PREF_LAST = "DontLate.Bgm.Last.";

        private readonly Dictionary<BgmSlot, List<AudioClip>> _pools =
            new Dictionary<BgmSlot, List<AudioClip>>();
        // 슬롯별 현재 곡. 세션 추첨으로 시작해 플레이리스트가 갱신한다.
        private readonly Dictionary<BgmSlot, AudioClip> _picked =
            new Dictionary<BgmSlot, AudioClip>();
        // 이번 세션에 이미 들어가 본 슬롯. 첫 진입은 추첨분을 그대로 쓰고(no-repeat 보존),
        // 재진입부터 다음 곡으로 넘긴다 — 낮↔밤을 오갈 때마다 새 곡이 나오도록.
        private readonly HashSet<BgmSlot> _entered = new HashSet<BgmSlot>();

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _active;
        private AudioSource _sfxSource;
        private AudioSource _ambSource; // amb_night 루프 전용 (AU-009)
        private Coroutine _fade;

        private BgmSlot _slot = BgmSlot.Unsorted;
        // 씬 전이 통지가 없는 무대(그레이박스)에서도 낮/밤이 정상 동작하도록 "타이틀인가"만 들고 있는다.
        private bool _titleScene;
        private bool _inDistrict; // AU-011 — 구역 앰비언스는 District 체류 중에만
        private bool _snowCover;  // AU-018 ③ — 적설 발소리 스왑 (SnowCoverChanged 캐시)
        private WeatherType _weather = WeatherType.Clear; // AU-018 ① — 날씨 앰비언스 선택
        private AudioClip _weatherBgm;   // AU-018 ② — 현재 날씨 override BGM (null=없음). 시간대 슬롯보다 우선
        private bool _weatherBgmActive;  // PlaylistTick 셀프 루프 분기용
        private DayPhase _phase;
        // S-009: BGM은 첫 대화(Home 인트로 전화)가 끝난 뒤에야 시작한다.
        [Tooltip("켜면 첫 DialogueEnded까지 BGM을 보류한다 (Home 인트로 연출).")]
        [SerializeField] private bool _holdUntilFirstDialogue = true;
        private bool _bgmReleased;

        public AudioClip CurrentClip => _active != null ? _active.clip : null;

        // ── 폰 음악앱 API (S-019 ⑤ — View가 Instance 명령으로 호출) ──
        public bool IsPaused { get; private set; }
        public float Volume => _volume;
        public BgmSlot CurrentSlot => _slot;

        public void TogglePause()
        {
            if (_active == null) return;
            if (IsPaused) _active.UnPause();
            else _active.Pause();
            IsPaused = !IsPaused;
        }

        public void SetVolume(float value)
        {
            _volume = Mathf.Clamp01(value);
            if (_active != null) _active.volume = _volume;
        }

        // S-065 설정 팝업 — SFX 볼륨.
        public float SfxVolume => _sfxVolume;

        public void SetSfxVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            if (_sfxSource != null) _sfxSource.volume = _sfxVolume;
            // S-068 ② — 앰비언스(빗소리 등)도 효과음 슬라이더를 따른다 (기본 0.7 기준 정규화).
            if (_ambSource != null) _ambSource.volume = _ambVolume * (_sfxVolume / 0.7f);
        }

        /// <summary>현재 슬롯 풀의 다음 곡으로 즉시 전환.</summary>
        public void NextTrack()
        {
            if (_slot == BgmSlot.Unsorted) return;
            _entered.Add(_slot); // 재진입 규칙 재사용 — 다음 곡 선택
            AudioClip clip = SelectForSlot(_slot);
            if (clip == null) return;
            SyncDebugIndex(_slot, clip);
            Crossfade(clip);
        }

        /// <summary>현재 슬롯 풀의 곡 이름 목록 (곡선택 UI용).</summary>
        public List<string> TrackNames()
        {
            var names = new List<string>();
            if (_pools.TryGetValue(_slot, out List<AudioClip> pool))
                foreach (AudioClip clip in pool) names.Add(clip.name);
            return names;
        }

        /// <summary>풀 인덱스로 곡 직접 선택.</summary>
        public void PlayTrackAt(int index)
        {
            if (!_pools.TryGetValue(_slot, out List<AudioClip> pool)) return;
            if (index < 0 || index >= pool.Count) return;
            _picked[_slot] = pool[index];
            SyncDebugIndex(_slot, pool[index]);
            Crossfade(pool[index]);
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            _sourceA = CreateSource();
            _sourceB = CreateSource();

            _sfxSource = CreateSource(); // 원샷 전용 — BGM 소스와 분리해 페이드에 휘둘리지 않게 한다
            _sfxSource.volume = _sfxVolume;

            _ambSource = CreateSource(); // amb_night 루프 전용 (AU-009)
            _ambSource.loop = true;
            _ambSource.volume = _ambVolume;

            BuildPools();
            PickForSession();
        }

        private void OnEnable()
        {
            WorldEvents.DialogueEnded += OnDialogueEnded;
            WorldEvents.DayPhaseChanged += OnDayPhaseChanged;
            WorldEvents.SceneTransitionCompleted += OnSceneTransitionCompleted;
            WorldEvents.PackagePickedUp += OnPackagePickedUp;
            WorldEvents.DeliveryCompleted += OnDeliveryCompleted;
            WorldEvents.DeliveryFailed += OnDeliveryFailed;
            WorldEvents.PackageDestroyed += OnPackageDestroyed;
            WorldEvents.BarcodeScanned += OnBarcodeScanned;
            WorldEvents.DebtIncreased += OnDebtIncreased;
            WorldEvents.MoneySpent += OnMoneySpent;
            WorldEvents.DeadlineWarned += OnDeadlineWarned;
            WorldEvents.PhoneRang += OnPhoneRang;
            WorldEvents.SceneTransitionStarted += OnSceneTransitionStarted;
            WorldEvents.SnowCoverChanged += OnSnowCoverChanged; // AU-018 ③
            WorldEvents.WeatherChanged += OnWeatherChanged;     // AU-018 ①
            WorldEvents.EndingStarted += OnEndingStarted;       // S-107 ①
            WorldEvents.PlayerLeveledUp += OnPlayerLeveledUp;   // AU-027
        }

        private void OnDisable()
        {
            WorldEvents.DialogueEnded -= OnDialogueEnded;
            WorldEvents.EndingStarted -= OnEndingStarted;
            WorldEvents.DayPhaseChanged -= OnDayPhaseChanged;
            WorldEvents.SceneTransitionCompleted -= OnSceneTransitionCompleted;
            WorldEvents.PackagePickedUp -= OnPackagePickedUp;
            WorldEvents.DeliveryCompleted -= OnDeliveryCompleted;
            WorldEvents.DeliveryFailed -= OnDeliveryFailed;
            WorldEvents.PackageDestroyed -= OnPackageDestroyed;
            WorldEvents.BarcodeScanned -= OnBarcodeScanned;
            WorldEvents.DebtIncreased -= OnDebtIncreased;
            WorldEvents.MoneySpent -= OnMoneySpent;
            WorldEvents.DeadlineWarned -= OnDeadlineWarned;
            WorldEvents.PhoneRang -= OnPhoneRang;
            WorldEvents.SceneTransitionStarted -= OnSceneTransitionStarted;
            WorldEvents.SnowCoverChanged -= OnSnowCoverChanged; // AU-018 ③
            WorldEvents.WeatherChanged -= OnWeatherChanged;     // AU-018 ①
            WorldEvents.PlayerLeveledUp -= OnPlayerLeveledUp;   // AU-027
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false; // 플레이리스트가 곡 끝을 잡아 다음 곡으로 넘긴다(D-046)
            source.spatialBlend = 0f; // 2D — 리스너 위치와 무관
            source.volume = 0f;
            return source;
        }

        // ── 풀·추첨 ──────────────────────────────────────────

        private void BuildPools()
        {
            if (_library == null) return;

            foreach (BgmLibrarySO.Entry entry in _library.entries)
            {
                if (entry == null || entry.clip == null) continue;

                // Unsorted 도 풀에는 담는다 — 청취 도구로 훑어봐야 분류를 정할 수 있다.
                // 다만 PickForSession 이 제외하므로 게임 진행 중에는 절대 선택되지 않는다.
                if (!_pools.TryGetValue(entry.slot, out List<AudioClip> pool))
                {
                    pool = new List<AudioClip>();
                    _pools[entry.slot] = pool;
                }
                pool.Add(entry.clip);
            }
        }

        /// <summary>슬롯마다 1곡씩 뽑아 세션 내내 고정한다. 직전 세션 곡은 제외(풀이 1곡이면 무시).</summary>
        private void PickForSession()
        {
            foreach (KeyValuePair<BgmSlot, List<AudioClip>> pair in _pools)
            {
                if (pair.Key == BgmSlot.Unsorted) continue; // 분류 미확정은 추첨 대상이 아니다

                List<AudioClip> pool = pair.Value;
                string key = PREF_LAST + pair.Key;
                string last = PlayerPrefs.GetString(key, string.Empty);

                int index = Random.Range(0, pool.Count);
                if (pool.Count > 1 && pool[index].name == last)
                    index = (index + 1 + Random.Range(0, pool.Count - 1)) % pool.Count;

                _picked[pair.Key] = pool[index];
                PlayerPrefs.SetString(key, pool[index].name);
            }
            PlayerPrefs.Save();

#if UNITY_EDITOR
            var log = new System.Text.StringBuilder("<color=#35e0c8>[BGM]</color> 세션 추첨");
            foreach (KeyValuePair<BgmSlot, AudioClip> pair in _picked)
                log.Append(" · ").Append(pair.Key).Append('=').Append(pair.Value.name);
            Debug.Log(log.ToString());
#endif
        }

        // ── 이벤트 ───────────────────────────────────────────

        private void OnDayPhaseChanged(DayPhase phase)
        {
            _phase = phase;
            ApplySlot();
            UpdateAmbient();
        }

        private void OnEndingStarted()
        {
            _endingActive = true; // S-107 ① — 엔딩 전용 슬롯 최우선 (클립 없으면 기존 곡 유지)
            ApplySlot();
        }

        private bool _endingActive;

        private void OnSceneTransitionCompleted(GameScene scene)
        {
            _titleScene = scene == GameScene.Main;
            if (_titleScene) _endingActive = false; // 타이틀 복귀 = 엔딩 종료
            _inDistrict = scene == GameScene.District; // AU-011
            ApplySlot();
            UpdateAmbient();

            // AU-018 ④ — 배송지(District·아파트·언덕) 진입 시 도착 차임. whoosh(떠남)와 짝.
            if (scene == GameScene.District || scene == GameScene.Apartment || scene == GameScene.Hillside)
                PlaySfx(_sfxArrive);
        }

        /// <summary>앰비언스 루프 채널 (AU-009 → AU-011 → AU-018 ① 확장) —
        /// 우선순위: 날씨(Rain·Snow·Heat) &gt; 구역(빌라촌·먹자골목) &gt; 시간대(저녁·밤 amb_night).
        /// 날씨가 가장 위인 이유 = "날씨 체감"이 이번 발주 목적. Clear·Cloudy·Fog는 날씨 클립이 없어
        /// 자연히 구역/시간대로 폴백(발주 "Clear는 기존 구역 앰비언스 겸용"). 타이틀 씬은 항상 무음.</summary>
        private void UpdateAmbient()
        {
            AudioClip target = null;
            if (!_titleScene)
            {
                target = _weather switch   // AU-018 ① — 체감 날씨가 최우선
                {
                    WeatherType.Rain => _ambWeatherRain,
                    WeatherType.Snow => _ambWeatherSnow,
                    WeatherType.Heat => _ambWeatherHeat,
                    _ => null
                };
                if (target == null && _inDistrict && _gameState != null)
                {
                    if (_gameState.currentDistrict == DeliveryOrderSO.DISTRICT_VILLATOWN) target = _ambVillatown;
                    else if (_gameState.currentDistrict == DeliveryOrderSO.DISTRICT_FOODALLEY) target = _ambFoodalley;
                }
                if (target == null && (_phase == DayPhase.Evening || _phase == DayPhase.Night))
                    target = _ambNight;
            }

            if (target == null) // 음원 미확보 포함 = 무음 (폴백 원칙)
            {
                if (_ambSource.isPlaying) { _ambSource.Stop(); _ambSource.clip = null; }
                return;
            }
            if (_ambSource.clip == target && _ambSource.isPlaying) return;

            _ambSource.clip = target;
            _ambSource.volume = _ambVolume * (_sfxVolume / 0.7f); // S-068 ② — 효과음 슬라이더 연동
            _ambSource.Play();
        }

        // ── SFX ──────────────────────────────────────────────
        // JUICE 표에 이미 있는 3건만 건다. 나머지는 J-1 승인 게이트 대기(BOM §8).

        private void OnPackagePickedUp(DeliveryData data) => PlaySfx(_sfxPickup);
        private void OnDeliveryCompleted(DeliveryData data) => PlaySfx(_sfxDeliveryOk);
        private void OnDeliveryFailed(DeliveryData data) => PlaySfx(_sfxLateBuzzer);
        private void OnPackageDestroyed() => PlaySfx(_sfxBoxBreak);                 // AU-008
        private void OnBarcodeScanned(DeliveryData data) => PlaySfx(_sfxBarcode);   // AU-008
        private void OnDebtIncreased(int amount) => PlaySfx(_sfxPenalty);           // AU-008
        private void OnMoneySpent(int amount) => PlaySfx(_sfxCoin);                 // S-030 ③ 지출 효과음
        private void OnDeadlineWarned(DeliveryData data) => PlaySfx(_sfxDeadlineWarn); // AU-009
        private void OnPhoneRang(PhoneCall call) => PlaySfx(_sfxPhoneRing);            // AU-009
        private void OnSceneTransitionStarted(GameScene scene) => PlaySfx(_sfxSceneWhoosh); // AU-009
        private void OnSnowCoverChanged(bool covered) => _snowCover = covered;              // AU-018 ③
        private void OnWeatherChanged(WeatherType weather) // AU-018 ①(amb) + ②(BGM)
        {
            _weather = weather;
            UpdateAmbient();
            AudioClip wb = weather switch // AU-018 ② — 날씨 무드 BGM (없는 날씨는 시간대 슬롯 유지)
            {
                WeatherType.Rain => _bgmRain,
                WeatherType.Snow => _bgmSnow,
                WeatherType.Heat => _bgmHeat,
                WeatherType.Fog => _bgmFog,
                _ => null
            };
            if (wb != _weatherBgm) { _weatherBgm = wb; ApplySlot(); }
        }

        // 이벤트 없는 지점(자판기·던지기·코인·폰 개폐)의 Instance 명령 API (AU-008).
        // 컴포넌트가 클립을 들지 않게 해 배선을 빌더 한 곳(Core)으로 모은다.
        public void PlayVendingSfx() => PlaySfx(_sfxVending);
        public void PlayThrowSfx() => PlaySfx(_sfxThrow);
        public void PlayCoinSfx() => PlaySfx(_sfxCoin);
        public void PlayPhoneToggleSfx() => PlaySfx(_sfxPhone);

        /// <summary>AU-025 — 튜토리얼 단계 성공. 클립이 없으면 무음(소켓 — 파일 도착 전에도 안전).</summary>
        public void PlayTutorialStepSfx() => PlaySfx(_sfxTutorialStep);

        // AU-027 — 레벨업. 이벤트 구독으로 자동 재생(호출부가 오디오를 몰라도 된다).
        private void OnPlayerLeveledUp(int _) => PlaySfx(_sfxLevelUp);

        // AU-009 — 리듬 판정(노트당 1회)·드링크·발소리(고빈도라 이벤트 금지, PlayThrowSfx 선례).
        public void PlayRhythmHitSfx() => PlaySfx(_sfxRhythmHit);
        public void PlayRhythmMissSfx() => PlaySfx(_sfxRhythmMiss);
        public void PlayDrinkSfx() => PlaySfx(_sfxDrink);
        // AU-018 ③ — 적설(HasSnowCover) 시 스노 크런치로 스왑, 아니면 기본 발소리.
        public void PlayFootstepSfx() => PlaySfx(_snowCover && _sfxFootstepSnow != null ? _sfxFootstepSnow : _sfxFootstep);

        // AU-018 ③ — 액션 3종 (Instance 명령 — 이벤트 없는 지점, PlayThrowSfx 선례).
        public void PlayBoxDamageSfx() => PlaySfx(_sfxBoxDamage);
        public void PlayJumpSfx() => PlaySfx(_sfxJump);
        public void PlayCarCrashSfx() => PlaySfx(_sfxCarCrash); // S-066 ③
        public void PlayLandSfx() => PlaySfx(_sfxLand);

        // AU-010 — 정산 요약(판정 재료가 SettlementView에만 있음)·가구 확정·공용 UI 틱.
        public void PlaySettleOkSfx() => PlaySfx(_sfxSettleOk);
        /// <summary>개척 해금 팡파레 (S-086) — sfx_fanfare 도착 전엔 정산 상행음 폴백.</summary>
        public void PlayFanfareSfx() => PlaySfx(_sfxFanfare != null ? _sfxFanfare : _sfxSettleOk);
        /// <summary>천둥 (S-088 ⑥) — 클립 도착 전엔 무음.</summary>
        public void PlayThunderSfx() => PlaySfx(_sfxThunder);
        public void PlaySettleBadSfx() => PlaySfx(_sfxSettleBad);
        public void PlayFurniturePlaceSfx() => PlaySfx(_sfxFurniturePlace);
        public void PlayUiTickSfx() => PlaySfx(_sfxUiTick);

        // AU-011 — 폰 지도 앱 3종 (핀 탭·경로 표시·출발 확정 — PhoneView가 호출).
        public void PlayMapPinSfx() => PlaySfx(_sfxMapPin);
        public void PlayMapRouteSfx() => PlaySfx(_sfxMapRoute);
        public void PlayMapDepartSfx() => PlaySfx(_sfxMapDepart);

        // AU-010 — 동일 프레임 클립별 1회 가드: 정산 일괄 판정이 DeliveryCompleted/Failed를
        // 같은 프레임에 N회 Raise해 원샷이 N중첩(음량 스파이크)되는 것을 수렴시킨다.
        private readonly Dictionary<AudioClip, int> _lastPlayedFrame = new Dictionary<AudioClip, int>();

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return; // 음원 미확보 = 무음 (폴백 원칙)
            if (_lastPlayedFrame.TryGetValue(clip, out int frame) && frame == Time.frameCount) return;
            _lastPlayedFrame[clip] = Time.frameCount;
            _sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        /// <summary>Main = 타이틀곡. 그 밖에는 Evening·Night = 밤곡, Morning·Day = 낮곡 (D-039).</summary>
        private void OnDialogueEnded(string _)
        {
            if (_bgmReleased) return;
            _bgmReleased = true;
            ApplySlot();
        }

        private void ApplySlot()
        {
            // 타이틀 곡은 시작 화면에서 바로 재생한다. 낮/밤/날씨 곡만 인트로 대화 종료까지 보류하고(S-009),
            // 타이틀을 벗어나 인트로로 들어갈 땐 타이틀 곡이 무음 구간으로 새지 않게 정지한다.
            if (!_titleScene && _holdUntilFirstDialogue && !_bgmReleased)
            {
                StopBgm();
                return;
            }

            // AU-018 ② — 날씨 BGM override: 타이틀 아닐 때 시간대 슬롯보다 우선(무드 지배).
            if (!_titleScene && _weatherBgm != null)
            {
                _weatherBgmActive = true;
                if (_active == null || _active.clip != _weatherBgm)
                {
                    _slot = BgmSlot.Unsorted; // 슬롯 커서 무효화 — 날씨 해제 시 시간대 곡으로 재크로스페이드
                    Crossfade(_weatherBgm);
                }
                return;
            }
            _weatherBgmActive = false;

            BgmSlot next;
            // S-107 ① — 엔딩 중이면 전용 슬롯 최우선 (클립 부재 시 SelectForSlot이 null → 기존 곡 유지 = 소켓만)
            if (_endingActive && SelectForSlot(BgmSlot.Ending) != null)
            {
                if (_slot != BgmSlot.Ending) { _slot = BgmSlot.Ending; Crossfade(SelectForSlot(BgmSlot.Ending)); }
                return;
            }
            if (_titleScene) next = BgmSlot.Title;
            else if (_phase == DayPhase.Evening || _phase == DayPhase.Night) next = BgmSlot.Night;
            else next = BgmSlot.Day;

            if (next == _slot) return;

            AudioClip clip = SelectForSlot(next);
            if (clip == null) return; // 빈 슬롯이면 현 재생 유지

            _slot = next;
            SyncDebugIndex(next, clip);
            Crossfade(clip);
        }

        /// <summary>BGM 즉시 정지. 타이틀 곡이 인트로 무음 구간(S-009)으로 새지 않게 한다.</summary>
        private void StopBgm()
        {
            if (_fade != null) { StopCoroutine(_fade); _fade = null; }
            if (_sourceA != null) { _sourceA.Stop(); _sourceA.clip = null; _sourceA.volume = 0f; }
            if (_sourceB != null) { _sourceB.Stop(); _sourceB.clip = null; _sourceB.volume = 0f; }
            _active = null;
            _slot = BgmSlot.Unsorted; // 재진입 시 다시 크로스페이드하도록 슬롯 커서 초기화
        }

        /// <summary>
        /// 슬롯에 들어갈 때 재생할 곡을 정한다. 첫 진입은 추첨분 그대로, 재진입은 다음 곡 —
        /// 낮↔밤을 오갈 때마다 같은 곡이 반복되지 않게 한다.
        /// </summary>
        private AudioClip SelectForSlot(BgmSlot slot)
        {
            if (!_pools.TryGetValue(slot, out List<AudioClip> pool) || pool.Count == 0) return null;

            if (_entered.Add(slot))
                return _picked.TryGetValue(slot, out AudioClip drawn) ? drawn : pool[0];

            AudioClip current = _picked.TryGetValue(slot, out AudioClip held) ? held : null;
            AudioClip next = pool[(pool.IndexOf(current) + 1) % pool.Count];
            _picked[slot] = next;
            return next;
        }

        /// <summary>청취 도구의 커서를 현재 곡에 맞춘다 — 안 맞추면 첫 N키가 같은 곡을 다시 고른다.</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void SyncDebugIndex(BgmSlot slot, AudioClip clip)
        {
#if UNITY_EDITOR
            if (_pools.TryGetValue(slot, out List<AudioClip> pool))
                _debugIndex = Mathf.Max(0, pool.IndexOf(clip));
#endif
        }

        // ── 재생 ─────────────────────────────────────────────

        private void Update()
        {
#if UNITY_EDITOR
            DebugKeys();
#endif
            PlaylistTick();
        }

        /// <summary>
        /// 곡이 끝나기 전에 같은 슬롯의 다음 곡으로 넘긴다 (D-046 플레이리스트).
        /// 같은 곡을 이어붙이지 않으므로 루프 이음새 문제가 구조적으로 사라진다.
        /// 슬롯에 곡이 1개뿐이면 자기 자신과 교차되어 매끄러운 루프가 된다.
        /// </summary>
        private void PlaylistTick()
        {
            if (_fade != null || _active == null || _active.clip == null || !_active.isPlaying) return;
            if (_active.clip.length - _active.time > _crossfadeSeconds) return;

            // AU-018 ② — 날씨 BGM은 단곡이라 자기 자신과 크로스페이드해 매끄럽게 루프한다.
            if (_weatherBgmActive) { Crossfade(_weatherBgm, allowSame: true); return; }

            if (!_pools.TryGetValue(_slot, out List<AudioClip> pool) || pool.Count == 0) return;

            int index = pool.IndexOf(_active.clip);
            AudioClip next = pool[(index + 1) % pool.Count]; // 못 찾으면(-1) 첫 곡부터

            _picked[_slot] = next; // 슬롯을 떠났다 돌아와도 이어서 재생
            SyncDebugIndex(_slot, next);
            Crossfade(next, allowSame: true);
        }

        private void Crossfade(AudioClip clip, bool allowSame = false)
        {
            if (!allowSame && _active != null && _active.clip == clip) return;

            AudioSource from = _active;
            AudioSource to = _active == _sourceA ? _sourceB : _sourceA;

            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(CrossfadeRoutine(from, to, clip));
        }

        private IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to, AudioClip clip)
        {
            to.clip = clip;
            to.volume = 0f;
            to.Play();
            _active = to;

            float elapsed = 0f;
            while (elapsed < _crossfadeSeconds)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / _crossfadeSeconds);
                to.volume = _volume * k;
                if (from != null) from.volume = _volume * (1f - k);
                yield return null;
            }

            to.volume = _volume;
            if (from != null)
            {
                from.Stop();
                from.clip = null;
                from.volume = 0f;
            }
            _fade = null;
        }

#if UNITY_EDITOR
        // ── 청취·판정 도구 (에디터 전용 — 릴리스 빌드에서 사라진다) ──
        // 곡 컷 판정을 인게임에서 하려면 곡을 넘겨보고 곡명을 볼 수 있어야 한다.
        // 게임 입력 계약(InputAction)에는 넣지 않는다.

        private int _debugIndex;

        private void DebugKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.nKey.wasPressedThisFrame) DebugStepClip();
            if (keyboard.bKey.wasPressedThisFrame) DebugToggleSlot();
        }

        private void DebugStepClip()
        {
            if (!_pools.TryGetValue(_slot, out List<AudioClip> pool) || pool.Count == 0) return;

            _debugIndex = (_debugIndex + 1) % pool.Count;
            _picked[_slot] = pool[_debugIndex];

            AudioSource source = _active != null ? _active : _sourceA;
            source.Stop();
            source.clip = pool[_debugIndex];
            source.volume = _volume;
            source.Play();
            _active = source;
        }

        // 청취 순회 순서. Unsorted 를 포함해야 분류 미정 곡을 들어보고 슬롯을 정할 수 있다.
        private static readonly BgmSlot[] DebugSlotOrder =
        {
            BgmSlot.Day, BgmSlot.Night, BgmSlot.Title, BgmSlot.Ending, BgmSlot.Unsorted
        };

        /// <summary>다음 슬롯으로 넘긴다. 빈 슬롯(예: 곡이 컷된 Title)은 건너뛴다.</summary>
        private void DebugToggleSlot()
        {
            int start = System.Array.IndexOf(DebugSlotOrder, _slot);

            for (int step = 1; step <= DebugSlotOrder.Length; step++)
            {
                BgmSlot next = DebugSlotOrder[(start + step + DebugSlotOrder.Length) % DebugSlotOrder.Length];

                AudioClip clip = SelectForSlot(next); // 재진입이면 다음 곡 — ApplySlot과 같은 규칙
                if (clip == null) continue;           // 빈 슬롯은 건너뛴다

                _slot = next;
                _debugIndex = Mathf.Max(0, _pools[next].IndexOf(clip));
                Crossfade(clip);
                return;
            }
        }

        private void OnGUI()
        {
            if (!DebugOverlays.Visible) return; // S-107 ④ — F1 토글 (촬영용)
            string clipName = CurrentClip != null ? CurrentClip.name : "(없음)";
            int count = _pools.TryGetValue(_slot, out List<AudioClip> pool) ? pool.Count : 0;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.208f, 0.878f, 0.784f) }
            };
            GUI.Label(new Rect(12f, 12f, 900f, 24f),
                $"[BGM {_slot} {_debugIndex + 1}/{count}] {clipName}   (N=다음곡  B=슬롯전환)", style);
        }
#endif
    }
}
