using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// Live2D 인물 한 명의 모션 표 (예: Assets/_Project/Data/Characters/SepiMotions.asset).
    ///
    /// · 기본(idle): <see cref="idleSequence"/> 를 차례로 되풀이한다 — 세피는 기본 → smile → 기본 → …
    /// · 특수 모션: <see cref="slots"/> 의 칸마다 이름(id)과 모션 클립. 대사 줄의 연출 이름(animationId)이나
    ///   표정(<see cref="expressionMotions"/>), 리액션 순간(<see cref="reactionSlot"/>)에 재생되고, 끝나면 기본으로 돌아온다.
    /// · 클립이 빈 칸은 조용히 건너뛴다 — 모션이 나중에 들어와도 칸에 끌어다 놓기만 하면 된다.
    ///
    /// 모션 클립은 .motion3.json 을 Unity 가 가져오면서 만든 같은 이름의 AnimationClip(.anim) 이다.
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Live2D Motion Set", fileName = "Motions")]
    public class Live2DMotionSet : ScriptableObject
    {
        [Serializable]
        public class MotionSlot
        {
            [Tooltip("이름. 대사 줄의 연출 이름(animationId)과 같으면 그 줄에서 재생된다. 예: shock, tremble")]
            public string id;

            [Tooltip("메모 — 어떤 모션인지.")]
            public string note;

            [Tooltip("모션 클립 (.motion3.json 을 가져와 만든 .anim). 비어 있으면 재생하지 않는다.")]
            public AnimationClip clip;

            [Tooltip("재생 속도.")]
            public float speed = 1f;

            [Tooltip("켜 두면 이 칸은 재생하지 않는다(표정 · 리액션 · 연출 이름 모두). 모션을 잠시 빼 둘 때.")]
            public bool disabled;

            [Tooltip("다른 이름으로도 부를 수 있게. 예: shock 칸에 startle 을 적으면 'startle' 연출에서도 이 모션이 나온다.")]
            public List<string> aliases = new List<string>();

            public bool Matches(string name)
            {
                if (string.IsNullOrEmpty(name)) return false;
                if (string.Equals(id, name, StringComparison.OrdinalIgnoreCase)) return true;
                if (aliases == null) return false;
                foreach (var a in aliases) if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
        }

        [Serializable]
        public class ExpressionMotion
        {
            [Tooltip("대사 줄의 표정")]
            public ExpressionId expression;

            [Tooltip("그 표정의 줄에서 재생할 특수 모션 칸 이름(id)")]
            public string slot;
        }

        [Header("기본 (idle)")]
        [Tooltip("차례로 되풀이되는 기본 모션. 한 개면 그 모션만 반복.")]
        public List<AnimationClip> idleSequence = new List<AnimationClip>();

        [Header("특수 모션")]
        [Tooltip("이름 붙은 모션 칸. 빈 칸은 나중에 클립만 끌어다 넣으면 된다.")]
        public List<MotionSlot> slots = new List<MotionSlot>();

        [Tooltip("심문 중 리액션(놀람 · 동공 떨림)을 포착할 수 있는 순간에 재생할 칸 이름.")]
        public string reactionSlot = "shock";

        [Tooltip("대사 줄의 연출 이름이 비어 있을 때, 표정에 따라 재생할 칸. 예: Surprised → shock")]
        public List<ExpressionMotion> expressionMotions = new List<ExpressionMotion>();

        public MotionSlot FindSlot(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var s in slots) if (s != null && !s.disabled && s.Matches(name)) return s;
            return null;
        }

        public string SlotForExpression(ExpressionId expression)
        {
            foreach (var e in expressionMotions) if (e != null && e.expression == expression) return e.slot;
            return null;
        }
    }
}
