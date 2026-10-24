using UnityEngine;

namespace ItIsYou.Characters
{
    /// <summary>
    /// Live2D 그림(RawImage)을 인물 자리 안에 어떻게 놓을지 — 크기 배율과 아래로 내리는 양.
    /// 세피 모델은 허벅지에서 잘려 있어서, 잘린 선이 화면 안에 보이지 않도록 조금 키워 아래로 내린다.
    /// 플레이 중에 바꾸면 바로 반영된다(씬을 다시 만들면 빌더 값으로 돌아간다 — 바꾼 값은 ChapterSceneBuilder 에 적을 것).
    /// </summary>
    [AddComponentMenu("It is You/Live2D Fit")]
    [DisallowMultipleComponent]
    public class Live2DFit : MonoBehaviour
    {
        [Tooltip("자리에 꼭 맞춘 크기에 대한 배율. 1 = 자리 안에 다 들어감.")]
        [SerializeField] private float zoom = 1f;
        [Tooltip("그림 높이에 대한 비율만큼 아래로 내린다. 0.2 = 아래 20% 가 자리 밖으로.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float sink;
        [Tooltip("좌우로 옮기는 양(px, 1920×1080 기준).")]
        [SerializeField] private float offsetX;

        public float Zoom => zoom;
        public float Sink => sink;
        public float OffsetX => offsetX;
    }
}
