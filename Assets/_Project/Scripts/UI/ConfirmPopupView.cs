using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 예/아니오 경고 팝업. 노션: "[아직 수집하지 못한 수집품/기록이 존재합니다. 단서 수집을 종료할까요?] 경고 팝업 후, 진행 여부 선택".
    /// 화면 전체를 반투명하게 덮어 뒤를 누르지 못하게 하고, 답을 받으면 닫힌다.
    /// </summary>
    [AddComponentMenu("It is You/Confirm Popup View")]
    public class ConfirmPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text message;
        [SerializeField] private Button yesButton;
        [SerializeField] private TMP_Text yesLabel;
        [SerializeField] private Button noButton;
        [SerializeField] private TMP_Text noLabel;

        private bool? _answer;
        private bool _wired;
        private bool _asking;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            Wire();
            // 루트가 꺼진 채로 씬에 저장돼 있으면 Awake 는 Ask 가 루트를 켜는 순간에야 불린다.
            // 그때 다시 끄면 팝업이 뜨자마자 사라지므로, 묻는 중에는 건드리지 않는다.
            if (root != null && !_asking) root.SetActive(false);
        }

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            if (yesButton != null) yesButton.onClick.AddListener(() => _answer = true);
            if (noButton != null) noButton.onClick.AddListener(() => _answer = false);
        }

        /// <summary>물어보고 답을 기다린다. done(예=true).</summary>
        public IEnumerator Ask(string text, string yes, string no, Action<bool> done)
        {
            Wire();
            _answer = null;
            _asking = true;
            if (message != null) message.text = text ?? string.Empty;
            if (yesLabel != null) yesLabel.text = yes ?? "예";
            if (noLabel != null) noLabel.text = no ?? "아니오";
            if (root != null) root.SetActive(true);
            while (_answer == null) yield return null;
            _asking = false;
            if (root != null) root.SetActive(false);
            done?.Invoke(_answer.Value);
        }

        /// <summary>바깥(테스트 등)에서 답한다.</summary>
        public void Answer(bool yes) => _answer = yes;
    }
}
