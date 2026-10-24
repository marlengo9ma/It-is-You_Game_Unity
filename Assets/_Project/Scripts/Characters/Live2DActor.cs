using ItIsYou.Data;
using ItIsYou.UI;
using UnityEngine;

namespace ItIsYou.Characters
{
    /// <summary>
    /// Live2D 모델 한 명의 모션을 돌린다 (모델 프리팹의 맨 위에 붙는다).
    ///
    /// · 평소: 모션 표(<see cref="Live2DMotionSet"/>)의 기본 모션을 차례로 되풀이 — 세피는 기본 → smile → 기본 → …
    /// · 대사: 이 인물의 줄이 나오면 그 줄의 연출 이름(animationId), 없으면 표정에 맞는 특수 모션을 재생한다.
    /// · 심문 리액션 순간: 표의 reactionSlot(기본 shock = 충격).
    /// 특수 모션이 끝나면 기본으로 돌아온다. 클립이 빈 칸은 건너뛴다.
    ///
    /// Live2D Cubism SDK 가 있을 때만 동작한다(LIVE2D_CUBISM). SDK 가 없으면 아무 일도 하지 않는다.
    /// </summary>
    [AddComponentMenu("It is You/Live2D Actor")]
    [DisallowMultipleComponent]
    public class Live2DActor : MonoBehaviour
    {
        [SerializeField] private string characterId = "SEPI";
        [Tooltip("이 인물의 대사 줄 화자. 그 줄에서 모션을 고른다.")]
        [SerializeField] private SpeakerId speaker = SpeakerId.Assistant;
        [SerializeField] private Live2DMotionSet motions;
        [Tooltip("대사창 — 이 인물의 줄이 나올 때 모션을 건다. 비워 두면 씬에서 찾는다.")]
        [SerializeField] private DialoguePresenter dialogue;

        [Tooltip("같은 모션이 이 시간(초) 안에 두 번 불리면 한 번만 재생한다 — 대사창과 심문이 같은 줄을 함께 알려 줄 때.")]
        [SerializeField] private float duplicateWindow = 0.3f;

        private int _idleIndex = -1;
        private float _retryAt;
        private bool _special;
        private string _lastName;
        private float _lastTime = -10f;

        public string CharacterId => characterId;
        public Live2DMotionSet Motions => motions;

        /// <summary>지금 재생 중인 특수 모션 이름(없으면 null) — 확인용.</summary>
        public string CurrentSpecial { get; private set; }

#if LIVE2D_CUBISM
        private Live2D.Cubism.Framework.Motion.CubismMotionController _controller;

        private void Awake()
        {
            _controller = GetComponentInChildren<Live2D.Cubism.Framework.Motion.CubismMotionController>(true);
            if (_controller == null)
                Debug.LogWarning($"[{name}] CubismMotionController 가 없어 모션이 재생되지 않습니다. 메뉴 It is You › 10 으로 다시 준비하세요.", this);
            if (dialogue == null) dialogue = FindFirstObjectByType<DialoguePresenter>(FindObjectsInactive.Include);
            if (dialogue != null) dialogue.LineStarted += OnLine;
        }

        private void OnDestroy()
        {
            if (dialogue != null) dialogue.LineStarted -= OnLine;
        }

        private void Update()
        {
            if (_controller == null || motions == null) return;
            if (_controller.IsPlayingAnimation(0)) return;
            if (Time.unscaledTime < _retryAt) return;
            _retryAt = Time.unscaledTime + 0.5f;   // 시작이 막혀 있으면(모션 목록 없음 등) 매 프레임 다시 시도하지 않는다

            // 특수 모션이 끝났거나 기본 모션 한 바퀴가 끝났다 — 다음 기본 모션.
            _special = false;
            CurrentSpecial = null;
            PlayNextIdle();
        }

        private void PlayNextIdle()
        {
            var list = motions.idleSequence;
            if (list == null || list.Count == 0) return;
            for (int tries = 0; tries < list.Count; tries++)
            {
                _idleIndex = (_idleIndex + 1) % list.Count;
                var clip = list[_idleIndex];
                if (clip == null) continue;
                // 기본이 한 개뿐이면 그대로 반복한다. 여러 개면 한 바퀴씩 돌고 다음으로.
                _controller.PlayAnimation(clip, 0, Live2D.Cubism.Framework.Motion.CubismMotionPriority.PriorityIdle,
                    isLoop: list.Count == 1, speed: 1f);
                return;
            }
        }

        private bool PlayClip(Live2DMotionSet.MotionSlot slot)
        {
            if (_controller == null || slot == null || slot.clip == null) return false;
            _controller.PlayAnimation(slot.clip, 0, Live2D.Cubism.Framework.Motion.CubismMotionPriority.PriorityForce,
                isLoop: false, speed: slot.speed <= 0f ? 1f : slot.speed);
            _special = true;
            CurrentSpecial = slot.id;
            return true;
        }
#else
        private void Awake() { }
        private bool PlayClip(Live2DMotionSet.MotionSlot slot) => false;
        private void OnLine(DialogueLine line) { }
#endif

        /// <summary>특수 모션을 이름으로 재생한다. 없는 이름이나 빈 칸이면 false.</summary>
        public bool Play(string motionName)
        {
            if (motions == null || string.IsNullOrEmpty(motionName)) return false;
            if (motionName == _lastName && Time.unscaledTime - _lastTime < duplicateWindow) return true;
            var slot = motions.FindSlot(motionName);
            if (!PlayClip(slot)) return false;
            _lastName = motionName;
            _lastTime = Time.unscaledTime;
            return true;
        }

        /// <summary>리액션 순간 — 표의 reactionSlot.</summary>
        public bool PlayReaction() => motions != null && Play(motions.reactionSlot);

        /// <summary>표정에 묶인 특수 모션이 있으면 재생한다.</summary>
        public bool PlayForExpression(ExpressionId expression)
            => motions != null && Play(motions.SlotForExpression(expression));

#if LIVE2D_CUBISM
        private void OnLine(DialogueLine line)
        {
            if (line == null || line.Speaker != speaker) return;
            if (!string.IsNullOrEmpty(line.AnimationId) && Play(line.AnimationId)) return;
            PlayForExpression(line.Expression);
        }
#endif
    }
}
