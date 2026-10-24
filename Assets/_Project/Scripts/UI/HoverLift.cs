using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 마우스를 올리면 버튼이 살짝 떠오르고 밝아진다 (타이틀 메뉴).
    ///
    /// 눌리는 판(이 오브젝트)은 그대로 두고 보이는 판(visual)만 움직인다 — 가장자리에서 떨리지 않게.
    /// 눌리지 않는 버튼(interactable 꺼짐)은 반응하지 않고 흐리게 보인다.
    /// </summary>
    [AddComponentMenu("It is You/Hover Lift")]
    [DisallowMultipleComponent]
    public class HoverLift : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Button button;
        [Tooltip("떠오르는 판. 보통 Visual.")]
        [SerializeField] private RectTransform visual;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;

        [Tooltip("떠오르는 높이(픽셀, 1920×1080 기준).")]
        [SerializeField] private float liftPixels = 8f;
        [Tooltip("떠오르고 가라앉는 데 걸리는 시간(초).")]
        [SerializeField] private float seconds = 0.14f;
        [Tooltip("마우스를 올렸을 때 판 색.")]
        [SerializeField] private Color highlightColor = new Color(0.30f, 0.24f, 0.12f, 0.92f);
        [Tooltip("마우스를 올렸을 때 글자 색.")]
        [SerializeField] private Color labelHighlightColor = new Color(0.95f, 0.84f, 0.55f, 1f);
        [Tooltip("눌리지 않는 버튼의 흐림 정도(0~1).")]
        [SerializeField] private float disabledAlpha = 0.45f;

        private Color _baseColor, _baseLabel;
        private Vector2 _basePos;
        private bool _captured;
        private bool _hover;
        private float _k;   // 0 = 평소, 1 = 떠오름

        private void Awake() => Capture();

        private void Capture()
        {
            if (_captured) return;
            _captured = true;
            if (background != null) _baseColor = background.color;
            if (label != null) _baseLabel = label.color;
            if (visual != null) _basePos = visual.anchoredPosition;
        }

        private void OnDisable()
        {
            _hover = false;
            _k = 0f;
            Apply();
        }

        private bool Interactable => button == null || button.IsInteractable();

        public void OnPointerEnter(PointerEventData eventData) => _hover = true;
        public void OnPointerExit(PointerEventData eventData) => _hover = false;
        public void OnSelect(BaseEventData eventData) => _hover = true;
        public void OnDeselect(BaseEventData eventData) => _hover = false;

        private void Update()
        {
            var target = _hover && Interactable ? 1f : 0f;
            var changed = _dirtyInteractable();
            if (Mathf.Approximately(_k, target) && !changed) return;
            _k = Mathf.MoveTowards(_k, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds));
            Apply();
        }

        private bool _lastInteractable = true;
        private bool _dirtyInteractable()
        {
            var now = Interactable;
            if (now == _lastInteractable) return false;
            _lastInteractable = now;
            return true;
        }

        private void Apply()
        {
            Capture();
            var e = _k * _k * (3f - 2f * _k);
            if (visual != null) visual.anchoredPosition = _basePos + new Vector2(0f, liftPixels * e);
            var dim = Interactable ? 1f : disabledAlpha;
            if (background != null)
            {
                var c = Color.Lerp(_baseColor, highlightColor, e);
                c.a *= dim;
                background.color = c;
            }
            if (label != null)
            {
                var c = Color.Lerp(_baseLabel, labelHighlightColor, e);
                c.a *= dim;
                label.color = c;
            }
        }

        private void Start() => Apply();
    }
}
