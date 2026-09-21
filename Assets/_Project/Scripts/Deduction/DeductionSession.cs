using System;
using System.Collections.Generic;
using ItIsYou.Clues;
using ItIsYou.Data;

namespace ItIsYou.Deduction
{
    /// <summary>한 번의 선택이 낳은 결과.</summary>
    public enum DeductionOutcome
    {
        /// <summary>정답 순서를 아직 다 고르지 않았다 (두 개 이상 고르는 문항).</summary>
        Pending = 0,
        Correct = 1,
        /// <summary>틀렸다. 잉크가 남아 있어 같은 문항을 다시 푼다.</summary>
        Wrong = 2,
        /// <summary>잉크가 없는 상태에서 틀렸다 — 자기 비하 게임오버.</summary>
        Fatal = 3
    }

    /// <summary>선택 한 번의 판정과, 화면이 알아야 할 부수 정보.</summary>
    public readonly struct DeductionResult
    {
        public readonly DeductionOutcome Outcome;
        /// <summary>이 오답이 잉크를 쓰지 않았다 (튜토리얼 첫 오답).</summary>
        public readonly bool FreeMiss;
        /// <summary>이 문항에서 몇 번째 오답인가 (1부터).</summary>
        public readonly int WrongRound;
        /// <summary>정답이면 새로 얻은 추론 단서.</summary>
        public readonly ClueDefinition Deduced;

        public DeductionResult(DeductionOutcome outcome, bool freeMiss, int wrongRound, ClueDefinition deduced)
        {
            Outcome = outcome;
            FreeMiss = freeMiss;
            WrongRound = wrongRound;
            Deduced = deduced;
        }
    }

    /// <summary>
    /// 단서 조합 페이즈의 규칙. MonoBehaviour 가 아니라 순수 C# — 화면 없이 검증할 수 있다.
    ///
    /// 개선안의 공통 뼈대: 잉크 방울(가시적 시도 자원) · 오답마다 회차 반응 · 잉크 0 + 오답 = 자기 비하 게임오버 ·
    /// 결과는 목록의 '추론' 카드. 문항 방식은 A 조수 문답.
    /// </summary>
    public class DeductionSession
    {
        private readonly ChapterDefinition _chapter;
        private readonly ClueInventory _inventory;
        private readonly List<DeductionQuestion> _questions = new List<DeductionQuestion>();
        private readonly List<string> _picked = new List<string>();
        private readonly HashSet<string> _triedWrong = new HashSet<string>();
        private readonly HashSet<string> _eliminated = new HashSet<string>();

        private int _index;
        private bool _freeMissUsed;

        public int Ink { get; private set; }
        public int InkCapacity { get; }
        public bool IsOver { get; private set; }
        public bool Succeeded { get; private set; }

        /// <summary>현재 문항에서 틀린 횟수.</summary>
        public int WrongOnCurrent { get; private set; }

        /// <summary>전체 오답 수. 사건 보고서 자리에 쓸 수 있다.</summary>
        public int TotalWrong { get; private set; }

        public DeductionQuestion Current => _index < _questions.Count ? _questions[_index] : null;
        public int QuestionIndex => _index;
        public int QuestionCount => _questions.Count;

        /// <summary>이미 틀린 것으로 골라 본 후보 — 흐리게 표시한다. 다시 누를 수는 있다.</summary>
        public IReadOnlyCollection<string> TriedWrong => _triedWrong;

        /// <summary>가이드 모드에서 조수가 지워 준 후보 — 목록에서 사라진다.</summary>
        public IReadOnlyCollection<string> Eliminated => _eliminated;

        /// <summary>잉크가 바뀌었다. (남은 수)</summary>
        public event Action<int> InkChanged;

        public DeductionSession(ChapterDefinition chapter, ClueInventory inventory)
        {
            _chapter = chapter ?? throw new ArgumentNullException(nameof(chapter));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));

            var source = chapter.DeductionQuestions;
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    if (source[i] != null) _questions.Add(source[i]);
                }
            }

            InkCapacity = chapter.DeductionInk;
            Ink = InkCapacity;

            // 이미 만든 추론(재도전)은 건너뛴다.
            SkipDone();
            if (Current == null) Finish(true);
        }

        /// <summary>현재 문항의 후보 — 실제로 가진 단서만. 놓친 단서는 후보에도 없다: 앞 페이즈의 대가.</summary>
        public List<ClueDefinition> BuildCandidates()
        {
            var result = new List<ClueDefinition>();
            var q = Current;
            if (q == null) return result;

            var ids = q.CandidateClueIds;
            for (int i = 0; i < ids.Count; i++)
            {
                if (_eliminated.Contains(ids[i])) continue;
                var clue = _inventory.Get(ids[i]);
                if (clue != null) result.Add(clue);
            }
            return result;
        }

        /// <summary>현재 문항의 정답을 하나라도 갖고 있지 않으면 — 풀 수 없는 문항이다.</summary>
        public bool CurrentIsUnanswerable
        {
            get
            {
                var q = Current;
                if (q == null) return false;
                for (int i = 0; i < q.AnswerClueIds.Count; i++)
                {
                    if (!_inventory.Has(q.AnswerClueIds[i])) return true;
                }
                return false;
            }
        }

        /// <summary>후보 하나를 고른다.</summary>
        public DeductionResult Select(string clueId)
        {
            var q = Current;
            if (IsOver || q == null || string.IsNullOrEmpty(clueId))
                return new DeductionResult(DeductionOutcome.Pending, false, WrongOnCurrent, null);

            _picked.Add(clueId);
            if (_picked.Count < q.AnswerCount)
            {
                // 아직 순서를 다 고르지 않았다. 다만 이미 어긋났으면 바로 판정한다.
                if (q.AnswerClueIds[_picked.Count - 1] == clueId)
                    return new DeductionResult(DeductionOutcome.Pending, false, WrongOnCurrent, null);
            }

            var correct = q.IsCorrect(_picked);
            _picked.Clear();

            if (correct)
            {
                var deduced = q.ResultClue;
                if (deduced != null) _inventory.Add(deduced);
                WrongOnCurrent = 0;
                _triedWrong.Clear();
                _eliminated.Clear();
                _index++;
                SkipDone();
                if (Current == null) Finish(true);
                return new DeductionResult(DeductionOutcome.Correct, false, 0, deduced);
            }

            _triedWrong.Add(clueId);
            WrongOnCurrent++;
            TotalWrong++;

            // 튜토리얼의 첫 오답은 무료 — 규칙을 배우는 값이다.
            var free = _chapter.FirstDeductionMissIsFree && !_freeMissUsed;
            if (free)
            {
                _freeMissUsed = true;
                return new DeductionResult(DeductionOutcome.Wrong, true, WrongOnCurrent, null);
            }

            if (Ink <= 0)
            {
                Finish(false);
                return new DeductionResult(DeductionOutcome.Fatal, false, WrongOnCurrent, null);
            }

            Ink--;
            InkChanged?.Invoke(Ink);
            return new DeductionResult(DeductionOutcome.Wrong, false, WrongOnCurrent, null);
        }

        /// <summary>
        /// 가이드 모드 — 조수가 오답 후보 하나를 지운다. 지운 단서 id 를 돌려주고, 지울 것이 없으면 null.
        /// 정답과 이미 틀려 본 것은 남긴다(틀려 본 것은 이미 플레이어가 안다).
        /// </summary>
        public string EliminateOneWrongCandidate()
        {
            var q = Current;
            if (q == null) return null;
            var ids = q.CandidateClueIds;
            for (int i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (q.IsAnswer(id) || _eliminated.Contains(id) || _triedWrong.Contains(id)) continue;
                if (!_inventory.Has(id)) continue;
                _eliminated.Add(id);
                return id;
            }
            return null;
        }

        private void SkipDone()
        {
            while (Current != null && Current.ResultClue != null && _inventory.Has(Current.ResultClue.Id))
                _index++;
        }

        private void Finish(bool success)
        {
            IsOver = true;
            Succeeded = success;
        }
    }
}
