using System;
using System.Collections.Generic;
using ItIsYou.Data;
using ItIsYou.Interrogation;
using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>
    /// 화면 좌측의 메인 선택지 목록.
    ///
    /// 기획서에서 선택지는 "용의자마다 최소 2가지에서 최대 5가지가 생성/변경될 수 있다"고
    /// 되어 있어, 목록 자체가 심문 중에 계속 다시 그려진다.
    /// 그래서 버튼을 풀로 돌려쓰고 매번 <see cref="Rebuild"/> 한다.
    /// </summary>
    [AddComponentMenu("It is You/Choice List View")]
    public class ChoiceListView : MonoBehaviour
    {
        [SerializeField] private RectTransform container;
        [SerializeField] private ChoiceButton buttonPrefab;
        [SerializeField] private CanvasGroup root;

        [Header("성향 색")]
        [SerializeField] private Color friendlyColor = new Color(0.80f, 0.84f, 0.78f);
        [SerializeField] private Color aggressiveColor = new Color(0.86f, 0.74f, 0.70f);
        [SerializeField] private Color evidenceColor = new Color(0.79f, 0.76f, 0.62f);
        [SerializeField] private Color specialColor = new Color(0.76f, 0.78f, 0.86f);

        private readonly List<ChoiceButton> _pool = new List<ChoiceButton>();

        /// <summary>선택지를 골랐다.</summary>
        public event Action<ChoiceDefinition> ChoiceSelected;

        public void SetInteractable(bool value)
        {
            if (root == null) return;
            root.interactable = value;
            root.blocksRaycasts = value;
            root.alpha = value ? 1f : 0.45f;
        }

        public void Rebuild(IReadOnlyList<ChoiceView> choices)
        {
            EnsurePool(choices?.Count ?? 0);

            for (int i = 0; i < _pool.Count; i++)
            {
                var active = choices != null && i < choices.Count;
                _pool[i].gameObject.SetActive(active);
                if (!active) continue;

                var view = choices[i];
                _pool[i].Bind(view, ToneColor(view.Definition.Tone), OnClicked);
            }
        }

        public void Clear() => Rebuild(null);

        private void OnClicked(ChoiceDefinition choice) => ChoiceSelected?.Invoke(choice);

        private Color ToneColor(ChoiceTone tone) => tone switch
        {
            ChoiceTone.Friendly => friendlyColor,
            ChoiceTone.Aggressive => aggressiveColor,
            ChoiceTone.Evidence => evidenceColor,
            _ => specialColor
        };

        private void EnsurePool(int needed)
        {
            if (buttonPrefab == null || container == null) return;
            while (_pool.Count < needed)
            {
                var button = Instantiate(buttonPrefab, container);
                _pool.Add(button);
            }
        }
    }
}
