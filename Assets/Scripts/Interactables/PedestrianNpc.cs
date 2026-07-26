using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 행인 NPC (S-052 ②). 시작 지점을 중심으로 X 왕복 배회 — 끝에서 잠깐 멈췄다 되돌아간다.
    /// 상호작용 없음, 콜라이더 없음(플레이어 통과) — 거리의 생활감만 담당한다.
    /// </summary>
    public class PedestrianNpc : MonoBehaviour
    {
        [Tooltip("시작 지점 기준 좌우 배회 반경(u).")]
        [SerializeField] private float _patrolHalf = 6f;
        [SerializeField] private float _speed = 1.1f;

        private float _centerX;
        private int _direction = 1;
        private float _pauseTimer;

        private void Start()
        {
            _centerX = transform.position.x;
            _direction = Random.value < 0.5f ? 1 : -1;
            // 같은 씬 행인들이 발맞추지 않게 시작 위상 분산.
            transform.position += Vector3.right * Random.Range(-_patrolHalf * 0.5f, _patrolHalf * 0.5f);
            Face();
        }

        private void Update()
        {
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                return;
            }

            transform.position += Vector3.right * (_direction * _speed * Time.deltaTime);

            float offset = transform.position.x - _centerX;
            if (Mathf.Abs(offset) >= _patrolHalf)
            {
                _direction = offset > 0f ? -1 : 1;
                _pauseTimer = Random.Range(0.8f, 2.4f); // 끝에서 잠깐 두리번
                Face();
            }
        }

        private void Face() => transform.rotation = Quaternion.Euler(0f, _direction > 0 ? 90f : -90f, 0f);
    }
}
