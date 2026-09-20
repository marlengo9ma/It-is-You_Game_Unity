using System.Collections.Generic;
using System.Text;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 지나간 대사를 다시 읽는 창.
    ///
    /// 대사를 넘기는 손이 화면을 앞질러 갈 때가 있다. 그때 방금 뭐라고 했는지
    /// 다시 볼 수 없으면 플레이어는 이야기를 놓친 채로 계속 눌러야 한다.
    ///
    /// 기록은 페이즈를 가리지 않고 계속 쌓되, 창을 여는 버튼은
    /// 평범한 대화 구간에서만 나타난다 — 심문 중에는 [기억 짐작] 이 그 역할을 한다.
    /// </summary>
    [AddComponentMenu("It is You/Dialogue Log View")]
    public class DialogueLogView : MonoBehaviour
    {
        [Header("기록 대상")]
        [SerializeField] private DialoguePresenter dialogue;

        [Header("화면")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text body;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text emptyLabel;

        [Header("한도")]
        [Tooltip("보관할 최대 줄 수. 넘치면 오래된 것부터 버린다.")]
        [SerializeField] private int maxLines = 300;

        private readonly List<string> _lines = new List<string>();
        private readonly StringBuilder _sb = new StringBuilder();

        public bool IsOpen => root != null && root.activeSelf;
        public int Count => _lines.Count;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (dialogue != null) dialogue.LineCompleted += Record;
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (dialogue != null) dialogue.LineCompleted -= Record;
        }

        /// <summary>한 줄이 끝까지 출력되면 기록한다.</summary>
        public void Record(DialogueLine line)
        {
            if (line == null) return;

            var text = DialogueMarkup.ToPlainText(line.Text);
            if (string.IsNullOrWhiteSpace(text)) return;

            var speaker = dialogue != null ? dialogue.SpeakerLabelFor(line.Speaker) : string.Empty;
            var entry = string.IsNullOrEmpty(speaker)
                ? text
                : $"<color={DialogueMarkup.HighlightColor}>{speaker}</color>\n{text}";

            _lines.Add(entry);
            if (_lines.Count > maxLines) _lines.RemoveAt(0);
        }

        public void Clear() => _lines.Clear();

        public void Open()
        {
            Rebuild();
            if (root != null) root.SetActive(true);

            // 마지막 줄부터 보여 준다. 방금 놓친 것을 찾으러 여는 창이다.
            if (scroll != null)
            {
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 0f;
            }
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void Rebuild()
        {
            if (emptyLabel != null) emptyLabel.gameObject.SetActive(_lines.Count == 0);
            if (body == null) return;

            _sb.Clear();
            for (int i = 0; i < _lines.Count; i++)
            {
                if (i > 0) _sb.Append("\n\n");
                _sb.Append(_lines[i]);
            }
            body.text = _sb.ToString();
        }
    }
}
