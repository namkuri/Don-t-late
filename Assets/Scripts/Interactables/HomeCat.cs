using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// 집 고양이 (S-059). 언덕에서 데려오면 집에 나타난다. 가방의 "고양이 사료"를 사용하면 급여 —
    /// 하루 넘게 굶기면 도망간다(다음 귀가 때 없음). 용품은 쇼핑 앱에서 산다.
    /// </summary>
    public class HomeCat : MonoBehaviour
    {
        [SerializeField] private GameStateSO _gameState;

        private void OnEnable() => WorldEvents.BagItemConsumed += OnBagItemConsumed;
        private void OnDisable() => WorldEvents.BagItemConsumed -= OnBagItemConsumed;

        private void Start()
        {
            if (_gameState == null) { gameObject.SetActive(false); return; }

            if (_gameState.catFriend && !_gameState.catRanAway
                && _gameState.day - _gameState.catFedDay > 1)
            {
                _gameState.catRanAway = true; // 하루 넘게 굶었다 — 떠났다
                _gameState.catFriend = false;
                Debug.Log("[고양이] 밥그릇이 빈 채로... 고양이가 떠났다.");
            }

            if (!_gameState.catFriend || _gameState.catRanAway)
            {
                gameObject.SetActive(false);
                return;
            }

            BuildVisual();
            Debug.Log(_gameState.day > _gameState.catFedDay
                ? "[고양이] 배고픈지 그릇을 쳐다본다 — 가방에서 사료를 사용하자."
                : "[고양이] 골골골... 만족스러워 보인다.");
        }

        private void OnBagItemConsumed(BagItem item)
        {
            if (item.id != "cat_food" || _gameState == null || !_gameState.catFriend) return;
            _gameState.catFedDay = _gameState.day;
            Debug.Log("[고양이] 냠냠 — 오늘 밥은 해결됐다.");
        }

        // 그레이박스 고양이 — 몸통·머리·귀 (작은 주황 덩어리).
        private void BuildVisual()
        {
            Material fur = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.85f, 0.55f, 0.25f) };

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.22f, 0.26f, 0.22f);
            body.GetComponent<Renderer>().sharedMaterial = fur;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0.22f, 0.3f, 0f);
            head.transform.localScale = Vector3.one * 0.22f;
            head.GetComponent<Renderer>().sharedMaterial = fur;

            for (int i = 0; i < 2; i++)
            {
                GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(ear.GetComponent<Collider>());
                ear.transform.SetParent(head.transform, false);
                ear.transform.localPosition = new Vector3(0f, 0.45f, i == 0 ? -0.3f : 0.3f);
                ear.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
                ear.GetComponent<Renderer>().sharedMaterial = fur;
            }
        }
    }
}
