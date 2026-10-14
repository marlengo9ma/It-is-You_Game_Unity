using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 서류 봉투 연출 (C4). 페이지에 붙인다.
    ///
    /// 페이지의 컷(봉투 뒤판 C4_B · 서류 C4_M · 봉투 앞판 C4_F)이 다 나온 뒤에도 넘어가지 않고 기다리다가,
    /// <see cref="clickArea"/> 를 누르면 서류가 봉투 밖으로 올라오고 → 서류 팝업이 뜬다 → 닫으면 다음 페이지.
    ///
    /// 서류 컷이 편집기에 놓인 자리 = <b>꺼낸 뒤</b>의 자리(C4_2).
    /// 봉투 안에 들어가 있을 때의 자리(C4_1)는 거기서 <see cref="insideOffset"/> 만큼 떨어진 곳이다.
    /// 서류를 고르면 Scene 뷰에 봉투 안 자리가 하늘색 상자로 보인다.
    /// </summary>
    [AddComponentMenu("It is You/Comic Envelope")]
    [DisallowMultipleComponent]
    public class ComicEnvelope : MonoBehaviour
    {
        [Tooltip("봉투에서 꺼내지는 서류 컷 (C4_M).")]
        [SerializeField] private ComicCutView document;

        [Tooltip("봉투 안에 있을 때의 자리 — 꺼낸 뒤 자리에서 얼마나 떨어져 있는가 (픽셀, 1920×1080 화면 기준). 아래로는 y 가 음수.")]
        [SerializeField] private Vector2 insideOffset = new Vector2(0f, -381f);

        [Tooltip("서류가 올라오는 데 걸리는 시간(초).")]
        [SerializeField] private float pullSeconds = 0.9f;

        [Tooltip("서류가 올라오는 감속 곡선.")]
        [SerializeField] private CutEase pullEase = CutEase.EaseOutStrong;

        [Tooltip("눌러야 하는 자리(봉투). 이 오브젝트의 RectTransform 크기를 Scene 뷰에서 바로 조절한다.")]
        [SerializeField] private Button clickArea;

        [Tooltip("봉투 위의 '클릭' 안내. 봉투를 누를 수 있을 때만 깜박인다.")]
        [SerializeField] private CanvasGroup hint;

        [Header("서류 팝업")]
        [Tooltip("서류가 다 올라온 뒤 팝업이 뜨기까지 기다리는 시간(초).")]
        [SerializeField] private float popupDelay = 0.25f;

        [Tooltip("팝업에 띄울 서류 그림. 비어 있으면 아래 안내 글이 대신 뜬다.")]
        [SerializeField] private Sprite popupImage;

        [Tooltip("그림이 들어오기 전까지 팝업에 띄울 안내 글.")]
        [TextArea(3, 12)]
        [SerializeField] private string popupPlaceholder;

        private bool _clicked;
        private bool _wired;

        public ComicCutView Document => document;

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            if (clickArea != null) clickArea.onClick.AddListener(() => _clicked = true);
        }

        public void Prepare()
        {
            Wire();
            if (document != null) document.SetShift(insideOffset);
            SetClickable(false);
        }

        public IEnumerator Run(IComicDirector d)
        {
            Wire();
            _clicked = false;
            SetClickable(true);

            var t = 0f;
            while (!_clicked && !d.Skipping)
            {
                if (hint != null)
                {
                    t += Time.unscaledDeltaTime;
                    hint.alpha = 0.55f + 0.45f * Mathf.Sin(t * 4f);
                }
                yield return null;
            }
            SetClickable(false);
            if (d.Skipping) yield break;

            // 서류를 꺼낸다.
            if (document != null)
            {
                var e = 0f;
                while (e < pullSeconds)
                {
                    if (d.Skipping) yield break;
                    if (!d.Paused) e += Time.unscaledDeltaTime;
                    var k = ComicEase.Evaluate(pullEase, pullSeconds <= 0f ? 1f : e / pullSeconds);
                    document.SetShift(Vector2.LerpUnclamped(insideOffset, Vector2.zero, k));
                    yield return null;
                }
                document.SetShift(Vector2.zero);
            }

            var w = 0f;
            while (w < popupDelay && !d.Skipping) { if (!d.Paused) w += Time.unscaledDeltaTime; yield return null; }
            if (d.Skipping) yield break;

            yield return d.ShowPopup(popupImage, popupPlaceholder);
        }

        private void SetClickable(bool on)
        {
            if (clickArea != null) clickArea.gameObject.SetActive(on);
            if (hint != null)
            {
                hint.gameObject.SetActive(on);
                hint.alpha = on ? 1f : 0f;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() => DrawInside();

        /// <summary>서류의 봉투 안 자리를 하늘색 상자로 그린다.</summary>
        public void DrawInside()
        {
            if (document == null) return;
            var rt = document.transform as RectTransform;
            if (rt == null) return;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var lossy = rt.lossyScale;
            var shift = new Vector3(insideOffset.x * lossy.x, insideOffset.y * lossy.y, 0f);
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(corners[i] + shift, corners[(i + 1) % 4] + shift);
        }
#endif
    }
}
