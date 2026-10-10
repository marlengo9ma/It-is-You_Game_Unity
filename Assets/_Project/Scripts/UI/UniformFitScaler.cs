using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>
    /// 기준 크기(1920×1080)로 짠 판을, 부모 안에 비율을 지킨 채 통째로 맞춘다.
    ///
    /// 추론 서류철처럼 그림 한 장처럼 배치된 화면은 요소마다 앵커를 따로 잡으면
    /// 창 비율이 바뀔 때(16:9 ↔ 4:3) 서류와 선과 탭이 제각각 밀린다.
    /// 판 하나를 기준 크기 그대로 두고 크기만 줄였다 늘리면 안의 배치는 그대로다.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("It is You/Uniform Fit Scaler")]
    [RequireComponent(typeof(RectTransform))]
    public class UniformFitScaler : MonoBehaviour
    {
        [SerializeField] private Vector2 reference = new Vector2(1920f, 1080f);

        private RectTransform _rt;
        private Vector2 _lastParent;

        private void OnEnable()
        {
            _rt = transform as RectTransform;
            _lastParent = Vector2.zero;
            Apply();
        }

        private void Update() => Apply();

        private void Apply()
        {
            if (_rt == null) _rt = transform as RectTransform;
            var parent = _rt.parent as RectTransform;
            if (parent == null) return;

            var size = parent.rect.size;
            if (size == _lastParent) return;
            _lastParent = size;

            _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _rt.anchoredPosition = Vector2.zero;
            _rt.sizeDelta = reference;

            var s = Mathf.Min(size.x / Mathf.Max(1f, reference.x), size.y / Mathf.Max(1f, reference.y));
            if (s <= 0f) s = 1f;
            _rt.localScale = new Vector3(s, s, 1f);
        }
    }
}
