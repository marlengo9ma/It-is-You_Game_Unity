using System;
using System.Collections;
using System.Collections.Generic;
using ItIsYou.Core;
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

        [Header("말풍선 — 상대의 말")]
        [Tooltip("심문 중 용의자의 대사가 나오는 위쪽 말풍선. 비워 두면 모든 대사가 아래 창으로 간다.")]
        [SerializeField] private CanvasGroup bubbleRoot;
        [SerializeField] private TMP_Text bubbleSpeakerLabel;
        [SerializeField] private TMP_Text bubbleBodyLabel;
        [SerializeField] private TMP_Text bubbleDirectionLabel;
        [SerializeField] private GameObject bubbleAdvanceIndicator;

        [Tooltip("상대가 말하는 동안 아래 창(주인공)이 남아 있을 때의 투명도. 반대도 같다.")]
        [Range(0f, 1f)]
        [SerializeField] private float inactiveSurfaceAlpha = 0.55f;

        [Header("영화식 대사 — 평범한 대화 구간")]
        [Tooltip("틀 없이 화면 아래 그라데이션 위에 이름표 + 본문. 비워 두면 수첩을 쓴다.")]
        [SerializeField] private CanvasGroup cinematicRoot;
        [SerializeField] private TMP_Text cinematicSpeakerLabel;
        [SerializeField] private TMP_Text cinematicRoleLabel;
        [SerializeField] private Image cinematicStroke;
        [SerializeField] private TMP_Text cinematicBodyLabel;
        [SerializeField] private TMP_Text cinematicDirectionLabel;
        [SerializeField] private GameObject cinematicAdvanceIndicator;

        [Header("AUTO · SKIP")]
        [Tooltip("SKIP 중 이미 본 줄의 출력 배속.")]
        [SerializeField] private float skipMultiplier = 8f;
        [Tooltip("화면을 이만큼 누르고 있으면 SKIP 이 걸린다(초). 평범한 대화 구간.")]
        [SerializeField] private float holdToSkipSeconds = 0.6f;

        [Header("타자기")]
        [Tooltip("초당 출력 글자 수. 평범한 대화 구간에서 쓴다.")]
        [SerializeField] private float charactersPerSecond = 45f;

        [Tooltip("심문 중의 초당 출력 글자 수. 실제로 말하는 속도에 맞춰 느리게.")]
        [SerializeField] private float speakingCharactersPerSecond = 16f;

        [Tooltip("평범한 대화에서 말줄임표(… 또는 온점 3개 이상)에 두는 쉼(초). 이 구간은 쉼표·마침표에서 쉬지 않는다 — 한 문장이 토막나 보이기 때문.")]
        [SerializeField] private float ellipsisPause = 0.22f;

        [Tooltip("심문에서 말줄임표에 두는 쉼(초). 말을 흐리는 자리라 평범한 대화보다 길게 둔다.")]
        [SerializeField] private float interrogationEllipsisPause = 0.5f;

        [Tooltip("심문에서 쉼표에 두는 쉼(초). 실제로 말하는 구간이라 숨을 고르는 자리가 있어야 한다.")]
        [SerializeField] private float interrogationCommaPause = 0.09f;

        [Tooltip("심문에서 마침표·물음표·느낌표에 두는 쉼(초).")]
        [SerializeField] private float interrogationPeriodPause = 0.17f;

        [Header("빨리 감기")]
        [Tooltip("좌클릭을 이만큼 유지하면 빨리 감기가 걸린다(초).")]
        [SerializeField] private float holdToFastForwardSeconds = 2f;

        [Tooltip("빨리 감기 배속.")]
        [SerializeField] private float fastForwardMultiplier = 2f;

        [Tooltip("심문처럼 넘길 수 없는 구간에서, 대사가 전부 출력된 뒤 다음 줄로 넘어가기까지 두는 시간(초).")]
        [SerializeField] private float interrogationHoldSeconds = 2f;

        [Header("화자 표기")]
        [Tooltip("주인공 이름표. 2026-10-11 제작자 지시로 「나」 → 「당신」.")]
        [SerializeField] private string protagonistName = "당신";
        [SerializeField] private string assistantName = "세피";
        [SerializeField] private string catName = "검은 고양이";
        [SerializeField] private string protagonistRole = "수사관";
        [SerializeField] private string assistantRole = "보조수사관";
        [SerializeField] private string catRole = "??";
        [Tooltip("주인공 이름표 색 — 와인레드 붓자국 (2026-10-11 제작자 정정: 보라는 고양이).")]
        [SerializeField] private Color protagonistColor = new Color32(0x7A, 0x1F, 0x2E, 0xFF);
        [SerializeField] private Color assistantColor = new Color32(0x3F, 0x8A, 0x4E, 0xFF);
        [SerializeField] private Color catColor = new Color32(0x4B, 0x3A, 0x8E, 0xFF);
        [SerializeField] private Color narrationColor = new Color32(0x6E, 0x74, 0x7B, 0xFF);

        // ---- 외부 연결 ----

        /// <summary>밑줄 구간을 클릭해 증언을 수집하려 한다. (단서 id)</summary>
        public event Action<string> TestimonyClicked;

        /// <summary>리액션 수집 창이 열렸다/닫혔다. (열림 여부, 단서 id)</summary>
        public event Action<bool, string> ReactionWindowChanged;

        /// <summary>한 줄의 출력이 시작됐다. 표정·연출은 이 시점에 건다.</summary>
        public event Action<DialogueLine> LineStarted;

        /// <summary>대사 묶음의 재생이 시작됐다.</summary>
        public event Action<DialogueSequence> SequenceStarted;

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

        // 화자에 따라 위(말풍선)/아래(수첩)로 갈라 쓰는가. 심문·조합에서 켠다.
        private bool _splitBySpeaker;
        private bool _useBubble;
        private bool _bubbleHasText;
        private bool _bottomHasText;

        // 평범한 대화 구간의 아래 창 모양. 영화식(그라데이션 + 이름표) 또는 수첩.
        private bool _cinematic;

        // AUTO — 한 줄이 다 나오면 설정의 시간 뒤 스스로 넘어간다.
        // SKIP — 누르고 있는 동안 이미 본 줄을 8배속으로 찍고 바로 넘긴다. 켜지는 순간 AUTO 는 꺼진다.
        private bool _auto;
        private bool _skipButtonHeld;
        private bool _skipping;
        private float _screenHoldSeconds;
        private static HashSet<string> _seenLines;
        private const string SeenKey = "dialogue.seen";

        // 심문 중에는 대사를 넘길 수 없다. 대신 좌클릭을 길게 눌러 빨리 감는다.
        private bool _allowSkip = true;
        private bool _slowSpeech;
        private float _holdSeconds;
        private bool _fastForward;

        public bool IsPlaying => _playing != null;

        /// <summary>
        /// 상대의 말은 위 말풍선에, 주인공의 말·속마음·지문은 아래 창에.
        /// 시선이 아래로 떨어지지 않게 하려는 배치다. 말풍선이 없는 씬에서는 무시된다.
        /// </summary>
        public void SetSplitBySpeaker(bool on)
        {
            _splitBySpeaker = on && bubbleRoot != null;
            if (!_splitBySpeaker) { _useBubble = false; _bubbleHasText = false; }
            ApplyVisibility();
        }

        /// <summary>평범한 대화 구간의 아래 창을 영화식으로 쓸지. 심문·조합은 수첩.</summary>
        public void SetCinematic(bool on)
        {
            _cinematic = on && cinematicRoot != null;
            ApplyVisibility();
        }

        public bool AutoEnabled => _auto;
        public bool IsSkipping => _skipping;

        /// <summary>AUTO 가 켜지거나 꺼졌다.</summary>
        public event Action<bool> AutoChanged;

        /// <summary>SKIP 이 걸리거나 풀렸다.</summary>
        public event Action<bool> SkipChanged;

        public void SetAuto(bool on)
        {
            if (_auto == on) return;
            _auto = on;
            AutoChanged?.Invoke(on);
        }

        public void ToggleAuto() => SetAuto(!_auto);

        /// <summary>SKIP 버튼을 누르고 있는 동안 true. 화면을 길게 눌러도 같은 상태가 된다.</summary>
        public void SetSkipHeld(bool held)
        {
            _skipButtonHeld = held;
            UpdateSkipping();
        }

        private void UpdateSkipping()
        {
            var on = _allowSkip && (_skipButtonHeld || _screenHoldSeconds >= holdToSkipSeconds);
            if (on == _skipping) return;
            _skipping = on;
            if (on) SetAuto(false);
            SkipChanged?.Invoke(on);
        }

        // ---- 본 적 있는 줄 — SKIP 은 이것에만 걸린다. 실행 사이에도 남도록 PlayerPrefs 에 둔다. ----

        private static HashSet<string> Seen
        {
            get
            {
                if (_seenLines != null) return _seenLines;
                _seenLines = new HashSet<string>();
                var raw = PlayerPrefs.GetString(SeenKey, string.Empty);
                if (!string.IsNullOrEmpty(raw))
                    foreach (var h in raw.Split(';')) if (h.Length > 0) _seenLines.Add(h);
                return _seenLines;
            }
        }

        private static string LineKey(DialogueLine line)
        {
            // 안정적인 해시 — string.GetHashCode 는 실행마다 달라질 수 있다.
            unchecked
            {
                uint h = 2166136261;
                var text = line.Text ?? string.Empty;
                for (int i = 0; i < text.Length; i++) { h ^= text[i]; h *= 16777619; }
                return h.ToString("x8");
            }
        }

        public static bool HasSeen(DialogueLine line) => line != null && Seen.Contains(LineKey(line));

        private static void MarkSeen(DialogueLine line)
        {
            if (line == null) return;
            var key = LineKey(line);
            if (!Seen.Add(key)) return;
            PlayerPrefs.SetString(SeenKey, string.Join(";", Seen));
        }

        private static bool IsOtherParty(SpeakerId speaker)
            => speaker == SpeakerId.Assistant || speaker == SpeakerId.Cat
            || speaker == SpeakerId.SuspectA || speaker == SpeakerId.SuspectB || speaker == SpeakerId.SuspectC;

        private CanvasGroup BottomRoot => _cinematic ? cinematicRoot : root;
        private TMP_Text BottomBody => _cinematic ? cinematicBodyLabel : bodyLabel;
        private TMP_Text BottomSpeaker => _cinematic ? cinematicSpeakerLabel : speakerLabel;
        private TMP_Text BottomDirection => _cinematic ? cinematicDirectionLabel : stageDirectionLabel;
        private GameObject BottomIndicator => _cinematic ? cinematicAdvanceIndicator : advanceIndicator;
        private TMP_Text ActiveBody => _useBubble ? bubbleBodyLabel : BottomBody;
        private GameObject ActiveIndicator => _useBubble ? bubbleAdvanceIndicator : BottomIndicator;
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
                // 평범한 대화만 설정의 글자 속도를 따른다. 심문의 말하는 속도는 연출이라 고정이다.
                var basis = _slowSpeech ? speakingCharactersPerSecond : charactersPerSecond * Mathf.Max(0.25f, GameSettings.TextSpeed);
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
            if (PauseMenu.IsPaused) return;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            var pressed = mouse != null && mouse.leftButton.isPressed;

            if (_allowSkip)
            {
                // 평범한 대화: 화면을 길게 누르면 SKIP. 버튼을 누르고 있어도 같다.
                _screenHoldSeconds = pressed ? _screenHoldSeconds + Time.unscaledDeltaTime : 0f;
                UpdateSkipping();
                return;
            }

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
            if (bubbleAdvanceIndicator != null) bubbleAdvanceIndicator.SetActive(false);
            if (cinematicAdvanceIndicator != null) cinematicAdvanceIndicator.SetActive(false);
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
            if (_currentLine != null && _lineFullyShown && ActiveBody != null)
                ActiveBody.text = DialogueMarkup.ToRichText(_currentLine.Text, _collectedIds);
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
            SequenceStarted?.Invoke(sequence);

            var lines = sequence.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                yield return ShowLine(lines[i], corruption, recallDepth);
            }

            _bubbleHasText = false;
            _bottomHasText = false;
            SetVisible(false);
            _playing = null;
        }

        private IEnumerator ShowLine(DialogueLine line, float corruption, int recallDepth)
        {
            _currentLine = line;
            _lineFullyShown = false;
            _advanceRequested = false;

            _useBubble = _splitBySpeaker && IsOtherParty(line.Speaker);
            if (_useBubble) _bubbleHasText = true; else _bottomHasText = true;
            ApplyVisibility();

            var speaker = _useBubble ? bubbleSpeakerLabel : BottomSpeaker;
            var direction = _useBubble ? bubbleDirectionLabel : BottomDirection;
            if (speaker != null) speaker.text = SpeakerLabelFor(line);
            if (!_useBubble && _cinematic)
            {
                var named = !string.IsNullOrEmpty(SpeakerLabelFor(line));
                var role = line.HasSpeakerNameOverride ? string.Empty : SpeakerRole(line.Speaker);
                if (cinematicRoleLabel != null) cinematicRoleLabel.text = named && !string.IsNullOrEmpty(role) ? "_ " + role : string.Empty;
                if (cinematicStroke != null)
                {
                    cinematicStroke.color = line.HasSpeakerNameOverride ? narrationColor : SpeakerColor(line.Speaker);
                    cinematicStroke.gameObject.SetActive(named);
                }
            }
            if (direction != null)
            {
                var hasDirection = !string.IsNullOrEmpty(line.StageDirection);
                direction.gameObject.SetActive(hasDirection);
                if (hasDirection) direction.text = line.StageDirection;
            }
            if (advanceIndicator != null) advanceIndicator.SetActive(false);
            if (bubbleAdvanceIndicator != null) bubbleAdvanceIndicator.SetActive(false);
            if (cinematicAdvanceIndicator != null) cinematicAdvanceIndicator.SetActive(false);

            // 기억 짐작에서 왜곡이 심하면 밑줄 구간을 아예 링크로 만들지 않는다.
            // "일부 흐릿해진 표정 리액션이나 왜곡된 텍스트에서는 수집이 불가능해집니다."
            var collectable = corruption < 0.5f;
            var displayText = DialogueMarkup.ToRichText(
                corruption > 0f ? Corrupt(line.Text, corruption) : line.Text,
                _collectedIds,
                collectable);

            var body = ActiveBody;
            if (body != null)
            {
                body.text = displayText;
                body.maxVisibleCharacters = 0;
            }

            LineStarted?.Invoke(line);

            var reactionRoutine = line.HasReaction && line.Reaction.IsCollectableAtRecallDepth(recallDepth)
                ? StartCoroutine(ReactionRoutine(line.Reaction))
                : null;

            yield return Typewriter();

            _lineFullyShown = true;
            if (ActiveIndicator != null) ActiveIndicator.SetActive(true);
            LineCompleted?.Invoke(line);

            var wasSeen = HasSeen(line);
            MarkSeen(line);

            // 플레이어가 넘길 때까지 기다린다. 그 동안 리액션 창은 계속 살아 있다.
            if (_allowSkip)
            {
                var autoClock = 0f;
                while (!_advanceRequested)
                {
                    if (PauseMenu.IsPaused) { yield return null; continue; }
                    // SKIP: 본 줄은 바로 넘어간다. 안 본 줄은 손을 떼거나 누를 때까지 기다린다.
                    if (_skipping && wasSeen) break;
                    // AUTO: 설정의 시간만큼 두고 넘어간다.
                    if (_auto)
                    {
                        autoClock += Time.unscaledDeltaTime;
                        if (autoClock >= Mathf.Max(0.2f, GameSettings.AutoAdvanceSeconds)) break;
                    }
                    yield return null;
                }
                while (PauseMenu.IsPaused) yield return null;
            }
            else
            {
                // 심문에서는 넘길 수 없다. 읽을 만큼만 두고 알아서 다음 줄로 간다.
                // 리액션을 포착할 창이 이 사이에도 살아 있어야 하므로 코루틴은 건드리지 않는다.
                var hold = interrogationHoldSeconds;
                var t = 0f;
                while (t < hold)
                {
                    if (PauseMenu.IsPaused) { yield return null; continue; }
                    t += Time.unscaledDeltaTime * (_fastForward ? Mathf.Max(1f, fastForwardMultiplier) : 1f);
                    yield return null;
                }
            }

            if (reactionRoutine != null) StopCoroutine(reactionRoutine);
            CloseReactionWindow();

            if (ActiveIndicator != null) ActiveIndicator.SetActive(false);
        }

        /// <summary>글자 뒤에 두는 숨. 구간에 따라 길이가 다르다.</summary>
        private enum Breath { None, Comma, Period, Ellipsis }

        /// <summary>
        /// 이 글자 뒤에 숨을 둘 자리인가, 둔다면 어떤 종류인가.
        ///
        /// 말줄임표는 '…' 한 글자이거나 온점 3개 이상이 이어진 것의 끝이다.
        /// 속마음 앞에 붙는 '..' (두 개) 는 쉬지 않는다 — 줄 첫머리에서 멈칫하면 어색하다.
        /// 물음표·느낌표가 연달아 올 때(?!)는 마지막 글자에서만 쉰다.
        /// </summary>
        private static Breath BreathAt(TMPro.TMP_CharacterInfo[] info, int index, int total)
        {
            var c = info[index].character;

            if (c == '…') return Breath.Ellipsis;
            if (c == ',' || c == '，' || c == '、') return Breath.Comma;

            if (c == '.')
            {
                // 뒤에 온점이 더 있으면 아직 끝이 아니다.
                if (index + 1 < total && info[index + 1].character == '.') return Breath.None;

                var run = 0;
                for (var i = index; i >= 0 && info[i].character == '.'; i--) run++;
                if (run >= 3) return Breath.Ellipsis;
                if (run == 2) return Breath.None;
                return Breath.Period;
            }

            if (c == '?' || c == '!' || c == '？' || c == '！')
            {
                if (index + 1 < total)
                {
                    var next = info[index + 1].character;
                    if (next == '?' || next == '!' || next == '？' || next == '！') return Breath.None;
                }
                return Breath.Period;
            }

            return Breath.None;
        }

        /// <summary>
        /// 구간에 따른 숨의 길이.
        ///
        /// 심문은 상대가 실제로 말하는 구간이라 쉼표·마침표에서도 숨을 고른다.
        /// 평범한 대화는 플레이어가 읽는 속도로 흐르므로 문장부호마다 끊으면 토막나 보인다 —
        /// 거기서는 말줄임표에서만 쉰다.
        /// </summary>
        private float PauseFor(Breath breath) => breath switch
        {
            Breath.Ellipsis => _slowSpeech ? interrogationEllipsisPause : ellipsisPause,
            Breath.Comma => _slowSpeech ? interrogationCommaPause : 0f,
            Breath.Period => _slowSpeech ? interrogationPeriodPause : 0f,
            _ => 0f
        };

        private IEnumerator Typewriter()
        {
            var bodyLabel = ActiveBody;
            if (bodyLabel == null) yield break;

            bodyLabel.ForceMeshUpdate();
            var total = bodyLabel.textInfo.characterCount;
            var shown = 0;

            while (shown < total)
            {
                if (PauseMenu.IsPaused) { yield return null; continue; }
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
                if (_skipping && HasSeen(_currentLine)) cps *= Mathf.Max(1f, skipMultiplier);
                var wait = cps <= 0f ? 0f : 1f / cps;
                // 문장부호에서 숨을 고른다. 어디서 얼마나 쉬는지는 구간이 정한다.
                if (shown - 1 < total)
                {
                    var pause = PauseFor(BreathAt(bodyLabel.textInfo.characterInfo, shown - 1, total));
                    if (pause > 0f)
                        wait += _fastForward ? pause / Mathf.Max(1f, fastForwardMultiplier) : pause;
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
            if (TryClickLink(ActiveBody, eventData)) return;

            // 심문에서는 눌러도 넘어가지 않는다. 밑줄 구간을 집는 것만 의미가 있다.
            if (!_allowSkip) return;
            RequestAdvance();
        }

        private bool TryClickLink(TMP_Text label, PointerEventData eventData)
        {
            if (label == null || !label.isActiveAndEnabled) return false;
            var linkIndex = TMP_TextUtilities.FindIntersectingLink(label, eventData.position, ResolveCamera());
            if (linkIndex < 0) return false;
            var id = label.textInfo.linkInfo[linkIndex].GetLinkID();
            if (string.IsNullOrEmpty(id) || _collectedIds.Contains(id)) return false;
            TestimonyClicked?.Invoke(id);
            return true;
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
            var shown = _visible && !_hidden;

            // 쓰지 않는 아래 창은 끈다.
            if (cinematicRoot != null) Apply(_cinematic ? root : cinematicRoot, false, 1f);
            var bottom = BottomRoot;

            if (bubbleRoot == null || !_splitBySpeaker)
            {
                Apply(bottom, shown, 1f);
                if (bubbleRoot != null) Apply(bubbleRoot, false, 1f);
                return;
            }

            // 지금 말하는 쪽은 또렷하게, 방금 말한 쪽은 흐리게 남긴다. 대화의 양쪽이 화면에 같이 있다.
            Apply(bubbleRoot, shown && _bubbleHasText, _useBubble ? 1f : inactiveSurfaceAlpha);
            Apply(bottom, shown && _bottomHasText, _useBubble ? inactiveSurfaceAlpha : 1f);
        }

        private static void Apply(CanvasGroup group, bool shown, float alpha)
        {
            if (group == null) return;
            group.alpha = shown ? alpha : 0f;
            group.interactable = shown;
            group.blocksRaycasts = shown;
        }

        /// <summary>대사 로그가 같은 표기를 쓰도록 화자 이름을 열어 둔다.</summary>
        public string SpeakerLabelFor(SpeakerId speaker) => SpeakerName(speaker);

        /// <summary>줄에 화자 이름이 직접 적혀 있으면 그것을, 아니면 화자 종류의 이름.</summary>
        public string SpeakerLabelFor(DialogueLine line)
            => line == null ? string.Empty : line.HasSpeakerNameOverride ? line.SpeakerNameOverride : SpeakerName(line.Speaker);

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

        private string SpeakerRole(SpeakerId speaker) => speaker switch
        {
            SpeakerId.Protagonist => protagonistRole,
            SpeakerId.Inner => protagonistRole,
            SpeakerId.Assistant => assistantRole,
            SpeakerId.Cat => catRole,
            _ => string.Empty
        };

        private Color SpeakerColor(SpeakerId speaker) => speaker switch
        {
            SpeakerId.Protagonist => protagonistColor,
            SpeakerId.Inner => protagonistColor,
            SpeakerId.Assistant => assistantColor,
            SpeakerId.Cat => catColor,
            _ => narrationColor
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
