using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 공용 카운트다운 타이머. MonoBehaviour가 아니므로 소유자가 Tick을 돌린다.
    /// </summary>
    public class GameTimer
    {
        private float _duration;
        private float _elapsed;

        public GameTimer(float duration) => Reset(duration);

        public float Elapsed => _elapsed;
        public float Remaining => Mathf.Max(0f, _duration - _elapsed);
        public float Progress => _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
        public bool IsDone => _elapsed >= _duration;
        public bool IsRunning { get; private set; }

        public void Reset(float duration)
        {
            _duration = duration;
            _elapsed = 0f;
            IsRunning = true;
        }

        public void Stop() => IsRunning = false;

        /// <summary>이번 Tick에서 완료되었으면 true를 1회 반환한다.</summary>
        public bool Tick(float deltaTime)
        {
            if (!IsRunning) return false;

            _elapsed += deltaTime;
            if (!IsDone) return false;

            IsRunning = false;
            return true;
        }
    }
}
