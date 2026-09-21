using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// ‹ 값 › 형태의 선택 컨트롤. 드롭다운 대신 쓴다 — 게임 설정 창에서 익숙하고,
    /// 마우스만으로도 키보드만으로도 다루기 쉽다.
    /// </summary>
    [AddComponentMenu("It is You/Option Cycler")]
    public class OptionCycler : MonoBehaviour
    {
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text valueLabel;

        private readonly List<string> _options = new List<string>();
        private int _index;

        /// <summary>선택이 바뀌었다. (새 인덱스)</summary>
        public event Action<int> Changed;

        public int Index => _index;
        public int Count => _options.Count;
        public string Current => _options.Count > 0 ? _options[Mathf.Clamp(_index, 0, _options.Count - 1)] : string.Empty;

        private void Awake()
        {
            if (prevButton != null) prevButton.onClick.AddListener(() => Step(-1));
            if (nextButton != null) nextButton.onClick.AddListener(() => Step(1));
        }

        public void SetOptions(IList<string> options, int index)
        {
            _options.Clear();
            if (options != null) _options.AddRange(options);
            _index = _options.Count == 0 ? 0 : Mathf.Clamp(index, 0, _options.Count - 1);
            Render();
        }

        public void SetIndex(int index, bool notify = false)
        {
            if (_options.Count == 0) return;
            _index = Mathf.Clamp(index, 0, _options.Count - 1);
            Render();
            if (notify) Changed?.Invoke(_index);
        }

        public void SetInteractable(bool value)
        {
            if (prevButton != null) prevButton.interactable = value;
            if (nextButton != null) nextButton.interactable = value;
            if (valueLabel != null) valueLabel.alpha = value ? 1f : 0.45f;
        }

        private void Step(int delta)
        {
            if (_options.Count == 0) return;
            _index = (_index + delta + _options.Count) % _options.Count;
            Render();
            Changed?.Invoke(_index);
        }

        private void Render()
        {
            if (valueLabel != null) valueLabel.text = Current;
        }
    }
}
