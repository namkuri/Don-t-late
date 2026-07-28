using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 교차 도로 신호등 (S-074 ⑨ — 남규님 발주, 매니페스트 직교 추가). 차량용 녹/적 주기를
    /// 소유하고, TrafficCar가 참조해 적신호면 정지선 앞에서 멈춘다. 등화는 이미시브 강도 스왑.
    /// </summary>
    public class TrafficLight : MonoBehaviour
    {
        [SerializeField] private float _greenSeconds = 7f;
        [SerializeField] private float _redSeconds = 5f;
        [SerializeField] private Renderer _redLamp;
        [SerializeField] private Renderer _greenLamp;

        /// <summary>차량 기준 — true면 주행 가능.</summary>
        public bool IsGreenForCars { get; private set; } = true;

        private float _timer;
        private MaterialPropertyBlock _mpb;

        private static readonly Color RED_ON = new Color(1f, 0.30f, 0.24f);
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
            IsGreenForCars = !IsGreenForCars;
            _timer = IsGreenForCars ? _greenSeconds : _redSeconds;
            ApplyLamps();
        }

        private void ApplyLamps()
        {
            _mpb ??= new MaterialPropertyBlock();
            SetLamp(_redLamp, RED_ON, !IsGreenForCars);
            SetLamp(_greenLamp, GREEN_ON, IsGreenForCars);
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
