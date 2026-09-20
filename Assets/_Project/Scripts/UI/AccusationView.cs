using System.Collections.Generic;
using ItIsYou.Core;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 범인 지목 화면.
    ///
    /// 기획서상 이 게임에는 정답이 없다 — "결국 플레이어의 행동에 따라 결과가 달라지며,
    /// 정답이 없는 추리 게임이라는 점이 포인트". 그래서 지목 버튼에 정오 표시를 두지 않는다.
    /// </summary>
    [AddComponentMenu("It is You/Accusation View")]
    public class AccusationView : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        [SerializeField] private ChapterDefinition chapter;
        [SerializeField] private RectTransform container;
        [SerializeField] private Button buttonPrefab;
        [SerializeField] private TMP_Text prompt;

        private readonly List<Button> _spawned = new List<Button>();
        private bool _built;

        private void OnEnable()
        {
            if (_built) return;
            Build();
        }

        private void Build()
        {
            if (chapter == null || container == null || buttonPrefab == null) return;

            if (prompt != null) prompt.text = "누가 가져갔다고 보십니까?";

            var targets = chapter.AccusationTargets;
            for (int i = 0; i < targets.Count; i++)
            {
                var suspect = targets[i];
                if (suspect == null) continue;

                var button = Instantiate(buttonPrefab, container);
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = suspect.DisplayName;

                var id = suspect.Id;
                button.onClick.AddListener(() => Choose(id));
                _spawned.Add(button);
            }

            _built = true;
        }

        private void Choose(string suspectId)
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) _spawned[i].interactable = false;
            }
            flow?.Accuse(suspectId);
        }
    }
}
