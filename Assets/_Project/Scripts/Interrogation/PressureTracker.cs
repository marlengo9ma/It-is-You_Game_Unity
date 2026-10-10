using ItIsYou.Data;

namespace ItIsYou.Interrogation
{
    /// <summary>플레이어의 심문 방식에 대한 판정 결과. 위쪽일수록 심각하다.</summary>
    public enum PressureVerdict
    {
        None = 0,

        /// <summary>심문 오브젝트만 반복해서 용의자가 의문/불쾌를 보인다.</summary>
        ObjectSpamWarning = 1,

        /// <summary>강압이 누적돼 경고가 필요하다.</summary>
        AggressionWarning = 2,

        /// <summary>친근/강압을 한 번 교차했다. 용의자가 의문·당황을 보인다.</summary>
        ToneAlternationWarning = 3,

        /// <summary>심문 오브젝트 반복으로 용의자가 분노해 심문을 거부한다.</summary>
        RefusedByAnger = 4,

        /// <summary>강압 누적으로 용의자가 공포·실신해 심문을 거부한다.</summary>
        RefusedByFear = 5,

        /// <summary>두 번째 교차 선택. 게임오버.</summary>
        GameOverByToneAlternation = 6
    }

    /// <summary>
    /// 기획서의 세 가지 페널티 규칙을 한곳에서 판정한다.
    ///
    /// <list type="number">
    /// <item><b>강압 누적</b> — 3번 이상이면 경고, 4번 이상이면 공포·실신으로 심문 거부.
    /// 연속이 아니라 이 심문 안에서의 누적 횟수다.</item>
    /// <item><b>심문 오브젝트 반복</b> — 메인 선택지 없이 오브젝트만 3번 연속이면 경고,
    /// 4번 연속이면 분노로 심문 거부. 메인 선택지를 고르면 연속이 끊긴다.</item>
    /// <item><b>성향 교차</b> — 친근과 강압을 번갈아 고르면 1번째는 의문·당황,
    /// 2번째는 게임오버. 단서 제시나 특수 선택지는 교차 판정에 끼지 않는다.</item>
    /// </list>
    ///
    /// 판정이 여러 개 겹치면 가장 심각한 것 하나만 돌려준다.
    /// </summary>
    public class PressureTracker
    {
        private readonly ChapterDefinition _chapter;

        private int _aggressiveCount;
        private int _consecutiveObjectCount;
        private int _alternationCount;
        private ChoiceTone? _lastToneForAlternation;

        private bool _aggressionWarned;
        private bool _objectSpamWarned;
        private int _alternationWarnedAt = -1;

        public int AggressiveCount => _aggressiveCount;
        public int ConsecutiveObjectCount => _consecutiveObjectCount;
        public int AlternationCount => _alternationCount;

        public PressureTracker(ChapterDefinition chapter)
        {
            _chapter = chapter;
        }

        /// <summary>메인 선택지를 골랐을 때 호출한다.</summary>
        public PressureVerdict RegisterChoice(ChoiceTone tone)
        {
            // 메인 선택지를 골랐으므로 오브젝트 연속은 끊긴다.
            _consecutiveObjectCount = 0;
            _objectSpamWarned = false;

            if (tone == ChoiceTone.Aggressive)
                _aggressiveCount++;

            var alternated = false;
            if (tone == ChoiceTone.Friendly || tone == ChoiceTone.Aggressive)
            {
                if (_lastToneForAlternation.HasValue && _lastToneForAlternation.Value != tone)
                {
                    _alternationCount++;
                    alternated = true;
                }
                _lastToneForAlternation = tone;
            }

            // 심각한 순서대로 판정한다.
            if (alternated && _alternationCount >= _chapter.ToneAlternationGameOverAt)
                return PressureVerdict.GameOverByToneAlternation;

            if (_aggressiveCount >= _chapter.AggressionRefuseAt)
                return PressureVerdict.RefusedByFear;

            if (alternated && _alternationCount >= _chapter.ToneAlternationWarnAt
                && _alternationWarnedAt != _alternationCount)
            {
                _alternationWarnedAt = _alternationCount;
                return PressureVerdict.ToneAlternationWarning;
            }

            if (_aggressiveCount >= _chapter.AggressionWarnAt && !_aggressionWarned)
            {
                _aggressionWarned = true;
                return PressureVerdict.AggressionWarning;
            }

            return PressureVerdict.None;
        }

        /// <summary>
        /// 이 성향을 지금 고르면 몇 번째 교차가 되는가. 교차가 아니면 0. 상태는 바꾸지 않는다.
        /// 선택지의 문구와 회차를 미리 정할 때 쓴다 — 목록에 뜨는 문장과 실제로 나오는 대사가 같아야 한다.
        /// </summary>
        public int PeekAlternation(ChoiceTone tone)
        {
            if (tone != ChoiceTone.Friendly && tone != ChoiceTone.Aggressive) return 0;
            if (!_lastToneForAlternation.HasValue || _lastToneForAlternation.Value == tone) return 0;
            return _alternationCount + 1;
        }

        /// <summary>심문 오브젝트를 골랐을 때 호출한다.</summary>
        public PressureVerdict RegisterInterrogationObject()
        {
            _consecutiveObjectCount++;

            if (_consecutiveObjectCount >= _chapter.ObjectSpamRefuseAt)
                return PressureVerdict.RefusedByAnger;

            if (_consecutiveObjectCount >= _chapter.ObjectSpamWarnAt && !_objectSpamWarned)
            {
                _objectSpamWarned = true;
                return PressureVerdict.ObjectSpamWarning;
            }

            return PressureVerdict.None;
        }

        /// <summary>이 판정이 심문을 끝내는 종류인가.</summary>
        public static bool IsTerminal(PressureVerdict verdict)
            => verdict == PressureVerdict.RefusedByAnger
            || verdict == PressureVerdict.RefusedByFear
            || verdict == PressureVerdict.GameOverByToneAlternation;

        /// <summary>이 판정이 곧 게임오버인가.</summary>
        public static bool IsGameOver(PressureVerdict verdict)
            => verdict == PressureVerdict.GameOverByToneAlternation;

        /// <summary>용의자가 바뀔 때 초기화한다. 페널티는 용의자마다 따로 쌓인다.</summary>
        public void Reset()
        {
            _aggressiveCount = 0;
            _consecutiveObjectCount = 0;
            _alternationCount = 0;
            _lastToneForAlternation = null;
            _aggressionWarned = false;
            _objectSpamWarned = false;
            _alternationWarnedAt = -1;
        }
    }
}
