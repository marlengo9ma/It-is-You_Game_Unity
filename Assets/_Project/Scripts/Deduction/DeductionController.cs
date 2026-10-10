using System;
using System.Collections;
using ItIsYou.Clues;
using ItIsYou.Core;
using ItIsYou.Data;
using ItIsYou.UI;
using UnityEngine;

namespace ItIsYou.Deduction
{
    /// <summary>어떻게 추론 페이즈를 떠났는가.</summary>
    public enum DeductionExit
    {
        /// <summary>추리를 마쳤다 — 가설을 갖고 2차 심문으로.</summary>
        Completed = 0,
        /// <summary>잉크가 다 말랐는데 가설이 없다 — 게임오버.</summary>
        Failed = 1,
        /// <summary>「돌아가기」 — 아직 심문하지 않은 인물에게 가려고 1차 인물 선택으로 돌아간다. 추론 상태는 그대로 남는다.</summary>
        Returned = 2
    }

    /// <summary>
    /// 추론(단서 조합 → 〈가설〉) 페이즈의 진행자. 규칙은 <see cref="DeductionSession"/> 이, 이 클래스는 화면과 입력만.
    ///
    /// 2026-10-11 화면 개편 — 책상 위 서류 두 장(<see cref="DeductionDossierView"/>):
    /// 진입 대사 → (튜토리얼 팝업 4) → [왼쪽 Evidence 하나 + 오른쪽 Leads 하나 → 붉은 선 → 「가설 추론」 →
    /// 화면이 어두워지고 주인공의 추론 + 「가설 도출 성공!」 → 가설 탭에 꽂힌다 /
    /// 실패: 「가설 도출 실패」 + 회차 반응] → 모든 시도가 잉크 −1 → 잉크 0 &amp; 가설 0 = 게임오버 /
    /// 잉크 0 &amp; 가설 ≥ 1 = 저절로 끝.
    ///
    /// 2026-10-11 2차 (제작자 지시):
    /// <list type="bullet">
    /// <item>「추리를 마친다」 — 가설 목록 맨 아래. 가설이 하나라도 있으면 누를 수 있다.</item>
    /// <item>「돌아가기」 — 1차 심문을 한 사람만 하고 넘어왔을 때, 인물 선택으로 돌아간다. 잉크·가설·실패한 짝은 그대로 남고,
    /// 인물 선택 화면의 「추론 진행하기」로 같은 상태에 다시 들어온다.</item>
    /// <item>가설이 가득 찬 채 새 가설이 생기면: 대사 → 가운데 새 가설 카드 + 「교체 취소」, 나머지는 어둡게 막고 우측 가설 목록만 살린다.</item>
    /// <item>가설 탭의 쓰레기통 — 경고 한 번 뒤 지운다. 잉크는 돌아오지 않는다.</item>
    /// </list>
    /// </summary>
    [AddComponentMenu("It is You/Deduction Controller")]
    public class DeductionController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private DialoguePresenter dialogue;
        [SerializeField] private DeductionDossierView dossier;
        [Tooltip("단서·가설을 크게 열어 읽는 열람창. 우측 목록이 숨겨져 있어도 열람창은 따로 뜬다.")]
        [SerializeField] private ClueTrayView clueTray;
        [SerializeField] private TutorialPopupView tutorialPopup;
        [SerializeField] private CursorController cursor;

        [Header("문구")]
        [SerializeField] private string successBanner = "가설 도출 성공!";
        [SerializeField] private string failBanner = "가설 도출 실패";
        [SerializeField] private string hintPick = "Evidence 하나, Leads 하나를 고른다.";
        [SerializeField] private string hintTried = "이 둘은 이미 이어 보았다.";
        [Tooltip("가설 교체 — 가운데 새 가설 카드 위에 뜨는 안내. (제작자 문구)")]
        [SerializeField] private string replacePrompt = "새로운 가설과 교체할 가설을 선택해주세요.";
        [Tooltip("{0} = 가설 이름, {1} = 을/를 (받침에 맞춰).")]
        [SerializeField] private string deleteConfirmFormat = "「{0}」{1} 지울까요?\n지운 가설은 되돌릴 수 없습니다.";

        private DeductionSession _session;
        private ChapterDefinition _chapter;
        private ClueInventory _inventory;
        private ClueDefinition _objectPick;
        private ClueDefinition _leadPick;
        private bool _busy;
        private bool _canReturn;
        private bool _combineRequested;
        private bool _endRequested;
        private bool _returnRequested;
        private string _deleteRequest;
        private string _replacePick;
        private bool _replaceCancelled;
        private Coroutine _routine;

        /// <summary>페이즈를 떠났다. (어떻게 떠났는가)</summary>
        public event Action<DeductionExit> Exited;

        /// <summary>새 가설이 생겼다.</summary>
        public event Action<ClueDefinition> Deduced;

        public DeductionSession Session => _session;
        public bool IsRunning => _session != null && !_session.IsOver;

        /// <summary>지난번 추론을 「돌아가기」로 떠났다 — 같은 상태로 다시 들어갈 수 있다.</summary>
        public bool CanResume => _session != null && !_session.IsOver;

        /// <summary>
        /// 추론을 시작한다. resume 이면 「돌아가기」로 떠났던 상태(잉크·가설·실패한 짝)를 이어 간다 —
        /// 진입 대사와 튜토리얼 팝업은 다시 나오지 않는다. canReturn 이면 「돌아가기」를 누를 수 있다.
        /// </summary>
        public void Begin(ChapterDefinition chapter, ClueInventory inventory, bool canReturn = false, bool resume = false)
        {
            var resuming = resume && CanResume && _chapter == chapter && _inventory == inventory;
            _chapter = chapter;
            _inventory = inventory;
            _canReturn = canReturn;
            if (!resuming) _session = new DeductionSession(chapter, inventory);
            _objectPick = null;
            _leadPick = null;

            // 책상에 서류를 펼친 자리다 — 모은 것은 전부 펼쳐 보이므로 이름도 세부 명칭으로 읽힌다.
            foreach (var id in new System.Collections.Generic.List<string>(inventory.OrderedIds))
            {
                var c = inventory.Get(id);
                if (c != null && (DeductionSession.IsObjectSide(c) || DeductionSession.IsLeadSide(c))) inventory.MarkRead(id);
            }

            Subscribe();

            if (dossier != null)
            {
                dossier.Open(inventory, chapter.FirstRoundSuspects, _session.MaxHypotheses, chapter.SceneClock, chapter.SceneLocation);
                dossier.SetInk(_session.InkCapacity, _session.Ink);
            }

            dialogue?.SetPacing(slowSpeech: false, allowSkip: true);
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Run(firstEntry: !resuming));
        }

        private void Subscribe()
        {
            Unsubscribe();
            if (dossier == null) return;
            dossier.EvidencePicked += OnEvidencePicked;
            dossier.LeadPicked += OnLeadPicked;
            dossier.CombineRequested += OnCombineRequested;
            dossier.EndRequested += OnEndRequested;
            dossier.BackRequested += OnBackRequested;
            dossier.DeleteRequested += OnDeleteRequested;
            dossier.ReadRequested += OnReadRequested;
            dossier.ReplacePicked += OnReplacePicked;
            dossier.ReplaceCancelled += OnReplaceCancelled;
        }

        private void Unsubscribe()
        {
            if (dossier == null) return;
            dossier.EvidencePicked -= OnEvidencePicked;
            dossier.LeadPicked -= OnLeadPicked;
            dossier.CombineRequested -= OnCombineRequested;
            dossier.EndRequested -= OnEndRequested;
            dossier.BackRequested -= OnBackRequested;
            dossier.DeleteRequested -= OnDeleteRequested;
            dossier.ReadRequested -= OnReadRequested;
            dossier.ReplacePicked -= OnReplacePicked;
            dossier.ReplaceCancelled -= OnReplaceCancelled;
        }

        private void OnDisable() => Unsubscribe();

        private IEnumerator Run(bool firstEntry)
        {
            _busy = true;
            dossier?.SetInteractable(false);
            RefreshView();

            if (firstEntry)
            {
                if (_chapter.DeductionIntro != null && !_chapter.DeductionIntro.IsEmpty)
                    yield return dialogue.Play(_chapter.DeductionIntro);

                yield return ShowTutorial(TutorialPopupTrigger.BeforeDeduction);
            }

            var returned = false;
            while (!_session.IsOver)
            {
                // 입력을 기다린다 — 고르기는 이벤트로, 여기서는 도장 · 마침 · 돌아가기 · 지우기만.
                _busy = false;
                _combineRequested = false;
                _endRequested = false;
                _returnRequested = false;
                _deleteRequest = null;
                RefreshView();
                dossier?.SetInteractable(true);
                while (!_combineRequested && !_endRequested && !_returnRequested && _deleteRequest == null) yield return null;
                _busy = true;
                dossier?.SetInteractable(false);

                if (_returnRequested)
                {
                    returned = true;
                    break;
                }

                if (_endRequested)
                {
                    if (_session.EndManually()) break;
                    continue;
                }

                if (_deleteRequest != null)
                {
                    yield return ConfirmDelete(_deleteRequest);
                    continue;
                }

                var result = _session.TryCombine(_objectPick, _leadPick);
                dossier?.SetInk(_session.InkCapacity, _session.Ink);   // 모든 시도가 잉크를 쓴다
                switch (result.Outcome)
                {
                    case HypothesisOutcome.Success:
                        yield return OnSuccess(result.Recipe);
                        break;
                    case HypothesisOutcome.SuccessNeedsReplace:
                        yield return OnNeedsReplace(result.Recipe);
                        break;
                    case HypothesisOutcome.Duplicate:
                        yield return OnDuplicate();
                        break;
                    case HypothesisOutcome.Fail:
                        yield return OnFail(result);
                        break;
                    case HypothesisOutcome.Fatal:
                        yield return OnFatal(result);
                        break;
                }

                // 잉크가 다 말랐는데 가설이 있으면 여기서 저절로 끝난다.
                if (_session.IsOver && _session.Succeeded && _chapter.DeductionInkOut != null && !_chapter.DeductionInkOut.IsEmpty)
                    yield return dialogue.Play(_chapter.DeductionInkOut);
            }

            dossier?.SetInteractable(false);
            ClearPicks();
            RefreshView();

            if (!returned && _session.Succeeded && _chapter.DeductionOutro != null && !_chapter.DeductionOutro.IsEmpty)
                yield return dialogue.Play(_chapter.DeductionOutro);

            _busy = false;
            _routine = null;
            Exited?.Invoke(returned ? DeductionExit.Returned : _session.Succeeded ? DeductionExit.Completed : DeductionExit.Failed);
        }

        // ------------------------------------------------------------------ 결과

        /// <summary>
        /// 대사를 틀면서, 마지막 줄이 다 나온 순간 가운데 한 줄을 띄운다 — 스케치 5 → 6.
        /// 대사가 없으면 한 줄만 잠깐 띄운다.
        /// </summary>
        private IEnumerator PlayWithBanner(DialogueSequence sequence, string text, bool success)
        {
            if (dossier == null)
            {
                if (sequence != null && !sequence.IsEmpty) yield return dialogue.Play(sequence);
                yield break;
            }

            if (sequence == null || sequence.IsEmpty)
            {
                dossier.ShowBanner(text, success);
                yield return new WaitForSecondsRealtime(1.4f);
                dossier.HideBanner();
                yield break;
            }

            var remaining = sequence.Count;
            void OnLine(DialogueLine _) { if (--remaining <= 0) dossier.ShowBanner(text, success); }
            dialogue.LineCompleted += OnLine;
            yield return dialogue.Play(sequence);
            dialogue.LineCompleted -= OnLine;
            dossier.HideBanner();
        }

        private IEnumerator OnSuccess(HypothesisRecipe recipe)
        {
            dossier?.SetDim(true);
            yield return PlayWithBanner(recipe.OnSuccess, successBanner, true);
            dossier?.SetDim(false);

            Deduced?.Invoke(recipe.Result);
            ClearPicks();
            RefreshView();
            if (recipe.Result != null) dossier?.Pulse(recipe.Result.Id);
        }

        /// <summary>
        /// 가설 칸이 가득 찼다 (제작자 지시 2026-10-11):
        /// 성공 연출 → 「이보다 더 많은 가설을…」 대사 → 가운데 새 가설 카드 + 「교체 취소」.
        /// 카드 · 교체 취소 · 우측 가설 목록만 밝게 남고 나머지는 어둡게 막힌다.
        /// </summary>
        private IEnumerator OnNeedsReplace(HypothesisRecipe recipe)
        {
            dossier?.SetDim(true);
            yield return PlayWithBanner(recipe.OnSuccess, successBanner, true);

            if (_chapter.DeductionReplacePrompt != null && !_chapter.DeductionReplacePrompt.IsEmpty)
                yield return dialogue.Play(_chapter.DeductionReplacePrompt);

            // 고른 두 단서와 선·도장은 걷는다 — 지금 고를 것은 가설 탭뿐이다.
            ClearPicks();
            RefreshView();

            _replacePick = null;
            _replaceCancelled = false;
            dossier?.BeginReplace(replacePrompt, recipe.Title);
            dossier?.SetInteractable(true);
            while (_replacePick == null && !_replaceCancelled) yield return null;
            dossier?.SetInteractable(false);
            dossier?.EndReplace();
            dossier?.SetDim(false);

            if (_replaceCancelled)
            {
                _session.CancelReplace();
                ClearPicks();
                RefreshView();
                yield break;
            }

            var added = _session.ConfirmReplace(_replacePick);
            ClearPicks();
            RefreshView();
            if (added != null)
            {
                Deduced?.Invoke(added);
                dossier?.Pulse(added.Id);
            }
        }

        private IEnumerator OnDuplicate()
        {
            ClearPicks();
            RefreshView();
            if (_chapter.DeductionDuplicate != null && !_chapter.DeductionDuplicate.IsEmpty)
                yield return dialogue.Play(_chapter.DeductionDuplicate);
        }

        private IEnumerator OnFail(HypothesisResult result)
        {
            dossier?.SetDim(true);
            yield return PlayWithBanner(_chapter.GetDeductionFailure(result.FailRound), failBanner, false);
            dossier?.SetDim(false);
            ClearPicks();
            RefreshView();

            if (result.FailRound == 1)
                yield return ShowTutorial(TutorialPopupTrigger.OnDeductionFirstMiss);
        }

        private IEnumerator OnFatal(HypothesisResult result)
        {
            dossier?.SetDim(true);
            yield return PlayWithBanner(_chapter.GetDeductionFailure(result.FailRound), failBanner, false);
            ClearPicks();
            RefreshView();

            if (_chapter.SelfLoathingEnding != null && !_chapter.SelfLoathingEnding.IsEmpty)
                yield return dialogue.Play(_chapter.SelfLoathingEnding);
            dossier?.SetDim(false);
        }

        /// <summary>쓰레기통 — 경고 한 번 뒤 지운다.</summary>
        private IEnumerator ConfirmDelete(string id)
        {
            var clue = _inventory != null ? _inventory.Get(id) : null;
            if (clue == null || dossier == null) yield break;

            var name = _inventory.DisplayName(id);
            bool? answer = null;
            dossier.ShowConfirm(string.Format(deleteConfirmFormat, name, ObjectParticle(name)), yes => answer = yes);
            dossier.SetInteractable(true);
            while (answer == null) yield return null;
            dossier.SetInteractable(false);

            if (answer == true && _session.RemoveHypothesis(id))
            {
                ClearPicks();
                RefreshView();
            }
        }

        // ------------------------------------------------------------------ 입력

        private void OnEvidencePicked(ClueDefinition clue)
        {
            if (_busy || !IsRunning || !DeductionSession.IsObjectSide(clue)) return;
            _objectPick = _objectPick == clue ? null : clue;
            RefreshView();
        }

        private void OnLeadPicked(ClueDefinition clue)
        {
            if (_busy || !IsRunning || !DeductionSession.IsLeadSide(clue)) return;
            _leadPick = _leadPick == clue ? null : clue;
            RefreshView();
        }

        private void OnCombineRequested()
        {
            if (_busy || !IsRunning || _objectPick == null || _leadPick == null) return;
            if (_session.TriedAndFailed(_objectPick.Id, _leadPick.Id)) return;
            _combineRequested = true;
        }

        private void OnEndRequested()
        {
            if (_busy || !IsRunning || !_session.CanEnd) return;
            _endRequested = true;
        }

        private void OnBackRequested()
        {
            if (_busy || !IsRunning || !_canReturn) return;
            _returnRequested = true;
        }

        private void OnDeleteRequested(string id)
        {
            if (_busy || !IsRunning || string.IsNullOrEmpty(id)) return;
            _deleteRequest = id;
        }

        private void OnReadRequested(ClueDefinition clue)
        {
            if (clue == null || clueTray == null) return;
            clueTray.OpenReading(clue);
        }

        private void OnReplacePicked(string id) => _replacePick = id;
        private void OnReplaceCancelled() => _replaceCancelled = true;

        private void ClearPicks()
        {
            _objectPick = null;
            _leadPick = null;
        }

        private void RefreshView()
        {
            if (dossier == null || _session == null) return;

            dossier.SetHypotheses(_session.Hypotheses);

            var both = _objectPick != null && _leadPick != null;
            var tried = both && _session.TriedAndFailed(_objectPick.Id, _leadPick.Id);
            var hint = !IsRunning ? string.Empty : tried ? hintTried : !both ? hintPick : string.Empty;
            dossier.SetSelection(_objectPick, _leadPick, both && !tried && IsRunning, hint);
            dossier.SetEndEnabled(_session.CanEnd && IsRunning);
            dossier.SetBackEnabled(_canReturn && IsRunning);
        }

        /// <summary>마지막 글자에 받침이 있으면 '을', 없으면 '를'. 한글이 아니면 '을(를)'.</summary>
        private static string ObjectParticle(string word)
        {
            if (string.IsNullOrEmpty(word)) return "을(를)";
            var c = word[word.Length - 1];
            if (c < '가' || c > '힣') return "을(를)";
            return (c - '가') % 28 == 0 ? "를" : "을";
        }

        private IEnumerator ShowTutorial(TutorialPopupTrigger trigger)
        {
            if (tutorialPopup == null || _chapter == null) yield break;
            var popup = _chapter.FindTutorialPopup(trigger);
            if (popup == null || tutorialPopup.HasShown(popup)) yield break;
            yield return tutorialPopup.ShowOnce(popup);
        }
    }
}
