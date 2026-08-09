using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DontLate
{
    /// <summary>씬에 배치된 NPC가 두 동작을 일정 시간마다 번갈아 재생한다.</summary>
    public sealed class AlternatingNpcAnimation : MonoBehaviour
    {
        [SerializeField] private GameObject _target;
        [SerializeField] private AnimationClip _firstClip;
        [SerializeField] private AnimationClip _secondClip;
        [SerializeField, Min(0.1f)] private float _firstDuration = 3f;
        [SerializeField, Min(0.1f)] private float _secondDuration = 3f;

        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable _firstPlayable;
        private AnimationClipPlayable _secondPlayable;
        private bool _showingFirst;
        private float _timer;

        private void Start()
        {
            if (_target == null || _firstClip == null || _secondClip == null) return;

            Animator animator = _target.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning($"[{nameof(AlternatingNpcAnimation)}] Animator not found on {_target.name}.", this);
                return;
            }

            animator.applyRootMotion = false;
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
        }

        private void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }

        private void Show(bool first)
        {
            _showingFirst = first;
            _timer = 0f;
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
