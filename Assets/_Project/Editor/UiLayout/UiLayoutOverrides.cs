using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.EditorTools
{
    /// <summary>바꿀 수 있는 항목. 한 오브젝트에서 바뀐 것만 켜진다.</summary>
    [Flags]
    public enum UiProp
    {
        None = 0,

        // ---- 자리 · 크기 (Rect Transform) ----
        Anchors = 1 << 0,
        Pivot = 1 << 1,
        Position = 1 << 2,
        Size = 1 << 3,
        Scale = 1 << 4,
        Rotation = 1 << 5,

        // ---- 이미지 (Image) ----
        Sprite = 1 << 6,
        ImageColor = 1 << 7,
        PreserveAspect = 1 << 8,
        ImageType = 1 << 9,

        // ---- 글자 모양 (TextMeshPro) — 글 내용은 다루지 않는다 ----
        Font = 1 << 10,
        FontSize = 1 << 11,
        TextColor = 1 << 12,
        Alignment = 1 << 13,
        FontStyle = 1 << 14,
        CharSpacing = 1 << 15,
        LineSpacing = 1 << 16,

        // ---- 자동 정렬 묶음 (Layout Group / Layout Element) ----
        LayoutGroup = 1 << 17,
        LayoutElement = 1 << 18,

        Rect = Anchors | Pivot | Position | Size | Scale | Rotation,
        Image = Sprite | ImageColor | PreserveAspect | ImageType,
        Text = Font | FontSize | TextColor | Alignment | FontStyle | CharSpacing | LineSpacing
    }

    /// <summary>한 오브젝트의 화면 값. 기준(빌더가 만든 값)과 바꾼 값 모두 이 모양으로 담는다.</summary>
    [Serializable]
    public class UiValues
    {
        public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta;
        public Vector3 scale = Vector3.one;
        public float rotationZ;

        public bool hasImage;
        public Sprite sprite;
        public Color imageColor = Color.white;
        public bool preserveAspect;
        public Image.Type imageType;

        public bool hasText;
        public TMP_FontAsset font;
        public float fontSize;
        public Color textColor = Color.white;
        public TextAlignmentOptions alignment;
        public FontStyles fontStyle;
        public float characterSpacing;
        public float lineSpacing;

        public bool hasLayoutGroup;
        /// <summary>왼쪽 · 오른쪽 · 위 · 아래 안쪽 여백.</summary>
        public Vector4 padding;
        /// <summary>칸 사이 간격. 가로/세로 묶음은 x 만 쓴다.</summary>
        public Vector2 spacing;
        /// <summary>격자 묶음의 칸 크기.</summary>
        public Vector2 cellSize;
        public TextAnchor childAlignment;

        public bool hasLayoutElement;
        public Vector2 minSize, preferredSize, flexibleSize;

        public UiValues Clone() => (UiValues)MemberwiseClone();
    }

    /// <summary>오브젝트 하나에서 바꾼 것.</summary>
    [Serializable]
    public class UiEntry
    {
        [Tooltip("씬(또는 프리팹) 안의 위치. 예: Canvas/CinematicDialogue/Body")]
        public string path;
        public UiProp props;
        public UiValues values = new UiValues();
    }

    /// <summary>한 부모 아래 자식들의 앞뒤 순서를 바꾼 것. 목록 뒤쪽일수록 화면 앞에 보인다.</summary>
    [Serializable]
    public class UiOrder
    {
        public string parentPath;
        public List<string> children = new List<string>();
    }

    /// <summary>씬 하나(또는 프리팹 하나)에서 바꾼 것 전부.</summary>
    [Serializable]
    public class UiTarget
    {
        [Tooltip("Assets/... 씬 또는 프리팹 경로")]
        public string key;
        public List<UiEntry> entries = new List<UiEntry>();
        public List<UiOrder> orders = new List<UiOrder>();

        public bool IsEmpty => entries.Count == 0 && orders.Count == 0;
    }

    /// <summary>
    /// UI 편집기에서 바꾼 자리 · 크기 · 이미지 · 글자 모양 · 앞뒤 순서를 모아 두는 곳.
    ///
    /// 씬은 메뉴 5 가 코드로 다시 만든다. 씬에서 손으로 고친 값은 그때 사라지므로,
    /// 씬을 저장(Ctrl+S)할 때 '빌더가 만든 값과 다른 것'만 골라 여기에 적어 두고,
    /// 빌더가 씬을 다 만든 뒤 마지막에 여기 적힌 값을 다시 입힌다.
    ///
    /// 직접 고칠 필요는 없다 — 메뉴 It is You › UI 편집기 에서 보고 되돌린다.
    /// </summary>
    public class UiLayoutOverrides : ScriptableObject
    {
        public List<UiTarget> targets = new List<UiTarget>();

        public UiTarget Find(string key)
        {
            for (int i = 0; i < targets.Count; i++)
                if (targets[i].key == key) return targets[i];
            return null;
        }

        public UiTarget GetOrAdd(string key)
        {
            var t = Find(key);
            if (t != null) return t;
            t = new UiTarget { key = key };
            targets.Add(t);
            return t;
        }
    }
}
