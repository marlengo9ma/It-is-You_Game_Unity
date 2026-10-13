using System;
using System.Collections;
using System.Collections.Generic;
using ItIsYou.Clues;
using ItIsYou.Data;
using ItIsYou.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.Investigation
{
    /// <summary>
    /// 단서 수집 페이즈의 진행자.
    ///
    /// 씬에 놓인 <see cref="InteractableObject"/> 들을 모아 붙이고,
    /// 클릭할 때마다 화면 중앙에 이미지 팝업을 띄우고, 설명 대사를 재생하고, 우측 목록에 넣는다.
    /// 일반 오브젝트도 '수집 오브젝트'로 목록에 들어간다 — 단서로 정리하는 것은 열람창의 일이다.
    /// 페이즈를 끝내려 할 때는 <see cref="CollectionGate"/> 로 수집률을 판정한다.
    ///
    /// 2026-10-13 노션 반영:
    /// <list type="bullet">
    /// <item>〈수집페이즈에 대한 튜토리얼 팝업〉 — 페이즈 처음에 한 번, 화면을 어둡게 하고 한 곳만 밝혀 차례로 따라 하게 한다(<see cref="CoachMarkView"/>).</item>
    /// <item>70% 이상 구간 — "[아직 수집하지 못한 수집품/기록이 존재합니다. 단서 수집을 종료할까요?] 경고 팝업 후, 진행 여부 선택".</item>
    /// <item>신문 · 객실 정보 — "수집 시 나레이션"은 누를 때, "*수집품을 읽고 난 뒤, 대사 및 나레이션"은 열람창을 처음 닫을 때.</item>
    /// </list>
    /// </summary>
    [AddComponentMenu("It is You/Investigation Controller")]
    public class InvestigationController : MonoBehaviour
    {
        [Header("씬")]
        [Tooltip("비워 두면 자식에서 자동으로 모은다.")]
        [SerializeField] private List<InteractableObject> sceneObjects = new List<InteractableObject>();

        [Header("UI")]
        [SerializeField] private DialoguePresenter dialogue;
        [SerializeField] private CursorController cursor;
        [SerializeField] private ClueTrayView clueTray;

        [Tooltip("오브젝트를 눌렀을 때 화면 중앙에 뜨는 이미지 팝업.")]
        [SerializeField] private CloseUpPopup closeUp;

        [Tooltip("다음 페이즈로 넘어가는 버튼. 에디터에서 붙인 연결은 저장되지 않으므로 런타임에 직접 잇는다.")]
        [SerializeField] private Button advanceButton;

        [Tooltip("수집 상황 표시. 남은 개수를 직접 세어 주지는 않는다 — 몇 개가 더 있는지 알려주면 탐색이 아니라 체크리스트가 된다.")]
        [SerializeField] private TMP_Text progressLabel;

        [Header("튜토리얼 · 확인")]
        [Tooltip("〈수집페이즈에 대한 튜토리얼 팝업〉의 어둡게 덮고 한 곳만 밝히는 연출.")]
        [SerializeField] private CoachMarkView coachMark;
        [Tooltip("수집률 경고 구간에서 진행 여부를 묻는 팝업.")]
        [SerializeField] private ConfirmPopupView confirmPopup;
        [SerializeField] private string guideNextLabel = "다음";
        [SerializeField] private string confirmYesLabel = "종료한다";
        [SerializeField] private string confirmNoLabel = "더 살펴본다";

        private ChapterDefinition _chapter;
        private ClueInventory _inventory;
        private bool _busy;
        private bool _active;
        private readonly HashSet<string> _afterReadPlayed = new HashSet<string>();

        // ---- 튜토리얼 진행 상태 ----
        private bool _guiding;
        private string _guideObjectId;        // 지금 누를 수 있는 오브젝트 (비면 전부)
        private string _guideCollectedId;
        private string _guideOpenedClueId;
        private string _guidePromotedClueId;
        private bool _guideButton;

        /// <summary>페이즈를 끝내도 된다는 판정이 났다.</summary>
        public event Action<CollectionGate> ReadyToAdvance;

        /// <summary>경고 구간이라 플레이어 확인이 필요하다.</summary>
        public event Action<CollectionGate> ConfirmationRequested;

        /// <summary>단서를 새로 얻었다.</summary>
        public event Action<ClueDefinition> ClueCollected;

        public bool IsBusy => _busy;
        public bool IsGuiding => _guiding;

        public void Begin(ChapterDefinition chapter, ClueInventory inventory)
        {
            _chapter = chapter;
            _inventory = inventory;
            _active = true;

            if (sceneObjects == null || sceneObjects.Count == 0)
                sceneObjects = new List<InteractableObject>(GetComponentsInChildren<InteractableObject>(true));

            for (int i = 0; i < sceneObjects.Count; i++)
            {
                var obj = sceneObjects[i];
                if (obj == null) continue;
                obj.HoverChanged -= OnHoverChanged;
                obj.Clicked -= OnClicked;
                obj.HoverChanged += OnHoverChanged;
                obj.Clicked += OnClicked;
                obj.SetEnabled(true);
            }

            if (advanceButton != null)
            {
                advanceButton.onClick.RemoveListener(RequestAdvance);
                advanceButton.onClick.AddListener(RequestAdvance);
            }

            if (clueTray != null)
            {
                clueTray.ReadingOpened -= OnReadingOpened;
                clueTray.ReadingClosed -= OnReadingClosed;
                clueTray.CluePromoted -= OnCluePromoted;
                clueTray.ReadingOpened += OnReadingOpened;
                clueTray.ReadingClosed += OnReadingClosed;
                clueTray.CluePromoted += OnCluePromoted;
            }
            if (coachMark != null)
            {
                coachMark.ButtonPressed -= OnGuideButton;
                coachMark.ButtonPressed += OnGuideButton;
                coachMark.Hide();
            }

            dialogue.SyncCollected(inventory.OwnedIds);
            RefreshTray();
            RefreshProgress();

            if (chapter.CollectionGuide != null && chapter.CollectionGuide.Count > 0 && coachMark != null)
                StartCoroutine(RunGuide(chapter.CollectionGuide));
        }

        private void OnDisable()
        {
            for (int i = 0; i < sceneObjects.Count; i++)
            {
                var obj = sceneObjects[i];
                if (obj == null) continue;
                obj.HoverChanged -= OnHoverChanged;
                obj.Clicked -= OnClicked;
            }
            if (clueTray != null)
            {
                clueTray.ReadingOpened -= OnReadingOpened;
                clueTray.ReadingClosed -= OnReadingClosed;
                clueTray.CluePromoted -= OnCluePromoted;
            }
            if (coachMark != null) coachMark.ButtonPressed -= OnGuideButton;
        }

        private void OnHoverChanged(InteractableObject obj, bool hovering)
        {
            if (_busy) return;
            if (_guiding && (obj == null || obj.Definition == null || obj.Definition.Id != _guideObjectId)) { cursor?.SetHoveringInteractable(false); return; }
            cursor?.SetHoveringInteractable(hovering);
        }

        private void OnClicked(InteractableObject obj)
        {
            if (_busy || obj == null || obj.Definition == null) return;
            if (clueTray != null && clueTray.IsTooltipOpen) return;
            if (_guiding && obj.Definition.Id != _guideObjectId) return;
            StartCoroutine(InspectRoutine(obj));
        }

        /// <summary>
        /// 기획: "좌클릭 시 &gt;&gt; 화면 중앙에 단서의 이미지 팝업과 &lt;주인공&gt; 혹은 &lt;조수&gt;의
        /// 설명 대사 출력 및 우측 ui내에 수집".
        /// 팝업 → 대사(있으면) → 수집 순서다. 대사가 없는 오브젝트는 팝업을 한 번 눌러 닫는다.
        /// </summary>
        private IEnumerator InspectRoutine(InteractableObject obj)
        {
            _busy = true;
            SetObjectsEnabled(false);
            cursor?.ResetState();
            if (_guiding) coachMark?.Hide();

            var def = obj.Definition;
            var lines = obj.Inspected ? def.OnReinspect : def.OnInspect;
            obj.MarkInspected();

            var label = def.GrantedClue != null ? def.GrantedClue.UnreadName : def.HoverLabel;
            if (closeUp != null) closeUp.Show(def.CloseUpImage, label);

            if (lines != null && lines.Count > 0)
                yield return dialogue.Play(new DialogueSequence(lines, def.Id));
            else if (closeUp != null)
                yield return closeUp.WaitForDismiss();

            if (closeUp != null) closeUp.Hide();

            // 일반 오브젝트도 '수집 오브젝트'로 목록에 들어간다.
            if (def.GrantsClue && _inventory.Add(def.GrantedClue))
            {
                dialogue.NotifyCollected(def.GrantedClue.Id);
                RefreshTray();
                ClueCollected?.Invoke(def.GrantedClue);
            }

            RefreshProgress();
            _busy = false;
            SetObjectsEnabled(true);
            if (_guiding) _guideCollectedId = def.Id;
        }

        // ------------------------------------------------------------------ 열람창

        private void OnReadingOpened(ClueDefinition clue)
        {
            if (_guiding && clue != null) _guideOpenedClueId = clue.Id;
        }

        private void OnCluePromoted(ClueDefinition clue)
        {
            if (_guiding && clue != null) _guidePromotedClueId = clue.Id;
        }

        /// <summary>"*수집품을 읽고 난 뒤, 대사 및 나레이션 재생" — 처음 한 번만, 단서 수집 페이즈에서만.</summary>
        private void OnReadingClosed(ClueDefinition clue)
        {
            if (!_active || clue == null || _afterReadPlayed.Contains(clue.Id)) return;
            var def = FindObjectFor(clue);
            if (def == null || def.OnAfterRead == null || def.OnAfterRead.Count == 0) return;
            _afterReadPlayed.Add(clue.Id);
            StartCoroutine(AfterReadRoutine(def));
        }

        private IEnumerator AfterReadRoutine(InteractableDefinition def)
        {
            while (_busy) yield return null;
            _busy = true;
            SetObjectsEnabled(false);
            yield return dialogue.Play(new DialogueSequence(def.OnAfterRead, def.Id + "_READ"));
            _busy = false;
            SetObjectsEnabled(true);
        }

        private InteractableDefinition FindObjectFor(ClueDefinition clue)
        {
            for (int i = 0; i < sceneObjects.Count; i++)
            {
                var d = sceneObjects[i] != null ? sceneObjects[i].Definition : null;
                if (d != null && d.GrantedClue == clue) return d;
            }
            return null;
        }

        // ------------------------------------------------------------------ 튜토리얼

        private void OnGuideButton() => _guideButton = true;

        /// <summary>
        /// 〈수집페이즈에 대한 튜토리얼 팝업〉. 단계마다 한 곳만 밝혀 두고, 그 자리를 실제로 눌러야 넘어간다.
        /// 설명만 있는 단계는 안내창의 버튼으로 넘어간다.
        /// </summary>
        private IEnumerator RunGuide(IReadOnlyList<GuideStep> steps)
        {
            _guiding = true;
            for (int s = 0; s < steps.Count; s++)
            {
                var step = steps[s];
                if (step == null) continue;

                _guideButton = false;
                _guideCollectedId = null;
                _guideOpenedClueId = null;
                _guidePromotedClueId = null;
                _guideObjectId = step.Advance == GuideAdvance.ObjectCollected ? step.TargetId : null;

                // 이미 해 버린 단계(목록에 이미 있다 등)는 건너뛴다.
                if (step.Advance == GuideAdvance.ObjectCollected && IsCollected(step.TargetId)) continue;
                if (step.Advance == GuideAdvance.Promoted && _inventory.IsPromoted(step.TargetId)) continue;

                while (_busy) yield return null;
                coachMark.Show(step.Text, step.Advance == GuideAdvance.Button ? guideNextLabel : null,
                    step.Focus != GuideFocus.None, HoleFor(step));

                switch (step.Advance)
                {
                    case GuideAdvance.Button:
                        while (!_guideButton) yield return null;
                        break;
                    case GuideAdvance.ObjectCollected:
                        while (_guideCollectedId != step.TargetId) yield return null;
                        break;
                    case GuideAdvance.TooltipOpened:
                        while (_guideOpenedClueId != step.TargetId) yield return null;
                        break;
                    case GuideAdvance.Promoted:
                        while (_guidePromotedClueId != step.TargetId) yield return null;
                        break;
                }
                coachMark.Hide();
            }
            _guideObjectId = null;
            _guiding = false;
        }

        private bool IsCollected(string objectId)
        {
            for (int i = 0; i < sceneObjects.Count; i++)
            {
                var d = sceneObjects[i] != null ? sceneObjects[i].Definition : null;
                if (d != null && d.Id == objectId) return d.GrantedClue != null && _inventory.Has(d.GrantedClue.Id);
            }
            return false;
        }

        private Func<(Vector3 min, Vector3 max)?> HoleFor(GuideStep step)
        {
            switch (step.Focus)
            {
                case GuideFocus.SceneObject:
                    {
                        InteractableObject target = null;
                        for (int i = 0; i < sceneObjects.Count; i++)
                            if (sceneObjects[i] != null && sceneObjects[i].Definition != null && sceneObjects[i].Definition.Id == step.TargetId) target = sceneObjects[i];
                        return () => target != null ? CoachMarkView.WorldRect(target.transform as RectTransform) : null;
                    }
                case GuideFocus.TraySlot:
                    return () => clueTray != null ? CoachMarkView.WorldRect(clueTray.SlotRectFor(step.TargetId)) : null;
                // 열람창을 닫아 버렸다면 그 단서의 칸을 다시 밝혀 — 다시 열 수 있게 한다.
                case GuideFocus.TooltipWindow:
                    return () => clueTray == null ? null
                        : CoachMarkView.WorldRect(clueTray.TooltipPanelRect) ?? CoachMarkView.WorldRect(clueTray.SlotRectFor(step.TargetId));
                case GuideFocus.TooltipHighlight:
                    return () => clueTray == null ? null
                        : clueTray.HighlightWorldRect() ?? CoachMarkView.WorldRect(clueTray.TooltipPanelRect) ?? CoachMarkView.WorldRect(clueTray.SlotRectFor(step.TargetId));
                default:
                    return () => null;
            }
        }

        // ------------------------------------------------------------------ 진행

        /// <summary>플레이어가 다음 페이즈로 넘어가려 한다.</summary>
        public void RequestAdvance()
        {
            if (_busy || _guiding) return;
            if (clueTray != null && clueTray.IsTooltipOpen) return;
            StartCoroutine(AdvanceRoutine());
        }

        private IEnumerator AdvanceRoutine()
        {
            _busy = true;
            SetObjectsEnabled(false);

            var gate = CollectionGate.Evaluate(_chapter, _inventory);
            var speech = gate.PickDialogue(_chapter);

            if (speech != null && !speech.IsEmpty)
                yield return dialogue.Play(speech);

            switch (gate.Result)
            {
                case CollectionGateResult.Pass:
                    _busy = false;
                    Finish(gate);
                    break;

                case CollectionGateResult.WarnAndConfirm:
                    // 노션: [아직 수집하지 못한 수집품/기록이 존재합니다. 단서 수집을 종료할까요?] 경고 팝업 후, 진행 여부 선택
                    ConfirmationRequested?.Invoke(gate);
                    if (confirmPopup != null)
                    {
                        var yes = false;
                        yield return confirmPopup.Ask(_chapter.CollectionWarnQuestion, confirmYesLabel, confirmNoLabel, a => yes = a);
                        _busy = false;
                        if (yes) { Finish(gate); break; }
                    }
                    _busy = false;
                    SetObjectsEnabled(true);
                    break;

                default:
                    // 진행 불가. 계속 뒤져야 한다.
                    _busy = false;
                    SetObjectsEnabled(true);
                    break;
            }
        }

        /// <summary>경고 팝업에서 "그래도 진행"을 골랐을 때.</summary>
        public void ConfirmAdvanceAnyway()
        {
            var gate = CollectionGate.Evaluate(_chapter, _inventory);
            if (gate.Result == CollectionGateResult.Blocked) return;
            Finish(gate);
        }

        private void Finish(CollectionGate gate)
        {
            _active = false;
            SetObjectsEnabled(false);
            if (clueTray != null && clueTray.IsTooltipOpen) clueTray.CloseTooltip();
            ReadyToAdvance?.Invoke(gate);
        }

        /// <summary>지금 수집 상태를 그대로 판정해 본다. UI 표시용.</summary>
        public CollectionGate PeekGate() => CollectionGate.Evaluate(_chapter, _inventory);

        private void SetObjectsEnabled(bool value)
        {
            for (int i = 0; i < sceneObjects.Count; i++)
                sceneObjects[i]?.SetEnabled(value);
        }

        private void RefreshTray()
        {
            if (clueTray != null) clueTray.Rebuild(_inventory);
            RefreshProgress();
        }

        /// <summary>조사한 곳과 남은 곳을 알린다. 단서 개수는 드러내지 않는다.</summary>
        private void RefreshProgress()
        {
            if (progressLabel == null) return;
            var seen = 0;
            for (int i = 0; i < sceneObjects.Count; i++)
            {
                if (sceneObjects[i] != null && sceneObjects[i].Inspected) seen++;
            }
            progressLabel.text = $"살펴본 곳  {seen} / {sceneObjects.Count}";
        }
    }
}
