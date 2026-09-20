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
        [Tooltip("초당 출력 글자 수. 평범한 대화 구간에서 쓴다.")]
        [SerializeField] private float charactersPerSecond = 45f;

        [Tooltip("심문 중의 초당 출력 글자 수. 실제로 말하는 속도에 맞춰 느리게.")]
        [SerializeField] private float speakingCharactersPerSecond = 16f;

        [Tooltip("문장부호에서 추가로 쉬는 시간(초).")]
        [SerializeField] private float punctuationPause = 0.12f;

        [Header("빨리 감기")]
        [Tooltip("좌클릭을 이만큼 유지하면 빨리 감기가 걸린다(초).")]
        [SerializeField] private float holdToFastForwardSeconds = 2f;

        [Tooltip("빨리 감기 배속.")]
        [SerializeField] private float fastForwardMultiplier = 2f;

        [Tooltip("넘기기가 막힌 구간에서 한 줄이 끝난 뒤 다음 줄까지 두는 시간(초).")]
        [SerializeField] private float autoAdvanceSeconds = 1.1f;

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

        // 심문 중에는 대사를 넘길 수 없다. 대신 좌클릭을 길게 눌러 빨리 감는다.
        private bool _allowSkip = true;
        private bool _slowSpeech;
        private float _holdSeconds;
        private bool _fastForward;

        public bool IsPlaying => _playing != null;
        public DialogueLine CurrentLine => _currentLine;

        /// <summary>리액션 수집 창이 지금 열려 있는가.</summary>
        public bool ReactionWindowOpen => !string.IsNullOrEmpty(_openReactionClueId);

        /// <summary>플레이어가 대사창을 치워 둔 상태인가.</summary>
        public bool IsHidden => _hidden;

        /// <summary>지금 빨리 감기가 걸려 있는가. 캐릭터 애니메이션도 이 값을 따라간다.</summary>
        public bool IsFastForwarding => _fastForward;

        /// <summary>빨리 감기가 켜지거나 꺼졌다. (배속)</summary>
        public event Action<float> FastForwardChanged;

        /// <summary>
        /// 구간에 맞는 말투를 정한다.
        ///
        /// 심문은 상대의 말을 듣는 구간이라 넘길 수 없고, 읽는 속도가 아니라
        /// 말하는 속도로 나온다. 평범한 대화는 반대로 플레이어가 속도를 쥔다.
        /// </summary>
        public void SetPacing(bool slowSpeech, bool allowSkip)
        {
            _slowSpeech = slowSpeech;
            _allowSkip = allowSkip;
            if (allowSkip) SetFastForward(false);
        }

        private float CharsPerSecond
        {
            get
            {
                var basis = _slowSpeech ? speakingCharactersPerSecond : charactersPerSecond;
                return _fastForward ? basis * Mathf.Max(1f, fastForwardMultiplier) : basis;
            }
        }

        private void SetFastForward(bool on)
        {
            if (_fastForward == on) return;
            _fastForward = on;
            FastForwardChanged?.Invoke(on ? Mathf.Max(1f, fastForwardMultiplier) : 1f);
        }

        /// <summary>
        /// 좌클릭을 길게 누르면 빨리 감기. UI 위 어디를 눌러도 걸리도록
        /// 이벤트 시스템이 아니라 마우스 상태를 직접 본다.
        /// </summary>
        private void Update()
        {
            if (_allowSkip) return;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            var pressed = mouse != null && mouse.leftButton.isPressed;

            if (!pressed)
            {
                _holdSeconds = 0f;
                SetFastForward(false);
                return;
            }

            _holdSeconds += Time.unscaledDeltaTime;
            if (_holdSeconds >= holdToFastForwardSeconds) SetFastForward(true);
        }

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
            if (_allowSkip)
            {
                while (!_advanceRequested) yield return null;
            }
            else
            {
                // 심문에서는 넘길 수 없다. 읽을 만큼만 두고 알아서 다음 줄로 간다.
                // 리액션을 포착할 창이 이 사이에도 살아 있어야 하므로 코루틴은 건드리지 않는다.
                var hold = autoAdvanceSeconds;
                var t = 0f;
                while (t < hold)
                {
                    t += Time.unscaledDeltaTime * (_fastForward ? Mathf.Max(1f, fastForwardMultiplier) : 1f);
                    yield return null;
                }
            }

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

            while (shown < total)
            {
                if (_allowSkip && _advanceRequested)
                {
                    // 출력 중 클릭하면 즉시 전부 보여주고, 그 클릭은 넘기기로 치지 않는다.
                    _advanceRequested = false;
                    break;
                }

                shown++;
                bodyLabel.maxVisibleCharacters = shown;

                // 배속은 매 글자마다 다시 읽는다. 누르고 있는 도중에 걸려도 바로 빨라지도록.
                var cps = CharsPerSecond;
                var wait = cps <= 0f ? 0f : 1f / cps;
                if (shown - 1 < total)
                {
                    var c = bodyLabel.textInfo.characterInfo[shown - 1].character;
                    if (c == '.' || c == ',' || c == '?' || c == '!' || c == '…')
                        wait += _fastForward ? punctuationPause / Mathf.Max(1f, fastForwardMultiplier) : punctuationPause;
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
                var linkIndex = TMP_TextUtilities.FindIntersectingLink(bodyLabel, eventData.position, ResolveCamera());
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

            // 심문에서는 눌러도 넘어가지 않는다. 밑줄 구간을 집는 것만 의미가 있다.
            if (!_allowSkip) return;
            RequestAdvance();
        }

        /// <summary>
        /// 링크 판정에 쓸 카메라.
        ///
        /// Screen Space - Overlay 캔버스에서는 null 이어야 좌표가 맞는다.
        /// 필드에 직접 적어 두면 캔버스 설정을 바꿨을 때 조용히 어긋나므로,
        /// 실제 캔버스를 보고 정한다.
        /// </summary>
        private Camera ResolveCamera()
        {
            if (uiCamera != null) return uiCamera;
            var canvas = bodyLabel != null ? bodyLabel.canvas : null;
            if (canvas == null) return null;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
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
