using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 대화가 재생되는 동안 이 오브젝트를 숨긴다(View) — S-009.
    /// Home 진행 버튼에 붙여 "인트로 전화가 끝나야 물류캠프 버튼이 나온다"를 만든다.
    /// 대화가 없으면 아무것도 하지 않으므로 재방문 씬에서도 안전하다.
    /// </summary>
    public class HideDuringDialogue : MonoBehaviour
    {
        [Tooltip("숨길 대상. ⚠ 자기 자신을 넣으면 비활성화와 함께 구독이 끊겨 다시 못 나온다 — 반드시 상시 활성 부모(캔버스)에 붙이고 자식을 지정할 것.")]
        [SerializeField] private GameObject _target;

        private void OnEnable()
        {
            WorldEvents.DialogueStarted += OnDialogueStarted;
            WorldEvents.DialogueEnded += OnDialogueEnded;
            // 씬 로드 시점에 이미 대화가 돌고 있으면(전이 완료 직후 자동 재생) 즉시 숨긴다.
            if (WorldDialogueManager.Instance != null && WorldDialogueManager.Instance.IsPlaying)
                _target.SetActive(false);
        }

        private void OnDisable()
        {
            WorldEvents.DialogueStarted -= OnDialogueStarted;
            WorldEvents.DialogueEnded -= OnDialogueEnded;
        }

        private void OnDialogueStarted(string _) => _target.SetActive(false);
        private void OnDialogueEnded(string _) => _target.SetActive(true);
    }
}
