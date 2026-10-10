using System;
using System.Collections;
using System.Collections.Generic;
using ItIsYou.Clues;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 추론(단서 조합) 페이즈의 화면 — 책상 위에 펼친 서류 두 장. (2026-10-11 제작자 스케치)
    ///
    /// <list type="number">
    /// <item>왼쪽 장 <b>Evidence</b> — 단서 수집에서 모은 물품. 한 장에 셋, 넘겨 본다.</item>
    /// <item>오른쪽 장 <b>Leads</b> — 심문에서 얻은 정황. 누구에게서 나온 말인지 그 사람의 인적 사항이 위에 붙는다.</item>
    /// <item>양쪽에서 하나씩 고르면 붉은 선이 두 장 사이의 골을 따라 이어지고, 선 위에 「가설 추론」 도장이 뜬다.</item>
    /// <item>도장을 누르면 화면이 어두워지고, 주인공의 추론과 함께 「가설 도출 성공!」 이 뜬다.</item>
    /// <item>세운 가설은 오른쪽 끝의 가설 탭에 꽂힌다. 잉크는 위 가운데, 시계는 우상단.</item>
    /// </list>
    ///
    /// 규칙은 모른다. 무엇을 골랐는지, 무엇이 눌렸는지만 알리고 판단은 DeductionController 가 한다.
    /// 칸(행)은 들어온 단서로 그때그때 만든다 — 단서 수는 챕터마다 다르다.
    /// </summary>
    [AddComponentMenu("It is You/Deduction Dossier View")]
    public class DeductionDossierView : MonoBehaviour
    {
        [Serializable]
        public class PortraitBinding
        {
            public string suspectId;
            public Sprite portrait;
        }

        [Serializable]
        public class HypothesisTab
        {
            public RectTransform root;
            public Image bar;
            public Image barFrame;
            public TMP_Text title;
            public Image lens;
            public Button button;
            [Tooltip("가설을 지우는 쓰레기통 (경고 한 번 뒤 지운다).")]
            public Button trash;
            [Tooltip("이 챕터에서 쓸 수 없는 칸을 덮는 빗금.")]
            public GameObject hatch;
            public TMP_Text pill;
        }

        [Header("틀")]
        [SerializeField] private CanvasGroup group;
        [Tooltip("잇는 선과 도장이 올라가는 층. 화면 전체를 덮는다.")]
        [SerializeField] private RectTransform overlay;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite panelSprite;

        [Header("Evidence 장")]
        [SerializeField] private RectTransform evidencePage;
        [SerializeField] private RectTransform evidenceBody;
        [SerializeField] private TMP_Text evidencePager;
        [SerializeField] private Button evidencePrev;
        [SerializeField] private Button evidenceNext;
        [SerializeField] private TMP_Text evidenceEmpty;
        [SerializeField] private int evidencePerPage = 3;

        [Header("Leads 장")]
        [SerializeField] private RectTransform leadsPage;
        [SerializeField] private RectTransform leadsBody;
        [SerializeField] private GameObject profileRoot;
        [SerializeField] private Image profilePortrait;
        [SerializeField] private TMP_Text profileName;
        [SerializeField] private TMP_Text profileText;
        [SerializeField] private TMP_Text leadsPager;
        [SerializeField] private Button leadsPrev;
        [SerializeField] private Button leadsNext;
        [SerializeField] private TMP_Text leadsEmpty;
        [SerializeField] private int leadsPerPage = 2;
        [SerializeField] private List<PortraitBinding> portraits = new List<PortraitBinding>();

        [Header("잇는 선 · 가설 추론")]
        [SerializeField] private Image lineA;
        [SerializeField] private Image lineB;
        [SerializeField] private Image lineC;
        [SerializeField] private Button stamp;
        [SerializeField] private TMP_Text stampLabel;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private float lineThickness = 5f;

        [Header("상단")]
        [SerializeField] private Button backButton;
        [SerializeField] private InkDropsView ink;
        [SerializeField] private TMP_Text clockLabel;
        [SerializeField] private TMP_Text locationLabel;

        [Header("가설 탭")]
        [Tooltip("가설 탭 묶음. 교체할 때 어두운 막 위로 올린다.")]
        [SerializeField] private RectTransform hypothesisList;
        [SerializeField] private List<HypothesisTab> tabs = new List<HypothesisTab>();
        [Tooltip("「추리를 마친다」 — 가설 목록 맨 아래.")]
        [SerializeField] private Button endButton;

        [Header("연출")]
        [SerializeField] private CanvasGroup scrim;
        [Tooltip("가설 교체 · 지우기 경고 때의 막 짙기(0~1).")]
        [SerializeField, Range(0f, 1f)] private float blockedScrimAlpha = 0.86f;
        [SerializeField] private CanvasGroup banner;
        [SerializeField] private TMP_Text bannerLabel;
        [Header("가설 교체")]
        [SerializeField] private GameObject replaceRoot;
        [SerializeField] private TMP_Text replacePrompt;
        [SerializeField] private TMP_Text replaceTitle;
        [SerializeField] private Button replaceCancel;

        [Header("지우기 경고")]
        [SerializeField] private GameObject confirmRoot;
        [SerializeField] private TMP_Text confirmText;
        [SerializeField] private Button confirmYes;
        [SerializeField] private Button confirmNo;

        [Header("글자")]
        [SerializeField] private float nameSize = 26f;
        [SerializeField] private float excerptSize = 21f;
        [SerializeField] private float tagSize = 20f;

        [Header("색")]
        [SerializeField] private Color inkColor = new Color32(0x2E, 0x24, 0x1D, 0xFF);
        [SerializeField] private Color inkSoft = new Color32(0x55, 0x48, 0x3E, 0xFF);
        [SerializeField] private Color available = new Color32(0xE8, 0x8A, 0x1A, 0xFF);
        [SerializeField] private Color selected = new Color32(0xC8, 0x24, 0x1C, 0xFF);
        [SerializeField] private Color selectedRow = new Color(0f, 0f, 0f, 0.14f);
        [SerializeField] private Color thumbFill = new Color32(0x4E, 0x40, 0x38, 0xFF);
        [SerializeField] private Color tagFill = new Color32(0x4A, 0x3E, 0x36, 0xFF);
        [SerializeField] private Color tagText = new Color32(0xE6, 0xDD, 0xD0, 0xFF);
        [SerializeField] private Color successBanner = new Color32(0xF2, 0xEA, 0xDC, 0xFF);
        [SerializeField] private Color failBanner = new Color32(0xE0, 0x9A, 0x90, 0xFF);

        // ---- 바깥으로 ----
        public event Action<ClueDefinition> EvidencePicked;
        public event Action<ClueDefinition> LeadPicked;
        public event Action CombineRequested;
        /// <summary>「추리를 마친다」.</summary>
        public event Action EndRequested;
        /// <summary>「돌아가기」 — 1차 인물 선택으로.</summary>
        public event Action BackRequested;
        /// <summary>가설 탭의 쓰레기통을 눌렀다. (가설 id)</summary>
        public event Action<string> DeleteRequested;
        /// <summary>단서나 가설을 크게 열어 읽으려 한다 — 행을 우클릭하거나 가설 탭을 눌렀다.</summary>
        public event Action<ClueDefinition> ReadRequested;
        public event Action<string> ReplacePicked;
        public event Action ReplaceCancelled;

        // ---- 상태 ----
        private class Row
        {
            public ClueDefinition clue;
            public RectTransform rt;
            public Image bg;
            public Image frame;      // Evidence: 썸네일 테두리 / Leads: 이름표 테두리
            public RectTransform anchor;  // 선이 붙는 곳
            public TMP_Text excerpt;
            public string excerptSource;
            public bool isLead;
        }

        private readonly List<Row> _rows = new List<Row>();
        private readonly List<ClueDefinition> _evidence = new List<ClueDefinition>();
        private readonly List<(SuspectDefinition suspect, List<ClueDefinition> leads)> _leadPages
            = new List<(SuspectDefinition, List<ClueDefinition>)>();
        private ClueInventory _inventory;
        private IReadOnlyList<SuspectDefinition> _suspects;
        private int _evidencePageIndex, _leadsPageIndex;
        private ClueDefinition _pickedEvidence, _pickedLead;
        private bool _combinable;
        private bool _replacing;
        private string[] _slots = new string[0];
        private int _maxHypotheses = 3;
        private Coroutine _bannerFade, _scrimFade;
        private Action<bool> _confirmDone;
        private float _scrimBaseAlpha = -1f;
        private int _listSiblingIndex = -1;

        public bool IsReplacing => _replacing;

        private void Awake()
        {
            if (evidencePrev != null) evidencePrev.onClick.AddListener(() => TurnEvidence(-1));
            if (evidenceNext != null) evidenceNext.onClick.AddListener(() => TurnEvidence(+1));
            if (leadsPrev != null) leadsPrev.onClick.AddListener(() => TurnLeads(-1));
            if (leadsNext != null) leadsNext.onClick.AddListener(() => TurnLeads(+1));
            if (stamp != null) stamp.onClick.AddListener(() => { if (_combinable && !_replacing) CombineRequested?.Invoke(); });
            if (backButton != null) backButton.onClick.AddListener(() => { if (!_replacing && !ConfirmOpen) BackRequested?.Invoke(); });
            if (endButton != null) endButton.onClick.AddListener(() => { if (!_replacing && !ConfirmOpen) EndRequested?.Invoke(); });
            if (replaceCancel != null) replaceCancel.onClick.AddListener(() => { if (_replacing) ReplaceCancelled?.Invoke(); });
            if (confirmYes != null) confirmYes.onClick.AddListener(() => CloseConfirm(true));
            if (confirmNo != null) confirmNo.onClick.AddListener(() => CloseConfirm(false));

            for (int i = 0; i < tabs.Count; i++)
            {
                var index = i;
                if (tabs[i]?.button != null) tabs[i].button.onClick.AddListener(() => OnTabClicked(index));
                if (tabs[i]?.trash != null) tabs[i].trash.onClick.AddListener(() => OnTrashClicked(index));
            }

            SetLineVisible(false);
            if (stamp != null) stamp.gameObject.SetActive(false);
            SetAlpha(scrim, 0f, false);
            SetAlpha(banner, 0f, false);
            if (replaceRoot != null) replaceRoot.SetActive(false);
            if (confirmRoot != null) confirmRoot.SetActive(false);
        }

        // ================================================================ 채우기

        /// <summary>
        /// 서류철을 편다. 물품은 왼쪽, 정황은 오른쪽 — 정황은 누구에게서 나온 말인지에 따라 장이 나뉜다.
        /// </summary>
        public void Open(ClueInventory inventory, IReadOnlyList<SuspectDefinition> suspects, int maxHypotheses,
            string clock, string location)
        {
            _inventory = inventory;
            _suspects = suspects;
            _maxHypotheses = Mathf.Clamp(maxHypotheses, 1, Mathf.Max(1, tabs.Count));
            // 「돌아가기」로 떠났다 다시 들어와도 탭 순서가 그대로이도록, 칸 수가 같으면 자리를 지킨다.
            if (_slots == null || _slots.Length != _maxHypotheses) _slots = new string[_maxHypotheses];
            _evidencePageIndex = 0;
            _leadsPageIndex = 0;
            _pickedEvidence = null;
            _pickedLead = null;
            _replacing = false;

            if (clockLabel != null) clockLabel.text = clock ?? string.Empty;
            if (locationLabel != null) locationLabel.text = location ?? string.Empty;

            // 칸은 언제나 전부 보인다 — 이 챕터에서 못 쓰는 칸은 빗금(튜토리얼은 2칸 + 빗금 1칸).
            for (int i = 0; i < tabs.Count; i++)
                if (tabs[i]?.root != null) tabs[i].root.gameObject.SetActive(true);

            Collect();
            RenderPages();
            SetLineVisible(false);
            if (stamp != null) stamp.gameObject.SetActive(false);
            SetAlpha(scrim, 0f, false);
            SetAlpha(banner, 0f, false);
            if (_replacing) EndReplace();
            if (replaceRoot != null) replaceRoot.SetActive(false);
            if (confirmRoot != null) confirmRoot.SetActive(false);
            _confirmDone = null;
            RenderTabs();
        }

        /// <summary>단서 목록이 바뀌었을 수 있다(가설이 생기거나 지워졌다). 장을 다시 그린다.</summary>
        public void Refresh()
        {
            if (_inventory == null) return;
            Collect();
            _evidencePageIndex = Mathf.Clamp(_evidencePageIndex, 0, Mathf.Max(0, EvidencePageCount - 1));
            _leadsPageIndex = Mathf.Clamp(_leadsPageIndex, 0, Mathf.Max(0, _leadPages.Count - 1));
            RenderPages();
        }

        private int EvidencePageCount => Mathf.Max(1, Mathf.CeilToInt(_evidence.Count / (float)Mathf.Max(1, evidencePerPage)));

        private void Collect()
        {
            _evidence.Clear();
            _leadPages.Clear();
            if (_inventory == null) return;

            var leads = new List<ClueDefinition>();
            foreach (var id in _inventory.OrderedIds)
            {
                var c = _inventory.Get(id);
                if (c == null) continue;
                if (c.Kind == ClueKind.Object) _evidence.Add(c);
                else if (c.Kind == ClueKind.Testimony || c.Kind == ClueKind.Reaction) leads.Add(c);
            }

            // 정황은 누구에게서 나온 말인지로 묶는다. 출처를 모르면 맨 뒤 '기타' 장.
            var bySuspect = new Dictionary<string, List<ClueDefinition>>();
            var order = new List<string>();
            foreach (var c in leads)
            {
                var key = string.IsNullOrEmpty(c.SourceSuspectId) ? string.Empty : c.SourceSuspectId;
                if (!bySuspect.TryGetValue(key, out var list)) { list = new List<ClueDefinition>(); bySuspect[key] = list; order.Add(key); }
                list.Add(c);
            }
            order.Sort((a, b) => string.IsNullOrEmpty(a) ? 1 : string.IsNullOrEmpty(b) ? -1 : SuspectOrder(a).CompareTo(SuspectOrder(b)));

            var per = Mathf.Max(1, leadsPerPage);
            foreach (var key in order)
            {
                var list = bySuspect[key];
                var suspect = FindSuspect(key);
                for (int i = 0; i < list.Count; i += per)
                    _leadPages.Add((suspect, list.GetRange(i, Mathf.Min(per, list.Count - i))));
            }
        }

        private int SuspectOrder(string id)
        {
            if (_suspects == null) return int.MaxValue;
            for (int i = 0; i < _suspects.Count; i++) if (_suspects[i] != null && _suspects[i].Id == id) return i;
            return int.MaxValue;
        }

        private SuspectDefinition FindSuspect(string id)
        {
            if (string.IsNullOrEmpty(id) || _suspects == null) return null;
            for (int i = 0; i < _suspects.Count; i++) if (_suspects[i] != null && _suspects[i].Id == id) return _suspects[i];
            return null;
        }

        private Sprite PortraitOf(SuspectDefinition s)
        {
            if (s == null) return null;
            if (s.Portrait != null) return s.Portrait;
            for (int i = 0; i < portraits.Count; i++)
                if (portraits[i] != null && portraits[i].suspectId == s.Id) return portraits[i].portrait;
            return null;
        }

        // ================================================================ 장 그리기

        private void TurnEvidence(int delta)
        {
            var next = Mathf.Clamp(_evidencePageIndex + delta, 0, EvidencePageCount - 1);
            if (next == _evidencePageIndex) return;
            _evidencePageIndex = next;
            RenderPages();
        }

        private void TurnLeads(int delta)
        {
            var next = Mathf.Clamp(_leadsPageIndex + delta, 0, Mathf.Max(0, _leadPages.Count - 1));
            if (next == _leadsPageIndex) return;
            _leadsPageIndex = next;
            RenderPages();
        }

        private void RenderPages()
        {
            for (int i = 0; i < _rows.Count; i++) if (_rows[i]?.rt != null) Destroy(_rows[i].rt.gameObject);
            _rows.Clear();

            // ---- Evidence ----
            var per = Mathf.Max(1, evidencePerPage);
            var start = _evidencePageIndex * per;
            var count = Mathf.Min(per, _evidence.Count - start);
            for (int i = 0; i < count; i++) _rows.Add(BuildEvidenceRow(_evidence[start + i], i, per));
            if (evidenceEmpty != null) evidenceEmpty.gameObject.SetActive(_evidence.Count == 0);
            SetPager(evidencePager, evidencePrev, evidenceNext, _evidencePageIndex, EvidencePageCount);

            // ---- Leads ----
            var hasLeads = _leadPages.Count > 0;
            if (leadsEmpty != null) leadsEmpty.gameObject.SetActive(!hasLeads);
            if (hasLeads)
            {
                var page = _leadPages[_leadsPageIndex];
                RenderProfile(page.suspect);
                for (int i = 0; i < page.leads.Count; i++) _rows.Add(BuildLeadRow(page.leads[i], i, Mathf.Max(1, leadsPerPage)));
            }
            else RenderProfile(null);
            SetPager(leadsPager, leadsPrev, leadsNext, _leadsPageIndex, Mathf.Max(1, _leadPages.Count));

            ApplySelectionVisuals();
            Canvas.ForceUpdateCanvases();
            UpdateConnector();
        }

        private static void SetPager(TMP_Text label, Button prev, Button next, int index, int total)
        {
            if (label != null) label.text = $"{index + 1} / {total}";
            if (prev != null) prev.interactable = index > 0;
            if (next != null) next.interactable = index < total - 1;
        }

        private void RenderProfile(SuspectDefinition s)
        {
            if (profileRoot != null) profileRoot.SetActive(s != null);
            if (s == null) return;
            if (profileName != null) profileName.text = s.DisplayName;
            if (profileText != null) profileText.text = s.Profile ?? string.Empty;
            if (profilePortrait != null)
            {
                var sprite = PortraitOf(s);
                profilePortrait.sprite = sprite;
                profilePortrait.color = sprite != null ? Color.white : thumbFill;
                profilePortrait.preserveAspect = true;
            }
        }

        // ---------------------------------------------------------------- Evidence 행

        private Row BuildEvidenceRow(ClueDefinition clue, int index, int perPage)
        {
            var row = new Row { clue = clue, isLead = false };
            var rt = NewRect("Evidence_" + clue.Id, evidenceBody);
            var h = 1f / perPage;
            rt.anchorMin = new Vector2(0f, 1f - h * (index + 1));
            rt.anchorMax = new Vector2(1f, 1f - h * index);
            rt.offsetMin = new Vector2(0f, 6f);
            rt.offsetMax = new Vector2(0f, -6f);
            row.rt = rt;

            row.bg = rt.gameObject.AddComponent<Image>();
            row.bg.sprite = panelSprite; row.bg.type = Image.Type.Sliced;
            row.bg.color = new Color(0f, 0f, 0f, 0f);

            // 지그재그 — 스케치처럼 그림이 왼쪽, 오른쪽, 왼쪽.
            var thumbLeft = index % 2 == 0;
            const float thumb = 164f;

            var frame = NewImage("Frame", rt, available);
            var frt = frame.rectTransform;
            frt.anchorMin = frt.anchorMax = new Vector2(thumbLeft ? 0f : 1f, 0.5f);
            frt.pivot = new Vector2(thumbLeft ? 0f : 1f, 0.5f);
            frt.sizeDelta = new Vector2(thumb, thumb);
            frt.anchoredPosition = new Vector2(thumbLeft ? 8f : -8f, 0f);
            frame.raycastTarget = false;
            row.frame = frame;

            var fill = NewImage("Fill", frt, thumbFill);
            Stretch(fill.rectTransform, 5f);
            fill.raycastTarget = false;
            if (clue.Icon != null)
            {
                var icon = NewImage("Icon", frt, Color.white);
                Stretch(icon.rectTransform, 12f);
                icon.sprite = clue.Icon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            var textBox = NewRect("Text", rt);
            textBox.anchorMin = new Vector2(0f, 0f);
            textBox.anchorMax = new Vector2(1f, 1f);
            textBox.offsetMin = new Vector2(thumbLeft ? thumb + 32f : 12f, 12f);
            textBox.offsetMax = new Vector2(thumbLeft ? -12f : -(thumb + 32f), -12f);

            var name = NewText("Name", textBox, NameOf(clue), nameSize, inkColor, thumbLeft ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight);
            name.fontStyle = FontStyles.Bold;
            var nrt = name.rectTransform;
            nrt.anchorMin = new Vector2(0f, 1f); nrt.anchorMax = new Vector2(1f, 1f); nrt.pivot = new Vector2(0.5f, 1f);
            nrt.sizeDelta = new Vector2(0f, nameSize * 1.5f); nrt.anchoredPosition = Vector2.zero;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.textWrappingMode = TextWrappingModes.NoWrap;

            row.excerptSource = clue.Tooltip;
            var ex = NewText("Excerpt", textBox, string.Empty, excerptSize, inkSoft, thumbLeft ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight);
            var ert = ex.rectTransform;
            ert.anchorMin = Vector2.zero; ert.anchorMax = Vector2.one;
            ert.offsetMin = Vector2.zero; ert.offsetMax = new Vector2(0f, -(nameSize * 1.6f));
            ex.textWrappingMode = TextWrappingModes.Normal;
            ex.overflowMode = TextOverflowModes.Ellipsis;
            ex.lineSpacing = 6f;
            row.excerpt = ex;

            // 선은 행의 오른쪽 끝(두 장 사이의 골 쪽)에서 나간다.
            var anchor = NewRect("LinkAnchor", rt);
            anchor.anchorMin = anchor.anchorMax = new Vector2(1f, 0.5f);
            anchor.sizeDelta = Vector2.zero;
            row.anchor = anchor;

            Hook(rt.gameObject, row);
            return row;
        }

        // ---------------------------------------------------------------- Leads 행

        private Row BuildLeadRow(ClueDefinition clue, int index, int perPage)
        {
            var row = new Row { clue = clue, isLead = true };
            var rt = NewRect("Lead_" + clue.Id, leadsBody);
            var h = 1f / perPage;
            rt.anchorMin = new Vector2(0f, 1f - h * (index + 1));
            rt.anchorMax = new Vector2(1f, 1f - h * index);
            rt.offsetMin = new Vector2(0f, 6f);
            rt.offsetMax = new Vector2(0f, -6f);
            row.rt = rt;

            row.bg = rt.gameObject.AddComponent<Image>();
            row.bg.sprite = panelSprite; row.bg.type = Image.Type.Sliced;
            row.bg.color = new Color(0f, 0f, 0f, 0f);

            // 이름표 — 테두리 판 + 점 + 이름.
            var tagName = NameOf(clue);
            var tagWidth = Mathf.Clamp(tagName.Length * tagSize * 0.95f + 64f, 180f, 460f);
            var frame = NewImage("TagFrame", rt, available);
            var frt = frame.rectTransform;
            frt.anchorMin = frt.anchorMax = new Vector2(0f, 1f);
            frt.pivot = new Vector2(0f, 1f);
            frt.sizeDelta = new Vector2(tagWidth, 52f);
            frt.anchoredPosition = new Vector2(8f, -8f);
            frame.raycastTarget = false;
            row.frame = frame;

            var tag = NewImage("Tag", frt, tagFill);
            Stretch(tag.rectTransform, 4f);
            tag.raycastTarget = false;
            var dot = NewText("Dot", frt, "●", tagSize - 4f, tagText, TextAlignmentOptions.Center);
            var drt = dot.rectTransform;
            drt.anchorMin = drt.anchorMax = new Vector2(0f, 0.5f); drt.pivot = new Vector2(0f, 0.5f);
            drt.sizeDelta = new Vector2(28f, 40f); drt.anchoredPosition = new Vector2(12f, 0f);
            var label = NewText("Name", frt, tagName, tagSize, tagText, TextAlignmentOptions.Left);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(44f, 0f); lrt.offsetMax = new Vector2(-12f, 0f);
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.textWrappingMode = TextWrappingModes.NoWrap;

            row.excerptSource = clue.Tooltip;
            var ex = NewText("Excerpt", rt, string.Empty, excerptSize, inkSoft, TextAlignmentOptions.TopLeft);
            var ert = ex.rectTransform;
            ert.anchorMin = Vector2.zero; ert.anchorMax = Vector2.one;
            ert.offsetMin = new Vector2(24f, 10f); ert.offsetMax = new Vector2(-16f, -72f);
            ex.textWrappingMode = TextWrappingModes.Normal;
            ex.overflowMode = TextOverflowModes.Ellipsis;
            ex.lineSpacing = 6f;
            row.excerpt = ex;

            // 선은 이름표의 왼쪽 끝으로 들어온다.
            var anchor = NewRect("LinkAnchor", frt);
            anchor.anchorMin = anchor.anchorMax = new Vector2(0f, 0.5f);
            anchor.sizeDelta = Vector2.zero;
            row.anchor = anchor;

            Hook(rt.gameObject, row);
            return row;
        }

        private string NameOf(ClueDefinition clue)
            => _inventory != null ? _inventory.DisplayName(clue.Id) : clue.ReadName;

        private void Hook(GameObject go, Row row)
        {
            var relay = go.AddComponent<ClickRelay>();
            relay.Clicked += e =>
            {
                if (_replacing || ConfirmOpen || (group != null && !group.interactable)) return;
                if (e.button == PointerEventData.InputButton.Right) { ReadRequested?.Invoke(row.clue); return; }
                if (e.button != PointerEventData.InputButton.Left) return;
                if (row.isLead) LeadPicked?.Invoke(row.clue); else EvidencePicked?.Invoke(row.clue);
            };
            relay.HoverChanged += on =>
            {
                if (row.bg == null || IsPicked(row)) return;
                row.bg.color = on && (group == null || group.interactable) && !_replacing
                    ? new Color(0f, 0f, 0f, 0.06f) : new Color(0f, 0f, 0f, 0f);
            };
        }

        // ================================================================ 고르기 · 잇기

        /// <summary>고른 것과, 도장을 누를 수 있는지. 둘 다 고르면 선이 이어진다.</summary>
        public void SetSelection(ClueDefinition evidence, ClueDefinition lead, bool combinable, string hint)
        {
            _pickedEvidence = evidence;
            _pickedLead = lead;
            _combinable = combinable;
            if (hintLabel != null) hintLabel.text = hint ?? string.Empty;
            if (stampLabel != null) stampLabel.alpha = combinable ? 1f : 0.45f;
            if (stamp != null) stamp.interactable = combinable;
            ApplySelectionVisuals();
            UpdateConnector();
        }

        private bool IsPicked(Row r) => r.clue != null && (r.clue == _pickedEvidence || r.clue == _pickedLead);

        private void ApplySelectionVisuals()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                var on = IsPicked(r);
                if (r.bg != null) r.bg.color = on ? selectedRow : new Color(0f, 0f, 0f, 0f);
                if (r.frame != null) r.frame.color = on ? selected : available;
                if (r.excerpt != null) r.excerpt.text = Excerpt(r.excerptSource, on ? selected : available);
            }
        }

        /// <summary>
        /// 툴팁의 강조 구간(&lt;hl&gt;)을 밑줄로 보여 준다 — 이 단서의 '쓸 만한 곳'이다.
        /// 강조 구간이 없는 물품은 첫 줄만. 서류철은 훑어보는 곳이고, 전문은 우클릭으로 연다.
        /// </summary>
        private static string Excerpt(string tooltip, Color hl)
        {
            if (string.IsNullOrEmpty(tooltip)) return string.Empty;
            var hex = ColorUtility.ToHtmlStringRGB(hl);
            var a = tooltip.IndexOf("<hl>", StringComparison.Ordinal);
            var b = a >= 0 ? tooltip.IndexOf("</hl>", a, StringComparison.Ordinal) : -1;
            if (a >= 0 && b > a)
            {
                var inner = tooltip.Substring(a + 4, b - a - 4).Replace("\n", " ");
                return $"<color=#{hex}><u>{inner}</u></color>";
            }
            var first = tooltip.Split('\n')[0];
            if (first.Length > 64) first = first.Substring(0, 64) + "…";
            return first.Replace("<hl>", string.Empty).Replace("</hl>", string.Empty);
        }

        private void LateUpdate()
        {
            // 레이아웃이 한 박자 늦게 잡히는 경우가 있어, 선이 떠 있는 동안은 매 프레임 맞춘다.
            if (_pickedEvidence != null && _pickedLead != null) UpdateConnector();
        }

        /// <summary>
        /// 두 행을 붉은 선으로 잇는다 — 물품 행의 오른쪽 끝에서 나와, 두 장 사이의 골을 따라 내려가,
        /// 정황 이름표의 왼쪽 끝으로 들어간다. 도장은 골 위, 선의 가운데에 뜬다.
        /// </summary>
        private void UpdateConnector()
        {
            Row ev = null, ld = null;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].clue == _pickedEvidence && !_rows[i].isLead) ev = _rows[i];
                if (_rows[i].clue == _pickedLead && _rows[i].isLead) ld = _rows[i];
            }

            var show = ev != null && ld != null && overlay != null;
            SetLineVisible(show);
            if (stamp != null) stamp.gameObject.SetActive(show);
            if (!show) return;

            var p0 = ToOverlay(ev.anchor);
            var p1 = ToOverlay(ld.anchor);
            var gx = GutterX();

            Segment(lineA, new Vector2(p0.x, p0.y), new Vector2(gx, p0.y));
            Segment(lineB, new Vector2(gx, p0.y), new Vector2(gx, p1.y));
            Segment(lineC, new Vector2(gx, p1.y), new Vector2(p1.x, p1.y));

            if (stamp != null)
            {
                var srt = stamp.transform as RectTransform;
                srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = new Vector2(gx, (p0.y + p1.y) * 0.5f);
            }
        }

        private float GutterX()
        {
            if (evidencePage == null || leadsPage == null) return 0f;
            var a = new Vector3[4]; var b = new Vector3[4];
            evidencePage.GetWorldCorners(a);
            leadsPage.GetWorldCorners(b);
            var right = overlay.InverseTransformPoint(a[2]).x;
            var left = overlay.InverseTransformPoint(b[0]).x;
            return (right + left) * 0.5f;
        }

        private Vector2 ToOverlay(RectTransform rt)
        {
            var p = overlay.InverseTransformPoint(rt.position);
            return new Vector2(p.x, p.y);
        }

        private void Segment(Image img, Vector2 from, Vector2 to)
        {
            if (img == null) return;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var horizontal = Mathf.Abs(from.y - to.y) < 0.5f;
            var len = horizontal ? Mathf.Abs(to.x - from.x) : Mathf.Abs(to.y - from.y);
            rt.sizeDelta = horizontal ? new Vector2(len + lineThickness, lineThickness) : new Vector2(lineThickness, len + lineThickness);
            rt.anchoredPosition = (from + to) * 0.5f;
        }

        private void SetLineVisible(bool on)
        {
            if (lineA != null) lineA.enabled = on;
            if (lineB != null) lineB.enabled = on;
            if (lineC != null) lineC.enabled = on;
        }

        // ================================================================ 가설 탭

        /// <summary>
        /// 가설 탭을 채운다. 이미 꽂힌 가설은 제자리를 지키고, 빠진 자리에 새 것이 들어간다 —
        /// 지우고 새로 세웠을 때 탭 순서가 뒤섞이지 않게.
        /// </summary>
        public void SetHypotheses(IReadOnlyList<ClueDefinition> hypotheses)
        {
            var ids = new HashSet<string>();
            if (hypotheses != null) foreach (var h in hypotheses) if (h != null) ids.Add(h.Id);

            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] != null && !ids.Contains(_slots[i])) _slots[i] = null;

            if (hypotheses != null)
            {
                foreach (var h in hypotheses)
                {
                    if (h == null || Array.IndexOf(_slots, h.Id) >= 0) continue;
                    var free = Array.IndexOf(_slots, null);
                    if (free >= 0) _slots[free] = h.Id;
                }
            }
            RenderTabs();
        }

        private void RenderTabs()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                var t = tabs[i];
                if (t?.root == null) continue;
                var locked = i >= _slots.Length;
                var filled = !locked && _slots[i] != null;
                if (t.title != null) t.title.text = filled ? "〈" + NameOfId(_slots[i]) + "〉" : string.Empty;
                if (t.lens != null) t.lens.enabled = filled;
                if (t.button != null) t.button.interactable = filled;
                if (t.barFrame != null) t.barFrame.color = _replacing && filled ? selected : new Color(0f, 0f, 0f, 0f);
                if (t.hatch != null) t.hatch.SetActive(locked);
                if (t.pill != null) t.pill.alpha = locked ? 0.4f : 1f;
                if (t.trash != null)
                {
                    t.trash.gameObject.SetActive(filled && !_replacing);
                    t.trash.interactable = filled && !_replacing;
                }
            }
        }

        private string NameOfId(string id)
        {
            if (_inventory == null) return id;
            var c = _inventory.Get(id);
            return c != null ? _inventory.DisplayName(id) : id;
        }

        private void OnTabClicked(int index)
        {
            if (index < 0 || index >= _slots.Length || _slots[index] == null) return;
            var id = _slots[index];
            if (_replacing) { ReplacePicked?.Invoke(id); return; }
            if (ConfirmOpen || (group != null && !group.interactable)) return;
            var clue = _inventory != null ? _inventory.Get(id) : null;
            if (clue != null) ReadRequested?.Invoke(clue);
        }

        private void OnTrashClicked(int index)
        {
            if (_replacing || ConfirmOpen || (group != null && !group.interactable)) return;
            if (index < 0 || index >= _slots.Length || _slots[index] == null) return;
            DeleteRequested?.Invoke(_slots[index]);
        }

        /// <summary>새 가설이 꽂힌 탭을 잠깐 밝힌다.</summary>
        public void Pulse(string hypothesisId)
        {
            var i = Array.IndexOf(_slots, hypothesisId);
            if (i < 0 || i >= tabs.Count || tabs[i]?.root == null || !isActiveAndEnabled) return;
            StartCoroutine(PulseRoutine(tabs[i].root));
        }

        private static IEnumerator PulseRoutine(RectTransform rt)
        {
            var baseScale = rt.localScale;
            var t = 0f;
            const float dur = 0.5f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = baseScale * (1f + 0.08f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / dur)));
                yield return null;
            }
            rt.localScale = baseScale;
        }

        /// <summary>
        /// 가설 칸이 가득 찼다 — 가운데 새 가설 카드와 「교체 취소」를 띄우고, 나머지는 어두운 막으로 덮어 막는다.
        /// 우측 가설 목록만 막 위로 올려, 누른 탭 자리에 새 가설이 들어간다.
        /// </summary>
        public void BeginReplace(string prompt, string newTitle)
        {
            _replacing = true;
            if (replacePrompt != null) replacePrompt.text = prompt ?? string.Empty;
            if (replaceTitle != null) replaceTitle.text = string.IsNullOrEmpty(newTitle) ? string.Empty : "〈" + newTitle + "〉";
            BlockBehind(true);
            RaiseList(true);
            if (replaceRoot != null) replaceRoot.SetActive(true);
            if (endButton != null) endButton.interactable = false;
            RenderTabs();
        }

        public void EndReplace()
        {
            _replacing = false;
            if (replaceRoot != null) replaceRoot.SetActive(false);
            RaiseList(false);
            BlockBehind(false);
            RenderTabs();
        }

        // ---------------------------------------------------------------- 지우기 경고

        public bool ConfirmOpen => confirmRoot != null && confirmRoot.activeSelf;

        /// <summary>경고 한 번 — 답을 받으면 done(예/아니오).</summary>
        public void ShowConfirm(string message, Action<bool> done)
        {
            if (confirmRoot == null) { done?.Invoke(true); return; }
            _confirmDone = done;
            if (confirmText != null) confirmText.text = message ?? string.Empty;
            BlockBehind(true);
            confirmRoot.SetActive(true);
        }

        private void CloseConfirm(bool yes)
        {
            if (!ConfirmOpen) return;
            confirmRoot.SetActive(false);
            BlockBehind(false);
            var done = _confirmDone;
            _confirmDone = null;
            done?.Invoke(yes);
        }

        /// <summary>어두운 막을 깔고 그 아래 입력을 막는다(덧창 · 교체용). 대사 중 어둡게 하는 SetDim 과 같은 막을 쓴다.</summary>
        private void BlockBehind(bool on)
        {
            if (scrim == null) return;
            if (_scrimFade != null) { StopCoroutine(_scrimFade); _scrimFade = null; }
            // 덧창 · 교체 때는 대사용 어둠보다 한 단계 더 짙게 — 밝게 남은 것만 눈에 들어오도록.
            var img = scrim.GetComponent<Image>();
            if (img != null)
            {
                if (_scrimBaseAlpha < 0f) _scrimBaseAlpha = img.color.a;
                var c = img.color; c.a = on ? blockedScrimAlpha : _scrimBaseAlpha; img.color = c;
            }
            scrim.alpha = on ? 1f : 0f;
            scrim.blocksRaycasts = on;
            scrim.interactable = false;
        }

        /// <summary>가설 목록을 막 위로(혹은 제자리로).</summary>
        private void RaiseList(bool on)
        {
            if (hypothesisList == null || scrim == null) return;
            if (on)
            {
                if (_listSiblingIndex < 0) _listSiblingIndex = hypothesisList.GetSiblingIndex();
                var s = scrim.transform.GetSiblingIndex();
                // 앞에서 뒤로 옮기면 그 자리에 들어가고 막은 한 칸 당겨진다 — 목록이 막 바로 위가 된다.
                hypothesisList.SetSiblingIndex(_listSiblingIndex < s ? s : s + 1);
            }
            else if (_listSiblingIndex >= 0)
            {
                hypothesisList.SetSiblingIndex(_listSiblingIndex);
                _listSiblingIndex = -1;
            }
        }

        // ================================================================ 상단 · 연출

        public void SetInk(int capacity, int remaining)
        {
            if (ink == null) return;
            ink.SetCapacity(capacity);
            ink.SetRemaining(remaining);
        }

        public void SetInteractable(bool on)
        {
            if (group == null) return;
            group.interactable = on;
            group.blocksRaycasts = true;   // 덮개는 남긴다 — 뒤의 화면이 눌리면 안 된다
        }

        /// <summary>「추리를 마친다」 — 가설이 하나라도 있을 때.</summary>
        public void SetEndEnabled(bool on)
        {
            if (endButton != null) endButton.interactable = on && !_replacing;
        }

        /// <summary>「돌아가기」 — 아직 심문하지 않은 인물이 남아 있을 때.</summary>
        public void SetBackEnabled(bool on)
        {
            if (backButton != null) backButton.interactable = on && !_replacing;
        }

        /// <summary>추론 대사가 흐르는 동안 서류철을 어둡게 덮는다.</summary>
        public void SetDim(bool on) => Fade(scrim, on ? 1f : 0f, 0.25f, ref _scrimFade);

        /// <summary>「가설 도출 성공!」 같은 한 줄을 가운데 띄운다.</summary>
        public void ShowBanner(string text, bool success)
        {
            if (bannerLabel != null)
            {
                bannerLabel.text = text ?? string.Empty;
                bannerLabel.color = success ? successBanner : failBanner;
            }
            Fade(banner, 1f, 0.3f, ref _bannerFade);
        }

        public void HideBanner() => Fade(banner, 0f, 0.25f, ref _bannerFade);

        private void Fade(CanvasGroup g, float target, float seconds, ref Coroutine handle)
        {
            if (g == null) return;
            if (handle != null) StopCoroutine(handle);
            if (!isActiveAndEnabled) { SetAlpha(g, target, false); handle = null; return; }
            handle = StartCoroutine(FadeRoutine(g, target, seconds));
        }

        private static IEnumerator FadeRoutine(CanvasGroup g, float target, float seconds)
        {
            var from = g.alpha;
            var t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            g.alpha = target;
        }

        private static void SetAlpha(CanvasGroup g, float a, bool blocks)
        {
            if (g == null) return;
            g.alpha = a;
            g.blocksRaycasts = blocks;
            g.interactable = blocks;
        }

        // ================================================================ 만들기 보조

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private Image NewImage(string name, Transform parent, Color color)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = panelSprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        private TMP_Text NewText(string name, Transform parent, string content, float size, Color color, TextAlignmentOptions align)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
