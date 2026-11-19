using ItIsYou.Data;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using P = ItIsYou.EditorTools.Layout.Palette;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 아트 세트의 'UI 스킨' 칸(버튼 · 창 · 구분선 · 슬라이더 · 아이콘 …)을 빌더가 만든 UI 에 입힌다.
    ///
    /// 칸이 비어 있으면 아무것도 하지 않는다 — 예전의 단색 판이 그대로 남는다.
    /// 씬 빌더는 저장 직전에 <see cref="ApplyButtons"/> 를 한 번 부른다. 그 뒤에 UI 편집기 기록이 입혀지므로
    /// 제작자가 UI 편집기로 바꾼 이미지가 언제나 이긴다.
    /// </summary>
    public static class UiSkin
    {
        public const string ArtSetPath = "Assets/_Project/Art/ArtSet.asset";

        /// <summary>와인색 강조 버튼(돌아가기 · 추리를 마친다 · 추론 진행하기 · 교체 취소).</summary>
        public static readonly Color Accent = new Color32(0x5A, 0x2E, 0x34, 0xFF);

        private static ArtSet _art;
        private static Sprite _builtin;

        public static ArtSet Art
        {
            get
            {
                if (_art == null) _art = AssetDatabase.LoadAssetAtPath<ArtSet>(ArtSetPath);
                return _art;
            }
        }

        private static Sprite Builtin
        {
            get
            {
                if (_builtin == null) _builtin = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                return _builtin;
            }
        }

        /// <summary>빌드를 시작할 때 부른다 — 아트 세트를 다시 읽는다.</summary>
        public static void Reload() => _art = null;

        // ------------------------------------------------------------ 버튼

        /// <summary>
        /// root 아래의 '기본 버튼'(단색 판 + 기본 스프라이트)을 찾아 스킨을 입힌다.
        /// 판 색이 기본 판 색이면 기본 버튼, 와인색이면 강조 버튼. 그 밖의 버튼(아이콘 · 투명 · 특수 색)은 건드리지 않는다.
        /// </summary>
        public static int ApplyButtons(Transform root)
        {
            if (root == null || Art == null) return 0;
            var count = 0;
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                var img = b.targetGraphic as Image;
                if (img == null || img.sprite != Builtin) continue;
                if (Near(img.color, P.Surface)) { if (SkinButton(b, false)) count++; }
                else if (Near(img.color, Accent)) { if (SkinButton(b, true)) count++; }
            }
            return count;
        }

        /// <summary>버튼 하나에 기본/강조 스킨을 입힌다. 그림이 없으면 false.</summary>
        public static bool SkinButton(Button button, bool accent)
        {
            var a = Art;
            if (a == null || button == null) return false;
            var normal = accent ? a.accentNormal : a.buttonNormal;
            if (normal == null) return false;
            var img = button.targetGraphic as Image;
            if (img == null) return false;

            img.sprite = normal;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = Or(accent ? a.accentHover : a.buttonHover, normal),
                pressedSprite = Or(accent ? a.accentPressed : a.buttonPressed, normal),
                selectedSprite = normal,   // 누른 뒤 '선택됨'으로 남아 밝게 보이지 않게
                disabledSprite = Or(accent ? a.accentDisabled : a.buttonDisabled, normal),
            };
            return true;
        }

        // ------------------------------------------------------------ 창 · 부품

        /// <summary>창 판. small = 확인 팝업 · 일시 정지 같은 작은 창. 테두리(Outline)는 그림이 대신하므로 뺀다.</summary>
        public static bool Panel(Image img, bool small)
        {
            var s = Art == null ? null : (small ? Art.smallPanel : Art.windowPanel);
            if (s == null || img == null) return false;
            img.sprite = s;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            var outline = img.GetComponent<Outline>();
            if (outline != null) Object.DestroyImmediate(outline);
            return true;
        }

        /// <summary>제목 아래 구분선. 그림이 있으면 높이를 키워 양 끝 장식이 보이게 한다.</summary>
        public static bool Divider(Image img, float height = 20f)
        {
            var s = Art == null ? null : Art.divider;
            if (s == null || img == null) return false;
            img.sprite = s;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            var rt = img.rectTransform;
            var mid = (rt.offsetMin.y + rt.offsetMax.y) * 0.5f;
            rt.offsetMin = new Vector2(rt.offsetMin.x, mid - height * 0.5f);
            rt.offsetMax = new Vector2(rt.offsetMax.x, mid + height * 0.5f);
            return true;
        }

        /// <summary>슬라이더 — 홈 · 채움 · 손잡이.</summary>
        public static void Slider(Slider slider)
        {
            var a = Art;
            if (a == null || slider == null) return;
            var bg = slider.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null && a.sliderTrack != null)
            {
                bg.sprite = a.sliderTrack; bg.type = Image.Type.Sliced; bg.color = Color.white;
                var rt = bg.rectTransform; rt.offsetMin = new Vector2(rt.offsetMin.x, -8f); rt.offsetMax = new Vector2(rt.offsetMax.x, 8f);
            }
            var fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
            if (fill != null && a.sliderFill != null)
            {
                fill.sprite = a.sliderFill; fill.type = Image.Type.Sliced; fill.color = Color.white;
                var area = slider.fillRect.parent as RectTransform;
                if (area != null) { area.offsetMin = new Vector2(area.offsetMin.x, -8f); area.offsetMax = new Vector2(area.offsetMax.x, 8f); }
            }
            var handle = slider.handleRect != null ? slider.handleRect.GetComponent<Image>() : null;
            if (handle != null && a.sliderKnob != null)
            {
                handle.sprite = a.sliderKnob; handle.type = Image.Type.Simple; handle.color = Color.white;
                handle.preserveAspect = true;
                slider.handleRect.sizeDelta = new Vector2(28f, 28f);
            }
        }

        /// <summary>‹ › 글자 버튼을 아이콘으로. color 는 아이콘 색(흰 아이콘에 입혀진다).</summary>
        public static bool ArrowIcon(Button button, bool next, Color color)
        {
            var s = Art == null ? null : (next ? Art.iconNext : Art.iconPrev);
            if (s == null || button == null) return false;
            var img = button.targetGraphic as Image;
            if (img == null) return false;
            img.sprite = s; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = color;
            button.transition = Selectable.Transition.ColorTint;
            var c = button.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            c.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            c.selectedColor = Color.white;
            c.disabledColor = new Color(1f, 1f, 1f, 0.3f);
            button.colors = c;
            var label = button.transform.Find("Label");
            if (label != null) label.gameObject.SetActive(false);
            return true;
        }

        /// <summary>
        /// ▼ 표시를 글자 대신 그림으로 만든다. 그림이 없으면 null — 그때는 예전처럼 글자를 쓴다.
        /// 오른쪽 아래 모서리를 기준으로 size 크기.
        /// </summary>
        public static Image AdvanceArrow(string name, Transform parent, Color color, float size = 22f)
        {
            var s = Art == null ? null : Art.advanceArrow;
            if (s == null) return null;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = s; img.preserveAspect = true; img.color = color; img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(size, size);
            return img;
        }

        private static Sprite Or(Sprite a, Sprite b) => a != null ? a : b;

        /// <summary>두 색이 거의 같은가.</summary>
        public static bool Near(Color a, Color b)
            => Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f && Mathf.Abs(a.a - b.a) < 0.02f;
    }
}
