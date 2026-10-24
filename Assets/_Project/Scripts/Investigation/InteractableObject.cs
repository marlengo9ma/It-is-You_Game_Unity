using System;
using ItIsYou.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItIsYou.Investigation
{
    /// <summary>
    /// 화면에 실제로 놓이는 클릭 대상. 배경 위 핫스팟 하나에 해당한다.
    ///
    /// 기획서: "상호 작용이 가능한 오브젝트 마우스 오버 시 &gt;&gt; 아웃라인의 약한 강조와
    /// 마우스 커서의 변경(기존-펜촉 &gt; 깜빡이는 눈의 형태)"
    ///
    /// 일반/특수를 겉모습으로 구분하지 않는 것이 중요하다. 기획 피드백대로
    /// 구분은 수집 UI 안에서만 드러나야, 플레이어가 마우스만 굴려 특수 오브젝트를
    /// 골라내는 일이 생기지 않는다.
    /// </summary>
    [AddComponentMenu("It is You/Interactable Object")]
    [RequireComponent(typeof(RectTransform))]
    public class InteractableObject : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private InteractableDefinition definition;

        [Header("강조")]
        [Tooltip("마우스 오버 시 켜지는 약한 아웃라인. 일반/특수 구분 없이 동일해야 한다.")]
        [SerializeField] private Outline outline;

        [SerializeField] private Graphic hitArea;

        [Header("예비 표시 (2026-10-24)")]
        [Tooltip("물건 자리를 늘 보여 주는 테두리 · 색 판. 나중에 물건 일러스트에 스트로크를 넣으면 비우거나 끈다. 일반/특수 모두 같은 모양.")]
        [SerializeField] private Graphic marker;
        [SerializeField] private Color markerColor = new Color(1f, 0.82f, 0.45f, 0.85f);
        [Tooltip("마우스를 올렸을 때")]
        [SerializeField] private Color markerHoverColor = new Color(1f, 0.9f, 0.62f, 1f);
        [Tooltip("이미 살펴본 뒤")]
        [SerializeField] private Color markerInspectedColor = new Color(0.75f, 0.72f, 0.66f, 0.45f);

        [Tooltip("아웃라인 강조 세기. 기획상 '약한 강조'라 기본값을 낮게 둔다.")]
        [SerializeField] private Color outlineColor = new Color(1f, 0.97f, 0.88f, 0.55f);

        private bool _inspected;
        private bool _hovering;

        /// <summary>마우스가 올라오거나 내려갔다. (이 오브젝트, 올라왔는지)</summary>
        public event Action<InteractableObject, bool> HoverChanged;

        /// <summary>클릭됐다.</summary>
        public event Action<InteractableObject> Clicked;

        public InteractableDefinition Definition => definition;
        public bool Inspected => _inspected;
        public bool IsHovering => _hovering;

        private void Awake()
        {
            if (outline != null)
            {
                outline.effectColor = outlineColor;
                outline.enabled = false;
            }

            // 투명한 히트박스도 레이캐스트를 받아야 한다.
            if (hitArea != null) hitArea.raycastTarget = true;
            if (marker != null) marker.raycastTarget = false;
            RefreshMarker();
        }

        private void RefreshMarker()
        {
            if (marker == null) return;
            marker.color = _hovering ? markerHoverColor : _inspected ? markerInspectedColor : markerColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovering = true;
            if (outline != null) outline.enabled = true;
            RefreshMarker();
            HoverChanged?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
            if (outline != null) outline.enabled = false;
            RefreshMarker();
            HoverChanged?.Invoke(this, false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(this);
        }

        /// <summary>조사 완료로 표시한다. 재조사 대사를 쓸지 판단하는 데 쓴다.</summary>
        public void MarkInspected()
        {
            _inspected = true;
            RefreshMarker();
        }

        public void SetEnabled(bool value)
        {
            if (hitArea != null) hitArea.raycastTarget = value;
            if (!value && outline != null) outline.enabled = false;
        }
    }
}
