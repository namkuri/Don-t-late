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
        [Tooltip("이 도로의 신호등 (S-074 ⑨) — 비면 무신호 도로(기존 동작).")]
        [SerializeField] private TrafficLight _signal;
        [Tooltip("정지선 |z| — 횡단보도 앞.")]
        [SerializeField] private float _stopLineZ = 4.5f;
        [Tooltip("S-116 ⑤ — 실모델 차량 프리팹(white_van·yellow_taxi 등). 비면 기존 큐브 차.")]
        [SerializeField] private GameObject[] _carVisualPrefabs;
        [Tooltip("실모델 길이(z) 정규화 기준(u).")]
        [SerializeField] private float _carLength = 2.9f;

        private float _timer;
        private int _direction = 1;

        private static readonly Color[] CarColors =
        {
            new Color(0.75f, 0.30f, 0.28f), new Color(0.30f, 0.45f, 0.70f),
            new Color(0.85f, 0.80f, 0.70f), new Color(0.35f, 0.35f, 0.38f),
        };

        // S-069 — 스폰마다 .material 인스턴스를 만들어 파괴 시 회수 안 되던 누수 봉인:
        // 색상 4종 머티리얼을 한 번만 만들어 전 차량이 공유한다 (드로우콜 배칭에도 이득).
        private static Material[] _carMaterials;

        private static Material MaterialFor(int index)
        {
            if (_carMaterials == null)
            {
                _carMaterials = new Material[CarColors.Length];
                for (int i = 0; i < CarColors.Length; i++)
                {
                    _carMaterials[i] = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    _carMaterials[i].color = CarColors[i];
                }
            }
            return _carMaterials[index];
        }

        // S-070 ① — 풀링: 스폰 주기(3.5~7s)마다 CreatePrimitive/Destroy가 만들던 스파이크 제거.
        // 도로당 차는 동시 1~2대뿐이라 풀 2대로 충분 — 비활성 재사용, 파괴 없음.
        private TrafficCar[] _pool;

        private void Start()
        {
            _timer = Random.Range(0.5f, _maxInterval);
            _pool = new TrafficCar[2];
            for (int i = 0; i < _pool.Length; i++)
            {
                GameObject car;
                if (_carVisualPrefabs != null && _carVisualPrefabs.Length > 0)
                {
                    // S-116 ⑤ — 실모델 차: 원본 비율 유지·길이(z) 기준 정규화·발 y=0 스냅.
                    car = new GameObject("TrafficCar");
                    BoxCollider trigger = car.AddComponent<BoxCollider>();
                    trigger.isTrigger = true;
                    trigger.size = new Vector3(1.5f, 1.1f, _carLength);
                    trigger.center = new Vector3(0f, 0.55f, 0f);
                    GameObject visual = Instantiate(
                        _carVisualPrefabs[i % _carVisualPrefabs.Length], car.transform);
                    visual.name = "Visual";
                    NormalizeCarVisual(visual);
                }
                else
                {
                    car = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    car.name = "TrafficCar";
                    car.transform.localScale = new Vector3(1.5f, 1.1f, 2.9f);
                    car.GetComponent<BoxCollider>().isTrigger = true;
                }
                _pool[i] = car.AddComponent<TrafficCar>();
                car.SetActive(false);
            }
        }

        // 길이(가장 긴 수평축)를 _carLength로 맞추고 진행축(z)에 정렬·발 y=0.
        private void NormalizeCarVisual(GameObject visual)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            // 모델의 긴 축이 x인 경우(옆모습 기준 제작) z 진행축으로 90도 돌린다.
            if (bounds.size.x > bounds.size.z * 1.15f)
            {
                visual.transform.Rotate(0f, 90f, 0f, Space.Self);
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            }

            float length = Mathf.Max(bounds.size.z, 0.001f);
            visual.transform.localScale *= _carLength / length;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            visual.transform.position += visual.transform.parent.position
                - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = Random.Range(_minInterval, _maxInterval);
            SpawnCar();
        }

        private void SpawnCar()
        {
            TrafficCar car = null;
            for (int i = 0; i < _pool.Length; i++)
                if (_pool[i] != null && !_pool[i].gameObject.activeSelf) { car = _pool[i]; break; }
            if (car == null) return; // 두 대 다 주행 중이면 이번 주기는 건너뜀

            _direction = -_direction; // 번갈아 반대편에서
            // S-076 ③ — 방향별 1차선 (우측통행): 진행 방향 기준 오른쪽 차선으로 스폰.
            float laneX = _direction > 0 ? 1.05f : -1.05f;
            bool realModel = _carVisualPrefabs != null && _carVisualPrefabs.Length > 0;
            float spawnY = realModel ? 0f : 0.55f; // 실모델은 발 y=0 정규화 · 큐브는 중심 스폰
            car.transform.position = transform.position + new Vector3(laneX, spawnY, -_halfSpan * _direction);
            if (realModel)
            {
                // S-116 ⑤ — 반대 방향 차량은 Y 180도 (가이드 §8).
                car.transform.rotation = Quaternion.Euler(0f, _direction > 0 ? 0f : 180f, 0f);
            }
            else
            {
                car.GetComponent<Renderer>().sharedMaterial = MaterialFor(Random.Range(0, CarColors.Length));
            }
            car.gameObject.SetActive(true);
            car.Launch(_direction * _carSpeed, _halfSpan, _signal, _stopLineZ);
        }
    }
}
