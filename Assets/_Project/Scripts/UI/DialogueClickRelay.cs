using UnityEngine;
using UnityEngine.EventSystems;

namespace ItIsYou.UI
{
    /// <summary>
    /// 말풍선처럼 대사창과 떨어져 있는 판의 클릭을 대사창으로 넘긴다.
    /// 밑줄 구간 수집과 넘기기가 어느 판에서든 같은 규칙으로 동작해야 한다.
    /// </summary>
    [AddComponentMenu("It is You/Dialogue Click Relay")]
    public class DialogueClickRelay : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private DialoguePresenter target;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (target != null) target.OnPointerClick(eventData);
        }
    }
}
