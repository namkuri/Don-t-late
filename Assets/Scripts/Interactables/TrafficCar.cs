using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 주행 중인 차 1대 (S-057). Z축으로 관통 주행 — 치이면:
    /// 짐(든 상자·대차 짐)은 날아가고, 플레이어가 치이면 병원행(PlayerHitByCar → 병원비·미배송 실패).
    /// </summary>
    public class TrafficCar : MonoBehaviour
    {
        private float _velocityZ;
        private float _killZ;
        private TrafficLight _signal;   // S-074 ⑨ — 적신호면 정지선 앞 대기
        private float _stopLineZ;
        private static float _lastHitTime = -10f; // 다중 히트 방지 (전 차량 공유)

        // S-166 ⑥ — 남규님 "3배 더 크게". 수평만 3배로 키운다 — 수직까지 3배면 22.5로
        // 지붕 높이까지 솟아 만화가 되고, 착지 전 낙사 안전망(FALL_LIMIT_Y)에 걸릴 수 있다.
        // S-210 — 행인도 **같은 상수를 쓴다**. "플레이어와 동일한 넉백"이 발주 문구였으니
        // 값을 복사해 두 벌로 두면 다음에 한쪽만 손질돼 어긋난다.
        private const float KNOCKBACK_GAIN = 3f;

        public void Launch(float velocityZ, float halfSpan, TrafficLight signal = null, float stopLineZ = 4.5f)
        {
            _velocityZ = velocityZ;
            _killZ = halfSpan + 2f;
            _signal = signal;
            _stopLineZ = stopLineZ;
        }

        private void Update()
        {
            // S-074 ⑨ — 적신호: 진행 방향 앞 정지선(횡단보도 앞)에 걸리면 대기. 통과 후엔 무시.
            if (_signal != null && !_signal.IsGreenForCars)
            {
                float stopZ = _velocityZ > 0f ? -_stopLineZ : _stopLineZ; // 진입 방향 쪽 정지선
                float distance = (stopZ - transform.position.z) * Mathf.Sign(_velocityZ);
                if (distance > 0f && distance < 1.2f) return; // 정지선 직전 — 대기
            }

            transform.position += new Vector3(0f, 0f, _velocityZ * Time.deltaTime);
            // S-070 ① — 풀링: 파괴 대신 비활성 반납 (TrafficRoad가 재사용).
            if (Mathf.Abs(transform.position.z) > _killZ) gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            // S-076 ② → **S-210에서 뒤집힘**: 행인은 죽지 않는다(남규님 지시).
            // 종전엔 `Destroy`라 씬을 다시 들어오기 전까지 그 행인이 영영 사라졌다 — 거리가 비어 가고
            // 소셜·호감도 대상까지 같이 없어졌다. 이제 플레이어와 **같은 규격의 넉백**만 준다.
            PedestrianNpc pedestrian = other.GetComponentInParent<PedestrianNpc>();
            if (pedestrian != null)
            {
                if (DistrictCaptureDemo.SuppressTrafficAccidents) return; // S-122 ⑯ — 촬영 중 사고 연출 금지
                Debug.Log("[교통사고] 행인이 차에 치였다 — 날아감(죽지 않는다).");
                WorldAudioManager.Instance?.PlayCarCrashSfx();
                pedestrian.ApplyKnockback(new Vector3(
                    Random.Range(-2.5f, 2.5f) * KNOCKBACK_GAIN,
                    9f,
                    Mathf.Sign(_velocityZ) * 5.5f * KNOCKBACK_GAIN));
                return;
            }

            // 짐·대차 짐 — 날려버린다.
            Rigidbody body = other.attachedRigidbody;
            if (body != null && !body.isKinematic && other.GetComponentInParent<PlayerManager>() == null)
            {
                Vector3 fling = new Vector3(Random.Range(-2f, 2f), 5.5f, Mathf.Sign(_velocityZ) * 4f);
                body.linearVelocity = fling;
                body.angularVelocity = Random.insideUnitSphere * 8f;
                return;
            }

            PlayerManager player = other.GetComponentInParent<PlayerManager>();
            if (player == null) return;
            if (DistrictCaptureDemo.SuppressTrafficAccidents) return; // S-116 ⑤ — 촬영 중 사고 무시
            if (Time.time - _lastHitTime < 3f) return; // 연속 충돌 무시
            _lastHitTime = Time.time;

            // 손의 짐부터 흩어진다 (두 슬롯 다).
            if (player.Status.IsCarrying) player.Status.ReleaseCarry(dropAsPhysics: true);
            if (player.Status.IsCarrying) player.Status.ReleaseCarry(dropAsPhysics: true); // 승격분

            // S-066 ③ — 사람이 날아간다 + 끼익!쿵! (클립 소켓 비면 무음 — AU-020).
            player.Locomotion.ApplyKnockback(new Vector3(
                Random.Range(-2.5f, 2.5f) * KNOCKBACK_GAIN,
                9f,
                Mathf.Sign(_velocityZ) * 5.5f * KNOCKBACK_GAIN));
            WorldAudioManager.Instance?.PlayCarCrashSfx();

            Debug.Log("[교통사고] 차에 치였다!");
            WorldEvents.RaisePlayerHitByCar();
        }
    }
}
