using System;
using System.Collections;
using System.Collections.Generic;
using ItIsYou.Clues;
using ItIsYou.Data;
using TMPro;
using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>
    /// 화면 우측의 단서 목록과 툴팁.
    ///
    /// 툴팁이 떠 있는 동안 무반응 타이머가 멈춰야 한다 —
    /// "단서 툴팁 팝업 시 - 텍스트 출력 중 및 출력 이후 1~20초 간 타이머 일시 정지".
    /// 타이머 자체를 직접 들고 있지는 않고, 열림/닫힘만 알린다.
    /// </summary>
    [AddComponentMenu("It is You/Clue Tray View")]
    public class ClueTrayView : MonoBehaviour
    {
        [Header("목록")]
        [SerializeField] private RectTransform container;
        [SerializeField] private ClueSlot slotPrefab;

        [Header("툴팁")]
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private TMP_Text tooltipTitle;
        [SerializeField] private TMP_Text tooltipBody;

        [Tooltip("툴팁을 닫은 뒤에도 타이머를 더 멈춰 두는 시간(초). 기획 범위 1~20초.")]
        [SerializeField] private float tooltipGraceSeconds = 3f;

        private readonly List<ClueSlot> _pool = new List<ClueSlot>();
        private ClueInventory _inventory;
        private Coroutine _grace;

        /// <summary>툴팁이 열렸다. 무반응 타이머를 멈출 시점.</summary>
        public event Action TooltipOpened;

        /// <summary>툴팁이 닫히고 유예 시간까지 지났다. 타이머를 다시 돌릴 시점.</summary>
        public event Action TooltipClosed;

        /// <summary>단서를 처음 읽었다. 표시 명칭이 바뀐다.</summary>
        public event Action<ClueDefinition> ClueRead;

        private void Awake()
        {
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
        }

        public void Rebuild(ClueInventory inventory)
        {
            _inventory = inventory;
            if (inventory == null || container == null || slotPrefab == null) return;

            var ids = inventory.OrderedIds;
            EnsurePool(ids.Count);

            for (int i = 0; i < _pool.Count; i++)
            {
                var active = i < ids.Count;
                _pool[i].gameObject.SetActive(active);
                if (!active) continue;

                var clue = inventory.Get(ids[i]);
                if (clue == null) continue;
                _pool[i].Bind(clue, inventory.HasBeenRead(clue.Id), OnSlotClicked);
            }
        }

        private void OnSlotClicked(ClueDefinition clue)
        {
            if (clue == null) return;

            var firstRead = _inventory != null && _inventory.MarkRead(clue.Id);

            if (tooltipTitle != null) tooltipTitle.text = clue.ReadName;
            if (tooltipBody != null)
            {
                // <hl> 구간이 강조색으로 바뀐다. 심문에서 쓸 수 있는 부분이라는 신호다.
                tooltipBody.text = DialogueMarkup.ToRichText(clue.Tooltip, null, collectable: false);
            }

            if (tooltipRoot != null) tooltipRoot.SetActive(true);

            if (_grace != null) StopCoroutine(_grace);
            TooltipOpened?.Invoke();

            if (firstRead)
            {
                ClueRead?.Invoke(clue);
                Rebuild(_inventory);
            }
        }

        public void CloseTooltip()
        {
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
            if (_grace != null) StopCoroutine(_grace);
            _grace = StartCoroutine(GraceRoutine());
        }

        private IEnumerator GraceRoutine()
        {
            if (tooltipGraceSeconds > 0f)
                yield return new WaitForSecondsRealtime(tooltipGraceSeconds);

            _grace = null;
            TooltipClosed?.Invoke();
        }

        private void EnsurePool(int needed)
        {
            while (_pool.Count < needed)
                _pool.Add(Instantiate(slotPrefab, container));
        }
    }
}
