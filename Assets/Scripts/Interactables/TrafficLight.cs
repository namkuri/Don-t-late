using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 교차 도로 신호등 (S-074 ⑨ → S-076 ① 3색). 차량용 녹→황→적 주기를 소유하고,
    /// TrafficCar·PedestrianNpc가 참조해 각자 멈춘다. 등화는 MPB 이미시브 스왑.
    /// </summary>
    public class TrafficLight : MonoBehaviour
    {
        public enum Phase { Green, Yellow, Red }

        [SerializeField] private float _greenSeconds = 7f;
        [SerializeField] private float _yellowSeconds = 1.5f;
        [SerializeField] private float _redSeconds = 5f;
        [SerializeField] private Renderer _redLamp;
        [SerializeField] private Renderer _yellowLamp;
        [SerializeField] private Renderer _greenLamp;

        public Phase Current { get; private set; } = Phase.Green;
        /// <summary>차량 기준 — 녹색에만 새로 진입한다 (황=정지선 앞이면 대기).</summary>
        public bool IsGreenForCars => Current == Phase.Green;
        /// <summary>보행자 기준 — 차가 완전히 멎는 적신호에만 건넌다.</summary>
        public bool IsWalkable => Current == Phase.Red;

        private float _timer;
        private MaterialPropertyBlock _mpb;

        private static readonly Color RED_ON = new Color(1f, 0.30f, 0.24f);
        private static readonly Color YELLOW_ON = new Color(1f, 0.82f, 0.25f);
        private static readonly Color GREEN_ON = new Color(0.25f, 0.95f, 0.45f);

        private void Start()
        {
            _timer = _greenSeconds;
            ApplyLamps();
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            Current = Current == Phase.Green ? Phase.Yellow
                    : Current == Phase.Yellow ? Phase.Red
                    : Phase.Green;
            _timer = Current == Phase.Green ? _greenSeconds
                   : Current == Phase.Yellow ? _yellowSeconds
                   : _redSeconds;
            ApplyLamps();
        }

        private void ApplyLamps()
        {
            _mpb ??= new MaterialPropertyBlock();
            SetLamp(_redLamp, RED_ON, Current == Phase.Red);
            SetLamp(_yellowLamp, YELLOW_ON, Current == Phase.Yellow);
            SetLamp(_greenLamp, GREEN_ON, Current == Phase.Green);
        }

        private void SetLamp(Renderer lamp, Color onColor, bool on)
        {
            if (lamp == null) return;
            lamp.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", on ? onColor : onColor * 0.18f);
            _mpb.SetColor("_EmissionColor", on ? onColor * 2.2f : Color.black);
            lamp.SetPropertyBlock(_mpb);
        }
    }
}
