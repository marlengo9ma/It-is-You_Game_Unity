using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 이벤트 팝업 #2 — 째려보는 주인공. 단서 조합에서 틀렸을 때 무대에 뜬다.
    ///
    /// 기획: "선택지 오른쪽에 이벤트 팝업 #2". 그림이 없는 동안은 더미 판과 이름표.
    /// 조수의 반응 대사가 끝난 뒤 클릭하면 닫힌다.
    /// </summary>
    [AddComponentMenu("It is You/Glare Popup View")]
    public class GlarePopupView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image image;
        [SerializeField] private TMP_Text placeholder;
        [SerializeField] private TMP_Text caption;
        [SerializeField] private Color placeholderColor = new Color32(0x2E, 0x34, 0x3B, 0xFF);
        [SerializeField] private float fadeSeconds = 0.18f;

        private bool _showRequested;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            // 비활성 루트에 붙은 컴포넌트는 첫 활성화 때 Awake 가 돈다 — 그때 다시 끄면 안 된다.
            if (!_showRequested && root != null && root != gameObject) root.SetActive(false);
        }

        public void Show(Sprite sprite, string captionText)
        {
            _showRequested = true;
            if (root != null) root.SetActive(true);

            if (image != null)
            {
                image.sprite = sprite;
                image.color = sprite != null ? Color.white : placeholderColor;
                image.preserveAspect = true;
            }
            if (placeholder != null) placeholder.gameObject.SetActive(sprite == null);
            if (caption != null) caption.text = captionText ?? string.Empty;

            if (group != null)
            {
                group.alpha = 0f;
                StartCoroutine(Fade(1f));
            }
        }

        public void Hide()
        {
            _showRequested = false;
            if (root != null) root.SetActive(false);
        }

        /// <summary>클릭 · 스페이스 · 엔터로 닫힐 때까지 기다린다.</summary>
        public IEnumerator WaitForDismiss()
        {
            // 열리자마자 들어온 클릭(직전 대사를 넘긴 것)은 무시한다.
            yield return null;
            while (IsOpen)
            {
                var mouse = Mouse.current;
                var kb = Keyboard.current;
                var click = mouse != null && mouse.leftButton.wasPressedThisFrame;
                var key = kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
                if (click || key) break;
                yield return null;
            }
            Hide();
        }

        private IEnumerator Fade(float target)
        {
            if (group == null) yield break;
            var start = group.alpha;
            var t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, t / fadeSeconds);
                yield return null;
            }
            group.alpha = target;
        }
    }
}
