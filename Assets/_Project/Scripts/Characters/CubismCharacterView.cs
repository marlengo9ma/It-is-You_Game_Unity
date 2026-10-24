using ItIsYou.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.Characters
{
    /// <summary>
    /// 심문 화면의 Live2D 인물 (ICharacterView).
    ///
    /// 화면에는 <see cref="Live2DStage"/> 가 찍은 그림(RawImage)이 보이고, 모션은 모델 쪽 <see cref="Live2DActor"/> 가 돌린다.
    /// 이 컴포넌트는 심문 규칙이 부르는 것을 Actor 에 넘기고(연출 · 리액션), 리액션 포착 테두리와 기억 왜곡만 직접 맡는다.
    /// 대사 줄마다의 모션은 Actor 가 대사창에서 직접 받으므로 표정(SetExpression)은 여기서 다시 넘기지 않는다.
    /// </summary>
    [AddComponentMenu("It is You/Cubism Character View")]
    public class CubismCharacterView : MonoBehaviour, ICharacterView
    {
        [SerializeField] private string characterId = "SEPI";
        [SerializeField] private Live2DActor actor;

        [Tooltip("리액션을 포착할 수 있을 때 켜는 테두리.")]
        [SerializeField] private Outline reactionOutline;

        [Tooltip("기억 짐작 왜곡 — 흐려질수록 투명해진다.")]
        [SerializeField] private CanvasGroup canvasGroup;

        public string CharacterId => characterId;

        public void SetExpression(ExpressionId expression) { }

        public void PlayReaction(ExpressionId expression)
        {
            if (actor != null) actor.PlayReaction();
        }

        public void PlayAnimation(string animationId)
        {
            if (actor != null) actor.Play(animationId);
        }

        /// <summary>Live2D 는 특수 모션이 끝나면 저절로 기본 모션으로 돌아온다.</summary>
        public void ResetPose() { }

        public void SetReactionCollectable(bool collectable)
        {
            if (reactionOutline != null) reactionOutline.enabled = collectable;
        }

        public void SetRecallDistortion(float blur, float silhouetteFade)
        {
            if (canvasGroup != null) canvasGroup.alpha = 1f - Mathf.Clamp01(silhouetteFade) * 0.85f;
        }

        public void SetSpeaking(bool speaking) { }
    }
}
