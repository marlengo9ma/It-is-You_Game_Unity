using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ItIsYou.UI
{
    /// <summary>
    /// TMP 본문 위의 &lt;link&gt; 구간 클릭을 링크 id 로 바꿔 알린다.
    /// 열람창의 강조 구간(수집 오브젝트 → 단서 오브젝트 승격)에 쓴다.
    /// </summary>
    [AddComponentMenu("It is You/TMP Link Click Relay")]
    [RequireComponent(typeof(TMP_Text))]
    public class TmpLinkClickRelay : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text _text;

        /// <summary>링크가 클릭됐다. (링크 id)</summary>
        public event Action<string> LinkClicked;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _text.raycastTarget = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_text == null) return;

            var canvas = _text.canvas;
            var cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                ? null : (canvas != null ? canvas.worldCamera : null);

            var index = TMP_TextUtilities.FindIntersectingLink(_text, eventData.position, cam);
            if (index < 0) return;

            var id = _text.textInfo.linkInfo[index].GetLinkID();
            if (!string.IsNullOrEmpty(id)) LinkClicked?.Invoke(id);
        }
    }
}
