using System;
using System.Collections;
using System.Collections.Generic;
using ItIsYou.Clues;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 화면 우측의 단서 목록과, 칸을 눌렀을 때 뜨는 열람창.
    ///
    /// 기획 〈시스템 기획 및 화면 전환〉:
    /// "수집품 목록에서 해당 수집품을 좌클릭해서 읽을 시, 기존 화면이 반투명하게 어두워지며,
    ///  화면 중앙에 단서의 이미지 및 우측에 스크롤 가능한 텍스트 툴팁 창이 팝업된다.
    ///  툴팁 출력 중, 해당 수집품이 단서로 쓸 수 있는 오브젝트인 경우에는 텍스트의 일부가 강조 표시가 된다.
    ///  이를 누를 시, 해당 오브젝트는 단순 '수집 오브젝트'에서 '단서 오브젝트'로 변경된다."
    ///
    /// 열람창이 떠 있는 동안 무반응 타이머가 멈춰야 한다 —
    /// "단서 툴팁 팝업 시 - 텍스트 출력 중 및 출력 이후 1~20초 간 타이머 일시 정지".
    /// 타이머 자체를 직접 들고 있지는 않고, 열림/닫힘만 알린다.
    /// </summary>
    [AddComponentMenu("It is You/Clue Tray View")]
    public class ClueTrayView : MonoBehaviour
    {
        [Header("목록")]
        [Tooltip("물품 증거(수집 오브젝트) 격자.")]
        [SerializeField] private RectTransform container;
        [SerializeField] private ClueSlot slotPrefab;

        [Tooltip("정황·실마리(증언 · 리액션 · 추론) 목록. 비워 두면 전부 위 격자에 들어간다.")]
        [SerializeField] private RectTransform leadsContainer;
        [SerializeField] private ClueSlot leadPrefab;

        [Tooltip("〈가설〉(추론) 목록. 비워 두면 정황 목록에 섞인다.")]
        [SerializeField] private RectTransform hypothesesContainer;
        [SerializeField] private ClueSlot hypothesisPrefab;
        [Tooltip("가설이 하나도 없을 때 보이는 안내.")]
        [SerializeField] private GameObject hypothesesEmpty;

        [Header("열람창")]
        [Tooltip("화면 전체를 덮는 열람창 루트. 반투명 배경 + 가운데 이미지 + 우측 텍스트.")]
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private Image tooltipImage;
        [SerializeField] private TMP_Text tooltipImagePlaceholder;
        [SerializeField] private TMP_Text tooltipTitle;
        [SerializeField] private TMP_Text tooltipBody;
        [SerializeField] private ScrollRect tooltipScroll;

        [Tooltip("승격할 수 있는 수집품일 때 아래에 뜨는 안내. 승격 뒤에는 확정 문구로 바뀐다.")]
        [SerializeField] private TMP_Text promoteHint;

        [Tooltip("열람창을 닫는 버튼. 여기에 연결해 두면 런타임에 스스로 붙는다.")]
        [SerializeField] private Button closeButton;

        [Tooltip("열람창을 닫은 뒤에도 타이머를 더 멈춰 두는 시간(초). 기획 범위 1~20초.")]
        [SerializeField] private float tooltipGraceSeconds = 3f;

        [Tooltip("그림이 없을 때 판의 색.")]
        [SerializeField] private Color placeholderColor = new Color32(0x2E, 0x34, 0x3B, 0xFF);

        private readonly List<ClueSlot> _pool = new List<ClueSlot>();
        private readonly List<ClueSlot> _leadPool = new List<ClueSlot>();
        private readonly List<ClueSlot> _hypothesisPool = new List<ClueSlot>();
        private readonly HashSet<string> _selectedIds = new HashSet<string>();
        private bool _selectionMode;
        private ClueInventory _inventory;
        private ClueDefinition _reading;
        private TmpLinkClickRelay _linkRelay;
        private Coroutine _grace;
        private bool _tooltipOpen;

        /// <summary>
        /// 타이머를 멈춰 달라고 이미 알렸는가. 열람창을 닫자마자 다른 단서를 열면
        /// 유예 코루틴이 취소되면서 '다시 돌려라'가 영영 오지 않는다 — 그때 또 '멈춰라'를 보내면
        /// 짝이 어긋나 타이머가 영구히 멈춘다. 그래서 이 한 개로 짝을 맞춘다.
        /// </summary>
        private bool _timerHeld;

        /// <summary>열람창이 열렸다. 무반응 타이머를 멈출 시점.</summary>
        public event Action TooltipOpened;

        /// <summary>열람창이 닫히고 유예 시간까지 지났다. 타이머를 다시 돌릴 시점.</summary>
        public event Action TooltipClosed;

        /// <summary>단서를 처음 읽었다. 표시 명칭이 바뀐다.</summary>
        public event Action<ClueDefinition> ClueRead;

        /// <summary>수집 오브젝트가 단서 오브젝트로 승격됐다.</summary>
        public event Action<ClueDefinition> CluePromoted;

        /// <summary>고르기 모드에서 물품·정황 칸을 눌렀다. (단서 조합 페이즈)</summary>
        public event Action<ClueDefinition> ClueSelected;

        /// <summary>열람창에 이 단서를 띄웠다.</summary>
        public event Action<ClueDefinition> ReadingOpened;

        /// <summary>이 단서를 읽던 열람창을 닫았다 (유예 시간과 상관없이 닫는 순간).</summary>
        public event Action<ClueDefinition> ReadingClosed;

        /// <summary>열람창에 떠 있는 단서. 닫혀 있으면 null.</summary>
        public ClueDefinition Reading => _tooltipOpen ? _reading : null;

        public bool IsTooltipOpen => _tooltipOpen;
        public bool SelectionMode => _selectionMode;

        /// <summary>
        /// 고르기 모드 — 칸을 누르면 열람창 대신 ClueSelected 가 난다. 가설 칸은 여전히 열람된다.
        /// 모드를 끄면 선택 표시도 지운다.
        /// </summary>
        public void SetSelectionMode(bool on)
        {
            _selectionMode = on;
            if (!on) _selectedIds.Clear();
            RebuildSlotsOnly();
        }

        /// <summary>지금 자리에 올라간 단서들을 목록에서 밝게 표시한다.</summary>
        public void SetSelection(string a, string b)
        {
            _selectedIds.Clear();
            if (!string.IsNullOrEmpty(a)) _selectedIds.Add(a);
            if (!string.IsNullOrEmpty(b)) _selectedIds.Add(b);
            RebuildSlotsOnly();
        }

        private void Awake()
        {
            // 씬 빌더가 에디터에서 AddListener 로 붙인 연결은 저장되지 않는다.
            // 그래서 버튼 연결은 런타임에 여기서 직접 한다.
            if (closeButton != null) closeButton.onClick.AddListener(CloseTooltip);

            if (tooltipBody != null)
            {
                _linkRelay = tooltipBody.GetComponent<TmpLinkClickRelay>();
                if (_linkRelay == null) _linkRelay = tooltipBody.gameObject.AddComponent<TmpLinkClickRelay>();
                _linkRelay.LinkClicked += OnTooltipLinkClicked;
            }

            if (tooltipRoot != null) tooltipRoot.SetActive(false);
            _tooltipOpen = false;
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseTooltip);
            if (_linkRelay != null) _linkRelay.LinkClicked -= OnTooltipLinkClicked;
        }

        public void Rebuild(ClueInventory inventory)
        {
            _inventory = inventory;
            if (inventory == null || container == null || slotPrefab == null) return;
            RebuildSlotsOnly();
        }

        private bool SplitLeads => leadsContainer != null && leadPrefab != null;

        /// <summary>물품 증거는 격자에, 정황·실마리는 아래 목록에. 둘 다 수집 순서대로.</summary>
        private static bool IsLead(ClueDefinition clue) => clue.Kind != ClueKind.Object;

        /// <summary>
        /// 바깥에서 열람창을 연다 — 고르기 모드여도 읽기로. 추론 서류철의 우클릭·가설 탭이 쓴다.
        /// 우측 목록이 숨겨진 페이즈에서도 열람창은 따로 떠 있으므로 그대로 동작한다.
        /// </summary>
        public void OpenReading(ClueDefinition clue)
        {
            if (clue == null) return;
            var was = _selectionMode;
            _selectionMode = false;
            OnSlotClicked(clue);
            _selectionMode = was;
        }

        private void OnSlotClicked(ClueDefinition clue)
        {
            if (clue == null || _inventory == null) return;

            if (_selectionMode && clue.Kind != ClueKind.Deduced)
            {
                // 고르기 모드: 읽은 것으로 치고 자리에 올린다. 열람창은 뜨지 않는다.
                var read = _inventory.MarkRead(clue.Id);
                if (read) ClueRead?.Invoke(clue);
                ClueSelected?.Invoke(clue);
                return;
            }

            var firstRead = _inventory.MarkRead(clue.Id);
            _reading = clue;

            RenderTooltip();
            if (tooltipRoot != null) tooltipRoot.SetActive(true);
            if (tooltipScroll != null) tooltipScroll.verticalNormalizedPosition = 1f;

            // 닫은 직후의 유예 중에 다시 열었다면, 그 유예는 없던 일이 된다.
            if (_grace != null) { StopCoroutine(_grace); _grace = null; }

            _tooltipOpen = true;
            if (!_timerHeld)
            {
                _timerHeld = true;
                TooltipOpened?.Invoke();
            }

            if (firstRead)
            {
                ClueRead?.Invoke(clue);
                RebuildSlotsOnly();
            }
            ReadingOpened?.Invoke(clue);
        }

        private void RenderTooltip()
        {
            var clue = _reading;
            if (clue == null) return;

            var promoted = _inventory.IsPromoted(clue.Id);
            var promotable = clue.IsPromotable;

            if (tooltipTitle != null) tooltipTitle.text = clue.ReadName;

            if (tooltipBody != null)
                tooltipBody.text = DialogueMarkup.ToTooltipRichText(clue.Tooltip, promotable, promoted);

            if (tooltipImage != null)
            {
                var sprite = clue.CloseUpImage;
                tooltipImage.sprite = sprite;
                tooltipImage.color = sprite != null ? Color.white : placeholderColor;
                tooltipImage.preserveAspect = true;
                if (tooltipImagePlaceholder != null)
                {
                    tooltipImagePlaceholder.gameObject.SetActive(sprite == null);
                    tooltipImagePlaceholder.text = clue.ReadName + "\n(이미지 대기)";
                }
            }

            if (promoteHint != null)
            {
                if (!promotable) promoteHint.text = string.Empty;
                else if (promoted) promoteHint.text = "단서로 정리해 두었다 — 심문에서 제시할 수 있다";
                else promoteHint.text = "강조된 구절을 누르면 단서로 정리한다";
            }
        }

        private void OnTooltipLinkClicked(string linkId)
        {
            if (linkId != DialogueMarkup.PromoteLinkId || _reading == null || _inventory == null) return;
            if (!_inventory.Promote(_reading.Id)) return;

            RenderTooltip();
            RebuildSlotsOnly();
            CluePromoted?.Invoke(_reading);
        }

        /// <summary>열람창을 닫는다. 닫기 버튼과 외부 모두 이걸 부른다.</summary>
        public void CloseTooltip()
        {
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
            var closed = _tooltipOpen ? _reading : null;
            _reading = null;
            if (closed != null) ReadingClosed?.Invoke(closed);

            if (!_tooltipOpen) return;
            _tooltipOpen = false;

            if (_grace != null) StopCoroutine(_grace);
            _grace = isActiveAndEnabled ? StartCoroutine(GraceRoutine()) : null;

            // 비활성 상태라 코루틴을 못 돌리면 유예 없이 바로 알린다.
            if (_grace == null) ReleaseTimer();
        }

        private IEnumerator GraceRoutine()
        {
            if (tooltipGraceSeconds > 0f)
                yield return new WaitForSecondsRealtime(tooltipGraceSeconds);

            _grace = null;
            ReleaseTimer();
        }

        /// <summary>멈춰 달라고 했던 것을 한 번만 되돌린다.</summary>
        private void ReleaseTimer()
        {
            if (!_timerHeld) return;
            _timerHeld = false;
            TooltipClosed?.Invoke();
        }

        /// <summary>목록만 다시 그린다. 열람창 상태는 건드리지 않는다.</summary>
        private void RebuildSlotsOnly()
        {
            if (_inventory == null || container == null || slotPrefab == null) return;
            var ids = _inventory.OrderedIds;

            var grid = new List<ClueDefinition>();
            var leads = new List<ClueDefinition>();
            var hyps = new List<ClueDefinition>();
            var splitHyps = hypothesesContainer != null && hypothesisPrefab != null;
            for (int i = 0; i < ids.Count; i++)
            {
                var clue = _inventory.Get(ids[i]);
                if (clue == null) continue;
                if (splitHyps && clue.Kind == ClueKind.Deduced) hyps.Add(clue);
                else if (SplitLeads && IsLead(clue)) leads.Add(clue);
                else grid.Add(clue);
            }

            // 노션: "’단서‘로 갱신된 수집품/기록들은 수집 인벤토리 내에서 표시가 되며, 상단에 우선 정렬됩니다." — 수집 순서는 지킨 채로.
            var promotedFirst = new List<ClueDefinition>();
            for (int i = 0; i < grid.Count; i++) if (_inventory.IsPromoted(grid[i].Id)) promotedFirst.Add(grid[i]);
            for (int i = 0; i < grid.Count; i++) if (!_inventory.IsPromoted(grid[i].Id)) promotedFirst.Add(grid[i]);
            grid = promotedFirst;

            Fill(_pool, slotPrefab, container, grid);
            if (SplitLeads) Fill(_leadPool, leadPrefab, leadsContainer, leads);
            if (splitHyps)
            {
                Fill(_hypothesisPool, hypothesisPrefab, hypothesesContainer, hyps);
                if (hypothesesEmpty != null) hypothesesEmpty.SetActive(hyps.Count == 0);
            }
        }

        // ---------------------------------------------------------------- 튜토리얼 하이라이트용 위치

        /// <summary>목록에서 이 단서가 있는 칸.</summary>
        public RectTransform SlotRectFor(string clueId)
        {
            foreach (var pool in new[] { _pool, _leadPool, _hypothesisPool })
                for (int i = 0; i < pool.Count; i++)
                    if (pool[i] != null && pool[i].gameObject.activeInHierarchy && pool[i].Clue != null && pool[i].Clue.Id == clueId)
                        return pool[i].transform as RectTransform;
            return null;
        }

        /// <summary>열람창의 판 (그림 + 글).</summary>
        public RectTransform TooltipPanelRect
            => _tooltipOpen && tooltipTitle != null ? tooltipTitle.transform.parent as RectTransform : null;

        /// <summary>열람창 글 속 강조된 구절(누르면 단서로 정리되는 곳)의 세계 좌표. 없으면 null.</summary>
        public (Vector3 min, Vector3 max)? HighlightWorldRect()
        {
            if (!_tooltipOpen || tooltipBody == null) return null;
            tooltipBody.ForceMeshUpdate();
            var info = tooltipBody.textInfo;
            for (int l = 0; l < info.linkCount; l++)
            {
                var link = info.linkInfo[l];
                if (link.GetLinkID() != DialogueMarkup.PromoteLinkId) continue;
                var min = new Vector3(float.MaxValue, float.MaxValue);
                var max = new Vector3(float.MinValue, float.MinValue);
                for (int c = link.linkTextfirstCharacterIndex; c < link.linkTextfirstCharacterIndex + link.linkTextLength && c < info.characterCount; c++)
                {
                    var ch = info.characterInfo[c];
                    if (!ch.isVisible) continue;
                    min = Vector3.Min(min, ch.bottomLeft); max = Vector3.Max(max, ch.topRight);
                }
                if (min.x == float.MaxValue) continue;
                var t = tooltipBody.transform;
                return (t.TransformPoint(min), t.TransformPoint(max));
            }
            return null;
        }

        private void Fill(List<ClueSlot> pool, ClueSlot prefab, RectTransform parent, List<ClueDefinition> clues)
        {
            while (pool.Count < clues.Count) pool.Add(Instantiate(prefab, parent));
            for (int i = 0; i < pool.Count; i++)
            {
                var active = i < clues.Count;
                pool[i].gameObject.SetActive(active);
                if (!active) continue;
                var clue = clues[i];
                pool[i].Bind(clue, _inventory.HasBeenRead(clue.Id), _inventory.IsPromoted(clue.Id), OnSlotClicked);
                pool[i].SetSelected(_selectedIds.Contains(clue.Id));
            }
        }
    }
}
