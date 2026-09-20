using System;
using ItIsYou.Data;
using ItIsYou.Interrogation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 선택지 버튼 하나.
    ///
    /// 기획서: "메인 선택지는 같은 선택지더라도 중복하여 여러 번 선택이 가능합니다.
    /// 경우에 따라, 중복되는 선택지를 선택하더라도 다른 내용의 심문을 이어갈 수 있습니다.
    /// 이 조건이 충족된 경우에는 이미 선택했었던 메인 선택지 위로, 별도로 아이콘이 표시 됩니다."
    ///
    /// 그래서 "이미 골랐음"과 "아직 볼 게 남았음"을 서로 다른 시각 요소로 분리한다.
    /// 흐림은 기시감을, 아이콘은 남은 여지를 나타낸다.
    /// </summary>
    [AddComponentMenu("It is You/Choice Button")]
    public class ChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image toneStrip;

        [Tooltip("아직 볼 내용이 남았다는 표시.")]
        [SerializeField] private GameObject unseenContentIcon;

        [Tooltip("이미 한 번 이상 고른 선택지에 붙는 흐림 처리.")]
        [SerializeField] private CanvasGroup usedDim;

        private ChoiceDefinition _definition;
        private Action<ChoiceDefinition> _onClick;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(ChoiceView view, Color toneColor, Action<ChoiceDefinition> onClick)
        {
            _definition = view.Definition;
            _onClick = onClick;

            if (label != null) label.text = view.Label;
            if (toneStrip != null) toneStrip.color = toneColor;

            if (unseenContentIcon != null)
                unseenContentIcon.SetActive(view.TimesUsed > 0 && view.HasUnseenContent);

            if (usedDim != null)
                usedDim.alpha = view.TimesUsed > 0 ? 0.72f : 1f;

            if (button != null) button.interactable = view.Affordable;
        }

        private void HandleClick()
        {
            if (_definition == null) return;
            _onClick?.Invoke(_definition);
        }
    }
}
