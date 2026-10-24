using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.Characters
{
    /// <summary>
    /// Live2D 모델을 UI 에 띄우는 무대.
    ///
    /// 모델은 화면 밖(이 오브젝트 아래)에 서 있고, 전용 카메라가 모델만 찍어 투명 배경의 그림(RenderTexture)으로 만든다.
    /// 화면의 RawImage 들(대화 화면 · 심문 화면의 인물 자리)이 그 그림을 보여 준다 — UI 순서(앞뒤) 안에 자연스럽게 섞인다.
    /// 카메라는 시작할 때 모델 전체(머리~발)가 들어오게 맞춘다.
    /// </summary>
    [AddComponentMenu("It is You/Live2D Stage")]
    public class Live2DStage : MonoBehaviour
    {
        [SerializeField] private Camera stageCamera;
        [Tooltip("찍을 모델(맨 위). 비우면 자식에서 찾는다.")]
        [SerializeField] private Transform model;
        [Tooltip("이 그림을 보여 줄 RawImage 들.")]
        [SerializeField] private List<RawImage> outputs = new List<RawImage>();

        [Tooltip("그림 세로 해상도(px). 화면에서 인물이 차지하는 높이의 1.5~2배면 충분하다.")]
        [SerializeField] private int textureHeight = 1600;
        [Tooltip("모델 둘레 여백(모델 높이에 대한 비율).")]
        [SerializeField] private float margin = 0.03f;

        private RenderTexture _rt;

        public RenderTexture Texture => _rt;

        private IEnumerator Start()
        {
            if (stageCamera == null) stageCamera = GetComponentInChildren<Camera>(true);
            if (stageCamera == null) yield break;
            stageCamera.enabled = false;

            // 모델의 메시는 첫 갱신 뒤에 자리를 잡는다 — 두 프레임 기다렸다가 크기를 잰다.
            yield return null;
            yield return null;

            var bounds = ModelBounds();
            var aspect = bounds.size.y > 0.0001f ? bounds.size.x / bounds.size.y : 0.6f;
            Debug.Log($"[Live2DStage] 찍을 범위 {bounds.center} 크기 {bounds.size}");
            var h = Mathf.Max(64, textureHeight);
            var w = Mathf.Max(64, Mathf.RoundToInt(h * aspect));

            // 화면에서는 보통 그림보다 작게 보인다 — 밉맵을 만들어 줄여 보일 때 거칠어지지 않게 한다.
            _rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
            {
                name = "Live2DStage", antiAliasing = 1,
                useMipMap = true, autoGenerateMips = true, filterMode = FilterMode.Trilinear
            };
            _rt.Create();

            stageCamera.orthographic = true;
            stageCamera.clearFlags = CameraClearFlags.SolidColor;
            stageCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            stageCamera.targetTexture = _rt;
            stageCamera.aspect = (float)w / h;
            var c = bounds.center;
            stageCamera.transform.position = new Vector3(c.x, c.y, c.z - 10f);
            stageCamera.orthographicSize = bounds.extents.y * (1f + margin * 2f);
            stageCamera.enabled = true;

            _aspect = (float)w / h;
            foreach (var raw in outputs)
            {
                if (raw == null) continue;
                raw.texture = _rt;
                raw.color = Color.white;
                raw.enabled = true;
            }
            FitOutputs();
        }

        private float _aspect = -1f;
        private readonly Dictionary<RawImage, Vector4> _fittedFor = new Dictionary<RawImage, Vector4>();

        private void LateUpdate()
        {
            if (_aspect > 0f) FitOutputs();
        }

        /// <summary>
        /// 그림을 인물 자리(부모 판) 안에 비율을 지켜 넣는다 — 가로 가운데, 아래는 바닥에. 자리 크기가 바뀌면 다시 맞춘다.
        /// 그림(RawImage)에 <see cref="Live2DFit"/> 가 있으면 그 값만큼 키우고(zoom) 아래로 내린다(sink) —
        /// 모델이 허벅지에서 잘려 있으므로, 잘린 선이 화면 아래로 빠지게 할 때 쓴다.
        /// 자리 · 크기는 부모 판(Sepi_Placeholder / Character_Sepi)을 UI 편집기로 옮기면 된다.
        /// </summary>
        private void FitOutputs()
        {
            foreach (var raw in outputs)
            {
                if (raw == null) continue;
                var parent = raw.rectTransform.parent as RectTransform;
                if (parent == null) continue;
                var size = parent.rect.size;
                var fit = raw.GetComponent<Live2DFit>();
                var zoom = fit != null ? Mathf.Max(0.1f, fit.Zoom) : 1f;
                var sink = fit != null ? fit.Sink : 0f;
                var key = new Vector4(size.x, size.y, zoom, sink + (fit != null ? fit.OffsetX * 1000f : 0f));
                if (_fittedFor.TryGetValue(raw, out var last) && last == key) continue;
                _fittedFor[raw] = key;

                var w = size.x; var h = w / _aspect;
                if (h > size.y) { h = size.y; w = h * _aspect; }
                w *= zoom; h *= zoom;
                var rt = raw.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(w, h);
                rt.anchoredPosition = new Vector2(fit != null ? fit.OffsetX : 0f, -sink * h);
            }
        }

        /// <summary>
        /// 찍을 범위. 모델의 캔버스(Cubism 에디터에서 정한 그림판 크기)를 먼저 쓰고, 없으면 메시 크기를 잰다.
        /// 빌드에서는 시작 직후 메시 크기가 0 으로 잡혀 카메라가 빈 곳을 찍는 일이 있어 캔버스를 기준으로 삼는다(2026-10-24 2차).
        /// </summary>
        private Bounds ModelBounds()
        {
#if LIVE2D_CUBISM
            var cubism = (model != null ? model : transform).GetComponentInChildren<Live2D.Cubism.Core.CubismModel>(true);
            var info = cubism != null ? cubism.CanvasInformation : null;
            if (info != null && info.PixelsPerUnit > 0f && info.CanvasWidth > 0f && info.CanvasHeight > 0f)
            {
                var ppu = info.PixelsPerUnit;
                var w = info.CanvasWidth / ppu;
                var h = info.CanvasHeight / ppu;
                // 원점(캔버스 왼쪽 위에서 잰 px)이 모델 위치다. 위가 +y.
                var left = -info.CanvasOriginX / ppu;
                var top = info.CanvasOriginY / ppu;
                var t = cubism.transform;
                var localCenter = new Vector3(left + w * 0.5f, top - h * 0.5f, 0f);
                var center = t.TransformPoint(localCenter);
                var scale = t.lossyScale;
                return new Bounds(center, new Vector3(w * Mathf.Abs(scale.x), h * Mathf.Abs(scale.y), 0.01f));
            }
#endif
            return RendererBounds();
        }

        private Bounds RendererBounds()
        {
            var root = model != null ? model : transform;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var has = false;
            var b = new Bounds(root.position, Vector3.one);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }

        private void OnDestroy()
        {
            if (stageCamera != null) stageCamera.targetTexture = null;
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
        }
    }
}
