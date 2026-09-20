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
    /// 기획서의 2단계 표기를 그대로 구현한다. 마우스 오버 시에는 ??? 또는 단순 명칭,
    /// 목록에서 한 번 읽고 나면 세부 명칭. 그리고 일반/특수의 구분은 수집 연출이 아니라
    /// 이 칸 안의 표기로만 드러난다.
    /// </summary>
    [AddComponentMenu("It is You/Clue Slot")]
    public class ClueSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text hoverLabel;

        [Tooltip("아직 읽지 않은 단서에 붙는 표시.")]
        [SerializeField] private GameObject unreadBadge;

        [Tooltip("심문에서 제시할 수 있는 단서라는 표기. 툴팁을 읽은 뒤에만 켜진다.")]
        [SerializeField] private GameObject usableBadge;

        private ClueDefinition _clue;
        private bool _read;
        private Action<ClueDefinition> _onClick;

        public ClueDefinition Clue => _clue;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
            if (hoverLabel != null) hoverLabel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(ClueDefinition clue, bool hasBeenRead, Action<ClueDefinition> onClick)
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

            // 읽기 전에는 이 단서가 쓸모 있는지조차 알려주지 않는다.
            // 읽어야 알 수 있다는 것이 이 게임의 수집 설계다.
            if (usableBadge != null) usableBadge.SetActive(hasBeenRead && clue.HasUsableHighlight);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hoverLabel == null || _clue == null) return;
            hoverLabel.text = _clue.DisplayName(_read);
            hoverLabel.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (hoverLabel != null) hoverLabel.gameObject.SetActive(false);
        }

        private void HandleClick()
        {
            if (_clue == null) return;
            _onClick?.Invoke(_clue);
        }
    }
}
