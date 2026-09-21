using System;
using System.Collections;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ItIsYou.Core
{
    /// <summary>
    /// 인트로 카툰을 재생한다.
    ///
    /// 대사창이 없는 구간이다. 카툰 파트는 텍스트를 읽는 곳이 아니라
    /// 그림이 넘어가는 곳이라서, 화면에는 컷과 클릭 안내만 있다.
    ///
    /// 예외는 서류 컷 하나. 기획서의 "전달받은 서류 내용 팝업(상호작용)" 이며,
    /// 그림 위의 서류를 직접 눌러야 다음으로 넘어간다.
    /// </summary>
    [AddComponentMenu("It is You/Prologue Controller")]
    public class PrologueController : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private PrologueDefinition prologue;

        [Header("컷")]
        [SerializeField] private Image cutImage;
        [SerializeField] private TMP_Text cutLabel;
        [SerializeField] private CanvasGroup cutGroup;

        [Tooltip("컷이 바뀔 때 페이드에 쓰는 시간(초).")]
        [SerializeField] private float crossFadeSeconds = 0.28f;

        [Header("진행")]
        [Tooltip("컷 전체를 덮는 투명 버튼. 아무 데나 누르면 다음 컷으로 넘어간다.")]
        [SerializeField] private Button advanceArea;

        [SerializeField] private GameObject advanceHint;

        [Header("상호작용")]
        [Tooltip("컷 안의 더미 오브젝트. 위치와 크기는 컷 데이터가 정한다.")]
        [SerializeField] private RectTransform propRoot;
        [SerializeField] private Button propButton;
        [SerializeField] private Image propImage;
        [SerializeField] private TMP_Text propLabel;

        [Header("팝업")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Image popupImage;
        [SerializeField] private TMP_Text popupPlaceholder;
        [SerializeField] private Button popupCloseButton;

        [Header("건너뛰기")]
        [SerializeField] private Button skipButton;

        private int _index;
        private bool _advanceRequested;
        private bool _propClicked;
        private bool _popupClosed;
        private bool _skipRequested;
        private bool _acceptingAdvance;

        /// <summary>카툰이 끝났다.</summary>
        public event Action Finished;

        public int CutIndex => _index;

        private void Awake()
        {
            // 에디터에서 붙인 연결은 씬에 저장되지 않는다. 그래서 런타임에 직접 잇는다.
            if (advanceArea != null) advanceArea.onClick.AddListener(OnAdvanceClicked);
            if (propButton != null) propButton.onClick.AddListener(OnPropClicked);
            if (popupCloseButton != null) popupCloseButton.onClick.AddListener(OnPopupClosed);
            if (skipButton != null) skipButton.onClick.AddListener(OnSkip);

            if (propRoot != null) propRoot.gameObject.SetActive(false);
            if (popupRoot != null) popupRoot.SetActive(false);
            if (advanceHint != null) advanceHint.SetActive(false);
        }

        private void OnDestroy()
        {
            if (advanceArea != null) advanceArea.onClick.RemoveListener(OnAdvanceClicked);
            if (propButton != null) propButton.onClick.RemoveListener(OnPropClicked);
            if (popupCloseButton != null) popupCloseButton.onClick.RemoveListener(OnPopupClosed);
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

        private void Update()
        {
            // 마우스뿐 아니라 스페이스/엔터로도 넘길 수 있게 둔다.
            if (!_acceptingAdvance || ItIsYou.UI.PauseMenu.IsPaused) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                _advanceRequested = true;
        }

        private IEnumerator Run()
        {
            var cuts = prologue.Cuts;
            for (_index = 0; _index < cuts.Count; _index++)
            {
                if (_skipRequested) break;
                yield return PlayCut(cuts[_index]);
            }

            Finished?.Invoke();

            if (!string.IsNullOrEmpty(prologue.NextSceneName))
                SceneManager.LoadScene(prologue.NextSceneName);
        }

        private IEnumerator PlayCut(PrologueCut cut)
        {
            yield return Fade(0f);
            ShowCut(cut);
            yield return Fade(1f);

            if (_skipRequested) yield break;

            if (cut.HasInteraction)
            {
                // 서류 컷. 그림 위의 물건을 직접 눌러야 넘어간다.
                // 다음 컷으로 흘려보내지 않는 이유는, 이 서류를 읽는 것이
                // 본편에서 주인공이 품는 의심의 근거이기 때문이다.
                yield return WaitForProp(cut.Interaction);
                yield break;
            }

            if (advanceHint != null) advanceHint.SetActive(true);
            _advanceRequested = false;
            _acceptingAdvance = true;

            while (!_advanceRequested && !_skipRequested) yield return null;

            _acceptingAdvance = false;
            if (advanceHint != null) advanceHint.SetActive(false);
        }

        private IEnumerator WaitForProp(PrologueProp prop)
        {
            PlaceProp(prop);
            if (propRoot != null) propRoot.gameObject.SetActive(true);

            _propClicked = false;
            while (!_propClicked && !_skipRequested) yield return null;

            if (propRoot != null) propRoot.gameObject.SetActive(false);
            if (_skipRequested) yield break;

            // 팝업을 띄우고, 닫을 때까지 기다린다.
            if (popupImage != null)
            {
                var hasArt = prop.PopupImage != null;
                popupImage.sprite = prop.PopupImage;
                popupImage.enabled = hasArt;
            }
            if (popupPlaceholder != null)
            {
                var showPlaceholder = prop.PopupImage == null;
                popupPlaceholder.gameObject.SetActive(showPlaceholder);
                if (showPlaceholder)
                    popupPlaceholder.text = DialogueMarkup.ToRichText(prop.PopupPlaceholder, null, collectable: false);
            }
            if (popupRoot != null) popupRoot.SetActive(true);

            _popupClosed = false;
            while (!_popupClosed && !_skipRequested) yield return null;

            if (popupRoot != null) popupRoot.SetActive(false);
        }

        /// <summary>컷 안의 비율 좌표를 실제 자리로 옮긴다.</summary>
        private void PlaceProp(PrologueProp prop)
        {
            if (propRoot == null) return;

            var a = prop.Area;
            propRoot.anchorMin = new Vector2(a.xMin, a.yMin);
            propRoot.anchorMax = new Vector2(a.xMax, a.yMax);
            propRoot.offsetMin = Vector2.zero;
            propRoot.offsetMax = Vector2.zero;

            if (propLabel != null) propLabel.text = prop.Label;
        }

        private void ShowCut(PrologueCut cut)
        {
            if (cutImage != null)
            {
                var hasArt = cut.Image != null;
                cutImage.sprite = cut.Image;
                cutImage.color = hasArt ? Color.white : new Color32(0x1E, 0x22, 0x27, 0xFF);
                cutImage.preserveAspect = hasArt;
            }

            // 그림이 없는 동안에는 컷 이름을 띄워, 어느 장면인지 알 수 있게 한다.
            if (cutLabel != null)
            {
                var n = _index + 1;
                var total = prologue.Cuts.Count;
                cutLabel.text = $"컷 {n} / {total}\n{cut.CutName}";
                cutLabel.gameObject.SetActive(cut.Image == null);
            }
        }

        private IEnumerator Fade(float target)
        {
            if (cutGroup == null || crossFadeSeconds <= 0f)
            {
                if (cutGroup != null) cutGroup.alpha = target;
                yield break;
            }

            var from = cutGroup.alpha;
            var t = 0f;
            while (t < crossFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                cutGroup.alpha = Mathf.Lerp(from, target, t / crossFadeSeconds);
                yield return null;
            }
            cutGroup.alpha = target;
        }

        private void OnAdvanceClicked()
        {
            if (_acceptingAdvance) _advanceRequested = true;
        }

        private void OnPropClicked() => _propClicked = true;
        private void OnPopupClosed() => _popupClosed = true;

        private void OnSkip() => _skipRequested = true;
    }
}
