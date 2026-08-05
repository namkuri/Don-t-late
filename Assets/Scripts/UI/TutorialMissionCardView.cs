using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>
    /// S-162 — 튜토리얼 미션 카드. 화면 오른쪽(세로 아래 1/3)에서 슬라이드 인 하고,
    /// 단계를 해내면 제목 옆에 "완료"가 붙고 배경이 초록으로 바뀐 뒤 오른쪽으로 사라진다.
    ///
    /// **표시만 한다** — 판정도 진행도 `CampTutorialDirector` 몫이다(뷰에 게임 로직 금지 규약).
    /// 여기는 `TutorialStepStarted`/`TutorialStepCleared` 두 이벤트만 듣는다.
    ///
    /// 대사창과 별개다: 대사는 흘러가지만 카드는 남아 "지금 뭘 해야 하나"에 상시 답한다.
    /// </summary>
    public class TutorialMissionCardView : MonoBehaviour
    {
        [SerializeField] private RectTransform _card;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _detailLabel;

        [Header("연출")]
        [Tooltip("들어오고 나가는 데 걸리는 시간(초).")]
        [SerializeField] private float _slideDuration = 0.45f;
        [Tooltip("완료 표시를 보여주는 시간(초). 이 뒤에 퇴장한다.")]
        [SerializeField] private float _clearedHold = 1.2f;
        [Tooltip("화면 밖으로 얼마나 물러나 있을지(px).")]
        [SerializeField] private float _hiddenOffset = 460f;

        private static readonly Color IdleColor = new Color(0.09f, 0.11f, 0.16f, 0.94f);
        private static readonly Color ClearedColor = new Color(0.16f, 0.55f, 0.28f, 0.96f);

        private Vector2 _shownPosition;
        private Coroutine _routine;
        private string _title;

        private void Awake()
        {
            if (_card == null) return;
            _shownPosition = _card.anchoredPosition;
            // 시작은 화면 밖. 첫 단계가 올 때까지 보이지 않는다.
            _card.anchoredPosition = _shownPosition + new Vector2(_hiddenOffset, 0f);
            _card.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            WorldEvents.TutorialStepStarted += OnStepStarted;
            WorldEvents.TutorialStepCleared += OnStepCleared;
        }

        private void OnDisable()
        {
            WorldEvents.TutorialStepStarted -= OnStepStarted;
            WorldEvents.TutorialStepCleared -= OnStepCleared;
        }

        private void OnStepStarted(string title, string detail, string _)
        {
            if (_card == null) return;
            _title = title;
            if (_titleLabel != null) _titleLabel.text = title;
            if (_detailLabel != null) _detailLabel.text = detail;
            if (_background != null) _background.color = IdleColor;

            if (_routine != null) StopCoroutine(_routine);
            _card.gameObject.SetActive(true);
            _routine = StartCoroutine(SlideTo(_shownPosition));
        }

        private void OnStepCleared()
        {
            if (_card == null || !_card.gameObject.activeSelf) return;
            if (_titleLabel != null) _titleLabel.text = _title + "   <color=#b8f5c8>완료</color>";
            if (_background != null) _background.color = ClearedColor;

            WorldAudioManager.Instance?.PlayTutorialStepSfx(); // AU-025 — 없으면 무음 폴백

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(HoldThenExit());
        }

        private IEnumerator HoldThenExit()
        {
            yield return new WaitForSeconds(_clearedHold);
            yield return SlideTo(_shownPosition + new Vector2(_hiddenOffset, 0f));
            _card.gameObject.SetActive(false);
        }

        /// <summary>
        /// 감속 이동. 등속으로 두면 끝이 뚝 끊겨 "미끄러졌다"가 아니라 "순간이동"으로 읽힌다
        /// (S-144 카메라 강하와 같은 이유).
        /// </summary>
        private IEnumerator SlideTo(Vector2 target)
        {
            Vector2 from = _card.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                elapsed += Time.unscaledDeltaTime; // 정산창 등 timeScale=0 상황에서도 움직인다
                float t = Mathf.Clamp01(elapsed / _slideDuration);
                _card.anchoredPosition = Vector2.Lerp(from, target, 1f - Mathf.Pow(1f - t, 3f));
                yield return null;
            }
            _card.anchoredPosition = target;
            _routine = null;
        }
    }
}
