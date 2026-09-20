using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// id 로 데이터를 찾기 위한 색인. 대사 태그나 CSV 가 문자열 id 로 서로를 참조하기 때문에
    /// 런타임에서 한 번은 id → 에셋 변환이 필요하다.
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        [SerializeField] private List<ChapterDefinition> chapters = new List<ChapterDefinition>();
        [SerializeField] private List<ClueDefinition> clues = new List<ClueDefinition>();
        [SerializeField] private List<ChoiceDefinition> choices = new List<ChoiceDefinition>();
        [SerializeField] private List<InteractableDefinition> interactables = new List<InteractableDefinition>();
        [SerializeField] private List<SuspectDefinition> suspects = new List<SuspectDefinition>();

        private Dictionary<string, ClueDefinition> _clueById;
        private Dictionary<string, ChoiceDefinition> _choiceById;
        private Dictionary<string, InteractableDefinition> _interactableById;
        private Dictionary<string, SuspectDefinition> _suspectById;
        private Dictionary<string, ChapterDefinition> _chapterById;

        public IReadOnlyList<ChapterDefinition> Chapters => chapters;
        public IReadOnlyList<ClueDefinition> Clues => clues;

        public ClueDefinition GetClue(string id) => Lookup(ref _clueById, clues, c => c.Id, id);
        public ChoiceDefinition GetChoice(string id) => Lookup(ref _choiceById, choices, c => c.Id, id);
        public InteractableDefinition GetInteractable(string id) => Lookup(ref _interactableById, interactables, c => c.Id, id);
        public SuspectDefinition GetSuspect(string id) => Lookup(ref _suspectById, suspects, c => c.Id, id);
        public ChapterDefinition GetChapter(string id) => Lookup(ref _chapterById, chapters, c => c.Id, id);

        /// <summary>임포터가 다시 채운 뒤 호출해 색인을 버린다.</summary>
        public void InvalidateIndex()
        {
            _clueById = null;
            _choiceById = null;
            _interactableById = null;
            _suspectById = null;
            _chapterById = null;
        }

        private static T Lookup<T>(ref Dictionary<string, T> cache, List<T> source,
            System.Func<T, string> idOf, string id) where T : Object
        {
            if (string.IsNullOrEmpty(id) || source == null) return null;

            if (cache == null)
            {
                cache = new Dictionary<string, T>(source.Count);
                for (int i = 0; i < source.Count; i++)
                {
                    var item = source[i];
                    if (item == null) continue;
                    var key = idOf(item);
                    if (string.IsNullOrEmpty(key)) continue;
                    cache[key] = item;
                }
            }

            return cache.TryGetValue(id, out var found) ? found : null;
        }

#if UNITY_EDITOR
        /// <summary>임포터에서 목록을 통째로 교체할 때 쓴다.</summary>
        public void EditorSetAll(
            List<ChapterDefinition> newChapters,
            List<ClueDefinition> newClues,
            List<ChoiceDefinition> newChoices,
            List<InteractableDefinition> newInteractables,
            List<SuspectDefinition> newSuspects)
        {
            if (newChapters != null) chapters = newChapters;
            if (newClues != null) clues = newClues;
            if (newChoices != null) choices = newChoices;
            if (newInteractables != null) interactables = newInteractables;
            if (newSuspects != null) suspects = newSuspects;
            InvalidateIndex();
        }
#endif
    }
}
