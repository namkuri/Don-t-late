using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DontLate
{
    /// <summary>씬에 배치된 NPC가 두 동작을 일정 시간마다 번갈아 재생한다.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class AlternatingNpcAnimation : MonoBehaviour
    {
        [SerializeField] private GameObject _target;
        [SerializeField] private Avatar _avatar;
        [SerializeField] private AnimationClip _firstClip;
        [SerializeField] private AnimationClip _secondClip;
        [SerializeField, Min(0.1f)] private float _firstDuration = 3f;
        [SerializeField, Min(0.1f)] private float _secondDuration = 3f;
        [SerializeField] private bool _usePedestrianMovement;
        [Tooltip("S-217 — 이 동작을 재생하는 동안 걸어도 되는가. 제자리 동작(인사·화내기)에 켜면 슬라이딩한다.")]
        [SerializeField] private bool _firstClipMoves;
        [SerializeField] private bool _secondClipMoves = true;
        [SerializeField] private TextAsset _randomTalkPool;
        [SerializeField] private Sprite _npcInfoBackground;

        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable _firstPlayable;
        private AnimationClipPlayable _secondPlayable;
        private bool _showingFirst;
        private float _timer;
        private Vector3 _rootPosition;
        private Quaternion _rootRotation;

        private void Start()
        {
            if (_target == null) return;
            if (_usePedestrianMovement || _randomTalkPool != null)
            {
                PedestrianNpc pedestrian = _target.GetComponent<PedestrianNpc>();
                if (pedestrian == null) pedestrian = _target.AddComponent<PedestrianNpc>();
                pedestrian.Configure(_usePedestrianMovement, _randomTalkPool, _npcInfoBackground);
                if (_usePedestrianMovement) pedestrian.CenterTurnsOnVisuals();
            }

            if (_firstClip == null || _secondClip == null) return;

            Animator animator = _target.GetComponentInChildren<Animator>(true);
            if (animator == null) animator = _target.AddComponent<Animator>();
            if (_avatar != null) animator.avatar = _avatar;
            bool requiresAvatar = _firstClip.isHumanMotion || _secondClip.isHumanMotion;
            if (requiresAvatar && animator.avatar == null)
            {
                Debug.LogWarning($"[{nameof(AlternatingNpcAnimation)}] Avatar not found on {_target.name}.", this);
                return;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = null;
            animator.Rebind();
            animator.Update(0f);
            _graph = PlayableGraph.Create(_target.name + "_AlternatingAnimation");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationMixerPlayable.Create(_graph, 2);
            _firstPlayable = AnimationClipPlayable.Create(_graph, _firstClip);
            _secondPlayable = AnimationClipPlayable.Create(_graph, _secondClip);
            _firstPlayable.SetApplyFootIK(true);
            _secondPlayable.SetApplyFootIK(true);
            _graph.Connect(_firstPlayable, 0, _mixer, 0);
            _graph.Connect(_secondPlayable, 0, _mixer, 1);

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
            output.SetSourcePlayable(_mixer);
            Show(first: true);
            _graph.Play();
        }

        private void Update()
        {
            if (!_graph.IsValid()) return;

            _timer += Time.deltaTime;
            LoopActiveClip();
            float duration = _showingFirst ? _firstDuration : _secondDuration;
            if (_timer >= duration) Show(!_showingFirst);

            _rootPosition = _target.transform.position;
            _rootRotation = _target.transform.rotation;
        }

        private void LateUpdate()
        {
            if (!_graph.IsValid()) return;
            _target.transform.SetPositionAndRotation(_rootPosition, _rootRotation);
        }

        private void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }

        /// <summary>
        /// S-217 ②③ — **제자리 동작 중엔 걷지 않는다.**
        /// 지혜의 두 클립(`jihye_Idle`·`Standing Greeting`)은 둘 다 발이 붙어 있는 동작인데
        /// 몸이 이동하니 미끄러져 다녔다(남규님 관찰: "손 흔들면서 슬라이딩으로 돌아다님").
        /// 걷기 클립이 없는 NPC는 이동시키면 반드시 슬라이딩한다 — 이동 허용을 **클립별로** 건다.
        /// 나아라는 두 번째가 진짜 걷기 클립이라 그때만 걷는다.
        /// </summary>
        private void ApplyMovementGate()
        {
            if (_target == null) return;
            PedestrianNpc pedestrian = _target.GetComponent<PedestrianNpc>();
            if (pedestrian == null) return;
            pedestrian.SetMovementAllowed(_usePedestrianMovement
                && (_showingFirst ? _firstClipMoves : _secondClipMoves));
        }

        private void Show(bool first)
        {
            _showingFirst = first;
            _timer = 0f;
            ApplyMovementGate();
            _mixer.SetInputWeight(0, first ? 1f : 0f);
            _mixer.SetInputWeight(1, first ? 0f : 1f);
            _firstPlayable.SetSpeed(first ? 1d : 0d);
            _secondPlayable.SetSpeed(first ? 0d : 1d);
            if (first) _firstPlayable.SetTime(0d);
            else _secondPlayable.SetTime(0d);
        }

        private void LoopActiveClip()
        {
            AnimationClip clip = _showingFirst ? _firstClip : _secondClip;
            double time = _showingFirst ? _firstPlayable.GetTime() : _secondPlayable.GetTime();
            if (clip.length <= 0f || time < clip.length) return;

            if (_showingFirst) _firstPlayable.SetTime(time % clip.length);
            else _secondPlayable.SetTime(time % clip.length);
        }
    }
}
