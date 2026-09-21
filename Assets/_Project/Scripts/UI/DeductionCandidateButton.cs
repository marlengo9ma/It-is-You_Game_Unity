using System;
using System.Collections.Generic;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 단서 조합 페이즈의 후보 버튼 하나 — 아이콘 + 이름. 텍스트는 이름뿐이다.
    /// </summary>
    [AddComponentMenu("It is You/Deduction Candidate Button")]
    public class DeductionCandidateButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text iconPlaceholder;
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup group;

        private ClueDefinition _clue;
        private Action<ClueDefinition> _onClick;

        public ClueDefinition Clue => _clue;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(ClueDefinition clue, string displayName, bool triedWrong, Action<ClueDefinition> onClick)
        {
            _clue = clue;
            _onClick = onClick;

            if (icon != null)
            {
                icon.sprite = clue.Icon;
                icon.enabled = clue.Icon != null;
            }
            if (iconPlaceholder != null) iconPlaceholder.gameObject.SetActive(clue.Icon == null);
            if (label != null) label.text = displayName;

            // 틀려 본 후보는 흐리게. 누를 수는 있다 — 다시 틀리는 것도 플레이어의 선택이다.
            if (group != null) group.alpha = triedWrong ? 0.4f : 1f;
        }

        public void SetInteractable(bool value)
        {
            if (button != null) button.interactable = value;
        }

        private void HandleClick()
        {
            if (_clue == null) return;
            _onClick?.Invoke(_clue);
        }
    }
}
