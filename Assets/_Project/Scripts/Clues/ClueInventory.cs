using System;
using System.Collections.Generic;
using ItIsYou.Data;

namespace ItIsYou.Clues
{
    /// <summary>
    /// 수집한 단서 보관함. MonoBehaviour 가 아니라 순수 C# 이라
    /// 플레이 모드 없이도 규칙을 테스트할 수 있다.
    ///
    /// 읽음 여부를 따로 들고 있는 이유는, 기획서의 2단계 명칭 규칙 때문이다.
    /// 수집만 한 단서는 ??? 로, 목록에서 한 번 읽은 단서는 세부 명칭으로 표시된다.
    /// </summary>
    public class ClueInventory
    {
        private readonly Dictionary<string, ClueDefinition> _owned = new Dictionary<string, ClueDefinition>();
        private readonly HashSet<string> _read = new HashSet<string>();
        private readonly List<string> _order = new List<string>();

        /// <summary>새 단서를 얻었을 때. 이미 가진 단서면 발생하지 않는다.</summary>
        public event Action<ClueDefinition> ClueAdded;

        /// <summary>단서를 처음 읽었을 때. 표시 명칭이 바뀌는 시점이다.</summary>
        public event Action<ClueDefinition> ClueRead;

        public int Count => _owned.Count;

        /// <summary>수집 순서대로의 단서 id.</summary>
        public IReadOnlyList<string> OrderedIds => _order;

        /// <summary>조건 검사에 넘기기 위한 id 집합.</summary>
        public ICollection<string> OwnedIds => _owned.Keys;

        public bool Has(string clueId) => !string.IsNullOrEmpty(clueId) && _owned.ContainsKey(clueId);

        public bool HasAll(IReadOnlyList<string> clueIds)
        {
            if (clueIds == null || clueIds.Count == 0) return true;
            for (int i = 0; i < clueIds.Count; i++)
            {
                if (!Has(clueIds[i])) return false;
            }
            return true;
        }

        public bool HasBeenRead(string clueId) => !string.IsNullOrEmpty(clueId) && _read.Contains(clueId);

        public ClueDefinition Get(string clueId)
            => (!string.IsNullOrEmpty(clueId) && _owned.TryGetValue(clueId, out var c)) ? c : null;

        /// <summary>단서를 추가한다. 새로 추가됐으면 true.</summary>
        public bool Add(ClueDefinition clue)
        {
            if (clue == null || string.IsNullOrEmpty(clue.Id)) return false;
            if (_owned.ContainsKey(clue.Id)) return false;

            _owned.Add(clue.Id, clue);
            _order.Add(clue.Id);
            ClueAdded?.Invoke(clue);
            return true;
        }

        /// <summary>목록에서 단서를 읽었다고 표시한다. 처음 읽은 것이면 true.</summary>
        public bool MarkRead(string clueId)
        {
            if (!Has(clueId)) return false;
            if (!_read.Add(clueId)) return false;

            ClueRead?.Invoke(_owned[clueId]);
            return true;
        }

        /// <summary>UI 에 뿌릴 표시 이름. 읽기 전에는 ??? 또는 단순 명칭.</summary>
        public string DisplayName(string clueId)
        {
            var clue = Get(clueId);
            if (clue == null) return string.Empty;
            return clue.DisplayName(HasBeenRead(clueId));
        }

        /// <summary>종류별로 걸러 본다. 가방 화면의 탭 구분에 쓴다.</summary>
        public IEnumerable<ClueDefinition> OfKind(ClueKind kind)
        {
            for (int i = 0; i < _order.Count; i++)
            {
                var clue = _owned[_order[i]];
                if (clue.Kind == kind) yield return clue;
            }
        }

        public void Clear()
        {
            _owned.Clear();
            _read.Clear();
            _order.Clear();
        }

        /// <summary>세이브 복원용.</summary>
        public void Restore(IEnumerable<ClueDefinition> owned, IEnumerable<string> readIds)
        {
            Clear();
            if (owned != null)
            {
                foreach (var clue in owned)
                {
                    if (clue == null || string.IsNullOrEmpty(clue.Id)) continue;
                    if (_owned.ContainsKey(clue.Id)) continue;
                    _owned.Add(clue.Id, clue);
                    _order.Add(clue.Id);
                }
            }
            if (readIds != null)
            {
                foreach (var id in readIds)
                {
                    if (_owned.ContainsKey(id)) _read.Add(id);
                }
            }
        }
    }
}
