using System.Collections.Generic;
using ItIsYou.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 세이브 칸 창 — 자동 저장 1칸 + 수동 저장 4칸 (2026-11-27).
    ///
    /// 저장하기: 수동 칸(1~4)을 누르면 지금 자리를 적는다. 이미 적힌 칸은 한 번 더 눌러야 덮어쓴다.
    /// 불러오기: 적힌 칸을 누르면 그 자리부터 이어 간다. 게임 중이면 한 번 더 눌러야 불러온다(지금 진행이 사라지므로).
    /// 타이틀의 '이어하기', 일시 정지 메뉴, 데모 종료 화면, 게임오버 화면이 같은 창을 쓴다.
    /// </summary>
    [AddComponentMenu("It is You/Save Slot View")]
    public class SaveSlotView : MonoBehaviour
    {
        public enum Mode { Save, Load }

        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text title;
        [Tooltip("칸 버튼. 0번 = 자동 저장, 1~4번 = 수동 저장.")]
        [SerializeField] private List<Button> slotButtons = new List<Button>();
        [SerializeField] private List<TMP_Text> slotNames = new List<TMP_Text>();
        [SerializeField] private List<TMP_Text> slotDetails = new List<TMP_Text>();
        [SerializeField] private TMP_Text message;
        [SerializeField] private Button closeButton;

        [Header("문구")]
        [SerializeField] private string saveTitle = "저장하기";
        [SerializeField] private string loadTitle = "불러오기";
        [SerializeField] private string emptyText = "— 비어 있음 —";
        [SerializeField] private string confirmOverwrite = "한 번 더 누르면 이 칸에 덮어씁니다.";
        [SerializeField] private string confirmLoad = "한 번 더 누르면 불러옵니다. 지금 진행 중인 내용은 저장되지 않습니다.";
        [SerializeField] private string savedFormat = "{0} 칸에 저장했습니다.";
        [SerializeField] private string autoNotWritable = "자동 저장 칸은 페이즈가 시작될 때와 대사마다 저절로 적힙니다.";

        private Mode _mode;
        private int _armed = -1;
        private bool _wired;

        /// <summary>어느 씬에서든 — 이 창이 떠 있으면 ESC 가 일시 정지 대신 이 창을 닫는다.</summary>
        public static bool AnyOpen { get; private set; }

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            Wire();
            if (root != null && root != gameObject) root.SetActive(false);
            AnyOpen = false;
        }

        private void OnDestroy() => AnyOpen = false;

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            for (int i = 0; i < slotButtons.Count; i++)
            {
                var index = i;
                if (slotButtons[i] != null) slotButtons[i].onClick.AddListener(() => OnSlot(index));
            }
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            if (!IsOpen) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }

        public void Open(Mode mode)
        {
            Wire();
            _mode = mode;
            _armed = -1;
            if (root != null) { root.SetActive(true); root.transform.SetAsLastSibling(); }
            AnyOpen = true;
            if (title != null) title.text = mode == Mode.Save ? saveTitle : loadTitle;
            SetMessage(string.Empty);
            Refresh();
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
            AnyOpen = false;
            _armed = -1;
        }

        private void Refresh()
        {
            for (int i = 0; i < slotButtons.Count; i++)
            {
                var data = SaveSystem.Read(i);
                if (i < slotNames.Count && slotNames[i] != null) slotNames[i].text = SaveSystem.SlotName(i);
                if (i < slotDetails.Count && slotDetails[i] != null)
                    slotDetails[i].text = data == null ? emptyText : $"{data.label}\n<size=80%>{data.savedAt}</size>";
                if (slotButtons[i] == null) continue;
                slotButtons[i].interactable = _mode == Mode.Save ? i != SaveSystem.AutoSlot : data != null;
            }
        }

        private void OnSlot(int index)
        {
            if (_mode == Mode.Save)
            {
                if (index == SaveSystem.AutoSlot) { SetMessage(autoNotWritable); return; }
                if (SaveSystem.Has(index) && _armed != index)
                {
                    _armed = index;
                    SetMessage(confirmOverwrite);
                    return;
                }
                _armed = -1;
                if (SaveSystem.SaveCurrent(index)) SetMessage(string.Format(savedFormat, SaveSystem.SlotName(index)));
                Refresh();
                return;
            }

            var data = SaveSystem.Read(index);
            if (data == null) return;
            // 게임 중(저장할 자리가 있는 씬)이면 한 번 더 확인한다.
            if (SaveSystem.CurrentProvider != null && _armed != index)
            {
                _armed = index;
                SetMessage(confirmLoad);
                return;
            }
            Close();
            SaveSystem.LoadAndPlay(data);
        }

        private void SetMessage(string text)
        {
            if (message != null) message.text = text ?? string.Empty;
        }
    }
}
