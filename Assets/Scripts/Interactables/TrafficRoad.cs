using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 교차(Z축) 골목 도로의 차량 스포너 (S-057). 플레이어 진행축(X)과 직각으로 차가 오간다 —
    /// 타이밍을 보고 건너야 한다. 스폰 간격·속도는 랜덤 폭.
    /// </summary>
    public class TrafficRoad : MonoBehaviour
    {
        [SerializeField] private float _minInterval = 3.5f;
        [SerializeField] private float _maxInterval = 7f;
        [SerializeField] private float _carSpeed = 6.5f;
        [SerializeField] private float _halfSpan = 10f; // z 주행 반경

        private float _timer;
        private int _direction = 1;

        private static readonly Color[] CarColors =
        {
            new Color(0.75f, 0.30f, 0.28f), new Color(0.30f, 0.45f, 0.70f),
            new Color(0.85f, 0.80f, 0.70f), new Color(0.35f, 0.35f, 0.38f),
        };

        private void Start() => _timer = Random.Range(0.5f, _maxInterval);

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = Random.Range(_minInterval, _maxInterval);
            SpawnCar();
        }

        private void SpawnCar()
        {
            _direction = -_direction; // 번갈아 반대편에서

            GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = "TrafficCar";
            car.transform.position = transform.position + new Vector3(0f, 0.55f, -_halfSpan * _direction);
            car.transform.localScale = new Vector3(1.5f, 1.1f, 2.9f);
            car.GetComponent<Renderer>().material.color = CarColors[Random.Range(0, CarColors.Length)];
            car.GetComponent<BoxCollider>().isTrigger = true;

            TrafficCar mover = car.AddComponent<TrafficCar>();
            mover.Launch(_direction * _carSpeed, _halfSpan);
        }
    }
}
