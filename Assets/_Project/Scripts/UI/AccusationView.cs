using System;
using System.Collections.Generic;
using ItIsYou.Data;
using TMPro;
using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>
    /// 범인 지목 화면. 용의자 선택과 같은 구성 — 자리에 앉은 인물들 중 하나를 고른다.
    ///
    /// 기획서상 이 게임에는 정답이 없다 — "결국 플레이어의 행동에 따라 결과가 달라지며,
    /// 정답이 없는 추리 게임이라는 점이 포인트". 그래서 카드에 정오 표시를 두지 않는다.
    /// 고르면 주인공이 '왜 이 사람인가'를 말하고(SuspectDefinition.AccusationSpeech) 엔딩으로 간다.
    /// </summary>
    [AddComponentMenu("It is You/Accusation View")]
    public class AccusationView : MonoBehaviour
    {
        [SerializeField] private List<SuspectCard> cards = new List<SuspectCard>();
        [SerializeField] private TMP_Text prompt;

        private bool _armed;

        /// <summary>플레이어가 범인을 골랐다.</summary>
        public event Action<SuspectDefinition> Picked;

        private void Awake()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                cards[i].Picked += OnPicked;
                cards[i].SetArmed(false);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null) cards[i].Picked -= OnPicked;
            }
        }

        /// <summary>지목 대상을 자리에 앉히고 고를 수 있게 한다. 잠긴 인물(고양이 등)도 여기서는 고를 수 있다.</summary>
        public void Begin(ChapterDefinition chapter)
        {
            var targets = chapter != null ? chapter.AccusationTargets : null;
            if (prompt != null) prompt.text = chapter != null ? chapter.AccusationPrompt : string.Empty;

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                var has = targets != null && i < targets.Count && targets[i] != null;
                cards[i].gameObject.SetActive(has);
                if (has) cards[i].Bind(targets[i], selectableOverride: true);
                cards[i].SetArmed(has);
            }
            _armed = true;
        }

        private void OnPicked(SuspectDefinition suspect)
        {
            if (!_armed) return;
            _armed = false;
            for (int i = 0; i < cards.Count; i++) if (cards[i] != null) cards[i].SetArmed(false);
            if (prompt != null) prompt.text = string.Empty;
            Picked?.Invoke(suspect);
        }
    }
}
