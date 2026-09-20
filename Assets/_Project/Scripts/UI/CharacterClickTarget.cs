using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 캐릭터 위의 클릭 판정 영역.
    ///
    /// 리액션 수집 창이 열려 있는 동안에만 의미가 있다.
    /// 창이 닫혀 있을 때 눌러도 아무 일도 일어나지 않아야 한다 —
    /// 아무 때나 눌러도 되면 "포착"이라는 행위가 성립하지 않는다.
    /// </summary>
    [AddComponentMenu("It is You/Character Click Target")]
    [RequireComponent(typeof(RectTransform))]
    public class CharacterClickTarget : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Graphic hitArea;
        [SerializeField] private CursorController cursor;

        private bool _armed;
        private bool _hovering;

        /// <summary>리액션 창이 열려 있는 동안 캐릭터를 클릭했다.</summary>
        public event Action ReactionClicked;

        private void Awake()
        {
            if (hitArea != null) hitArea.raycastTarget = true;
        }

        /// <summary>리액션 수집 창의 개폐에 맞춰 켜고 끈다.</summary>
        public void SetArmed(bool armed)
        {
            _armed = armed;
            UpdateCursor();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovering = true;
            UpdateCursor();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
            UpdateCursor();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_armed) return;
            ReactionClicked?.Invoke();
        }

        private void UpdateCursor()
        {
            if (cursor == null) return;
            cursor.SetReactionAvailable(_armed && _hovering);
        }
    }
}
