using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 단서 수집 페이즈에서 오브젝트를 눌렀을 때 화면 중앙에 뜨는 이미지 팝업.
    ///
    /// 기획: "상호 작용이 가능한 오브젝트 마우스 좌클릭 시 &gt;&gt; 화면 중앙에 단서의 이미지 팝업과
    /// &lt;주인공&gt; 혹은 &lt;조수&gt;의 설명 대사 출력 및 우측 ui내에 수집"
    ///
    /// 설명 대사는 대사창이 맡는다. 이 팝업은 그림과 이름만 보여 주고,
    /// 대사가 없는 오브젝트라면 클릭 한 번으로 닫힌다.
    ///
    /// 이 컴포넌트는 루트 오브젝트 자체에 붙어 있고 씬에는 꺼진 채 저장된다.
    /// 그래서 Awake 에서 루트를 다시 끄면 안 된다 — 첫 Show 가 루트를 켜는 순간 Awake 가 불리기 때문이다.
    /// </summary>
    [AddComponentMenu("It is You/Close-Up Popup")]
    public class CloseUpPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image image;
        [SerializeField] private TMP_Text caption;
        [SerializeField] private TMP_Text placeholder;
        [SerializeField] private GameObject continueHint;

        [Tooltip("그림이 없을 때 판의 색.")]
        [SerializeField] private Color placeholderColor = new Color32(0x2E, 0x34, 0x3B, 0xFF);

        private bool _showRequested;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (!_showRequested && root != null && root != gameObject) root.SetActive(false);
        }

        public void Show(Sprite sprite, string label)
        {
            _showRequested = true;
            if (image != null)
            {
                image.sprite = sprite;
                image.color = sprite != null ? Color.white : placeholderColor;
                image.preserveAspect = true;
            }
            if (placeholder != null)
            {
                placeholder.gameObject.SetActive(sprite == null);
                placeholder.text = string.IsNullOrEmpty(label) ? "(이미지 대기)" : label + "\n(이미지 대기)";
            }
            if (caption != null) caption.text = label ?? string.Empty;
            if (continueHint != null) continueHint.SetActive(false);
            if (root != null) root.SetActive(true);
        }

        public void Hide()
        {
            _showRequested = false;
            if (root != null) root.SetActive(false);
        }

        /// <summary>대사가 없을 때 — 클릭이나 스페이스/엔터로 닫힐 때까지 기다린다.</summary>
        public IEnumerator WaitForDismiss()
        {
            if (continueHint != null) continueHint.SetActive(true);

            // 팝업을 연 클릭이 같은 프레임에 닫기로 읽히지 않게 한 프레임 쉰다.
            yield return null;

            while (IsOpen)
            {
                var mouse = Mouse.current;
                var keyboard = Keyboard.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame) break;
                if (keyboard != null &&
                    (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) break;
                yield return null;
            }

            if (continueHint != null) continueHint.SetActive(false);
        }
    }
}
