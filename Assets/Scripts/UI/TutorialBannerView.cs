using TMPro;
using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-269 — 화면 상단 안내 배너. 종전엔 씬 빌더가 문구를 **박아 넣은 정적 UI**라, 미션이 바뀌어도
    /// 같은 문장이 남고 닫기 전엔 영구히 자리를 점유했다(남규님 지시로 동적 전환·자동 소멸로 바꾼다).
    ///
    /// 뷰는 표시만 한다 — 문구는 진행부(<see cref="WorldEvents.TutorialStepStarted"/>)가 준 것을
    /// 그대로 쓴다. 미션카드(<see cref="TutorialMissionCardView"/>)와 **같은 `detail` 문장**을 받으므로
    /// 둘이 어긋날 수 없다.
    /// </summary>
    public class TutorialBannerView : MonoBehaviour
    {
        [Tooltip("배너 본문. 비면 자식에서 첫 TMP_Text를 찾는다(빌더 산출물 호환).")]
        [SerializeField] private TMP_Text _label;

        /// <summary>표시 유지 시간(초) — 남규님 지정 20초.</summary>
        private const float HOLD_SECONDS = 20f;

        private float _hideAt = -1f;
        private string _sceneDefault;
        private CanvasGroup _group;

        private void Awake()
        {
            if (_label == null) _label = GetComponentInChildren<TMP_Text>(true);
            // 씬 빌더가 넣어 둔 문장 — 튜토리얼이 없는 씬에서는 이게 그대로 쓰인다.
            if (_label != null) _sceneDefault = _label.text;

            // **오브젝트를 끄지 않는다.** `SetActive(false)`면 `OnDisable`이 돌아 구독이 끊기고
            // 다음 단계 안내를 영영 못 듣는다 — 투명도로 감춘다.
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            // 씬 진입 시 떠 있는 기본 안내도 20초면 물러난다(남규님 지시: "뜬 다음엔 20초 뒤").
            _hideAt = Time.unscaledTime + HOLD_SECONDS;
        }

        private void OnEnable()
        {
            WorldEvents.TutorialStepStarted += OnStepStarted;
            WorldEvents.PhoneRang += OnPhoneRang;
        }

        private void OnDisable()
        {
            WorldEvents.TutorialStepStarted -= OnStepStarted;
            WorldEvents.PhoneRang -= OnPhoneRang;
        }

        private void OnStepStarted(string title, string detail, string _)
        {
            Show(string.IsNullOrEmpty(detail) ? title : detail);
        }

        // 박말순 전화 = 리듬 미니게임 예고. 받기 전에 조작을 알려 줘야 첫 통화에서 안 당한다.
        private void OnPhoneRang(PhoneCall call)
        {
            Show("박말순 전화를 받고 화면과 동일하게 방향키 빠르게 눌러야한다.");
        }

        private void Show(string message)
        {
            if (_label != null) _label.text = message;
            SetVisible(true);
            _hideAt = Time.unscaledTime + HOLD_SECONDS; // 대화 중 시간 정지에도 흐르게 unscaled
        }

        private void Update()
        {
            if (_hideAt < 0f || Time.unscaledTime < _hideAt) return;
            _hideAt = -1f;
            SetVisible(false);
        }

        private void SetVisible(bool on)
        {
            if (_group == null) return;
            _group.alpha = on ? 1f : 0f;
            _group.blocksRaycasts = on;   // 감춰진 배너가 닫기 버튼으로 클릭을 먹지 않게
            _group.interactable = on;
        }
    }
}
