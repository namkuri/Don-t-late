using UnityEngine;
using UnityEngine.UI;

namespace DontLate
{
    /// <summary>튜토리얼 배너의 X 버튼. 대상이 비어 있으면 바로 위 부모 배너를 닫는다.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class DismissTutorialButton : MonoBehaviour
    {
        [SerializeField] private GameObject _target;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Dismiss);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(Dismiss);
        }

        public void Dismiss()
        {
            GameObject target = _target != null ? _target : transform.parent?.gameObject;
            if (target != null) target.SetActive(false);
        }
    }
}
