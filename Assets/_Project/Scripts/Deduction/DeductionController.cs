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
    /// 단서 조합 페이즈의 진행자. 규칙은 <see cref="DeductionSession"/> 이, 이 클래스는 화면과 입력만.
    ///
    /// 흐름: 진입 대사 → (튜토리얼 팝업 4) → 문항마다 [조수의 질문 → 후보 → 선택 →
    /// 정답: 추론 카드 + 확정 대사 / 오답: 팝업 #2 + 회차 반응 + 잉크 −1] → 전부 맞히면 마무리 대사.
    /// 잉크 0 에서 또 틀리면 자기 비하 독백 뒤 실패로 끝난다.
    /// </summary>
    [AddComponentMenu("It is You/Deduction Controller")]
    public class DeductionController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private DialoguePresenter dialogue;
        [SerializeField] private DeductionCandidateView candidates;
        [SerializeField] private InkDropsView ink;
        [SerializeField] private GlarePopupView glarePopup;
        [SerializeField] private TutorialPopupView tutorialPopup;
        [SerializeField] private CursorController cursor;

        [Tooltip("무대에 남아 있는 조수의 질문. 대사창이 닫혀도 무엇에 답하는지 보이게.")]
        [SerializeField] private TMPro.TMP_Text questionLabel;

        [Tooltip("추론 카드가 생겼을 때 잠깐 놋쇠 빛으로 물드는 판. 보통 단서 패널.")]
        [SerializeField] private CanvasGroup confirmFlash;

        private DeductionSession _session;
        private ChapterDefinition _chapter;
        private ClueInventory _inventory;
        private ClueDefinition _selected;
        private bool _busy;

        /// <summary>페이즈가 끝났다. (필요한 추론을 다 만들었는가)</summary>
        public event Action<bool> Finished;

        /// <summary>새 추론이 생겼다.</summary>
        public event Action<ClueDefinition> Deduced;

        public DeductionSession Session => _session;
        public bool IsRunning => _session != null && !_session.IsOver;

        public void Begin(ChapterDefinition chapter, ClueInventory inventory)
        {
            _chapter = chapter;
            _inventory = inventory;
            _session = new DeductionSession(chapter, inventory);

            if (candidates != null)
            {
                candidates.CandidateSelected -= OnCandidateSelected;
                candidates.CandidateSelected += OnCandidateSelected;
                candidates.Clear();
            }
            if (questionLabel != null) questionLabel.text = string.Empty;
            if (ink != null)
            {
                ink.SetCapacity(_session.InkCapacity);
                ink.SetRemaining(_session.Ink);
            }

            dialogue?.SetPacing(slowSpeech: false, allowSkip: true);
            StartCoroutine(Run());
        }

        private void OnDisable()
        {
            if (candidates != null) candidates.CandidateSelected -= OnCandidateSelected;
        }

        private IEnumerator Run()
        {
            _busy = true;

            // 문항이 없거나 이미 다 풀었으면 조용히 지나간다.
            if (_session.IsOver)
            {
                _busy = false;
                Finished?.Invoke(_session.Succeeded);
                yield break;
            }

            if (_chapter.DeductionIntro != null && !_chapter.DeductionIntro.IsEmpty)
                yield return dialogue.Play(_chapter.DeductionIntro);

            yield return ShowTutorial(TutorialPopupTrigger.BeforeDeduction);

            while (!_session.IsOver)
            {
                var q = _session.Current;

                if (q.Prompt != null && !q.Prompt.IsEmpty)
                {
                    yield return dialogue.Play(q.Prompt);
                    if (questionLabel != null) questionLabel.text = "“" + q.Prompt.Lines[q.Prompt.Count - 1].Text + "”";
                }

                // 후보를 열고 선택을 기다린다.
                RebuildCandidates();
                candidates?.SetInteractable(true);
                _busy = false;
                _selected = null;
                while (_selected == null) yield return null;

                _busy = true;
                candidates?.SetInteractable(false);
                var picked = _selected;
                _selected = null;

                var result = _session.Select(picked.Id);
                switch (result.Outcome)
                {
                    case DeductionOutcome.Pending:
                        RebuildCandidates();
                        continue;

                    case DeductionOutcome.Correct:
                        yield return OnCorrect(q, result);
                        break;

                    case DeductionOutcome.Wrong:
                        yield return OnWrong(q, result);
                        break;

                    case DeductionOutcome.Fatal:
                        yield return OnFatal(q, result);
                        break;
                }
            }

            candidates?.Clear();
            glarePopup?.Hide();
            if (questionLabel != null) questionLabel.text = string.Empty;

            if (_session.Succeeded && _chapter.DeductionOutro != null && !_chapter.DeductionOutro.IsEmpty)
                yield return dialogue.Play(_chapter.DeductionOutro);

            _busy = false;
            Finished?.Invoke(_session.Succeeded);
        }

        private IEnumerator OnCorrect(DeductionQuestion q, DeductionResult result)
        {
            candidates?.Clear();
            if (result.Deduced != null)
            {
                Deduced?.Invoke(result.Deduced);
                StartCoroutine(Flash());
            }
            if (q.OnCorrect != null && !q.OnCorrect.IsEmpty)
                yield return dialogue.Play(q.OnCorrect);
        }

        private IEnumerator OnWrong(DeductionQuestion q, DeductionResult result)
        {
            // 팝업 #2 는 반응 대사와 함께 뜬다 — "틀렸다"를 그림이 먼저 말한다.
            glarePopup?.Show(_chapter.DeductionGlareImage, "누르면 닫힘");

            var reaction = q.GetWrongReaction(result.WrongRound);
            if (reaction != null && !reaction.IsEmpty)
                yield return dialogue.Play(reaction);

            if (!result.FreeMiss && ink != null) ink.SetRemaining(_session.Ink);

            if (glarePopup != null) yield return glarePopup.WaitForDismiss();

            if (result.FreeMiss)
                yield return ShowTutorial(TutorialPopupTrigger.OnDeductionFirstMiss);

            // 가이드 모드 — 조수가 오답 후보 하나를 지워 준다.
            if (GameSettings.GuideMode)
            {
                var removed = _session.EliminateOneWrongCandidate();
                if (removed != null && q.GuideHint != null && !q.GuideHint.IsEmpty)
                    yield return dialogue.Play(q.GuideHint);
            }
        }

        private IEnumerator OnFatal(DeductionQuestion q, DeductionResult result)
        {
            glarePopup?.Show(_chapter.DeductionGlareImage, "누르면 닫힘");

            var reaction = q.GetWrongReaction(result.WrongRound);
            if (reaction != null && !reaction.IsEmpty)
                yield return dialogue.Play(reaction);

            if (glarePopup != null) yield return glarePopup.WaitForDismiss();

            if (_chapter.SelfLoathingEnding != null && !_chapter.SelfLoathingEnding.IsEmpty)
                yield return dialogue.Play(_chapter.SelfLoathingEnding);
        }

        private void RebuildCandidates()
        {
            if (candidates == null || _session == null) return;
            candidates.Rebuild(_session.BuildCandidates(), id => _inventory.DisplayName(id), _session.TriedWrong);
        }

        private void OnCandidateSelected(ClueDefinition clue)
        {
            if (_busy || !IsRunning) return;
            _selected = clue;
        }

        private IEnumerator ShowTutorial(TutorialPopupTrigger trigger)
        {
            if (tutorialPopup == null || _chapter == null) yield break;
            var popup = _chapter.FindTutorialPopup(trigger);
            if (popup == null || tutorialPopup.HasShown(popup)) yield break;
            yield return tutorialPopup.ShowOnce(popup);
        }

        /// <summary>추론 카드가 생긴 순간 단서 패널이 잠깐 밝아진다.</summary>
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
