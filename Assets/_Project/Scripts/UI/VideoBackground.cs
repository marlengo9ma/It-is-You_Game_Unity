using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ItIsYou.UI
{
    /// <summary>
    /// UI 위에 영상 배경을 깐다 (타이틀의 애니메이션 배경). 영상 크기의 RenderTexture 를 만들어
    /// VideoPlayer 가 그리고 RawImage 가 보여 준다. 영상은 아트 세트 › titleBackgroundVideo.
    /// </summary>
    [AddComponentMenu("It is You/Video Background")]
    [RequireComponent(typeof(RawImage), typeof(VideoPlayer))]
    public class VideoBackground : MonoBehaviour
    {
        private RenderTexture _rt;

        private void Awake()
        {
            var player = GetComponent<VideoPlayer>();
            var raw = GetComponent<RawImage>();
            if (player.clip == null) return;
            var w = (int)Mathf.Max(16, player.clip.width);
            var h = (int)Mathf.Max(16, player.clip.height);
            _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { name = "TitleVideo" };
            _rt.Create();
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = _rt;
            raw.texture = _rt;
            if (!player.isPlaying && player.playOnAwake) player.Play();
        }

        private void OnDestroy()
        {
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
        }
    }
}
