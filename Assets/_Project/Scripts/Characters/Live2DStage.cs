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
            var h = Mathf.Max(64, textureHeight);
            var w = Mathf.Max(64, Mathf.RoundToInt(h * aspect));

            _rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "Live2DStage", antiAliasing = 1 };
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
        private readonly Dictionary<RawImage, Vector2> _fittedFor = new Dictionary<RawImage, Vector2>();

        private void LateUpdate()
        {
            if (_aspect > 0f) FitOutputs();
        }

        /// <summary>
        /// 그림을 인물 자리(부모 판) 안에 비율을 지켜 넣는다 — 가로 가운데, 발은 바닥에. 자리 크기가 바뀌면 다시 맞춘다.
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
                if (_fittedFor.TryGetValue(raw, out var last) && last == size) continue;
                _fittedFor[raw] = size;

                var w = size.x; var h = w / _aspect;
                if (h > size.y) { h = size.y; w = h * _aspect; }
                var rt = raw.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(w, h);
                rt.anchoredPosition = Vector2.zero;
            }
        }

        private Bounds ModelBounds()
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
