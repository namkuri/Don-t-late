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

        [Header("믹스")]
        [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.7f;
        // Evening 진입 시 낮→밤 전환에 쓰는 교차 시간(초).
        [SerializeField] private float _crossfadeSeconds = 3f;

        // 직전 세션에 뽑힌 곡을 기억해 연속 중복을 피한다(no-repeat).
        private const string PREF_LAST = "DontLate.Bgm.Last.";

        private readonly Dictionary<BgmSlot, List<AudioClip>> _pools =
            new Dictionary<BgmSlot, List<AudioClip>>();
        // 세션 추첨 결과 — 세션 내내 고정.
        private readonly Dictionary<BgmSlot, AudioClip> _picked =
            new Dictionary<BgmSlot, AudioClip>();

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _active;
        private AudioSource _sfxSource;
        private Coroutine _fade;

        private BgmSlot _slot = BgmSlot.Unsorted;
        // 씬 전이 통지가 없는 무대(그레이박스)에서도 낮/밤이 정상 동작하도록 "타이틀인가"만 들고 있는다.
        private bool _titleScene;
        private DayPhase _phase;
        // S-009: BGM은 첫 대화(Home 인트로 전화)가 끝난 뒤에야 시작한다.
        [Tooltip("켜면 첫 DialogueEnded까지 BGM을 보류한다 (Home 인트로 연출).")]
        [SerializeField] private bool _holdUntilFirstDialogue = true;
        private bool _bgmReleased;

        public AudioClip CurrentClip => _active != null ? _active.clip : null;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            _sourceA = CreateSource();
            _sourceB = CreateSource();

            _sfxSource = CreateSource(); // 원샷 전용 — BGM 소스와 분리해 페이드에 휘둘리지 않게 한다
            _sfxSource.volume = _sfxVolume;

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
        }

        private void OnDisable()
        {
            WorldEvents.DialogueEnded -= OnDialogueEnded;
            WorldEvents.DayPhaseChanged -= OnDayPhaseChanged;
            WorldEvents.SceneTransitionCompleted -= OnSceneTransitionCompleted;
            WorldEvents.PackagePickedUp -= OnPackagePickedUp;
            WorldEvents.DeliveryCompleted -= OnDeliveryCompleted;
            WorldEvents.DeliveryFailed -= OnDeliveryFailed;
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
        }

        private void OnSceneTransitionCompleted(GameScene scene)
        {
            _titleScene = scene == GameScene.Main;
            ApplySlot();
        }

        // ── SFX ──────────────────────────────────────────────
        // JUICE 표에 이미 있는 3건만 건다. 나머지는 J-1 승인 게이트 대기(BOM §8).

        private void OnPackagePickedUp(DeliveryData data) => PlaySfx(_sfxPickup);
        private void OnDeliveryCompleted(DeliveryData data) => PlaySfx(_sfxDeliveryOk);
        private void OnDeliveryFailed(DeliveryData data) => PlaySfx(_sfxLateBuzzer);

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return; // 음원 미확보 = 무음 (폴백 원칙)
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
            if (_holdUntilFirstDialogue && !_bgmReleased) return; // 인트로 전 무음 (S-009)

            BgmSlot next;
            if (_titleScene) next = BgmSlot.Title;
            else if (_phase == DayPhase.Evening || _phase == DayPhase.Night) next = BgmSlot.Night;
            else next = BgmSlot.Day;

            if (next == _slot) return;
            if (!_picked.TryGetValue(next, out AudioClip clip)) return; // 빈 슬롯이면 현 재생 유지

            _slot = next;
            SyncDebugIndex(next, clip);
            Crossfade(clip);
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
            BgmSlot.Day, BgmSlot.Night, BgmSlot.Title, BgmSlot.Unsorted
        };

        /// <summary>다음 슬롯으로 넘긴다. 빈 슬롯(예: 곡이 컷된 Title)은 건너뛴다.</summary>
        private void DebugToggleSlot()
        {
            int start = System.Array.IndexOf(DebugSlotOrder, _slot);

            for (int step = 1; step <= DebugSlotOrder.Length; step++)
            {
                BgmSlot next = DebugSlotOrder[(start + step + DebugSlotOrder.Length) % DebugSlotOrder.Length];
                if (!_pools.TryGetValue(next, out List<AudioClip> pool) || pool.Count == 0) continue;

                if (!_picked.TryGetValue(next, out AudioClip clip))
                {
                    clip = pool[0];
                    _picked[next] = clip; // Unsorted 는 추첨이 없으므로 첫 곡부터 시작
                }

                _slot = next;
                _debugIndex = Mathf.Max(0, pool.IndexOf(clip));
                Crossfade(clip);
                return;
            }
        }

        private void OnGUI()
        {
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
