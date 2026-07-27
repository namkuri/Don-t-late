using UnityEngine;

namespace DontLate
{
    /// <summary>둥둥 떠다니는 방향 화살표 (S-062 ③ — 엣지 게이트 표식). 코드 트윈 — 상하 봅 + 진행축 살랑.</summary>
    public class FloatingArrow : MonoBehaviour
    {
        [SerializeField] private float _bobHeight = 0.28f;
        [SerializeField] private float _bobSpeed = 2.2f;
        [Tooltip("가리키는 방향으로 살짝 밀었다 돌아오는 폭(u).")]
        [SerializeField] private float _pushAmount = 0.18f;
        [SerializeField] private Vector3 _pushDirection = Vector3.right;

        private Vector3 _basePosition;

        private void Start() => _basePosition = transform.localPosition;

        private void Update()
        {
            float t = Time.time * _bobSpeed;
            transform.localPosition = _basePosition
                + Vector3.up * (Mathf.Sin(t) * _bobHeight)
                + _pushDirection * (Mathf.Sin(t * 0.5f) * _pushAmount);
        }
    }
}
