using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-195 — 짐을 들었을 때 **양손을 상자에 붙인다**(Animator IK).
    ///
    /// 왜 필요한가: 캐리 애니메이션은 상자 크기를 모른 채 만들어진 한 벌이라, 실제로 든 상자와
    /// 손 위치가 어긋난다 — 팔이 상자를 뚫거나 허공을 쥔다. IK는 그 마지막 한 뼘을 매 프레임
    /// 맞춰 주는 장치다. 애니메이션을 상자마다 새로 만들 필요가 없어진다.
    ///
    /// 왜 Player 도메인이 아니라 Utils인가: 플레이어와 **타이틀 러너**(연출 인형, PlayerManager가
    /// 없다)가 같이 쓴다. 그래서 허브를 참조하지 않고 Animator의 `IsCarrying` 파라미터만 읽는다 —
    /// 누가 그 값을 세우든 상관없다.
    ///
    /// ⚠ 두 가지가 갖춰져야 동작한다. 하나라도 빠지면 **조용히 아무 일도 일어나지 않는다**:
    ///   ① 이 컴포넌트가 Animator와 **같은 게임오브젝트**에 있을 것 (OnAnimatorIK 호출 규칙)
    ///   ② 애니메이터 컨트롤러 레이어의 **IK Pass가 켜져 있을 것** (빌더가 켠다)
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CarryHandIK : MonoBehaviour
    {
        private static readonly int CarryingHash = Animator.StringToHash("IsCarrying");

        [Tooltip("짐이 매달리는 앵커. 손 목표는 이 기준으로 좌우 대칭 배치된다.")]
        [SerializeField] private Transform _carryAnchor;

        [Tooltip("앵커 기준 오른손 잡는 점(로컬). 왼손은 x를 뒤집어 쓴다. 상자 0.7u 기준값.")]
        [SerializeField] private Vector3 _gripOffset = new Vector3(0.34f, 0.34f, 0f);

        [Tooltip("손이 붙는 정도. 1이면 완전히 상자에 고정, 낮추면 애니메이션이 섞인다.")]
        [Range(0f, 1f)][SerializeField] private float _positionWeight = 0.9f;

        [Tooltip("손목 각도를 상자에 맞추는 정도. 과하면 손목이 꺾여 보인다.")]
        [Range(0f, 1f)][SerializeField] private float _rotationWeight = 0.55f;

        [Tooltip("들고/놓을 때 손이 옮겨 가는 시간(초). 0이면 툭 끊긴다.")]
        [SerializeField] private float _blendSeconds = 0.18f;

        private Animator _animator;
        private float _blend; // 0=애니메이션 그대로, 1=IK 최대

        private void Awake() => _animator = GetComponent<Animator>();

        private void Update()
        {
            if (_animator == null) return;

            bool carrying = _carryAnchor != null && _animator.GetBool(CarryingHash);
            float step = _blendSeconds > 0.001f ? Time.deltaTime / _blendSeconds : 1f;
            _blend = Mathf.MoveTowards(_blend, carrying ? 1f : 0f, step);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || _carryAnchor == null) return;
            if (_blend <= 0.001f)
            {
                // 손을 놓을 땐 가중치를 0으로 **명시**해야 한다. 안 그러면 마지막 값이 남아
                // 짐이 없는데도 팔이 허공을 쥔 채로 굳는다.
                SetHand(AvatarIKGoal.LeftHand, Vector3.zero, Quaternion.identity, 0f);
                SetHand(AvatarIKGoal.RightHand, Vector3.zero, Quaternion.identity, 0f);
                return;
            }

            Vector3 right = _carryAnchor.TransformPoint(_gripOffset);
            Vector3 left = _carryAnchor.TransformPoint(
                new Vector3(-_gripOffset.x, _gripOffset.y, _gripOffset.z));

            // 손바닥이 상자를 향하도록 — 각자 안쪽(상자 중심)을 바라보게 세운다.
            Quaternion rightRot = Quaternion.LookRotation(_carryAnchor.forward, _carryAnchor.right);
            Quaternion leftRot = Quaternion.LookRotation(_carryAnchor.forward, -_carryAnchor.right);

            SetHand(AvatarIKGoal.RightHand, right, rightRot, _blend);
            SetHand(AvatarIKGoal.LeftHand, left, leftRot, _blend);
        }

        private void SetHand(AvatarIKGoal goal, Vector3 position, Quaternion rotation, float blend)
        {
            _animator.SetIKPositionWeight(goal, _positionWeight * blend);
            _animator.SetIKRotationWeight(goal, _rotationWeight * blend);
            if (blend <= 0.001f) return;
            _animator.SetIKPosition(goal, position);
            _animator.SetIKRotation(goal, rotation);
        }
    }
}
