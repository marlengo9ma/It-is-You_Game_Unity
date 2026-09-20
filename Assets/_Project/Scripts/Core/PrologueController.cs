using System;
using System.Collections;
using ItIsYou.Data;
using ItIsYou.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ItIsYou.Core
{
    /// <summary>
    /// 게임 시작부터 사건 발생 직전까지를 재생한다.
    ///
    /// 여기에는 페이즈 규칙이 하나도 없다 — 타임 토큰도, 수집률도, 페널티도.
    /// 그래서 챕터 흐름과 분리했다. 이 구간에서 플레이어가 하는 일은 읽는 것과,
    /// 기획서의 "서류 내용 팝업(상호작용)" 한 번뿐이다.
    /// </summary>
    [AddComponentMenu("It is You/Prologue Controller")]
    public class PrologueController : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private PrologueDefinition prologue;

        [Header("화면")]
        [SerializeField] private DialoguePresenter dialogue;
        [SerializeField] private Image backdropImage;
        [SerializeField] private TMP_Text backdropLabel;

        [Header("상호작용")]
        [SerializeField] private GameObject interactionRoot;
        [SerializeField] private Button interactionButton;
        [SerializeField] private TMP_Text interactionLabel;

        [Header("서류 팝업")]
        [SerializeField] private GameObject documentRoot;
        [SerializeField] private TMP_Text documentTitle;
        [SerializeField] private TMP_Text documentBody;
        [SerializeField] private Button documentCloseButton;

        [Header("건너뛰기")]
        [SerializeField] private Button skipButton;

        private bool _interactionDone;
        private bool _documentClosed;
        private bool _skipRequested;

        /// <summary>도입부가 끝났다.</summary>
        public event Action Finished;

        private void Awake()
        {
            // 에디터에서 붙인 연결은 씬에 저장되지 않는다. 그래서 런타임에 직접 잇는다.
            if (interactionButton != null) interactionButton.onClick.AddListener(OnInteractionClicked);
            if (documentCloseButton != null) documentCloseButton.onClick.AddListener(OnDocumentClosed);
            if (skipButton != null) skipButton.onClick.AddListener(OnSkip);

            if (interactionRoot != null) interactionRoot.SetActive(false);
            if (documentRoot != null) documentRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (interactionButton != null) interactionButton.onClick.RemoveListener(OnInteractionClicked);
            if (documentCloseButton != null) documentCloseButton.onClick.RemoveListener(OnDocumentClosed);
            if (skipButton != null) skipButton.onClick.RemoveListener(OnSkip);
        }

        private void Start()
        {
            if (prologue == null)
            {
                Debug.LogError("[인트로] PrologueDefinition 이 비어 있습니다.", this);
                enabled = false;
                return;
            }
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var beats = prologue.Beats;
            for (int i = 0; i < beats.Count; i++)
            {
                if (_skipRequested) break;
                yield return PlayBeat(beats[i]);
            }

            Finished?.Invoke();

            if (!string.IsNullOrEmpty(prologue.NextSceneName))
                SceneManager.LoadScene(prologue.NextSceneName);
        }

        private IEnumerator PlayBeat(PrologueBeat beat)
        {
            ShowBackdrop(beat);

            if (beat.Lines != null && !beat.Lines.IsEmpty)
                yield return dialogue.Play(beat.Lines);

            if (_skipRequested || !beat.HasInteraction) yield break;

            // 기획서의 "전달받은 서류 내용 팝업(상호작용)".
            // 읽지 않고는 넘어갈 수 없게 한다. 조수의 이력을 먼저 알아야
            // 본편에서 주인공이 품는 의심에 플레이어도 같이 설 수 있다.
            var prop = beat.Interaction;
            _interactionDone = false;

            if (interactionLabel != null) interactionLabel.text = prop.Prompt;
            if (interactionRoot != null) interactionRoot.SetActive(true);

            while (!_interactionDone && !_skipRequested) yield return null;

            if (interactionRoot != null) interactionRoot.SetActive(false);
            if (_skipRequested) yield break;

            // 팝업을 띄우고 닫을 때까지 기다린다.
            _documentClosed = false;
            if (documentTitle != null) documentTitle.text = prop.DocumentTitle;
            if (documentBody != null)
                documentBody.text = DialogueMarkup.ToRichText(prop.DocumentBody, null, collectable: false);
            if (documentRoot != null) documentRoot.SetActive(true);

            while (!_documentClosed && !_skipRequested) yield return null;

            if (documentRoot != null) documentRoot.SetActive(false);
            if (_skipRequested) yield break;

            if (prop.AfterReading != null && !prop.AfterReading.IsEmpty)
                yield return dialogue.Play(prop.AfterReading);
        }

        private void ShowBackdrop(PrologueBeat beat)
        {
            if (backdropImage != null)
            {
                var hasArt = beat.Backdrop != null;
                backdropImage.sprite = beat.Backdrop;
                backdropImage.color = hasArt ? Color.white : new Color32(0x1E, 0x22, 0x27, 0xFF);
            }

            // 아트가 없는 동안에는 장면 이름을 띄워, 어디를 보고 있는지 알 수 있게 한다.
            if (backdropLabel != null)
            {
                backdropLabel.text = beat.BackdropName;
                backdropLabel.gameObject.SetActive(beat.Backdrop == null);
            }
        }

        private void OnInteractionClicked() => _interactionDone = true;
        private void OnDocumentClosed() => _documentClosed = true;

        private void OnSkip()
        {
            _skipRequested = true;
            dialogue?.Stop();
        }
    }
}
