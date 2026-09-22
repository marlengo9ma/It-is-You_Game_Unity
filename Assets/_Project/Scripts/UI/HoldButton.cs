using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ItIsYou.UI
{
    /// <summary>
    /// 누르고 있는 동안만 켜지는 버튼. SKIP 처럼 "쥐고 있는 동안" 이 의미인 조작에 쓴다.
    /// 포인터가 버튼 밖으로 나가거나 손을 떼면 즉시 꺼진다.
    /// </summary>
    [AddComponentMenu("It is You/Hold Button")]
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public event Action<bool> HeldChanged;

        public bool Held { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => Set(true);
        public void OnPointerUp(PointerEventData eventData) => Set(false);
        public void OnPointerExit(PointerEventData eventData) => Set(false);

        private void OnDisable() => Set(false);

        private void Set(bool held)
        {
            if (Held == held) return;
            Held = held;
            HeldChanged?.Invoke(held);
        }
    }
}
