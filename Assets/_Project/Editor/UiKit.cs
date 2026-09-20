using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using P = ItIsYou.EditorTools.Layout.Palette;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 씬 빌더들이 공유하는 조립 헬퍼.
    ///
    /// 화면마다 캔버스를 손으로 다시 세우다 보면 스케일러 설정이나 입력 모듈이
    /// 한 씬에서만 달라지고, 그 차이는 꼭 나중에 "왜 이 화면만 클릭이 안 되지"로 돌아온다.
    /// 그래서 캔버스·카메라·이벤트 시스템을 여기 한 곳에서만 만든다.
    /// </summary>
    public static class UiKit
    {
        private static TMP_FontAsset _font;
        private static Sprite _uiSprite;

        /// <summary>씬을 만들기 전에 한 번 부른다. 글꼴과 기본 스프라이트를 준비한다.</summary>
        public static void Prepare()
        {
            _font = FontSetup.Ensure();
            _uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        public static TMP_FontAsset Font => _font;
        public static Sprite UiSprite => _uiSprite;

        /// <summary>카메라 + 이벤트 시스템 + 캔버스를 세우고 캔버스의 RectTransform 을 돌려준다.</summary>
        public static RectTransform Boot(Color background)
        {
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.orthographic = true;
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            // 이 프로젝트는 새 Input System 을 쓴다. 구형 모듈을 붙이면 클릭이 전부 죽는다.
            new GameObject("EventSystem", typeof(EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Layout.Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvasGo.transform as RectTransform;
        }

        public static Vector2 V(float x, float y) => new Vector2(x, y);

        public static GameObject UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public static Image Img(string name, Transform parent, Color color)
        {
            var go = UI(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        /// <summary>글자는 언제나 자기 전용 오브젝트에 만든다. 한 오브젝트에 Graphic 은 하나뿐이다.</summary>
        public static TMP_Text Text(string name, Transform parent, string content, float size,
            Color color, TextAlignmentOptions align)
        {
            var go = UI(name, parent);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            if (_font != null) t.font = _font;
            return t;
        }

        /// <summary>구획 제목처럼 자간을 넓혀 쓰는 작은 라벨.</summary>
        public static void Spaced(TMP_Text t)
        {
            t.characterSpacing = 10f;
            t.fontStyle = FontStyles.UpperCase;
        }

        public static (RectTransform rect, Button button, TMP_Text label) Button(
            string name, Transform parent, string label, float size)
        {
            var go = UI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = P.Surface;
            img.sprite = _uiSprite;
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            var colors = button.colors;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            var text = Text("Label", rt, label, size, P.Ink, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            return (rt, button, text);
        }

        public static void Stretch(RectTransform rt)
            => Anchor(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        public static void Anchor(RectTransform rt, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
