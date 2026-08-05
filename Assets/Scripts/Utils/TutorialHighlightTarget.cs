using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-164 ② — 튜토리얼 단계가 가리키는 대상을 눈에 띄게 흔든다(남규님: "해당하는 부분을 하이라이트").
    ///
    /// **대상이 스스로 등록한다**: 진행부가 대상을 찾아다니지 않고(=`Find` 금지 규약),
    /// 각 오브젝트에 이 컴포넌트를 붙이고 `_id`만 맞춰두면 된다. 진행부는 id만 방송하고,
    /// 자기 id를 들은 컴포넌트가 알아서 반응한다 — 씬을 넘나들어도 배선이 끊기지 않는다.
    ///
    /// 연출은 **크기 맥동**이다. 색을 바꾸면 대상마다 머티리얼 구조가 달라 손이 많이 가고,
    /// 월드 오브젝트와 UI 버튼을 한 방식으로 다룰 수 없다. 스케일은 둘 다 통한다.
    /// </summary>
    public class TutorialHighlightTarget : MonoBehaviour
    {
        [Tooltip("튜토리얼 단계의 highlightId와 맞으면 반응한다. 비면 아무 것도 안 한다.")]
        [SerializeField] private string _id;
        [Tooltip("맥동 크기(배). 1.12면 12% 커졌다 작아진다.")]
        [SerializeField] private float _pulseScale = 1.12f;
        [Tooltip("초당 맥동 횟수.")]
        [SerializeField] private float _pulseSpeed = 2.2f;

        private Vector3 _baseScale;
        private bool _active;

        private void Awake() => _baseScale = transform.localScale;

        private void OnEnable()
        {
            WorldEvents.TutorialStepStarted += OnStepStarted;
            WorldEvents.TutorialStepCleared += OnStepCleared;
        }

        private void OnDisable()
        {
            WorldEvents.TutorialStepStarted -= OnStepStarted;
            WorldEvents.TutorialStepCleared -= OnStepCleared;
            Restore();
        }

        private void OnStepStarted(string title, string detail, string targetId)
        {
            bool mine = !string.IsNullOrEmpty(_id) && _id == targetId;
            if (mine == _active) return;
            _active = mine;
            if (!_active) Restore();
        }

        private void OnStepCleared()
        {
            if (!_active) return;
            _active = false;
            Restore();
        }

        private void Update()
        {
            if (!_active) return;
            // 사인파 0~1 → 기본 크기와 맥동 크기 사이. unscaled라 정산창(timeScale=0)에서도 움직인다.
            float t = (Mathf.Sin(Time.unscaledTime * _pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            transform.localScale = _baseScale * Mathf.Lerp(1f, _pulseScale, t);
        }

        private void Restore()
        {
            if (_baseScale == Vector3.zero) return;
            transform.localScale = _baseScale;
        }
    }
}
