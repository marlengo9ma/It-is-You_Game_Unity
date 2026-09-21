using System;
using System.Collections;
using System.Collections.Generic;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 숫자마다 페이지가 나뉜 튜토리얼 팝업.
    ///
    /// 기획: "각각 숫자마다 페이지가 구분되어 있는 팝업창이 팝업됨. 타이밍은 시나리오 텍스트 중간에 기재."
    /// 떠 있는 동안 뒤의 화면은 눌리지 않고, 무반응 타이머는 컨트롤러가 멈춘다.
    /// 같은 트리거의 팝업은 한 챕터에 한 번만 뜬다 — 그 기록도 여기서 든다.
    ///
    /// 본문은 서식 없이 그대로 찍는다. 원문에 &lt;메인 선택지&gt; 같은 꺾쇠가 많아
    /// TMP 가 태그로 읽어 지워 버리기 때문이다.
    /// 컴포넌트가 루트 자체에 붙어 꺼진 채 저장되므로, Awake 에서 루트를 다시 끄지 않는다.
    /// </summary>
    [AddComponentMenu("It is You/Tutorial Popup View")]
    public class TutorialPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField] private ScrollRect scroll;

        [Tooltip("다음 페이지로 넘기거나, 마지막 페이지에서는 닫는다.")]
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextLabel;

        private readonly HashSet<string> _shown = new HashSet<string>();
        private TutorialPopupDefinition _current;
        private int _page;
        private bool _open;
        private bool _wired;

        /// <summary>팝업이 열렸다. 타이머를 멈출 시점.</summary>
        public event Action Opened;

        /// <summary>팝업이 닫혔다.</summary>
        public event Action Closed;

        public bool IsOpen => _open;

        private void Awake()
        {
            Wire();
            if (!_open && root != null && root != gameObject) root.SetActive(false);
        }

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (bodyLabel != null) bodyLabel.richText = false;
        }

        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
        }

        /// <summary>이 팝업을 이미 띄운 적이 있는가.</summary>
        public bool HasShown(TutorialPopupDefinition popup)
            => popup != null && _shown.Contains(Key(popup));

        /// <summary>팝업을 띄우고 닫힐 때까지 기다린다. 이미 띄운 것이면 바로 돌아온다.</summary>
        public IEnumerator ShowOnce(TutorialPopupDefinition popup)
        {
            if (popup == null || popup.IsEmpty || HasShown(popup)) yield break;
            _shown.Add(Key(popup));
            yield return Show(popup);
        }

        /// <summary>팝업을 띄우고 닫힐 때까지 기다린다.</summary>
        public IEnumerator Show(TutorialPopupDefinition popup)
        {
            if (popup == null || popup.IsEmpty) yield break;

            Wire();
            _current = popup;
            _page = 0;
            _open = true;
            if (root != null) root.SetActive(true);
            Opened?.Invoke();
            Render();

            while (_open) yield return null;
        }

        /// <summary>다음 페이지 또는 닫기. 버튼과 바깥(시뮬레이션) 모두 이걸 부른다.</summary>
        public void Next() => OnNext();

        private void OnNext()
        {
            if (!_open || _current == null) return;

            if (_page + 1 < _current.Pages.Count)
            {
                _page++;
                Render();
                return;
            }

            Close();
        }

        public void Close()
        {
            if (!_open) return;
            _open = false;
            _current = null;
            if (root != null) root.SetActive(false);
            Closed?.Invoke();
        }

        private void Render()
        {
            if (_current == null) return;
            var total = _current.Pages.Count;

            if (titleLabel != null) titleLabel.text = _current.Title;
            if (bodyLabel != null) bodyLabel.text = _current.Pages[_page];
            if (pageLabel != null) pageLabel.text = total > 1 ? $"{_page + 1} / {total}" : string.Empty;
            if (nextLabel != null) nextLabel.text = _page + 1 < total ? "다음" : "닫기";
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        private static string Key(TutorialPopupDefinition popup)
            => string.IsNullOrEmpty(popup.Id) ? popup.Trigger.ToString() : popup.Id;
    }
}
