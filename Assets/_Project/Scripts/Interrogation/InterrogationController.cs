using System;
using System.Collections;
using ItIsYou.Characters;
using ItIsYou.Clues;
using ItIsYou.Data;
using ItIsYou.UI;
using UnityEngine;

namespace ItIsYou.Interrogation
{
    /// <summary>
    /// 심문 페이즈의 진행자.
    ///
    /// 규칙 판단은 전부 <see cref="InterrogationSession"/> 이 한다.
    /// 이 클래스가 하는 일은 그 판단을 화면에 옮기고, 화면에서 들어온 입력을 다시 넘기는 것뿐이다.
    /// 그래서 연출을 갈아엎어도 심문 규칙은 그대로 남는다.
    /// </summary>
    [AddComponentMenu("It is You/Interrogation Controller")]
    public class InterrogationController : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private GameDatabase database;

        [Header("UI")]
        [SerializeField] private DialoguePresenter dialogue;
        [SerializeField] private ChoiceListView choiceList;
        [SerializeField] private CursorController cursor;

        [Header("연출")]
        [Tooltip("무반응 타이머가 차오를수록 커지는 시계 초침 소리.")]
        [SerializeField] private AudioSource clockTickSource;

        [Tooltip("무반응 타이머가 끝까지 찼을 때 초침이 도달할 최대 볼륨.")]
        [Range(0f, 1f)]
        [SerializeField] private float clockMaxVolume = 0.8f;

        [Tooltip("초침이 커지는 만큼 줄어드는 나머지 사운드.")]
        [SerializeField] private AudioSource ambienceSource;

        private InterrogationSession _session;
        private ICharacterView _characterView;
        private ClueInventory _inventory;
        private bool _busy;

        /// <summary>심문이 끝났다. (사유)</summary>
        public event Action<InterrogationEndReason> Finished;

        /// <summary>튜토리얼에서 페널티가 유예됐다. 설명 팝업을 띄울 시점. (판정)</summary>
        public event Action<PressureVerdict> TutorialLessonRequested;

        public InterrogationSession Session => _session;
        public bool IsRunning => _session != null && !_session.IsOver;

        // ------------------------------------------------------------- 시작

        public void Begin(ChapterDefinition chapter, SuspectDefinition suspect,
            ClueInventory inventory, ICharacterView characterView)
        {
            _inventory = inventory;
            _characterView = characterView;

            _session = new InterrogationSession(chapter, suspect, inventory);
            _session.Idle.Elapsed += OnIdleElapsed;

            Subscribe();

            dialogue.SyncCollected(inventory.OwnedIds);

            // 심문은 듣는 구간이다. 말하는 속도로 나오고, 넘길 수 없다.
            dialogue?.SetPacing(slowSpeech: true, allowSkip: false);

            _session.Begin();
            StartCoroutine(OpenRoutine());
        }

        /// <summary>
        /// 심문에 막 들어갔을 때 상대가 먼저 한 마디 한다.
        ///
        /// 선택지부터 띄우면 플레이어가 무엇을 묻는 자리인지 모른 채 고르게 된다.
        /// 상대가 먼저 입을 떼야 "지금부터 이 사람을 상대한다"가 성립한다.
        /// </summary>
        private IEnumerator OpenRoutine()
        {
            var opening = _session.Suspect.OnEnterInterrogation;
            if (opening != null && !opening.IsEmpty)
            {
                _busy = true;
                if (choiceList != null) choiceList.Clear();
                _session.Idle.Hold();

                LogSequence(opening, null);
                yield return dialogue.Play(opening);

                _busy = false;
                _session.Idle.Release();
                _session.Idle.Restart();
            }

            RebuildChoices();
            if (choiceList != null) choiceList.SetInteractable(true);
        }

        private void Subscribe()
        {
            if (choiceList != null) choiceList.ChoiceSelected += OnChoiceSelected;
            if (dialogue != null)
            {
                dialogue.TestimonyClicked += OnTestimonyClicked;
                dialogue.ReactionWindowChanged += OnReactionWindowChanged;
            }
        }

        private void Unsubscribe()
        {
            if (choiceList != null) choiceList.ChoiceSelected -= OnChoiceSelected;
            if (dialogue != null)
            {
                dialogue.TestimonyClicked -= OnTestimonyClicked;
                dialogue.ReactionWindowChanged -= OnReactionWindowChanged;
            }
            if (_session != null) _session.Idle.Elapsed -= OnIdleElapsed;
        }

        private void OnDisable() => Unsubscribe();

        // ------------------------------------------------------------- 루프

        private void Update()
        {
            if (!IsRunning) return;

            _session.Tick(Time.unscaledDeltaTime);
            DriveClockAudio(_session.Idle.Progress);
        }

        /// <summary>
        /// 무반응이 다가올수록 초침 소리가 커지고 나머지 사운드가 줄어든다.
        /// 기획서가 이 요소를 "플레이어에게 실질적인 스트레스를 주는 요소"로 규정했으므로,
        /// 시각 표시보다 청각 변화를 우선한다.
        /// </summary>
        private void DriveClockAudio(float progress)
        {
            if (clockTickSource != null)
                clockTickSource.volume = Mathf.Lerp(0f, clockMaxVolume, progress);

            if (ambienceSource != null)
                ambienceSource.volume = Mathf.Lerp(1f, 0.25f, progress);
        }

        // ------------------------------------------------------------- 입력

        private void OnChoiceSelected(ChoiceDefinition choice)
        {
            if (_busy || !IsRunning) return;
            StartCoroutine(RunStep(_session.SelectChoice(choice), choice.Tone));
        }

        /// <summary>심문 화면 안의 오브젝트를 클릭했을 때 외부에서 호출한다.</summary>
        public void SelectInterrogationObject(InteractableDefinition target)
        {
            if (_busy || !IsRunning) return;
            StartCoroutine(RunStep(_session.SelectInterrogationObject(target), null));
        }

        /// <summary>플레이어가 직접 심문을 끝낸다.</summary>
        public void EndManually()
        {
            if (_busy || !IsRunning) return;
            StartCoroutine(RunStep(_session.EndManually(), null));
        }

        private void OnTestimonyClicked(string clueId)
        {
            var clue = database != null ? database.GetClue(clueId) : null;
            if (clue == null)
            {
                Debug.LogWarning($"대사에 걸린 단서 id '{clueId}' 를 GameDatabase 에서 찾지 못했습니다.", this);
                return;
            }

            if (_session.CollectTestimony(clue))
                dialogue.NotifyCollected(clueId);
        }

        private void OnReactionWindowChanged(bool open, string clueId)
        {
            _characterView?.SetReactionCollectable(open);
            if (cursor != null && !open) cursor.SetReactionAvailable(false);

            if (open && _characterView != null && dialogue.CurrentLine != null)
                _characterView.PlayReaction(dialogue.CurrentLine.Reaction.Expression);
        }

        /// <summary>
        /// 리액션이 열려 있는 동안 캐릭터를 클릭했을 때 호출한다.
        /// 캐릭터 위 히트박스가 이 메서드를 부른다.
        /// </summary>
        public void TryCollectReaction()
        {
            if (!IsRunning || !dialogue.ReactionWindowOpen) return;

            var line = dialogue.CurrentLine;
            if (line == null || !line.HasReaction) return;

            var clue = database != null ? database.GetClue(line.Reaction.ClueId) : null;
            if (clue == null) return;

            var entry = _session.Log.Append(line, _session.Suspect.Id, null);
            if (_session.CollectReaction(entry, clue))
            {
                dialogue.NotifyCollected(clue.Id);
                _characterView?.SetReactionCollectable(false);
            }
        }

        private void OnIdleElapsed()
        {
            if (_busy || !IsRunning) return;
            StartCoroutine(RunStep(_session.ApplyIdlePenalty(), null));
        }

        // ------------------------------------------------------------- 진행

        private IEnumerator RunStep(InterrogationStep step, ChoiceTone? tone)
        {
            _busy = true;
            // 말하는 도중에 끼어드는 행동은 강압에 해당한다. 그래서 강압 선택지만 살려 둔다.
            if (choiceList != null) choiceList.SetInteractableExcept(ChoiceTone.Aggressive);

            // 대사가 나오는 동안에는 무반응 타이머를 멈춘다. 읽는 시간은 플레이어의 침묵이 아니다.
            _session.Idle.Hold();

            var distortion = _session.Recall.CurrentDistortion();

            if (step.Main != null && !step.Main.IsEmpty)
            {
                LogSequence(step.Main, tone);
                yield return dialogue.Play(step.Main, distortion.TextCorruption, _session.Recall.EffectiveDepth);
            }

            if (step.Follow != null && !step.Follow.IsEmpty)
            {
                LogSequence(step.Follow, null);
                yield return dialogue.Play(step.Follow, distortion.TextCorruption, _session.Recall.EffectiveDepth);
            }

            if (step.TutorialLesson)
                TutorialLessonRequested?.Invoke(step.Verdict);

            _busy = false;

            if (step.Ends)
            {
                if (choiceList != null) choiceList.Clear();
                cursor?.ResetState();
                DriveClockAudio(0f);
                dialogue?.SetPacing(slowSpeech: false, allowSkip: true);
                Finished?.Invoke(step.EndReason);
                yield break;
            }

            RebuildChoices();
            if (choiceList != null) choiceList.SetInteractable(true);

            // 스크립트가 전부 끝나고 유예 시간이 지난 뒤에야 타이머가 다시 돈다.
            var grace = _session.Chapter.IdleGraceAfterDialogueSeconds;
            if (grace > 0f) yield return new WaitForSecondsRealtime(grace);

            _session.Idle.Release();
            _session.Idle.Restart();
        }

        private void LogSequence(DialogueSequence sequence, ChoiceTone? tone)
        {
            if (sequence == null) return;
            var lines = sequence.Lines;
            for (int i = 0; i < lines.Count; i++)
                _session.Log.Append(lines[i], _session.Suspect.Id, sequence.Note);
        }

        private void RebuildChoices()
        {
            if (choiceList == null || _session == null) return;
            choiceList.Rebuild(_session.BuildChoiceList());
        }

        // --------------------------------------------------------- 기억 짐작

        public RecallDistortion EnterRecall()
        {
            var d = _session.EnterRecall();
            _characterView?.SetRecallDistortion(d.Blur, d.SilhouetteFade);
            return d;
        }

        public bool UseRecallAssist()
        {
            if (!_session.UseRecallAssist()) return false;
            var d = _session.Recall.CurrentDistortion();
            _characterView?.SetRecallDistortion(d.Blur, d.SilhouetteFade);
            return true;
        }

        public void ExitRecall()
        {
            _session.ExitRecall();
            _characterView?.SetRecallDistortion(0f, 0f);
        }
    }
}
