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
    /// 범인 지목 페이즈 (2026-09-23 기획).
    ///
    /// 1) 〈가설〉을 고른다 (좌측 목록) → 2) 〈용의자〉를 고른다 (자리에 앉은 인물) →
    /// 3) 그 방향의 〈추론〉 두 갈래 중 하나를 고른다 (좌측 목록) → 주인공의 추론 대사 + 주변 반응 →
    /// (경우에 따라) 지목당한 쪽의 억울함 호소 → 우측 목록에서 단서를 골라 반박, 맞을 때까지 → 최종 결과 → 엔딩.
    ///
    /// 기획서상 정답 표시는 두지 않는다 — "정답이 없는 추리 게임".
    /// </summary>
    [AddComponentMenu("It is You/Accusation View")]
    public class AccusationView : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private DialoguePresenter dialogue;
        [SerializeField] private ClueTrayView clueTray;

        [Header("화면")]
        [SerializeField] private List<SuspectCard> cards = new List<SuspectCard>();
        [SerializeField] private TMP_Text prompt;

        [Tooltip("좌측 목록 — 가설, 그다음 추론 갈래가 들어간다.")]
        [SerializeField] private GameObject listRoot;
        [SerializeField] private TMP_Text listTitle;
        [SerializeField] private RectTransform listContainer;
        [SerializeField] private Button buttonPrefab;

        [Tooltip("지금까지 고른 것을 보여 주는 줄. 예: 가설 「…」 · 세피")]
        [SerializeField] private TMP_Text summary;

        private readonly List<Button> _spawned = new List<Button>();
        private ChapterDefinition _chapter;
        private ClueInventory _inventory;
        private bool _armed;
        private ClueDefinition _pickedHypothesis;
        private SuspectDefinition _pickedSuspect;
        private AccusationOption _pickedOption;
        private ClueDefinition _refuteClue;

        /// <summary>지목이 끝났다. (지목한 용의자, 결과 제목)</summary>
        public event Action<SuspectDefinition, string> Finished;

        public SuspectDefinition Accused { get; private set; }

        private void Awake()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                cards[i].Picked += OnSuspectPicked;
                cards[i].SetArmed(false);
            }
            if (listRoot != null) listRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < cards.Count; i++)
                if (cards[i] != null) cards[i].Picked -= OnSuspectPicked;
            if (clueTray != null) clueTray.ClueSelected -= OnRefuteClue;
        }

        public void Begin(ChapterDefinition chapter, ClueInventory inventory)
        {
            _chapter = chapter;
            _inventory = inventory;
            Accused = null;
            BindCards();
            StartCoroutine(Run());
        }

        private void BindCards()
        {
            var targets = _chapter != null ? _chapter.AccusationTargets : null;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                var has = targets != null && i < targets.Count && targets[i] != null;
                cards[i].gameObject.SetActive(has);
                if (has) cards[i].Bind(targets[i], selectableOverride: true);   // 지목에서는 고양이도 고를 수 있다
                cards[i].SetArmed(false);
            }
        }

        private IEnumerator Run()
        {
            while (true)
            {
                // ---- 1) 가설 ----
                _pickedHypothesis = null;
                SetSummary(null, null);
                ShowList("Hypothesis — 가설", BuildHypothesisEntries());
                SetPrompt(_chapter.AccusationHypothesisPrompt);
                while (_pickedHypothesis == null) yield return null;
                SetSummary(_pickedHypothesis, null);

                // ---- 2) 용의자 ----
                _pickedSuspect = null;
                HideList();
                SetPrompt(_chapter.AccusationPrompt);
                SetArmed(true);
                while (_pickedSuspect == null) yield return null;
                SetArmed(false);
                SetSummary(_pickedHypothesis, _pickedSuspect);

                // ---- 3) 추론 두 갈래 ----
                var branch = _chapter.FindAccusationBranch(_pickedHypothesis.Id, _pickedSuspect.Id);
                if (branch == null || branch.Options == null || branch.Options.Count == 0)
                {
                    // 데이터가 없으면 그냥 지목으로 친다.
                    Accused = _pickedSuspect;
                    break;
                }

                _pickedOption = null;
                ShowList("Reasoning — 추론", BuildOptionEntries(branch));
                SetPrompt(_chapter.AccusationReasoningPrompt);
                while (_pickedOption == null) yield return null;
                HideList();
                SetPrompt(string.Empty);

                var option = _pickedOption;
                if (option.Speech != null && !option.Speech.IsEmpty)
                    yield return dialogue.Play(option.Speech);

                if (option.ReturnsToSelection) continue;   // "…아직 확신이 없다" — 처음부터

                Accused = _pickedSuspect;

                // ---- 4) 억울함 호소 → 반박 (맞을 때까지) ----
                if (option.HasPlea)
                {
                    yield return dialogue.Play(option.Plea);
                    yield return RefuteLoop(option);
                }

                // ---- 5) 결과 ----
                if (option.Result != null && !option.Result.IsEmpty)
                    yield return dialogue.Play(option.Result);

                Finished?.Invoke(Accused, option.ResultTitle);
                yield break;
            }

            Finished?.Invoke(Accused, string.Empty);
        }

        private IEnumerator RefuteLoop(AccusationOption option)
        {
            if (clueTray == null)
            {
                yield return dialogue.Play(option.RefuteSuccess);
                yield break;
            }

            clueTray.ClueSelected -= OnRefuteClue;
            clueTray.ClueSelected += OnRefuteClue;
            clueTray.SetSelectionMode(true);

            while (true)
            {
                _refuteClue = null;
                SetPrompt(_chapter.RefutePrompt);
                while (_refuteClue == null) yield return null;
                var clue = _refuteClue;
                clueTray.SetSelection(clue.Id, null);
                SetPrompt(string.Empty);

                if (option.Refutes(clue.Id))
                {
                    if (option.RefuteSuccess != null && !option.RefuteSuccess.IsEmpty)
                        yield return dialogue.Play(option.RefuteSuccess);
                    break;
                }

                if (option.RefuteWrong != null && !option.RefuteWrong.IsEmpty)
                    yield return dialogue.Play(option.RefuteWrong);
                clueTray.SetSelection(null, null);
            }

            clueTray.ClueSelected -= OnRefuteClue;
            clueTray.SetSelectionMode(false);
        }

        // ------------------------------------------------------------------ 목록

        private List<(string label, Action onClick)> BuildHypothesisEntries()
        {
            var list = new List<(string, Action)>();
            foreach (var clue in _inventory.OfKind(ClueKind.Deduced))
            {
                var c = clue;
                list.Add(("〈가설〉 " + _inventory.DisplayName(c.Id), () => _pickedHypothesis = c));
            }
            return list;
        }

        private List<(string label, Action onClick)> BuildOptionEntries(AccusationBranch branch)
        {
            var list = new List<(string, Action)>();
            for (int i = 0; i < branch.Options.Count; i++)
            {
                var o = branch.Options[i];
                if (o == null) continue;
                list.Add((o.Label, () => _pickedOption = o));
            }
            return list;
        }

        private void ShowList(string title, List<(string label, Action onClick)> entries)
        {
            for (int i = 0; i < _spawned.Count; i++) if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
            _spawned.Clear();
            if (listRoot == null || listContainer == null || buttonPrefab == null)
            {
                // 목록이 없는 씬 — 첫 항목을 자동으로 고른다.
                if (entries.Count > 0) entries[0].onClick();
                return;
            }
            if (listTitle != null) listTitle.text = title;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var b = Instantiate(buttonPrefab, listContainer);
                var label = b.transform.Find("Label")?.GetComponent<TMP_Text>() ?? b.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = e.label;
                    label.textWrappingMode = TextWrappingModes.Normal;
                    label.alignment = TextAlignmentOptions.Left;
                    label.margin = new Vector4(16f, 6f, 16f, 6f);
                }
                var rt = b.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, 88f);
                b.onClick.AddListener(() => e.onClick());
                _spawned.Add(b);
            }
            listRoot.SetActive(true);
        }

        private void HideList()
        {
            if (listRoot != null) listRoot.SetActive(false);
        }

        private void SetPrompt(string text)
        {
            if (prompt != null) prompt.text = text ?? string.Empty;
        }

        private void SetSummary(ClueDefinition hypothesis, SuspectDefinition suspect)
        {
            if (summary == null) return;
            if (hypothesis == null) { summary.text = string.Empty; return; }
            var t = "가설 「" + _inventory.DisplayName(hypothesis.Id) + "」";
            if (suspect != null) t += "  ·  " + suspect.DisplayName;
            summary.text = t;
        }

        private void SetArmed(bool armed)
        {
            _armed = armed;
            for (int i = 0; i < cards.Count; i++) if (cards[i] != null) cards[i].SetArmed(armed);
        }

        private void OnSuspectPicked(SuspectDefinition suspect)
        {
            if (!_armed) return;
            _pickedSuspect = suspect;
        }

        private void OnRefuteClue(ClueDefinition clue)
        {
            if (clue == null || clue.Kind == ClueKind.Deduced) return;
            _refuteClue = clue;
        }
    }
}
