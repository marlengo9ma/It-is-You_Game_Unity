using System.Collections.Generic;
using ItIsYou.Data;

namespace ItIsYou.Interrogation
{
    /// <summary>심문 중 실제로 출력된 대사 한 줄의 기록.</summary>
    public class InterrogationLogEntry
    {
        public DialogueLine Line { get; }
        public string SuspectId { get; }

        /// <summary>이 줄을 이끌어낸 선택지 id. 오브젝트나 자동 진행이면 null.</summary>
        public string ChoiceId { get; }

        /// <summary>이 줄에 리액션이 있었는데 플레이어가 놓쳤는가.</summary>
        public bool ReactionMissed { get; internal set; }

        /// <summary>이 줄의 리액션을 이미 수집했는가.</summary>
        public bool ReactionCollected { get; internal set; }

        public InterrogationLogEntry(DialogueLine line, string suspectId, string choiceId)
        {
            Line = line;
            SuspectId = suspectId;
            ChoiceId = choiceId;
        }

        /// <summary>아직 회수할 여지가 남은 줄인가 — 기억 짐작에서 보여줄 가치가 있는지.</summary>
        public bool HasUnclaimedReaction => Line != null && Line.HasReaction && !ReactionCollected;
    }

    /// <summary>
    /// 이번 심문에서 오간 대사 전체. 기억 짐작이 여기서 과거를 되짚는다.
    /// </summary>
    public class InterrogationLog
    {
        private readonly List<InterrogationLogEntry> _entries = new List<InterrogationLogEntry>();

        public IReadOnlyList<InterrogationLogEntry> Entries => _entries;
        public int Count => _entries.Count;

        public InterrogationLogEntry Append(DialogueLine line, string suspectId, string choiceId)
        {
            var entry = new InterrogationLogEntry(line, suspectId, choiceId);
            _entries.Add(entry);
            return entry;
        }

        /// <summary>특정 선택지로 이어진 대사만 추린다. 되돌아보기 목록에 쓴다.</summary>
        public IEnumerable<InterrogationLogEntry> ByChoice(string choiceId)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].ChoiceId == choiceId) yield return _entries[i];
            }
        }

        /// <summary>이 심문에서 나왔던 선택지 id 들을 등장 순서대로.</summary>
        public IEnumerable<string> DistinctChoiceIds()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < _entries.Count; i++)
            {
                var id = _entries[i].ChoiceId;
                if (string.IsNullOrEmpty(id)) continue;
                if (seen.Add(id)) yield return id;
            }
        }

        public void Clear() => _entries.Clear();
    }
}
