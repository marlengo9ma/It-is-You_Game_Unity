using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// AUTO 토글의 모양 — 알약 판 안의 구슬이 미끄러진다.
    ///
    /// · 꺼짐: 어두운 판, 밝은 구슬이 왼쪽, 'AUTO' 글자는 오른쪽.
    /// · 켜짐: 밝은 판, 어두운 구슬이 오른쪽으로 부드럽게 옮겨 가고, 글자는 왼쪽으로.
    /// 판 · 구슬 그림은 꺼짐/켜짐 두 장을 겹쳐 두고 켜짐 쪽을 서서히 나타나게 한다.
    /// 눌렀을 때 무엇을 하는지는 <see cref="DialogueToolbar"/> 가 정하고, 여기는 보이는 모양만 맡는다.
    /// </summary>
    [AddComponentMenu("It is You/Auto Toggle View")]
    public class AutoToggleView : MonoBehaviour
    {
        [Header("그림")]
        [Tooltip("켜짐 판(밝은 알약). 꺼짐 판 위에 겹쳐 두고 투명도로 바꾼다.")]
        [SerializeField] private Graphic trackOn;
        [Tooltip("움직이는 구슬(부모). 이 안에 꺼짐/켜짐 구슬 그림이 있다.")]
        [SerializeField] private RectTransform knob;
        [Tooltip("켜짐 구슬(어두운 구슬). 꺼짐 구슬 위에 겹쳐 둔다.")]
        [SerializeField] private Graphic knobOn;
        [SerializeField] private TMP_Text label;

        [Header("자리 (판 안의 x 위치, 판 가운데 기준 px)")]
        [Tooltip("꺼짐일 때 구슬 자리.")]
        [SerializeField] private float knobOffX = -52f;
        [Tooltip("켜짐일 때 구슬 자리.")]
        [SerializeField] private float knobOnX = 52f;
        [Tooltip("꺼짐일 때 글자 자리(구슬 반대편).")]
        [SerializeField] private float labelOffX = 16f;
        [Tooltip("켜짐일 때 글자 자리.")]
        [SerializeField] private float labelOnX = -16f;

        [Header("색")]
        [SerializeField] private Color labelOffColor = new Color32(0xEE, 0xE6, 0xD6, 0xFF);
        [SerializeField] private Color labelOnColor = new Color32(0x1B, 0x1D, 0x22, 0xFF);

        [Header("움직임")]
        [Tooltip("구슬이 옮겨 가는 데 걸리는 시간(초).")]
        [SerializeField] private float seconds = 0.22f;

        private float _k;        // 0 = 꺼짐, 1 = 켜짐
        private float _target;

        public bool IsOn => _target > 0.5f;

        /// <summary>켜짐/꺼짐. instant 면 움직임 없이 바로.</summary>
        public void SetOn(bool on, bool instant = false)
        {
            _target = on ? 1f : 0f;
            if (instant || !isActiveAndEnabled) _k = _target;
            Apply();
        }

        private void OnEnable()
        {
            _k = _target;
            Apply();
        }

        private void Update()
        {
            if (Mathf.Approximately(_k, _target)) return;
            _k = Mathf.MoveTowards(_k, _target, Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds));
            Apply();
        }

        private void Apply()
        {
            var e = _k * _k * (3f - 2f * _k);
            if (trackOn != null) SetAlpha(trackOn, e);
            if (knobOn != null) SetAlpha(knobOn, e);
            if (knob != null) knob.anchoredPosition = new Vector2(Mathf.Lerp(knobOffX, knobOnX, e), knob.anchoredPosition.y);
            if (label != null)
            {
                label.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(labelOffX, labelOnX, e), label.rectTransform.anchoredPosition.y);
                label.color = Color.Lerp(labelOffColor, labelOnColor, e);
            }
        }

        private static void SetAlpha(Graphic g, float a)
        {
            var c = g.color; c.a = a; g.color = c;
        }
    }
}
