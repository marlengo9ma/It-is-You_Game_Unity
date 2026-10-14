using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>
    /// 인트로 페이지 안의 컷 한 장 (또는 말풍선 한 개).
    ///
    /// 컷의 <b>자리와 크기</b>는 이 오브젝트의 RectTransform 이 그대로 정한다 — Scene 뷰에서 끌어 옮기고 늘이면 된다.
    /// 지금 놓인 자리가 "다 나온 뒤의 자리"이고, 등장할 때는 <see cref="motion"/> 방향으로
    /// <see cref="distance"/> 만큼 떨어진 곳에서 미끄러져 들어온다.
    ///
    /// <b>나오는 순서</b>는 <see cref="order"/> (파일 이름 Cx_y 의 y), <b>앞뒤 겹침</b>은 Hierarchy 순서다
    /// (아래에 있을수록 앞). 둘을 따로 두는 이유는, 나중에 나오는 컷이 뒤에 깔리는 경우가 있어서다
    /// (C1_3 은 세 번째로 나오지만 맨 뒤 배경).
    /// </summary>
    [AddComponentMenu("It is You/Comic Cut View")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ComicCutView : MonoBehaviour
    {
        [Tooltip("이 페이지에서 몇 번째로 나오는가 — 파일 이름 Cx_y 의 y. 같은 번호끼리는 함께 나온다.\n" +
                 "앞뒤 겹침은 이 번호가 아니라 Hierarchy 순서(아래일수록 앞)로 정한다.")]
        [SerializeField] private int order = 1;

        [Header("등장 애니메이션")]
        [Tooltip("어느 쪽에서 들어오는가.")]
        [SerializeField] private CutMotion motion = CutMotion.FromTop;

        [Tooltip("얼마나 먼 곳에서 출발하는가 (픽셀, 1920×1080 화면 기준). 0 이면 제자리에서 나타난다.")]
        [SerializeField] private float distance = 90f;

        [Tooltip("들어오는 데 걸리는 시간(초). 작을수록 빠르다.")]
        [SerializeField] private float duration = 0.55f;

        [Tooltip("앞 순번의 컷이 다 나온 뒤, 이 컷이 출발하기 전까지 기다리는 시간(초).")]
        [SerializeField] private float delay = 0.25f;

        [Tooltip("감속 곡선.")]
        [SerializeField] private CutEase ease = CutEase.EaseOutStrong;

        [Tooltip("들어오면서 투명 → 불투명으로 함께 나타나는가. 끄면 처음부터 진하게 보인 채로 미끄러진다.")]
        [SerializeField] private bool fade = true;

        [Tooltip("톡 튀어나옴(말풍선)일 때 시작 크기. 1 이 원래 크기.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float popStartScale = 0.6f;

        [Tooltip("켜면 화면을 한 번 눌러야 이 컷이 나온다. 끄면 앞 컷에 이어 저절로 나온다.")]
        [SerializeField] private bool waitForClick;

        private RectTransform _rt;
        private CanvasGroup _group;
        private Vector2 _home;
        private Vector2 _shift;
        private Vector3 _homeScale = Vector3.one;
        private bool _captured;

        public int Order => order;
        public float Duration => motion == CutMotion.Instant ? 0f : Mathf.Max(0f, duration);
        public float Delay => Mathf.Max(0f, delay);
        public bool WaitForClick => waitForClick;
        public CutMotion Motion => motion;

        /// <summary>다 나온 뒤의 자리 (편집기에서 놓아 둔 자리).</summary>
        public Vector2 Home { get { Capture(); return _home; } }

        public RectTransform Rect { get { Capture(); return _rt; } }

        private void Capture()
        {
            if (_captured) return;
            _rt = (RectTransform)transform;
            _home = _rt.anchoredPosition;
            _homeScale = _rt.localScale;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _captured = true;
        }

        /// <summary>
        /// 자리를 통째로 밀어 둔다. 봉투 속 서류처럼, 등장한 뒤에도 다른 자리에 있어야 하는 컷에 쓴다.
        /// </summary>
        public void SetShift(Vector2 shift)
        {
            Capture();
            _shift = shift;
            _rt.anchoredPosition = _home + _shift;
        }

        /// <summary>등장 전 — 보이지 않는 출발 자리에 둔다.</summary>
        public void ResetToStart()
        {
            Capture();
            Apply(0f);
            _group.alpha = 0f;
        }

        /// <summary>등장 진행도 0~1 을 화면에 적용한다.</summary>
        public void Apply(float progress)
        {
            Capture();
            var p = Mathf.Clamp01(progress);
            var e = ComicEase.Evaluate(ease, p);

            _rt.anchoredPosition = _home + _shift + StartOffset() * (1f - e);

            if (motion == CutMotion.Pop)
            {
                var s = Mathf.LerpUnclamped(popStartScale, 1f, e);
                _rt.localScale = new Vector3(_homeScale.x * s, _homeScale.y * s, _homeScale.z);
            }
            else
            {
                _rt.localScale = _homeScale;
            }

            var useFade = fade || motion == CutMotion.Fade || motion == CutMotion.Pop;
            _group.alpha = motion == CutMotion.Instant ? (p > 0f ? 1f : 0f)
                         : useFade ? Mathf.Clamp01(p * 1.6f)
                         : (p > 0f ? 1f : 0f);
        }

        /// <summary>다 나온 상태로 바로 둔다.</summary>
        public void Complete()
        {
            Apply(1f);
            _group.alpha = 1f;
        }

        private Vector2 StartOffset()
        {
            switch (motion)
            {
                case CutMotion.FromTop: return new Vector2(0f, distance);
                case CutMotion.FromBottom: return new Vector2(0f, -distance);
                case CutMotion.FromLeft: return new Vector2(-distance, 0f);
                case CutMotion.FromRight: return new Vector2(distance, 0f);
                default: return Vector2.zero;
            }
        }

#if UNITY_EDITOR
        // 컷을 고르면 출발 자리를 Scene 뷰에 점선 상자로 보여 준다 — 방향과 거리를 눈으로 맞추기 위해서.
        private void OnDrawGizmosSelected()
        {
            var rt = transform as RectTransform;
            if (rt == null || motion == CutMotion.Fade || motion == CutMotion.Instant || motion == CutMotion.Pop) return;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var lossy = rt.lossyScale;
            var o = StartOffset();
            var shift = new Vector3(o.x * lossy.x, o.y * lossy.y, 0f);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(corners[i] + shift, corners[(i + 1) % 4] + shift);
            var c = (corners[0] + corners[2]) * 0.5f;
            Gizmos.DrawLine(c + shift, c);
        }
#endif
    }
}
