using System;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 용의자 선택 화면에 서 있는 인물 하나.
    ///
    /// 카드가 아니라 "그 자리에 앉아 있는 사람"으로 보여야 하므로,
    /// 틀을 그리지 않고 인물 그림 자체가 반응한다.
    /// 마우스를 올리면 살짝 떠오르고 테두리가 들어온다.
    ///
    /// 고를 수 없는 인물도 화면에는 남는다. 사라지면 "둘 중 하나를 고른다"는
    /// 상황 자체가 전달되지 않기 때문이다. 대신 눌러도 사유만 뜬다.
    /// </summary>
    [AddComponentMenu("It is You/Suspect Card")]
    public class SuspectCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("구성")]
        [SerializeField] private Button button;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text stateLabel;
        [SerializeField] private Outline highlight;
        [SerializeField] private CanvasGroup group;

        [Header("반응")]
        [Tooltip("마우스를 올렸을 때 떠오르는 높이(px).")]
        [SerializeField] private float hoverRise = 16f;

        [SerializeField] private float hoverSeconds = 0.12f;

        [Tooltip("고를 수 없는 인물의 흐림 정도.")]
        [SerializeField] private float lockedAlpha = 0.5f;

        private RectTransform _rect;
        private Vector2 _basePosition;
        private SuspectDefinition _suspect;
        private bool _selectable;
        private bool _armed;
        private Coroutine _move;

        /// <summary>이 인물을 골랐다.</summary>
        public event Action<SuspectDefinition> Picked;

        /// <summary>고를 수 없는 인물을 눌렀다. (사유)</summary>
        public event Action<string> Refused;

        public SuspectDefinition Suspect => _suspect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _basePosition = _rect.anchoredPosition;
            if (button != null) button.onClick.AddListener(HandleClick);
            if (highlight != null) highlight.enabled = false;
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(SuspectDefinition suspect)
        {
            _suspect = suspect;
            _selectable = suspect != null && suspect.Selectable;

            if (nameLabel != null) nameLabel.text = suspect != null ? suspect.DisplayName : string.Empty;

            if (portrait != null && suspect != null && suspect.Portrait != null)
            {
                portrait.sprite = suspect.Portrait;
                portrait.color = Color.white;
                portrait.preserveAspect = true;
            }

            if (stateLabel != null)
            {
                stateLabel.text = _selectable ? string.Empty : (suspect != null ? suspect.LockedReason : string.Empty);
                stateLabel.gameObject.SetActive(!_selectable);
            }

            if (group != null) group.alpha = _selectable ? 1f : lockedAlpha;
        }

        /// <summary>대화가 끝나기 전에는 아무도 고를 수 없다.</summary>
        public void SetArmed(bool armed)
        {
            _armed = armed;
            if (button != null) button.interactable = armed;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_armed) return;
            if (highlight != null) highlight.enabled = _selectable;
            MoveTo(_basePosition + new Vector2(0f, _selectable ? hoverRise : hoverRise * 0.25f));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (highlight != null) highlight.enabled = false;
            MoveTo(_basePosition);
        }

        private void HandleClick()
        {
            if (!_armed || _suspect == null) return;

            if (!_selectable)
            {
                Refused?.Invoke(_suspect.LockedReason);
                return;
            }
            Picked?.Invoke(_suspect);
        }

        private void MoveTo(Vector2 target)
        {
            if (!isActiveAndEnabled) return;
            if (_move != null) StopCoroutine(_move);
            _move = StartCoroutine(MoveRoutine(target));
        }

        private System.Collections.IEnumerator MoveRoutine(Vector2 target)
        {
            var from = _rect.anchoredPosition;
            var t = 0f;
            while (t < hoverSeconds)
            {
                t += Time.unscaledDeltaTime;
                _rect.anchoredPosition = Vector2.Lerp(from, target, t / hoverSeconds);
                yield return null;
            }
            _rect.anchoredPosition = target;
            _move = null;
        }
    }
}
