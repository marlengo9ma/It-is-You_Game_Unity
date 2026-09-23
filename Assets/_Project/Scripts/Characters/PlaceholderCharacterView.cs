using System;
using System.Collections.Generic;
using ItIsYou.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.Characters
{
    /// <summary>
    /// 아트가 들어오기 전까지 쓰는 캐릭터 뷰.
    ///
    /// 표정마다 스프라이트를 하나씩 갈아 끼우고, 없으면 색만 바꾼다.
    /// 이 단계에서 중요한 건 그림이 아니라 "리액션 창이 언제 열리고 닫히는지"를
    /// 눈으로 확인하는 것이라, 수집 가능 상태를 테두리로 드러낸다.
    /// </summary>
    [AddComponentMenu("It is You/Placeholder Character View")]
    public class PlaceholderCharacterView : MonoBehaviour, ICharacterView
    {
        [Serializable]
        private struct ExpressionSprite
        {
            public ExpressionId expression;
            public Sprite sprite;
            public Color tint;
        }

        [SerializeField] private string characterId;
        [SerializeField] private Image body;
        [SerializeField] private Outline reactionOutline;
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private List<ExpressionSprite> expressions = new List<ExpressionSprite>();

        [Tooltip("켜면 진짜 그림이 들어온 것으로 보고, 등록되지 않은 표정을 색으로 물들이지 않는다 (기본 그림 그대로).")]
        [SerializeField] private bool hasArt;
        [SerializeField] private Sprite defaultSprite;

        [Header("연출")]
        [SerializeField] private float reactionPunchScale = 1.06f;
        [SerializeField] private float reactionPunchSeconds = 0.18f;
        [SerializeField] private Color reactionOutlineColor = new Color(0.79f, 0.64f, 0.15f, 1f);

        private ExpressionId _current = ExpressionId.Neutral;
        private Coroutine _punch;
        private Vector3 _baseScale;

        public string CharacterId => characterId;

        private void Awake()
        {
            _baseScale = transform.localScale;
            if (reactionOutline != null)
            {
                reactionOutline.effectColor = reactionOutlineColor;
                reactionOutline.enabled = false;
            }
        }

        public void SetExpression(ExpressionId expression)
        {
            _current = expression;
            Apply(expression);
        }

        public void PlayReaction(ExpressionId expression)
        {
            Apply(expression);

            if (!isActiveAndEnabled) return;
            if (_punch != null) StopCoroutine(_punch);
            _punch = StartCoroutine(PunchRoutine());
        }

        public void SetReactionCollectable(bool collectable)
        {
            if (reactionOutline != null) reactionOutline.enabled = collectable;
        }

        public void SetRecallDistortion(float blur, float silhouetteFade)
        {
            // 플레이스홀더에서는 셰이더 대신 투명도와 명도로 흉내만 낸다.
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(silhouetteFade));

            if (body != null)
            {
                var c = body.color;
                var grey = Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(blur));
                body.color = new Color(c.r * grey, c.g * grey, c.b * grey, c.a);
            }
        }

        public void SetSpeaking(bool speaking)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = speaking ? 1f : 0.85f;
        }

        private void Apply(ExpressionId expression)
        {
            if (body == null) return;

            for (int i = 0; i < expressions.Count; i++)
            {
                if (expressions[i].expression != expression) continue;
                if (expressions[i].sprite != null) body.sprite = expressions[i].sprite;
                if (expressions[i].tint.a > 0f) body.color = expressions[i].tint;
                return;
            }

            if (hasArt)
            {
                // 진짜 그림이 있으면 없는 표정은 기본 그림으로. 색을 입히면 그림이 망가진다.
                if (defaultSprite != null) body.sprite = defaultSprite;
                body.color = Color.white;
                return;
            }

            // 등록되지 않은 표정은 색으로만 구분해, 데이터가 비어 있어도 흐름을 볼 수 있게 한다.
            body.color = FallbackTint(expression);
        }

        private static Color FallbackTint(ExpressionId expression) => expression switch
        {
            ExpressionId.Smile => new Color(0.95f, 0.90f, 0.75f),
            ExpressionId.Nervous => new Color(0.85f, 0.88f, 0.72f),
            ExpressionId.Flustered => new Color(0.92f, 0.78f, 0.72f),
            ExpressionId.Angry => new Color(0.85f, 0.55f, 0.50f),
            ExpressionId.Sad => new Color(0.70f, 0.76f, 0.86f),
            ExpressionId.Surprised => new Color(0.95f, 0.95f, 0.85f),
            ExpressionId.Fearful => new Color(0.72f, 0.72f, 0.84f),
            ExpressionId.Thinking => new Color(0.82f, 0.82f, 0.80f),
            ExpressionId.Sly => new Color(0.88f, 0.82f, 0.66f),
            ExpressionId.Exhausted => new Color(0.62f, 0.62f, 0.64f),
            _ => Color.white
        };

        private System.Collections.IEnumerator PunchRoutine()
        {
            var t = 0f;
            while (t < reactionPunchSeconds)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / reactionPunchSeconds));
                transform.localScale = _baseScale * Mathf.Lerp(1f, reactionPunchScale, k);
                yield return null;
            }
            transform.localScale = _baseScale;
            _punch = null;
        }

        /// <summary>에디터에서 확인용. 현재 표정을 돌려준다.</summary>
        public ExpressionId CurrentExpression => _current;
    }
}
