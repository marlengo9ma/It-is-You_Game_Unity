using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 튜토리얼의 &lt;화면이 어두워지고 …만이 하이라이트 된다&gt; 연출.
    ///
    /// 화면 전체를 어두운 판 네 장(위·아래·왼쪽·오른쪽)으로 덮고, 가운데 구멍만 비워 둔다.
    /// 구멍 안은 아래 화면이 그대로 보이고 그대로 눌린다 — 그래서 "저 물건을 눌러 보세요" 를
    /// 실제로 그 물건을 눌러서 따라 할 수 있다. 구멍 밖은 눌리지 않는다.
    /// 구멍 자리는 매 프레임 다시 잰다(열람창이 뜨거나 목록이 다시 그려져도 따라간다).
    /// </summary>
    [AddComponentMenu("It is You/Coach Mark View")]
    public class CoachMarkView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image top;
        [SerializeField] private Image bottom;
        [SerializeField] private Image left;
        [SerializeField] private Image right;
        [Tooltip("구멍 둘레의 테두리.")]
        [SerializeField] private Image frame;

        [Header("안내")]
        [SerializeField] private RectTransform caption;
        [SerializeField] private TMP_Text captionText;
        [SerializeField] private Button captionButton;
        [SerializeField] private TMP_Text captionButtonLabel;
        [SerializeField] private float holePadding = 12f;
        [SerializeField] private float captionMargin = 40f;
        [Tooltip("버튼이 있을 때 글 오른쪽에 비워 둘 폭.")]
        [SerializeField] private float buttonReserve = 220f;
        [SerializeField] private float captionPadding = 40f;

        private Func<(Vector3 min, Vector3 max)?> _hole;
        private bool _dim;
        private bool _shown;

        /// <summary>안내창의 버튼을 눌렀다.</summary>
        public event Action ButtonPressed;

        public bool IsShown => _shown;

        private void Awake()
        {
            if (captionButton != null) captionButton.onClick.AddListener(() => ButtonPressed?.Invoke());
            Hide();
        }

        /// <summary>
        /// 안내를 띄운다. hole 이 null 을 돌려주면 구멍 없이 전부 어둡게(dim 이 꺼져 있으면 덮지 않는다).
        /// 버튼 문구가 비면 버튼을 숨긴다 — 그때는 화면의 그 자리를 직접 눌러 넘어간다.
        /// </summary>
        public void Show(string text, string buttonLabel, bool dim, Func<(Vector3 min, Vector3 max)?> hole)
        {
            _hole = hole;
            _dim = dim;
            _shown = true;
            gameObject.SetActive(true);
            if (captionText != null) captionText.text = text ?? string.Empty;
            var hasButton = !string.IsNullOrEmpty(buttonLabel);
            if (captionButton != null) captionButton.gameObject.SetActive(hasButton);
            if (captionButtonLabel != null) captionButtonLabel.text = buttonLabel ?? string.Empty;
            if (captionText != null)
            {
                // 버튼이 없을 때는 버튼 자리까지 글을 넓혀, 줄이 일찍 꺾이지 않게 한다.
                var tr = captionText.rectTransform;
                tr.offsetMax = new Vector2(hasButton ? -buttonReserve : -captionPadding, tr.offsetMax.y);
            }
            if (caption != null) caption.gameObject.SetActive(!string.IsNullOrEmpty(text));
            Layout();
        }

        /// <summary>대사가 흐르는 동안처럼, 잠시 다 걷는다.</summary>
        public void Hide()
        {
            _shown = false;
            _hole = null;
            SetPanels(false);
            if (frame != null) frame.enabled = false;
            if (caption != null) caption.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_shown) Layout();
        }

        private void Layout()
        {
            if (root == null) return;
            var r = root.rect;
            (Vector3 min, Vector3 max)? world = null;
            try { world = _hole?.Invoke(); } catch { world = null; }

            if (world == null)
            {
                // 구멍 없음 — 전부 덮거나(설명만), 덮지 않는다.
                SetPanels(false);
                if (_dim && top != null) { top.enabled = true; Place(top, r.xMin, r.yMin, r.xMax, r.yMax); }
                if (frame != null) frame.enabled = false;
                PlaceCaption(r, null);
                return;
            }

            var a = root.InverseTransformPoint(world.Value.min);
            var b = root.InverseTransformPoint(world.Value.max);
            var hx0 = Mathf.Clamp(Mathf.Min(a.x, b.x) - holePadding, r.xMin, r.xMax);
            var hx1 = Mathf.Clamp(Mathf.Max(a.x, b.x) + holePadding, r.xMin, r.xMax);
            var hy0 = Mathf.Clamp(Mathf.Min(a.y, b.y) - holePadding, r.yMin, r.yMax);
            var hy1 = Mathf.Clamp(Mathf.Max(a.y, b.y) + holePadding, r.yMin, r.yMax);

            SetPanels(true);
            Place(top, r.xMin, hy1, r.xMax, r.yMax);
            Place(bottom, r.xMin, r.yMin, r.xMax, hy0);
            Place(left, r.xMin, hy0, hx0, hy1);
            Place(right, hx1, hy0, r.xMax, hy1);
            if (frame != null)
            {
                frame.enabled = true;
                Place(frame, hx0 - 4f, hy0 - 4f, hx1 + 4f, hy1 + 4f);
            }
            PlaceCaption(r, new Rect(hx0, hy0, hx1 - hx0, hy1 - hy0));
        }

        /// <summary>안내창은 구멍을 가리지 않는 쪽(위 또는 아래)에 놓는다.</summary>
        private void PlaceCaption(Rect r, Rect? hole)
        {
            if (caption == null) return;
            caption.anchorMin = caption.anchorMax = new Vector2(0.5f, 0.5f);
            var h = caption.rect.height;
            var topY = r.yMax - captionMargin - h * 0.5f;
            var bottomY = r.yMin + captionMargin + h * 0.5f;
            var y = topY;
            if (hole.HasValue)
            {
                var hr = hole.Value;
                var clearAbove = r.yMax - hr.yMax;
                var clearBelow = hr.yMin - r.yMin;
                y = clearAbove >= h + captionMargin * 2f || clearAbove >= clearBelow ? topY : bottomY;
            }
            caption.anchoredPosition = new Vector2(0f, y);
        }

        private void SetPanels(bool on)
        {
            if (top != null) top.enabled = on;
            if (bottom != null) bottom.enabled = on;
            if (left != null) left.enabled = on;
            if (right != null) right.enabled = on;
        }

        private static void Place(Image img, float x0, float y0, float x1, float y1)
        {
            if (img == null) return;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
            rt.anchoredPosition = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
        }

        /// <summary>RectTransform 의 세계 좌표 모서리 두 점.</summary>
        public static (Vector3 min, Vector3 max)? WorldRect(RectTransform rt)
        {
            if (rt == null || !rt.gameObject.activeInHierarchy) return null;
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return (c[0], c[2]);
        }
    }
}
