using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ItIsYou.UI
{
    /// <summary>
    /// 눌림을 바깥으로 알리기만 하는 작은 부품. 어느 버튼으로 눌렀는지도 같이 넘긴다 —
    /// 추론 서류철에서 좌클릭은 고르기, 우클릭은 열람으로 갈린다.
    /// </summary>
    [AddComponentMenu("It is You/Click Relay")]
    public class ClickRelay : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<PointerEventData> Clicked;
        public event Action<bool> HoverChanged;

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(eventData);
        public void OnPointerEnter(PointerEventData eventData) => HoverChanged?.Invoke(true);
        public void OnPointerExit(PointerEventData eventData) => HoverChanged?.Invoke(false);
    }
}
