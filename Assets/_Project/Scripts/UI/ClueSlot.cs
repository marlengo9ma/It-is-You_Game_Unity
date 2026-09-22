using System;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 단서 목록의 칸 하나.
    ///
    /// 기획서의 표기 규칙을 그대로 구현한다.
    /// 마우스 오버 시에는 ??? 또는 단순 명칭, 목록에서 한 번 읽고 나면 세부 명칭.
    /// "'단서 오브젝트'로 변경된 수집품의 경우, 해당 수집품의 아이콘 칸의 테두리에
    ///  금색의 테두리가 둘러쌓이는 등, 별도로 표시가 활성화 된다."
    /// 일반/특수의 구분은 수집 연출이 아니라 이 칸 안의 표기로만 드러난다.
    /// </summary>
    [AddComponentMenu("It is You/Clue Slot")]
    public class ClueSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;

        [Tooltip("이름표 글자.")]
        [SerializeField] private TMP_Text hoverLabel;

        [Tooltip("이름표를 통째로 켜고 끌 오브젝트. 글자 뒤에 배경이 있을 때 그 배경을 넣는다. " +
                 "비워 두면 글자 오브젝트 자체를 켜고 끈다.")]
        [SerializeField] private GameObject hoverRoot;

        [Tooltip("아직 읽지 않은 단서에 붙는 표시.")]
        [SerializeField] private GameObject unreadBadge;

        [Tooltip("읽었지만 아직 단서로 정리하지 않은 수집품에 붙는 표시. 강조 구간을 누르라는 신호.")]
        [SerializeField] private GameObject usableBadge;

        [Tooltip("단서 오브젝트로 승격된 칸의 금색 테두리.")]
        [SerializeField] private GameObject promotedFrame;

        [Tooltip("단서 조합에서 자리에 올라간 칸의 표시. 없어도 된다.")]
        [SerializeField] private GameObject selectedMark;

        [Tooltip("켜면 이름표를 항상 보인다. 정황/실마리 목록의 줄 형태에서 쓴다.")]
        [SerializeField] private bool alwaysShowLabel = false;

        private ClueDefinition _clue;
        private bool _read;
        private Action<ClueDefinition> _onClick;

        public ClueDefinition Clue => _clue;

        private GameObject HoverTarget =>
            hoverRoot != null ? hoverRoot : (hoverLabel != null ? hoverLabel.gameObject : null);

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
            SetHoverVisible(false);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(ClueDefinition clue, bool hasBeenRead, bool promoted, Action<ClueDefinition> onClick)
        {
            _clue = clue;
            _read = hasBeenRead;
            _onClick = onClick;

            if (icon != null)
            {
                icon.sprite = clue.Icon;
                icon.enabled = clue.Icon != null;
            }

            if (hoverLabel != null) hoverLabel.text = clue.DisplayName(hasBeenRead);
            if (unreadBadge != null) unreadBadge.SetActive(!hasBeenRead);

            // 읽기 전에는 이 수집품이 쓸모 있는지조차 알려주지 않는다.
            // 읽어야 알 수 있다는 것이 이 게임의 수집 설계다.
            if (usableBadge != null) usableBadge.SetActive(hasBeenRead && clue.IsPromotable && !promoted);
            if (promotedFrame != null) promotedFrame.SetActive(promoted && clue.Kind == ClueKind.Object);

            // 다시 바인딩되면 이름표는 접어 둔다.
            SetHoverVisible(false);
            SetSelected(false);
        }

        public void SetSelected(bool on)
        {
            if (selectedMark != null) selectedMark.SetActive(on);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_clue == null) return;
            if (hoverLabel != null) hoverLabel.text = _clue.DisplayName(_read);
            SetHoverVisible(true);
        }

        public void OnPointerExit(PointerEventData eventData) => SetHoverVisible(false);

        private void SetHoverVisible(bool visible)
        {
            var target = HoverTarget;
            if (target != null) target.SetActive(visible || alwaysShowLabel);
        }

        private void HandleClick()
        {
            if (_clue == null) return;
            _onClick?.Invoke(_clue);
        }
    }
}
