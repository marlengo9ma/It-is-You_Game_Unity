using System;
using System.Collections.Generic;
using ItIsYou.Data;
using UnityEngine;

namespace ItIsYou.Characters
{
    /// <summary>
    /// Live2D Cubism 구현체.
    ///
    /// Cubism SDK 가 프로젝트에 없으면 이 컴포넌트는 안내 로그만 남기고 아무 일도 하지 않는다.
    /// SDK 를 임포트하면 asmdef 의 versionDefine 이 LIVE2D_CUBISM 을 켜고,
    /// 아래 실제 구현이 컴파일에 들어간다. 그래서 SDK 없이도 프로젝트가 항상 빌드된다.
    ///
    /// 표정 이름은 Cubism 의 .exp3.json 파일 이름과 맞춘다.
    /// </summary>
    [AddComponentMenu("It is You/Cubism Character View")]
    public class CubismCharacterView : MonoBehaviour, ICharacterView
    {
        [Serializable]
        private struct ExpressionBinding
        {
            [Tooltip("게임 쪽 표정 id")]
            public ExpressionId expression;

            [Tooltip("Cubism 표정 목록에서의 인덱스")]
            public int cubismIndex;

            [Tooltip("리액션으로 쓸 때 재생할 모션 이름. 비우면 표정만 바꾼다.")]
            public string reactionMotion;
        }

        [SerializeField] private string characterId;
        [SerializeField] private List<ExpressionBinding> bindings = new List<ExpressionBinding>();

        [Tooltip("리액션 수집 창이 열렸을 때 켜는 오브젝트. 보통 외곽선 이펙트.")]
        [SerializeField] private GameObject collectableHighlight;

        [Tooltip("기억 짐작 왜곡을 적용할 머티리얼. 블러/실루엣 파라미터를 가진 셰이더를 물린다.")]
        [SerializeField] private Renderer distortionTarget;

        private static readonly int BlurProperty = Shader.PropertyToID("_RecallBlur");
        private static readonly int FadeProperty = Shader.PropertyToID("_SilhouetteFade");

        private MaterialPropertyBlock _block;

        public string CharacterId => characterId;

#if LIVE2D_CUBISM
        private Live2D.Cubism.Framework.Expression.CubismExpressionController _expressionController;
        private Live2D.Cubism.Framework.Motion.CubismMotionController _motionController;

        private void Awake()
        {
            _expressionController = GetComponentInChildren<Live2D.Cubism.Framework.Expression.CubismExpressionController>();
            _motionController = GetComponentInChildren<Live2D.Cubism.Framework.Motion.CubismMotionController>();

            if (_expressionController == null)
                Debug.LogWarning($"[{name}] CubismExpressionController 를 찾지 못했습니다. 표정 전환이 동작하지 않습니다.", this);
        }

        public void SetExpression(ExpressionId expression)
        {
            if (_expressionController == null) return;
            if (!TryGetBinding(expression, out var binding)) return;
            _expressionController.CurrentExpressionIndex = binding.cubismIndex;
        }

        public void PlayReaction(ExpressionId expression)
        {
            SetExpression(expression);

            if (_motionController == null) return;
            if (!TryGetBinding(expression, out var binding)) return;
            if (string.IsNullOrEmpty(binding.reactionMotion)) return;

            var clip = Resources.Load<Live2D.Cubism.Framework.Motion.CubismMotionController>(binding.reactionMotion);
            if (clip != null) return; // 프로젝트의 모션 로딩 방식이 정해지면 여기에 연결한다.
        }
#else
        private bool _warned;

        public void SetExpression(ExpressionId expression) => WarnOnce();

        public void PlayReaction(ExpressionId expression) => WarnOnce();

        private void WarnOnce()
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning(
                $"[{name}] Live2D Cubism SDK 가 프로젝트에 없어 표정 전환이 동작하지 않습니다. " +
                "SDK 를 임포트하면 자동으로 활성화됩니다. 그 전까지는 PlaceholderCharacterView 를 쓰세요.",
                this);
        }
#endif

        public void SetReactionCollectable(bool collectable)
        {
            if (collectableHighlight != null) collectableHighlight.SetActive(collectable);
        }

        public void SetRecallDistortion(float blur, float silhouetteFade)
        {
            if (distortionTarget == null) return;
            _block ??= new MaterialPropertyBlock();

            distortionTarget.GetPropertyBlock(_block);
            _block.SetFloat(BlurProperty, Mathf.Clamp01(blur));
            _block.SetFloat(FadeProperty, Mathf.Clamp01(silhouetteFade));
            distortionTarget.SetPropertyBlock(_block);
        }

        public void SetSpeaking(bool speaking)
        {
            // 립싱크는 Cubism 의 MouthMovement 컴포넌트가 오디오를 직접 물고 처리하므로
            // 여기서는 화자 강조만 담당한다. 필요해지면 여기서 볼륨 소스를 토글한다.
        }

        private bool TryGetBinding(ExpressionId expression, out ExpressionBinding binding)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].expression != expression) continue;
                binding = bindings[i];
                return true;
            }
            binding = default;
            return false;
        }
    }
}
