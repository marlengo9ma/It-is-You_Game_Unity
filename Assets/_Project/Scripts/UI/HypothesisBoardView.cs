using System;
using System.Collections;
using System.Collections.Generic;
using ItIsYou.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 단서 조합 무대의 판. 물품 자리 + 정황 자리, "가설 도출" · "추리를 마친다" 버튼, 성공/실패 연출,
    /// 그리고 가설이 가득 찼을 때 지울 것을 고르는 덧창.
    ///
    /// 규칙은 모른다 — 자리에 무엇이 들었는지, 버튼이 눌리는지만 안다. 판단은 DeductionController 가 한다.
    /// </summary>
    [AddComponentMenu("It is You/Hypothesis Board View")]
    public class HypothesisBoardView : MonoBehaviour
    {
        [Header("물품 자리")]
        [SerializeField] private RectTransform objectSlot;
        [SerializeField] private Image objectFrame;
        [SerializeField] private Image objectIcon;
        [SerializeField] private TMP_Text objectName;
        [SerializeField] private TMP_Text objectPlaceholder;
        [SerializeField] private Button objectClear;

        [Header("정황 자리")]
        [SerializeField] private RectTransform leadSlot;
        [SerializeField] private Image leadFrame;
        [SerializeField] private Image leadIcon;
        [SerializeField] private TMP_Text leadName;
        [SerializeField] private TMP_Text leadPlaceholder;
        [SerializeField] private Button leadClear;

        [Header("가운데")]
        [SerializeField] private TMP_Text linkMark;
        [SerializeField] private Button combineButton;
        [SerializeField] private Button endButton;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private CanvasGroup successFlash;
        [SerializeField] private CanvasGroup group;

        [Header("지우기 덧창")]
        [SerializeField] private GameObject replaceRoot;
        [SerializeField] private TMP_Text replacePrompt;
        [SerializeField] private RectTransform replaceContainer;
        [SerializeField] private Button replaceButtonPrefab;
        [SerializeField] private Button replaceCancel;

        [Header("색")]
        [SerializeField] private Color emptyFrame = new Color(0.18f, 0.20f, 0.23f, 1f);
        [SerializeField] private Color filledFrame = new Color(0.77f, 0.65f, 0.36f, 1f);
        [SerializeField] private Color failFrame = new Color(0.72f, 0.24f, 0.20f, 1f);

        private readonly List<Button> _replaceButtons = new List<Button>();
        private Vector2 _objectBase, _leadBase;
        private bool _basesCached;

        public event Action CombineRequested;
        public event Action EndRequested;
        /// <summary>자리를 비웠다. (true = 물품 자리)</summary>
        public event Action<bool> SlotCleared;
        public event Action<string> ReplacePicked;
        public event Action ReplaceCancelled;

        private void Awake()
        {
            if (combineButton != null) combineButton.onClick.AddListener(() => CombineRequested?.Invoke());
            if (endButton != null) endButton.onClick.AddListener(() => EndRequested?.Invoke());
            if (objectClear != null) objectClear.onClick.AddListener(() => SlotCleared?.Invoke(true));
            if (leadClear != null) leadClear.onClick.AddListener(() => SlotCleared?.Invoke(false));
            if (replaceCancel != null) replaceCancel.onClick.AddListener(() => ReplaceCancelled?.Invoke());
            if (replaceRoot != null) replaceRoot.SetActive(false);
            SetSlot(true, null, null);
            SetSlot(false, null, null);
            SetResult(string.Empty);
        }

        private void CacheBases()
        {
            if (_basesCached) return;
            if (objectSlot != null) _objectBase = objectSlot.anchoredPosition;
            if (leadSlot != null) _leadBase = leadSlot.anchoredPosition;
            _basesCached = true;
        }

        public void SetSlot(bool objectSide, ClueDefinition clue, string displayName)
        {
            var frame = objectSide ? objectFrame : leadFrame;
            var icon = objectSide ? objectIcon : leadIcon;
            var name = objectSide ? objectName : leadName;
            var placeholder = objectSide ? objectPlaceholder : leadPlaceholder;
            var clear = objectSide ? objectClear : leadClear;

            var filled = clue != null;
            if (frame != null) frame.color = filled ? filledFrame : emptyFrame;
            if (icon != null)
            {
                icon.sprite = filled ? clue.Icon : null;
                icon.enabled = filled && clue.Icon != null;
            }
            if (name != null) name.text = filled ? displayName : string.Empty;
            if (placeholder != null) placeholder.gameObject.SetActive(!filled);
            if (clear != null) clear.gameObject.SetActive(filled);
        }

        public void SetCombineEnabled(bool on)
        {
            if (combineButton != null) combineButton.interactable = on;
            if (linkMark != null) linkMark.color = on ? filledFrame : emptyFrame;
        }

        public void SetEndEnabled(bool on)
        {
            if (endButton != null) endButton.interactable = on;
        }

        public void SetInteractable(bool on)
        {
            if (group != null) { group.interactable = on; group.blocksRaycasts = on; }
        }

        public void SetHint(string text)
        {
            if (hintLabel != null) hintLabel.text = text ?? string.Empty;
        }

        public void SetResult(string text)
        {
            if (resultLabel != null) resultLabel.text = text ?? string.Empty;
        }

        /// <summary>성공 — 두 자리가 가운데로 당겨지며 놋쇠 빛이 번진다.</summary>
        public IEnumerator PlaySuccess()
        {
            CacheBases();
            const float pull = 0.22f, hold = 0.35f, back = 0.25f;
            var t = 0f;
            while (t < pull)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.SmoothStep(0f, 1f, t / pull);
                if (objectSlot != null) objectSlot.anchoredPosition = Vector2.Lerp(_objectBase, _objectBase + new Vector2(40f, 0f), k);
                if (leadSlot != null) leadSlot.anchoredPosition = Vector2.Lerp(_leadBase, _leadBase + new Vector2(-40f, 0f), k);
                if (successFlash != null) successFlash.alpha = k;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(hold);
            t = 0f;
            while (t < back)
            {
                t += Time.unscaledDeltaTime;
                var k = t / back;
                if (objectSlot != null) objectSlot.anchoredPosition = Vector2.Lerp(_objectBase + new Vector2(40f, 0f), _objectBase, k);
                if (leadSlot != null) leadSlot.anchoredPosition = Vector2.Lerp(_leadBase + new Vector2(-40f, 0f), _leadBase, k);
                if (successFlash != null) successFlash.alpha = 1f - k;
                yield return null;
            }
            if (objectSlot != null) objectSlot.anchoredPosition = _objectBase;
            if (leadSlot != null) leadSlot.anchoredPosition = _leadBase;
            if (successFlash != null) successFlash.alpha = 0f;
        }

        /// <summary>실패 — 두 자리가 붉게 물들며 떨린다.</summary>
        public IEnumerator PlayFail()
        {
            CacheBases();
            const float dur = 0.45f;
            var t = 0f;
            if (objectFrame != null) objectFrame.color = failFrame;
            if (leadFrame != null) leadFrame.color = failFrame;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                var amp = Mathf.Lerp(10f, 0f, t / dur);
                var dx = Mathf.Sin(t * 60f) * amp;
                if (objectSlot != null) objectSlot.anchoredPosition = _objectBase + new Vector2(dx, 0f);
                if (leadSlot != null) leadSlot.anchoredPosition = _leadBase + new Vector2(-dx, 0f);
                yield return null;
            }
            if (objectSlot != null) objectSlot.anchoredPosition = _objectBase;
            if (leadSlot != null) leadSlot.anchoredPosition = _leadBase;
            if (objectFrame != null) objectFrame.color = emptyFrame;
            if (leadFrame != null) leadFrame.color = emptyFrame;
        }

        /// <summary>가설이 가득 찼다 — 지울 가설을 고르는 덧창.</summary>
        public void ShowReplace(string prompt, IReadOnlyList<ClueDefinition> existing, Func<string, string> nameOf)
        {
            if (replaceRoot == null || replaceContainer == null || replaceButtonPrefab == null) return;
            for (int i = 0; i < _replaceButtons.Count; i++) if (_replaceButtons[i] != null) Destroy(_replaceButtons[i].gameObject);
            _replaceButtons.Clear();

            if (replacePrompt != null) replacePrompt.text = prompt ?? string.Empty;
            for (int i = 0; i < existing.Count; i++)
            {
                var clue = existing[i];
                if (clue == null) continue;
                var b = Instantiate(replaceButtonPrefab, replaceContainer);
                var label = b.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = "지운다 — " + (nameOf != null ? nameOf(clue.Id) : clue.ReadName);
                var id = clue.Id;
                b.onClick.AddListener(() => ReplacePicked?.Invoke(id));
                _replaceButtons.Add(b);
            }
            replaceRoot.SetActive(true);
        }

        public void HideReplace()
        {
            if (replaceRoot != null) replaceRoot.SetActive(false);
        }
    }
}
