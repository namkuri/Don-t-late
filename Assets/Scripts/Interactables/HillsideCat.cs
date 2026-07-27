using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 언덕주택가 고양이 (S-059). 말 걸면 마음을 열고 — 집에 돌아가면 우리 집 고양이가 된다.
    /// 이미 데려왔으면(또는 도망갔으면) 씬에 나타나지 않는다.
    /// </summary>
    public class HillsideCat : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private DialogueScenarioSO _meetScenario;
        [SerializeField] private Renderer _highlightRenderer;
        [SerializeField] private Material _normalMaterial;
        [SerializeField] private Material _highlightMaterial;

        private void Start()
        {
            if (_gameState != null && _gameState.catFriend) gameObject.SetActive(false); // 이미 우리 집에 있다
        }

        public void Interact(PlayerContext ctx)
        {
            if (_gameState == null || _gameState.catFriend) return;
            _gameState.catFriend = true;
            _gameState.catRanAway = false;
            _gameState.catFedDay = _gameState.day;
            if (WorldDialogueManager.Instance != null && _meetScenario != null && !WorldDialogueManager.Instance.IsPlaying)
                WorldDialogueManager.Instance.PlayScenario(_meetScenario);
            Debug.Log("[고양이] 마음을 열었다 — 집에 가면 기다리고 있을 것이다. 사료는 쇼핑 앱에서.");
            gameObject.SetActive(false); // 먼저 집으로 뛰어갔다 치자
        }

        public void SetHighlight(bool on)
        {
            if (_highlightRenderer == null) return;
            _highlightRenderer.sharedMaterial = on && _highlightMaterial != null ? _highlightMaterial : _normalMaterial;
        }
    }
}
