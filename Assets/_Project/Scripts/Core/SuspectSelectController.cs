using System;
using System.Collections;
using System.Collections.Generic;
using ItIsYou.Data;
using ItIsYou.UI;
using TMPro;
using UnityEngine;

namespace ItIsYou.Core
{
    /// <summary>
    /// 누구부터 심문할지 고르는 구간.
    ///
    /// 심문 화면과 같은 레이아웃을 쓰되, 선택지 자리에는 아직 아무것도 없고
    /// 좌측에는 대화 구간처럼 인물과 대사창이 있다. 여기서 오가는 대화가
    /// 끝나야 두 사람을 고를 수 있게 된다 — 말이 끝나기도 전에 고를 수 있으면
    /// 대화가 장식이 되어 버린다.
    ///
    /// 타임 토큰도 무반응 타이머도 돌지 않는다. 아직 심문이 아니다.
    /// </summary>
    [AddComponentMenu("It is You/Suspect Select Controller")]
    public class SuspectSelectController : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private DialoguePresenter dialogue;

        [Tooltip("화면에 서 있는 인물들. 챕터의 1차 심문 대상과 순서대로 짝지어진다.")]
        [SerializeField] private List<SuspectCard> cards = new List<SuspectCard>();

        [Header("표시")]
        [SerializeField] private TMP_Text promptLabel;

        private ChapterDefinition _chapter;
        private IReadOnlyList<SuspectDefinition> _suspects;
        private ISet<string> _finished;
        private string _prompt;
        private bool _skipTalk;
        private bool _running;

        /// <summary>플레이어가 심문할 상대를 골랐다.</summary>
        public event Action<SuspectDefinition> Picked;

        /// <summary>고를 수 없는 상대를 눌렀다. (사유)</summary>
        public event Action<string> Refused;

        public bool IsRunning => _running;

        private void Awake()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                cards[i].Picked += OnPicked;
                cards[i].Refused += OnRefused;
                cards[i].SetArmed(false);
            }
            if (promptLabel != null) promptLabel.text = string.Empty;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                cards[i].Picked -= OnPicked;
                cards[i].Refused -= OnRefused;
            }
        }

        public void Begin(ChapterDefinition chapter)
            => BeginRound(chapter, chapter.FirstRoundSuspects, null, chapter.SuspectSelectPrompt, skipTalk: false);

        /// <summary>
        /// 2차 심문 등 — 대상과 재촉 문구를 따로 주고, 대화는 건너뛸 수 있다.
        /// finishedIds 에 든 상대는 '이미 이야기를 마쳤다' 로 잠긴다.
        /// </summary>
        public void BeginRound(ChapterDefinition chapter, IReadOnlyList<SuspectDefinition> suspects,
            ISet<string> finishedIds, string prompt, bool skipTalk)
        {
            _chapter = chapter;
            _suspects = suspects;
            _finished = finishedIds;
            _prompt = prompt;
            _skipTalk = skipTalk;
            _running = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            BindCards();

            // 대화가 끝날 때까지는 아무도 고를 수 없다.
            SetArmed(false);
            if (promptLabel != null) promptLabel.text = string.Empty;

            if (!_skipTalk)
            {
                if (_chapter.BeforeSuspectSelect != null && !_chapter.BeforeSuspectSelect.IsEmpty)
                    yield return dialogue.Play(_chapter.BeforeSuspectSelect);

                if (_chapter.SuspectSelectTalk != null && !_chapter.SuspectSelectTalk.IsEmpty)
                    yield return dialogue.Play(_chapter.SuspectSelectTalk);
            }

            if (promptLabel != null) promptLabel.text = _prompt ?? string.Empty;
            SetArmed(true);
        }

        private void BindCards()
        {
            var suspects = _suspects;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;

                var has = suspects != null && i < suspects.Count && suspects[i] != null;
                cards[i].gameObject.SetActive(has);
                if (!has) continue;

                var done = _finished != null && _finished.Contains(suspects[i].Id);
                if (done) cards[i].Bind(suspects[i], false, "이미 이야기를 마쳤다.");
                else cards[i].Bind(suspects[i]);
            }
        }

        private void SetArmed(bool armed)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null) cards[i].SetArmed(armed);
            }
        }

        private void OnPicked(SuspectDefinition suspect)
        {
            if (!_running) return;
            _running = false;
            SetArmed(false);
            if (promptLabel != null) promptLabel.text = string.Empty;
            Picked?.Invoke(suspect);
        }

        private void OnRefused(string reason) => Refused?.Invoke(reason);
    }
}
