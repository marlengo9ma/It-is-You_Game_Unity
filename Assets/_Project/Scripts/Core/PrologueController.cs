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
    /// 인트로 카툰을 재생한다.
    ///
    /// 한 페이지(C1~C5)에 여러 컷과 말풍선이 순서대로 들어오는 방식이다. 컷의 그림 · 자리 · 애니메이션은
    /// IntroComic 프리팹에 있고, 여기서는 페이지를 차례로 넘기면서 클릭 · 건너뛰기 · 서류 팝업만 맡는다.
    ///
    /// 카툰이 끝나면 챕터 씬으로 넘어가고, 거기서 기차역 대화(대사창)가 이어진다.
    /// </summary>
    [AddComponentMenu("It is You/Prologue Controller")]
    public class PrologueController : MonoBehaviour, IComicDirector
    {
        [Header("데이터")]
        [SerializeField] private PrologueDefinition prologue;

        [Header("카툰")]
        [Tooltip("IntroComic 프리팹 인스턴스.")]
        [SerializeField] private ComicBookView comic;

        [Header("진행")]
        [Tooltip("화면 전체를 덮는 투명 버튼. 컷 뒤에 깔려 있어 어디를 눌러도 '다음'이 된다.")]
        [SerializeField] private Button advanceArea;

        [SerializeField] private GameObject advanceHint;

        [Header("서류 팝업")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Image popupImage;
        [SerializeField] private TMP_Text popupPlaceholder;
        [SerializeField] private Button popupCloseButton;

        [Header("건너뛰기")]
        [SerializeField] private Button skipButton;

        [Header("끝")]
        [Tooltip("카툰이 끝나고 다음 씬으로 넘어가기 전에 화면을 덮는 검은 막.")]
        [SerializeField] private CanvasGroup endFade;
        [SerializeField] private float endFadeSeconds = 1.0f;

        private int _clicks;
        private bool _skipRequested;
        private bool _popupClosed;

        /// <summary>카툰이 끝났다.</summary>
        public event Action Finished;

        public int PageIndex { get; private set; } = -1;

        public bool Skipping => _skipRequested;
        public bool Paused => PauseMenu.IsPaused;

        private void Awake()
        {
            // 에디터에서 붙인 연결은 씬에 저장되지 않는다. 그래서 런타임에 직접 잇는다.
            if (advanceArea != null) advanceArea.onClick.AddListener(OnAdvanceClicked);
            if (popupCloseButton != null) popupCloseButton.onClick.AddListener(OnPopupClosed);
            if (skipButton != null) skipButton.onClick.AddListener(OnSkip);

            if (popupRoot != null) popupRoot.SetActive(false);
            if (advanceHint != null) advanceHint.SetActive(false);
            if (endFade != null) { endFade.alpha = 0f; endFade.blocksRaycasts = false; }
            if (comic != null) comic.HideAll();
        }

        private void OnDestroy()
        {
            if (advanceArea != null) advanceArea.onClick.RemoveListener(OnAdvanceClicked);
            if (popupCloseButton != null) popupCloseButton.onClick.RemoveListener(OnPopupClosed);
            if (skipButton != null) skipButton.onClick.RemoveListener(OnSkip);
        }

        private void Start()
        {
            if (comic == null)
            {
                Debug.LogError("[인트로] IntroComic 이 비어 있습니다. 메뉴 9 로 인트로 컷을 가져온 뒤 씬을 다시 만드세요.", this);
                enabled = false;
                return;
            }
            StartCoroutine(Run());
        }

        private void Update()
        {
            // 마우스뿐 아니라 스페이스/엔터로도 넘길 수 있게 둔다.
            if (Paused) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
            {
                if (popupRoot != null && popupRoot.activeSelf) _popupClosed = true;
                else _clicks++;
            }
        }

        private IEnumerator Run()
        {
            var pages = comic.Pages;
            for (PageIndex = 0; PageIndex < pages.Count; PageIndex++)
            {
                if (_skipRequested) break;
                var page = pages[PageIndex];
                page.Prepare();
                yield return page.Play(this);
                // 건너뛰기면 페이지를 그대로 둔 채 검은 막으로 덮는다 — 툭 사라지지 않게.
                if (!_skipRequested) page.Hide();
            }

            SetAdvanceHint(false);
            if (popupRoot != null) popupRoot.SetActive(false);
            Finished?.Invoke();

            if (endFade != null)
            {
                endFade.blocksRaycasts = true;
                var t = 0f;
                while (t < endFadeSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    var k = Mathf.Clamp01(t / endFadeSeconds);
                    endFade.alpha = k * k * (3f - 2f * k);
                    yield return null;
                }
                endFade.alpha = 1f;
                // 까만 화면에서 한 박자 쉬고 넘어간다. 다음 씬(챕터)이 까만 화면에서 천천히 밝아진다.
                yield return new WaitForSecondsRealtime(0.25f);
            }

            if (prologue != null && !string.IsNullOrEmpty(prologue.NextSceneName))
                SceneManager.LoadScene(prologue.NextSceneName);
        }

        // ------------------------------------------------------------ IComicDirector

        public bool ConsumeClick()
        {
            if (_clicks <= 0) return false;
            _clicks = 0;
            return true;
        }

        public void ClearClicks() => _clicks = 0;

        public void SetAdvanceHint(bool visible)
        {
            if (advanceHint != null) advanceHint.SetActive(visible);
        }

        public IEnumerator ShowPopup(Sprite image, string placeholder)
        {
            if (popupImage != null)
            {
                popupImage.sprite = image;
                popupImage.enabled = image != null;
            }
            if (popupPlaceholder != null)
            {
                var showText = image == null;
                popupPlaceholder.gameObject.SetActive(showText);
                if (showText)
                    popupPlaceholder.text = DialogueMarkup.ToRichText(placeholder ?? string.Empty, null, collectable: false);
            }
            if (popupRoot != null) popupRoot.SetActive(true);

            _popupClosed = false;
            while (!_popupClosed && !_skipRequested) yield return null;

            if (popupRoot != null) popupRoot.SetActive(false);
            ClearClicks();
        }

        // ------------------------------------------------------------ 입력

        private void OnAdvanceClicked()
        {
            if (Paused) return;
            _clicks++;
        }

        private void OnPopupClosed() => _popupClosed = true;

        private void OnSkip() => _skipRequested = true;
    }
}
