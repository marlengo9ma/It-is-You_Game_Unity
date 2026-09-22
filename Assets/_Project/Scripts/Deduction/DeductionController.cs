using System;
using System.Collections;
using ItIsYou.Clues;
using ItIsYou.Core;
using ItIsYou.Data;
using ItIsYou.UI;
using UnityEngine;

namespace ItIsYou.Deduction
{
    /// <summary>
    /// 단서 조합(가설) 페이즈의 진행자. 규칙은 <see cref="DeductionSession"/> 이, 이 클래스는 화면과 입력만.
    ///
    /// 흐름: 진입 대사 → (튜토리얼 팝업 4) → [우측 목록에서 물품 하나 + 정황 하나 → "가설 도출" →
    /// 성공: 연출 + 주인공 대사 + 가설 칸에 저장 (가득 차면 지울 것을 고른다) /
    /// 실패: 연출 + 회차 반응 + 잉크 −1] → 잉크 0 & 가설 0 = 게임오버 / 잉크 0 & 가설 ≥ 1 = 저절로 끝 /
    /// 가설 ≥ 1 이면 "추리를 마친다" 로 먼저 끝낼 수 있다.
    /// </summary>
    [AddComponentMenu("It is You/Deduction Controller")]
    public class DeductionController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private DialoguePresenter dialogue;
        [SerializeField] private HypothesisBoardView board;
        [SerializeField] private ClueTrayView clueTray;
        [SerializeField] private InkDropsView ink;
        [SerializeField] private TutorialPopupView tutorialPopup;
        [SerializeField] private CursorController cursor;

        [Tooltip("가설이 생겼을 때 잠깐 놋쇠 빛으로 물드는 판. 보통 단서 패널.")]
        [SerializeField] private CanvasGroup confirmFlash;

        private DeductionSession _session;
        private ChapterDefinition _chapter;
        private ClueInventory _inventory;
        private ClueDefinition _objectPick;
        private ClueDefinition _leadPick;
        private bool _busy;
        private bool _combineRequested;
        private bool _endRequested;
        private string _replacePick;
        private bool _replaceCancelled;

        /// <summary>페이즈가 끝났다. (가설을 하나라도 갖고 끝났는가)</summary>
        public event Action<bool> Finished;

        /// <summary>새 가설이 생겼다.</summary>
        public event Action<ClueDefinition> Deduced;

        public DeductionSession Session => _session;
        public bool IsRunning => _session != null && !_session.IsOver;

        public void Begin(ChapterDefinition chapter, ClueInventory inventory)
        {
            _chapter = chapter;
            _inventory = inventory;
            _session = new DeductionSession(chapter, inventory);
            _objectPick = null;
            _leadPick = null;

            Subscribe();

            if (ink != null)
            {
                ink.SetCapacity(_session.InkCapacity);
                ink.SetRemaining(_session.Ink);
            }

            dialogue?.SetPacing(slowSpeech: false, allowSkip: true);
            StartCoroutine(Run());
        }

        private void Subscribe()
        {
            Unsubscribe();
            if (board != null)
            {
                board.CombineRequested += OnCombineRequested;
                board.EndRequested += OnEndRequested;
                board.SlotCleared += OnSlotCleared;
                board.ReplacePicked += OnReplacePicked;
                board.ReplaceCancelled += OnReplaceCancelled;
            }
            if (clueTray != null) clueTray.ClueSelected += OnClueSelected;
        }

        private void Unsubscribe()
        {
            if (board != null)
            {
                board.CombineRequested -= OnCombineRequested;
                board.EndRequested -= OnEndRequested;
                board.SlotCleared -= OnSlotCleared;
                board.ReplacePicked -= OnReplacePicked;
                board.ReplaceCancelled -= OnReplaceCancelled;
            }
            if (clueTray != null) clueTray.ClueSelected -= OnClueSelected;
        }

        private void OnDisable() => Unsubscribe();

        private IEnumerator Run()
        {
            _busy = true;
            board?.SetInteractable(false);
            board?.SetResult(string.Empty);
            RefreshBoard();

            if (_chapter.DeductionIntro != null && !_chapter.DeductionIntro.IsEmpty)
                yield return dialogue.Play(_chapter.DeductionIntro);

            yield return ShowTutorial(TutorialPopupTrigger.BeforeDeduction);

            clueTray?.SetSelectionMode(true);
            board?.SetInteractable(true);

            while (!_session.IsOver)
            {
                // 입력을 기다린다 — 자리 채우기는 이벤트로, 여기서는 버튼만.
                _busy = false;
                _combineRequested = false;
                _endRequested = false;
                RefreshBoard();
                while (!_combineRequested && !_endRequested) yield return null;
                _busy = true;
                board?.SetInteractable(false);

                if (_endRequested)
                {
                    if (_session.EndManually()) break;
                    board?.SetInteractable(true);
                    continue;
                }

                var result = _session.TryCombine(_objectPick, _leadPick);
                if (ink != null) ink.SetRemaining(_session.Ink);   // 모든 시도가 잉크를 쓴다
                switch (result.Outcome)
                {
                    case HypothesisOutcome.Success:
                        yield return OnSuccess(result.Recipe);
                        break;
                    case HypothesisOutcome.SuccessNeedsReplace:
                        yield return OnNeedsReplace(result.Recipe);
                        break;
                    case HypothesisOutcome.Duplicate:
                        yield return OnDuplicate(result.Recipe);
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

                board?.SetInteractable(true);
            }

            clueTray?.SetSelectionMode(false);
            board?.SetInteractable(false);
            ClearPicks();
            RefreshBoard();

            if (_session.Succeeded && _chapter.DeductionOutro != null && !_chapter.DeductionOutro.IsEmpty)
                yield return dialogue.Play(_chapter.DeductionOutro);

            _busy = false;
            Finished?.Invoke(_session.Succeeded);
        }

        // ------------------------------------------------------------------ 결과

        private IEnumerator OnSuccess(HypothesisRecipe recipe)
        {
            if (board != null) yield return board.PlaySuccess();
            board?.SetResult("〈가설〉 " + recipe.Title);
            Deduced?.Invoke(recipe.Result);
            StartCoroutine(Flash());
            ClearPicks();
            RefreshBoard();
            if (recipe.OnSuccess != null && !recipe.OnSuccess.IsEmpty)
                yield return dialogue.Play(recipe.OnSuccess);
        }

        private IEnumerator OnNeedsReplace(HypothesisRecipe recipe)
        {
            if (board != null) yield return board.PlaySuccess();
            board?.SetResult("〈가설〉 " + recipe.Title + " — 자리가 없다");

            if (_chapter.DeductionReplacePrompt != null && !_chapter.DeductionReplacePrompt.IsEmpty)
                yield return dialogue.Play(_chapter.DeductionReplacePrompt);

            _replacePick = null;
            _replaceCancelled = false;
            board?.ShowReplace($"새 가설 「{recipe.Title}」 을 남기려면 지울 가설을 고른다",
                _session.Hypotheses, id => _inventory.DisplayName(id));
            board?.SetInteractable(true);
            while (_replacePick == null && !_replaceCancelled) yield return null;
            board?.HideReplace();

            if (_replaceCancelled)
            {
                _session.CancelReplace();
                board?.SetResult(string.Empty);
                ClearPicks();
                RefreshBoard();
                yield break;
            }

            var added = _session.ConfirmReplace(_replacePick);
            if (added != null)
            {
                Deduced?.Invoke(added);
                StartCoroutine(Flash());
            }
            ClearPicks();
            RefreshBoard();
            if (recipe.OnSuccess != null && !recipe.OnSuccess.IsEmpty)
                yield return dialogue.Play(recipe.OnSuccess);
        }

        private IEnumerator OnDuplicate(HypothesisRecipe recipe)
        {
            board?.SetResult("이미 세운 가설이다 — " + recipe.Title);
            ClearPicks();
            RefreshBoard();
            if (_chapter.DeductionDuplicate != null && !_chapter.DeductionDuplicate.IsEmpty)
                yield return dialogue.Play(_chapter.DeductionDuplicate);
        }

        private IEnumerator OnFail(HypothesisResult result)
        {
            if (board != null) yield return board.PlayFail();
            board?.SetResult("이어지지 않는다");
            ClearPicks();
            RefreshBoard();

            var reaction = _chapter.GetDeductionFailure(result.FailRound);
            if (reaction != null && !reaction.IsEmpty)
                yield return dialogue.Play(reaction);

            if (result.FailRound == 1)
                yield return ShowTutorial(TutorialPopupTrigger.OnDeductionFirstMiss);
        }

        private IEnumerator OnFatal(HypothesisResult result)
        {
            if (board != null) yield return board.PlayFail();
            board?.SetResult("잉크가 다 말랐다");
            ClearPicks();
            RefreshBoard();

            var reaction = _chapter.GetDeductionFailure(result.FailRound);
            if (reaction != null && !reaction.IsEmpty)
                yield return dialogue.Play(reaction);

            if (_chapter.SelfLoathingEnding != null && !_chapter.SelfLoathingEnding.IsEmpty)
                yield return dialogue.Play(_chapter.SelfLoathingEnding);
        }

        // ------------------------------------------------------------------ 입력

        private void OnClueSelected(ClueDefinition clue)
        {
            if (_busy || !IsRunning || clue == null) return;

            if (DeductionSession.IsObjectSide(clue)) _objectPick = _objectPick == clue ? null : clue;
            else if (DeductionSession.IsLeadSide(clue)) _leadPick = _leadPick == clue ? null : clue;
            else return;

            board?.SetResult(string.Empty);
            RefreshBoard();
        }

        private void OnSlotCleared(bool objectSide)
        {
            if (_busy || !IsRunning) return;
            if (objectSide) _objectPick = null; else _leadPick = null;
            RefreshBoard();
        }

        private void OnCombineRequested()
        {
            if (_busy || !IsRunning || _objectPick == null || _leadPick == null) return;
            _combineRequested = true;
        }

        private void OnEndRequested()
        {
            if (_busy || !IsRunning || !_session.CanEnd) return;
            _endRequested = true;
        }

        private void OnReplacePicked(string id) => _replacePick = id;
        private void OnReplaceCancelled() => _replaceCancelled = true;

        private void ClearPicks()
        {
            _objectPick = null;
            _leadPick = null;
        }

        private void RefreshBoard()
        {
            if (board == null) return;
            board.SetSlot(true, _objectPick, _objectPick != null ? _inventory.DisplayName(_objectPick.Id) : null);
            board.SetSlot(false, _leadPick, _leadPick != null ? _inventory.DisplayName(_leadPick.Id) : null);

            var both = _objectPick != null && _leadPick != null;
            var tried = both && _session != null && _session.TriedAndFailed(_objectPick.Id, _leadPick.Id);
            board.SetCombineEnabled(both && !tried && IsRunning);
            board.SetEndEnabled(_session != null && _session.CanEnd && IsRunning);

            if (_session == null || _session.IsOver) board.SetHint(string.Empty);
            else if (tried) board.SetHint("이 둘은 이미 이어 보았다.");
            else if (!both) board.SetHint("우측 목록에서 물품 하나, 정황 하나를 고른다.");
            else board.SetHint($"가설 {_session.HypothesisCount}/{_session.MaxHypotheses} · 잉크 {_session.Ink}");

            clueTray?.SetSelection(_objectPick?.Id, _leadPick?.Id);
        }

        private IEnumerator ShowTutorial(TutorialPopupTrigger trigger)
        {
            if (tutorialPopup == null || _chapter == null) yield break;
            var popup = _chapter.FindTutorialPopup(trigger);
            if (popup == null || tutorialPopup.HasShown(popup)) yield break;
            yield return tutorialPopup.ShowOnce(popup);
        }

        /// <summary>가설이 생긴 순간 단서 패널이 잠깐 밝아진다.</summary>
        private IEnumerator Flash()
        {
            if (confirmFlash == null) yield break;
            var t = 0f;
            const float up = 0.12f, down = 0.5f;
            while (t < up) { t += Time.unscaledDeltaTime; confirmFlash.alpha = Mathf.Lerp(0f, 1f, t / up); yield return null; }
            t = 0f;
            while (t < down) { t += Time.unscaledDeltaTime; confirmFlash.alpha = Mathf.Lerp(1f, 0f, t / down); yield return null; }
            confirmFlash.alpha = 0f;
        }
    }
}
