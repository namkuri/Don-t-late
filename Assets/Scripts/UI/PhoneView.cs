using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontLate
{
    /// <summary>
    /// 스마트폰 "배송상차" 화면(View) — S-011. Tab으로 좌하단에서 GTA식 슬라이드 개폐.
    /// 열려 있는 동안 마우스가 가리키는 상자의 송장번호를 보여주고, 클릭하면 바코드 등록
    /// (등록은 WorldDeliveryManager 몫 — 여기는 레이캐스트·표시만). 중복이면 경고.
    /// 목록 컬럼: No · 운송장번호 · 배송순번(마감 빠른 순 제안) · 목적지.
    /// </summary>
    public class PhoneView : MonoBehaviour
    {
        private const float SLIDE_SECONDS = 0.22f;

        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _hoverLabel;
        [SerializeField] private TMP_Text _listLabel;
        [SerializeField] private TMP_Text _warnLabel;
        [Tooltip("닫힘/열림 anchoredPosition Y.")]
        [SerializeField] private float _hiddenY = -640f;
        [SerializeField] private float _shownY = 24f;

        private readonly List<DeliveryData> _scanned = new List<DeliveryData>();
        // 운송장별 상태 (S-014): 0=진행 · 1=완료 · 2=지각 실패. 콘솔만 보던 지각을 폰에서 보이게.
        private readonly Dictionary<int, int> _status = new Dictionary<int, int>();
        private InputAction _toggle;
        private bool _open;
        private Coroutine _slide;
        private Coroutine _warnFade;

        private void Awake()
        {
            _toggle = new InputAction("PhoneToggle", InputActionType.Button);
            _toggle.AddBinding("<Keyboard>/tab");
        }

        private void OnEnable()
        {
            _toggle.Enable();
            _toggle.performed += OnToggle;
            WorldEvents.BarcodeScanned += OnBarcodeScanned;
            WorldEvents.DeliveryCompleted += OnDeliveryCompleted;
            WorldEvents.DeliveryFailed += OnDeliveryFailed;
            WorldEvents.ClockTicked += OnClockTicked;
        }

        private void OnDisable()
        {
            _toggle.performed -= OnToggle;
            _toggle.Disable();
            WorldEvents.BarcodeScanned -= OnBarcodeScanned;
            WorldEvents.DeliveryCompleted -= OnDeliveryCompleted;
            WorldEvents.DeliveryFailed -= OnDeliveryFailed;
            WorldEvents.ClockTicked -= OnClockTicked;
        }

        private void OnDeliveryCompleted(DeliveryData data) { _status[data.OrderId] = 1; RefreshList(); }
        private void OnDeliveryFailed(DeliveryData data) { _status[data.OrderId] = 2; RefreshList(); }

        // 남은 시간 표시용 — 열려 있을 때만 분 단위로 갱신 (S-015).
        private void OnClockTicked(GameClock clock)
        {
            _lastClockMinute = clock.MinuteOfDay;
            if (_open) RefreshList();
        }

        private int _lastClockMinute = -1;

        private void OnDestroy() => _toggle.Dispose();

        private void Start()
        {
            if (_panel != null)
                _panel.anchoredPosition = new Vector2(_panel.anchoredPosition.x, _hiddenY);
            RefreshList();
            if (_warnLabel != null) _warnLabel.text = string.Empty;
        }

        private void Update()
        {
            if (!_open) return;
            ScanPointer();
        }

        // ── 개폐 ─────────────────────────────────────────────

        private void OnToggle(InputAction.CallbackContext _)
        {
            _open = !_open;
            if (_slide != null) StopCoroutine(_slide);
            _slide = StartCoroutine(Slide(_open ? _shownY : _hiddenY));
            if (!_open && _hoverLabel != null) _hoverLabel.text = "-";
        }

        private IEnumerator Slide(float targetY)
        {
            float startY = _panel.anchoredPosition.y;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / SLIDE_SECONDS;
                float y = Mathf.Lerp(startY, targetY, Mathf.SmoothStep(0f, 1f, t));
                _panel.anchoredPosition = new Vector2(_panel.anchoredPosition.x, y);
                yield return null;
            }
            _slide = null;
        }

        // ── 스캔 (표시 계산 + 등록 위임) ─────────────────────

        private void ScanPointer()
        {
            Camera camera = Camera.main;
            Mouse mouse = Mouse.current;
            if (camera == null || mouse == null) return;

            // 첫 히트만 보면 WalkableVolume(거리 전체를 덮는 트리거)이 상자를 가린다 —
            // 전체 히트에서 PickupBox만 골라 가장 가까운 것을 취한다 (S-011 수정).
            PickupBox box = null;
            float nearest = float.MaxValue;
            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            foreach (RaycastHit hit in Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Collide))
            {
                if (!hit.collider.TryGetComponent(out PickupBox candidate)) continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                box = candidate;
            }

            if (_hoverLabel != null)
                _hoverLabel.text = box != null && box.Order != null
                    ? "송장 " + InvoiceNo(box.Order.orderId)
                    : "-";

            if (box == null || box.Order == null) return;
            if (!mouse.leftButton.wasPressedThisFrame) return;

            if (WorldDeliveryManager.Instance == null) return;
            if (!WorldDeliveryManager.Instance.RegisterBarcode(box.Order))
                ShowWarn("⚠ " + InvoiceNo(box.Order.orderId) + " — 이미 등록된 운송장");
        }

        private void OnBarcodeScanned(DeliveryData data)
        {
            _scanned.Add(data);
            RefreshList();
        }

        // ── 표시 ─────────────────────────────────────────────

        private static string InvoiceNo(int orderId) => "DL-" + orderId.ToString("0000");

        private void RefreshList()
        {
            if (_listLabel == null) return;

            var sb = new System.Text.StringBuilder();
            sb.Append("<color=#8a93a8>No  운송장      순번  목적지</color>\n");

            // 배송순번 = 마감 빠른 순 제안 순위.
            var byDeadline = new List<DeliveryData>(_scanned);
            byDeadline.Sort((a, b) => a.DeadlineMinuteOfDay.CompareTo(b.DeadlineMinuteOfDay));

            for (int i = 0; i < _scanned.Count; i++)
            {
                DeliveryData d = _scanned[i];
                int rank = byDeadline.FindIndex(x => x.OrderId == d.OrderId) + 1;
                string row = (i + 1).ToString().PadRight(4)
                    + InvoiceNo(d.OrderId).PadRight(12)
                    + rank.ToString().PadRight(6)
                    + d.Address;

                int status = _status.TryGetValue(d.OrderId, out int s) ? s : 0;
                if (status == 1) sb.Append("<color=#8a93a8>").Append(row).Append("  ✓완료</color>");
                else if (status == 2) sb.Append("<color=#ff7359><s>").Append(row).Append("</s>  지각</color>");
                else sb.Append(row);
                sb.Append('\n');

                // 구역·남은 시간 부제 줄 (S-015) — 어디로 가야 하고 얼마나 급한지.
                if (status == 0)
                {
                    string where = string.IsNullOrEmpty(d.District) ? "구역 미지정" : d.District;
                    if (_lastClockMinute >= 0)
                    {
                        int remain = Mathf.RoundToInt(d.DeadlineMinuteOfDay) - _lastClockMinute;
                        string remainText = remain >= 0
                            ? (remain <= 30 ? "<color=#ff9f45>남은 " + remain + "분</color>" : "남은 " + remain + "분")
                            : "<color=#ff7359>마감 지남</color>";
                        sb.Append("<size=80%><color=#8a93a8>    └ </color>").Append(where)
                          .Append("<color=#8a93a8> · </color>").Append(remainText).Append("</size>\n");
                    }
                    else
                    {
                        sb.Append("<size=80%><color=#8a93a8>    └ </color>").Append(where).Append("</size>\n");
                    }
                }
            }
            if (_scanned.Count == 0) sb.Append("<color=#8a93a8>박스를 클릭해 송장을 찍어라</color>");

            _listLabel.text = sb.ToString();
        }

        private void ShowWarn(string message)
        {
            if (_warnLabel == null) return;
            if (_warnFade != null) StopCoroutine(_warnFade);
            _warnLabel.text = message;
            _warnFade = StartCoroutine(ClearWarnAfter(1.6f));
        }

        private IEnumerator ClearWarnAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            _warnLabel.text = string.Empty;
            _warnFade = null;
        }
    }
}
