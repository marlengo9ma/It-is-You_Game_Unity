using System;
using System.Collections;
using System.Collections.Generic;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 대사창. 타자기 출력, 밑줄 구간 클릭 수집, 리액션 수집 창 개폐를 담당한다.
    ///
    /// 화자 이름과 본문만 찍는 단순한 창이 아니라, 이 게임에서 대사창은
    /// 플레이어가 단서를 줍는 주요 수집 지점이기도 하다. 그래서 출력과 입력을
    /// 한 곳에서 관리한다.
    /// </summary>
    [AddComponentMenu("It is You/Dialogue Presenter")]
    public class DialoguePresenter : MonoBehaviour, IPointerClickHandler
    {
        [Header("참조")]
        [SerializeField] private CanvasGroup root;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private TMP_Text stageDirectionLabel;
        [SerializeField] private GameObject advanceIndicator;
        [SerializeField] private Camera uiCamera;

        [Header("타자기")]
        [Tooltip("초당 출력 글자 수.")]
        [SerializeField] private float charactersPerSecond = 45f;

        [Tooltip("문장부호에서 추가로 쉬는 시간(초).")]
        [SerializeField] private float punctuationPause = 0.12f;

        [Header("화자 표기")]
        [SerializeField] private string protagonistName = "나";
        [SerializeField] private string assistantName = "세피";
        [SerializeField] private string catName = "검은 고양이";

        // ---- 외부 연결 ----

        /// <summary>밑줄 구간을 클릭해 증언을 수집하려 한다. (단서 id)</summary>
        public event Action<string> TestimonyClicked;

        /// <summary>리액션 수집 창이 열렸다/닫혔다. (열림 여부, 단서 id)</summary>
        public event Action<bool, string> ReactionWindowChanged;

        /// <summary>한 줄의 출력이 모두 끝났다.</summary>
        public event Action<DialogueLine> LineCompleted;

        /// <summary>플레이어가 화면을 눌러 다음으로 넘겼다.</summary>
        public event Action Advanced;

        // ---- 상태 ----

        private readonly HashSet<string> _collectedIds = new HashSet<string>();
        private Coroutine _playing;
        private bool _lineFullyShown;
        private bool _advanceRequested;
        private DialogueLine _currentLine;
        private string _openReactionClueId;

        // 대사가 있느냐(_visible)와, 플레이어가 창을 치워 뒀느냐(_hidden)는 별개다.
        // 둘을 섞으면 창을 숨긴 채 다음 대사가 오는 순간 멋대로 다시 켜진다.
        private bool _visible;
        private bool _hidden;

        public bool IsPlaying => _playing != null;
        public DialogueLine CurrentLine => _currentLine;

        /// <summary>리액션 수집 창이 지금 열려 있는가.</summary>
        public bool ReactionWindowOpen => !string.IsNullOrEmpty(_openReactionClueId);

        /// <summary>플레이어가 대사창을 치워 둔 상태인가.</summary>
        public bool IsHidden => _hidden;

        private void Awake()
        {
            SetVisible(false);
            if (advanceIndicator != null) advanceIndicator.SetActive(false);
        }

        /// <summary>이미 수집한 단서 id 집합을 넘겨 두면, 수집된 구간은 밑줄 대신 강조색으로 그린다.</summary>
        public void SyncCollected(IEnumerable<string> collectedIds)
        {
            _collectedIds.Clear();
            if (collectedIds == null) return;
            foreach (var id in collectedIds) _collectedIds.Add(id);
        }

        public void NotifyCollected(string clueId)
        {
            if (string.IsNullOrEmpty(clueId)) return;
            _collectedIds.Add(clueId);

            // 이미 출력된 본문을 다시 그려 밑줄을 없앤다.
            if (_currentLine != null && _lineFullyShown)
                bodyLabel.text = DialogueMarkup.ToRichText(_currentLine.Text, _collectedIds);
        }

        // -------------------------------------------------------------- 재생

        /// <summary>대사 묶음을 순서대로 재생한다. 끝날 때까지 기다리려면 코루틴으로 감싼다.</summary>
        public Coroutine Play(DialogueSequence sequence, float recallTextCorruption = 0f, int recallDepth = 0)
        {
            Stop();
            _playing = StartCoroutine(PlayRoutine(sequence, recallTextCorruption, recallDepth));
            return _playing;
        }

        public void Stop()
        {
            if (_playing != null) StopCoroutine(_playing);
            _playing = null;
            CloseReactionWindow();
        }

        private IEnumerator PlayRoutine(DialogueSequence sequence, float corruption, int recallDepth)
        {
            if (sequence == null || sequence.IsEmpty)
            {
                _playing = null;
                yield break;
            }

            SetVisible(true);

            var lines = sequence.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                yield return ShowLine(lines[i], corruption, recallDepth);
            }

            SetVisible(false);
            _playing = null;
        }

        private IEnumerator ShowLine(DialogueLine line, float corruption, int recallDepth)
        {
            _currentLine = line;
            _lineFullyShown = false;
            _advanceRequested = false;

            if (speakerLabel != null) speakerLabel.text = SpeakerName(line.Speaker);
            if (stageDirectionLabel != null)
            {
                var hasDirection = !string.IsNullOrEmpty(line.StageDirection);
                stageDirectionLabel.gameObject.SetActive(hasDirection);
                if (hasDirection) stageDirectionLabel.text = line.StageDirection;
            }
            if (advanceIndicator != null) advanceIndicator.SetActive(false);

            // 기억 짐작에서 왜곡이 심하면 밑줄 구간을 아예 링크로 만들지 않는다.
            // "일부 흐릿해진 표정 리액션이나 왜곡된 텍스트에서는 수집이 불가능해집니다."
            var collectable = corruption < 0.5f;
            var displayText = DialogueMarkup.ToRichText(
                corruption > 0f ? Corrupt(line.Text, corruption) : line.Text,
                _collectedIds,
                collectable);

            if (bodyLabel != null)
            {
                bodyLabel.text = displayText;
                bodyLabel.maxVisibleCharacters = 0;
            }

            var reactionRoutine = line.HasReaction && line.Reaction.IsCollectableAtRecallDepth(recallDepth)
                ? StartCoroutine(ReactionRoutine(line.Reaction))
                : null;

            yield return Typewriter();

            _lineFullyShown = true;
            if (advanceIndicator != null) advanceIndicator.SetActive(true);
            LineCompleted?.Invoke(line);

            // 플레이어가 넘길 때까지 기다린다. 그 동안 리액션 창은 계속 살아 있다.
            while (!_advanceRequested) yield return null;

            if (reactionRoutine != null) StopCoroutine(reactionRoutine);
            CloseReactionWindow();

            if (advanceIndicator != null) advanceIndicator.SetActive(false);
        }

        private IEnumerator Typewriter()
        {
            if (bodyLabel == null) yield break;

            bodyLabel.ForceMeshUpdate();
            var total = bodyLabel.textInfo.characterCount;
            var shown = 0;
            var perChar = charactersPerSecond <= 0f ? 0f : 1f / charactersPerSecond;

            while (shown < total)
            {
                if (_advanceRequested)
                {
                    // 출력 중 클릭하면 즉시 전부 보여주고, 그 클릭은 넘기기로 치지 않는다.
                    _advanceRequested = false;
                    break;
                }

                shown++;
                bodyLabel.maxVisibleCharacters = shown;

                var wait = perChar;
                if (shown - 1 < total)
                {
                    var c = bodyLabel.textInfo.characterInfo[shown - 1].character;
                    if (c == '.' || c == ',' || c == '?' || c == '!' || c == '…') wait += punctuationPause;
                }

                if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
            }

            bodyLabel.maxVisibleCharacters = int.MaxValue;
        }

        private IEnumerator ReactionRoutine(ReactionCue cue)
        {
            if (cue.OpenDelay > 0f) yield return new WaitForSecondsRealtime(cue.OpenDelay);

            OpenReactionWindow(cue.ClueId);
            yield return new WaitForSecondsRealtime(cue.Window);
            CloseReactionWindow();
        }

        private void OpenReactionWindow(string clueId)
        {
            _openReactionClueId = clueId;
            ReactionWindowChanged?.Invoke(true, clueId);
        }

        private void CloseReactionWindow()
        {
            if (string.IsNullOrEmpty(_openReactionClueId)) return;
            var id = _openReactionClueId;
            _openReactionClueId = null;
            ReactionWindowChanged?.Invoke(false, id);
        }

        // -------------------------------------------------------------- 입력

        public void OnPointerClick(PointerEventData eventData)
        {
            // 밑줄 구간을 눌렀는지 먼저 본다. 눌렀다면 대사를 넘기지 않는다.
            if (bodyLabel != null)
            {
                var linkIndex = TMP_TextUtilities.FindIntersectingLink(bodyLabel, eventData.position, uiCamera);
                if (linkIndex >= 0)
                {
                    var link = bodyLabel.textInfo.linkInfo[linkIndex];
                    var id = link.GetLinkID();
                    if (!string.IsNullOrEmpty(id) && !_collectedIds.Contains(id))
                    {
                        TestimonyClicked?.Invoke(id);
                        return;
                    }
                }
            }

            RequestAdvance();
        }

        /// <summary>바깥에서도 넘기기를 요청할 수 있게 열어 둔다. (스페이스 키 등)</summary>
        public void RequestAdvance()
        {
            _advanceRequested = true;
            Advanced?.Invoke();
        }

        // -------------------------------------------------------------- 보조

        private void SetVisible(bool visible)
        {
            _visible = visible;
            ApplyVisibility();
        }

        /// <summary>플레이어가 대사창을 잠시 치워 둔다. 그림을 보려고 누르는 버튼이다.</summary>
        public void SetHidden(bool hidden)
        {
            _hidden = hidden;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            if (root == null) return;
            var shown = _visible && !_hidden;
            root.alpha = shown ? 1f : 0f;
            root.interactable = shown;
            root.blocksRaycasts = shown;
        }

        /// <summary>대사 로그가 같은 표기를 쓰도록 화자 이름을 열어 둔다.</summary>
        public string SpeakerLabelFor(SpeakerId speaker) => SpeakerName(speaker);

        private string SpeakerName(SpeakerId speaker) => speaker switch
        {
            SpeakerId.Protagonist => protagonistName,
            SpeakerId.Inner => protagonistName,
            SpeakerId.Assistant => assistantName,
            SpeakerId.Cat => catName,
            SpeakerId.Narration => string.Empty,
            SpeakerId.System => string.Empty,
            _ => string.Empty
        };

        /// <summary>
        /// 기억 짐작의 텍스트 왜곡. 글자 일부를 가려 "왜곡된 내용"으로 만든다.
        /// 마크업 태그 안쪽은 건드리지 않아야 링크가 깨지지 않는다.
        /// </summary>
        private static string Corrupt(string source, float amount)
        {
            if (string.IsNullOrEmpty(source) || amount <= 0f) return source;

            var chars = source.ToCharArray();
            var insideTag = false;
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] == '<') insideTag = true;
                else if (chars[i] == '>') insideTag = false;
                else if (!insideTag && chars[i] != ' ' && UnityEngine.Random.value < amount)
                    chars[i] = '▒';
            }
            return new string(chars);
        }
    }
}
