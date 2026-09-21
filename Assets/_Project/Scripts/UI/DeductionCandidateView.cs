using System;
using System.Collections.Generic;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 좌측 패널에 후보 단서를 나열한다. 선택지 목록과 같은 자리, 같은 리듬 — 다만 글 대신 그림이다.
    /// </summary>
    [AddComponentMenu("It is You/Deduction Candidate View")]
    public class DeductionCandidateView : MonoBehaviour
    {
        [SerializeField] private RectTransform container;
        [SerializeField] private DeductionCandidateButton buttonPrefab;
        [SerializeField] private CanvasGroup root;
        [SerializeField] private TMP_Text hint;

        private readonly List<DeductionCandidateButton> _pool = new List<DeductionCandidateButton>();

        /// <summary>후보를 골랐다.</summary>
        public event Action<ClueDefinition> CandidateSelected;

        public void Rebuild(IReadOnlyList<ClueDefinition> candidates, Func<string, string> displayName,
            IReadOnlyCollection<string> triedWrong)
        {
            EnsurePool(candidates?.Count ?? 0);
            for (int i = 0; i < _pool.Count; i++)
            {
                var active = candidates != null && i < candidates.Count;
                _pool[i].gameObject.SetActive(active);
                if (!active) continue;

                var clue = candidates[i];
                var name = displayName != null ? displayName(clue.Id) : clue.ReadName;
                var tried = triedWrong != null && System.Linq.Enumerable.Contains(triedWrong, clue.Id);
                _pool[i].Bind(clue, name, tried, OnClicked);
            }

            if (hint != null)
                hint.text = candidates == null ? string.Empty
                    : candidates.Count == 0 ? "답할 수 있는 단서가 없다."
                    : "하나를 골라 답한다";
        }

        public void Clear() => Rebuild(null, null, null);

        public void SetInteractable(bool value)
        {
            if (root != null)
            {
                root.interactable = value;
                root.blocksRaycasts = value;
                root.alpha = value ? 1f : 0.6f;
            }
            for (int i = 0; i < _pool.Count; i++) _pool[i].SetInteractable(value);
        }

        private void OnClicked(ClueDefinition clue) => CandidateSelected?.Invoke(clue);

        private void EnsurePool(int needed)
        {
            if (buttonPrefab == null || container == null) return;
            while (_pool.Count < needed)
                _pool.Add(Instantiate(buttonPrefab, container));
        }
    }
}
