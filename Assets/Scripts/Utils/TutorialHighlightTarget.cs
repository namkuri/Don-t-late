using UnityEngine;

namespace DontLate
{
    /// <summary>
    /// S-164 ② — 튜토리얼 단계가 가리키는 대상을 눈에 띄게 한다(남규님: "해당하는 부분을 하이라이트").
    ///
    /// **대상이 스스로 등록한다**: 진행부가 대상을 찾아다니지 않고(=`Find` 금지 규약),
    /// 각 오브젝트에 이 컴포넌트를 붙이고 `_id`만 맞춰두면 된다. 진행부는 id만 방송하고,
    /// 자기 id를 들은 컴포넌트가 알아서 반응한다 — 씬을 넘나들어도 배선이 끊기지 않는다.
    ///
    /// S-166 ① — **연출이 둘로 갈린다.**
    /// 월드 오브젝트는 **머리 위 화살표 상하 왕복**이다. 처음엔 크기 맥동으로 만들었는데,
    /// 상자는 강체라 스케일이 커지면 콜라이더가 이웃을 밀어내 **스택이 무너졌다**(남규님 실관찰
    /// "상자 쓰러지고 난리남"). 물리 오브젝트의 스케일은 건드리면 안 된다.
    /// UI는 화살표를 띄울 자리가 없어 크기 맥동을 유지하되, **중심 기준**으로 키운다.
    /// </summary>
    public class TutorialHighlightTarget : MonoBehaviour
    {
        [Tooltip("튜토리얼 단계의 highlightId와 맞으면 반응한다. 비면 아무 것도 안 한다.")]
        [SerializeField] private string _id;
        [Tooltip("UI 맥동 크기(배). 월드 대상은 화살표를 쓰므로 무관.")]
        [SerializeField] private float _pulseScale = 1.12f;
        [Tooltip("초당 왕복 횟수(화살표·맥동 공용).")]
        [SerializeField] private float _pulseSpeed = 2.2f;
        [Tooltip("월드 대상 화살표 **끝**이 뜰 높이(대상 상단에서 +u). 크면 대상과 떨어져 읽힌다.")]
        [SerializeField] private float _arrowGap = 0.45f;
        [Tooltip("화살표 상하 진폭(u).")]
        [SerializeField] private float _arrowBob = 0.28f;

        private static readonly Color ARROW_COLOR = new Color(0.21f, 0.88f, 0.78f, 1f); // 상호작용 시안

        private RectTransform _rect;   // UI면 non-null
        private Vector3 _baseScale;
        private bool _active;
        private GameObject _arrow;
        private float _arrowBaseY;

        private void Awake()
        {
            _rect = transform as RectTransform;
            _baseScale = transform.localScale;

            // S-166 ② — UI는 **중심 기준**으로 커져야 한다. 가방 버튼 피벗이 (1,1)이라 커질 때
            // 좌하단으로 자라 우측 상단이 붙박인 것처럼 보였다(남규님 지적). 피벗을 가운데로
            // 옮기고 그만큼 위치를 보정해 **보이는 자리는 그대로 두고** 기준만 바꾼다.
            if (_rect != null) RecenterPivot(_rect);
        }

        private static void RecenterPivot(RectTransform rect)
        {
            Vector2 pivot = rect.pivot;
            if (Mathf.Approximately(pivot.x, 0.5f) && Mathf.Approximately(pivot.y, 0.5f)) return;
            Vector2 size = rect.rect.size;
            Vector2 delta = new Vector2(0.5f - pivot.x, 0.5f - pivot.y);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition += new Vector2(delta.x * size.x, delta.y * size.y);
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
            Stop();
        }

        private void OnDestroy()
        {
            if (_arrow != null) Destroy(_arrow);
        }

        private void OnStepStarted(string title, string detail, string targetId)
        {
            bool mine = !string.IsNullOrEmpty(_id) && _id == targetId;
            if (mine == _active) return;
            _active = mine;
            if (_active) Begin();
            else Stop();
        }

        private void OnStepCleared()
        {
            if (!_active) return;
            _active = false;
            Stop();
        }

        private void Begin()
        {
            if (_rect != null) return;           // UI는 맥동만 — 화살표 불요
            if (_arrow == null) _arrow = BuildArrow();
            _arrow.SetActive(true);
        }

        private void Stop()
        {
            if (_rect != null && _baseScale != Vector3.zero) transform.localScale = _baseScale;
            if (_arrow != null) _arrow.SetActive(false);
        }

        private void Update()
        {
            if (!_active) return;
            // unscaled — 정산창(timeScale=0)에서도 멈추지 않는다.
            float wave = Mathf.Sin(Time.unscaledTime * _pulseSpeed * Mathf.PI * 2f);

            if (_rect != null)
            {
                transform.localScale = _baseScale * Mathf.Lerp(1f, _pulseScale, (wave + 1f) * 0.5f);
                return;
            }

            if (_arrow == null) return;
            // 화살표만 흔든다 — **대상 자체는 건드리지 않는다**(물리 비침습).
            Vector3 local = _arrow.transform.localPosition;
            local.y = _arrowBaseY + wave * _arrowBob;
            _arrow.transform.localPosition = local;
        }

        /// <summary>
        /// 대상 머리 위 아래방향 화살표. 콜라이더 없는 순수 비주얼이라 물리에 영향이 없다.
        /// 대상의 결합 바운즈 위에 띄운다 — 크기가 제각각인 상자·자판기를 한 규칙으로 다룬다.
        /// </summary>
        private GameObject BuildArrow()
        {
            float topY = 1f;
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                topY = bounds.max.y - transform.position.y;
            }

            GameObject root = new GameObject("TutorialArrow");
            root.transform.SetParent(transform, false);
            _arrowBaseY = topY + _arrowGap;
            root.transform.localPosition = new Vector3(0f, _arrowBaseY, 0f);

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = ARROW_COLOR;
            material.SetFloat("_Cull", 0f); // 양면 — 뿔 감기 방향을 신경 쓰지 않아도 된다

            // 화살촉: 아래로 뾰족한 사각뿔. 큐브를 45° 비틀어 쓰면 육각형으로 찌그러져
            // 체스 말처럼 읽힌다(실측) — 방향을 가리키는 게 목적이므로 꼭짓점이 있어야 한다.
            GameObject head = new GameObject("Head");
            head.transform.SetParent(root.transform, false);
            head.AddComponent<MeshFilter>().sharedMesh = BuildDownwardPyramid(0.22f, 0.34f);
            head.AddComponent<MeshRenderer>().sharedMaterial = material;

            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "Shaft";
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            shaft.transform.localScale = new Vector3(0.12f, 0.34f, 0.12f);
            Destroy(shaft.GetComponent<Collider>()); // 물리 개입 금지 — 이게 이번 수리의 요점이다
            shaft.GetComponent<Renderer>().sharedMaterial = material;
            return root;
        }

        /// <summary>꼭짓점이 원점, 밑면이 +y인 사각뿔. 콜라이더 없음.</summary>
        private static Mesh BuildDownwardPyramid(float halfWidth, float height)
        {
            var mesh = new Mesh { name = "TutorialArrowHead" };
            mesh.vertices = new[]
            {
                Vector3.zero,                                    // 0 — 꼭짓점(아래)
                new Vector3(-halfWidth, height, -halfWidth),     // 1
                new Vector3( halfWidth, height, -halfWidth),     // 2
                new Vector3( halfWidth, height,  halfWidth),     // 3
                new Vector3(-halfWidth, height,  halfWidth),     // 4
            };
            mesh.triangles = new[]
            {
                0, 1, 2,  0, 2, 3,  0, 3, 4,  0, 4, 1,   // 옆면 4장
                1, 3, 2,  1, 4, 3,                        // 밑면(위에서 내려다볼 때 보인다)
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
