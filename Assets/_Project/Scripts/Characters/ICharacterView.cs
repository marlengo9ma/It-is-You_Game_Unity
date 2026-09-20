using ItIsYou.Data;

namespace ItIsYou.Characters
{
    /// <summary>
    /// 화면 위 캐릭터 한 명. 게임 로직은 이 인터페이스만 알고,
    /// 실제 그림이 Live2D 인지 Spine 인지 사각형 플레이스홀더인지는 신경 쓰지 않는다.
    ///
    /// 표정 반응이 이 게임의 핵심 재미 요소라 구현체를 갈아끼울 일이 잦을 수밖에 없는데,
    /// 그때 심문 로직을 건드리지 않기 위한 경계선이다.
    /// </summary>
    public interface ICharacterView
    {
        /// <summary>이 뷰가 대응하는 인물 id.</summary>
        string CharacterId { get; }

        /// <summary>표정을 바꾼다. 즉시 전환이 아니라 자연스럽게 섞여 들어가야 한다.</summary>
        void SetExpression(ExpressionId expression);

        /// <summary>
        /// 리액션을 재생한다. 표정 변화보다 크고 짧은 반응 —
        /// 놀람, 동공 떨림처럼 플레이어가 포착해야 하는 순간.
        /// </summary>
        void PlayReaction(ExpressionId expression);

        /// <summary>
        /// 지금 클릭해서 수집할 수 있는 상태인지 표시한다.
        /// 켜져 있는 동안 캐릭터 위에서 커서가 바뀐다.
        /// </summary>
        void SetReactionCollectable(bool collectable);

        /// <summary>기억 짐작의 왜곡 정도를 반영한다. 0이면 또렷하고 1이면 인영만 남는다.</summary>
        void SetRecallDistortion(float blur, float silhouetteFade);

        /// <summary>말하는 중인지. 화자 강조와 립싱크에 쓴다.</summary>
        void SetSpeaking(bool speaking);
    }
}
