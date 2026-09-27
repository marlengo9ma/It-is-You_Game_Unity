using System;
using System.Collections;
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
    ///
    /// 연출(<see cref="PlayAnimation"/>)도 마찬가지로 흉내만 낸다. 진짜 모션이 아니라
    /// 위치·기울기·크기를 잠깐 흔드는 것뿐이지만, 어느 대사에 어떤 연출이 걸려 있는지는
    /// 그것만으로도 눈에 보인다. Live2D 가 붙으면 같은 이름의 모션으로 갈아끼운다.
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

        [Tooltip("연출의 움직임 폭을 한꺼번에 줄이거나 키운다. 0이면 연출이 꺼진다.")]
        [Range(0f, 2f)]
        [SerializeField] private float motionScale = 1f;

        private ExpressionId _current = ExpressionId.Neutral;
        private Coroutine _punch;
        private Coroutine _motion;
        private Vector3 _baseScale;
        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private bool _baseCaptured;

        public string CharacterId => characterId;

        private void Awake() => CaptureBase();

        private void CaptureBase()
        {
            if (_baseCaptured) return;
            _baseCaptured = true;
            _baseScale = transform.localScale;
            _basePosition = transform.localPosition;
            _baseRotation = transform.localRotation;
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

        // ------------------------------------------------------------- 연출

        /// <summary>
        /// 한 연출의 모양. 흔들림(주파수·폭), 밀어내기, 기울기, 크기, 시간.
        /// 이 다섯 가지 조합으로 시나리오의 괄호 지시문 대부분을 흉내낼 수 있다.
        /// </summary>
        private readonly struct Motion
        {
            public readonly float Seconds, ShakeHz, ShakeX, ShakeY, OffsetX, OffsetY, Tilt, Scale;
            public readonly bool Hold;   // 켜면 끝나도 원위치로 돌아가지 않고 그 자세로 머문다

            public Motion(float seconds, float shakeHz = 0f, float shakeX = 0f, float shakeY = 0f,
                float offsetX = 0f, float offsetY = 0f, float tilt = 0f, float scale = 1f, bool hold = false)
            {
                Seconds = seconds; ShakeHz = shakeHz; ShakeX = shakeX; ShakeY = shakeY;
                OffsetX = offsetX; OffsetY = offsetY; Tilt = tilt; Scale = scale; Hold = hold;
            }
        }

        /// <summary>이름 붙은 연출. 모르는 이름은 조용히 넘긴다 — 데이터를 미리 적어 둘 수 있게.</summary>
        public void PlayAnimation(string animationId)
        {
            if (string.IsNullOrEmpty(animationId) || motionScale <= 0f) return;
            if (!isActiveAndEnabled) return;
            if (!TryGetMotion(animationId, out var motion)) return;

            CaptureBase();
            if (_motion != null) StopCoroutine(_motion);
            _motion = StartCoroutine(MotionRoutine(motion));
        }

        private static bool TryGetMotion(string id, out Motion motion)
        {
            switch (id)
            {
                // (동공이 떨리는 모습) — 잔떨림
                case StageAnimation.Tremble:
                    motion = new Motion(0.9f, shakeHz: 22f, shakeX: 1.6f, shakeY: 0.8f); return true;

                // (살짝 눈을 피하며) — 옆으로 조금 물러나 그대로 있는다
                case StageAnimation.LookAway:
                    motion = new Motion(0.45f, offsetX: -10f, tilt: -1.5f, hold: true); return true;

                // (고양이 쪽을 힐끗 바라보며) — 봤다가 돌아온다
                case StageAnimation.GlanceCat:
                    motion = new Motion(0.75f, offsetX: 16f, tilt: 2.5f); return true;

                // (작은 한숨 후) — 내려갔다 올라온다
                case StageAnimation.Sigh:
                    motion = new Motion(0.8f, offsetY: -9f); return true;

                // (놀라며) — 짧고 크게 튄다
                case StageAnimation.Startle:
                    motion = new Motion(0.3f, offsetY: 14f, scale: 1.05f); return true;

                // (째려보며) — 앞으로 조금 기울인 채 멈춘다
                case StageAnimation.Glare:
                    motion = new Motion(0.6f, offsetY: -4f, scale: 1.03f, tilt: 1f, hold: true); return true;

                // (부들거리며) — 빠르고 작은 진동
                case StageAnimation.Fume:
                    motion = new Motion(1.1f, shakeHz: 30f, shakeX: 2.4f, shakeY: 1.2f); return true;

                // (어깨가 움츠러든다)
                case StageAnimation.Shrink:
                    motion = new Motion(0.5f, offsetY: -12f, scale: 0.96f, hold: true); return true;

                // (하얗게 질린다) — 아주 느리게 내려앉는다
                case StageAnimation.Pale:
                    motion = new Motion(1.0f, offsetY: -6f, scale: 0.98f, hold: true); return true;

                // (고개를 숙이며)
                case StageAnimation.Bow:
                    motion = new Motion(0.7f, offsetY: -16f, tilt: -4f); return true;

                // (눈을 감고 생각하는 듯) — 아주 느린 끄덕임
                case StageAnimation.Ponder:
                    motion = new Motion(1.4f, shakeHz: 0.8f, shakeY: 5f); return true;

                default:
                    motion = default; return false;
            }
        }

        private IEnumerator MotionRoutine(Motion m)
        {
            var k = Mathf.Max(0f, motionScale);
            var t = 0f;
            var seconds = Mathf.Max(0.01f, m.Seconds);

            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                var p = Mathf.Clamp01(t / seconds);

                // hold 는 들어가서 머물고, 나머지는 갔다가 돌아온다.
                var envelope = m.Hold ? Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, p * 3f))
                                      : Mathf.Sin(Mathf.PI * p);

                var shake = m.ShakeHz > 0f ? Mathf.Sin(t * m.ShakeHz * Mathf.PI * 2f) : 0f;
                var decay = m.Hold ? 1f : envelope;

                transform.localPosition = _basePosition + new Vector3(
                    (m.OffsetX * envelope + m.ShakeX * shake * decay) * k,
                    (m.OffsetY * envelope + m.ShakeY * shake * decay) * k,
                    0f);
                transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, m.Tilt * envelope * k);
                transform.localScale = _baseScale * Mathf.Lerp(1f, m.Scale, envelope * k);

                yield return null;
            }

            if (!m.Hold) ResetPose();
            _motion = null;
        }

        /// <summary>연출로 어긋난 자세를 원래대로. 대사 묶음이 끝날 때 불러 준다.</summary>
        public void ResetPose()
        {
            CaptureBase();
            if (_motion != null) { StopCoroutine(_motion); _motion = null; }
            transform.localPosition = _basePosition;
            transform.localRotation = _baseRotation;
            transform.localScale = _baseScale;
        }

        // ------------------------------------------------------------- 표정

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

        private IEnumerator PunchRoutine()
        {
            CaptureBase();
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
