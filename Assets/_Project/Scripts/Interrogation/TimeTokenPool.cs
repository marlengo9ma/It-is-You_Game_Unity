using System;
using UnityEngine;

namespace ItIsYou.Interrogation
{
    /// <summary>
    /// 타임 토큰. 기획서에서 "게임내에서의 시간을 토큰으로 시각화 한 요소"이며,
    /// 할당량이 전부 소진되면 해당 페이즈가 강제 종료된다.
    ///
    /// 소모 사유를 함께 받는 이유는, 잔여량이 아니라 "무엇에 시간을 썼는지"가
    /// 연출과 로그에 쓰이기 때문이다.
    /// </summary>
    public class TimeTokenPool
    {
        public enum SpendReason
        {
            Choice,          // 선택지 선택 & 재선택
            InterrogationObject, // 심문 오브젝트 선택
            Idle,            // 30초 무반응 판정
            AssistedRecall,  // 기억 짐작에서 조수의 도움
            ForcedDialogue,  // 스토리 진행으로 인한 강제 대화
            Refusal          // 심문 거부로 인한 전량 소진
        }

        private readonly int _capacity;
        private int _remaining;

        /// <summary>(소모량, 사유, 잔여량)</summary>
        public event Action<int, SpendReason, int> Spent;

        /// <summary>잔여량이 0이 된 순간 한 번.</summary>
        public event Action Exhausted;

        public int Capacity => _capacity;
        public int Remaining => _remaining;
        public bool IsExhausted => _remaining <= 0;
        public float Ratio => _capacity <= 0 ? 0f : (float)_remaining / _capacity;

        public TimeTokenPool(int capacity)
        {
            _capacity = Mathf.Max(1, capacity);
            _remaining = _capacity;
        }

        /// <summary>토큰을 쓴다. 실제로 줄어든 양을 돌려준다.</summary>
        public int Spend(int amount, SpendReason reason)
        {
            if (amount <= 0 || IsExhausted) return 0;

            var before = _remaining;
            _remaining = Mathf.Max(0, _remaining - amount);
            var actual = before - _remaining;

            if (actual > 0) Spent?.Invoke(actual, reason, _remaining);
            if (_remaining == 0 && before > 0) Exhausted?.Invoke();

            return actual;
        }

        /// <summary>
        /// 남은 양과 무관하게 전량 소진시킨다.
        /// 심문 거부 판정이 났을 때 쓴다 — "잔여 타임 토큰의 갯수와 상관 없이".
        /// </summary>
        public void DrainAll(SpendReason reason = SpendReason.Refusal)
        {
            if (IsExhausted) return;
            Spend(_remaining, reason);
        }

        /// <summary>이만큼 쓸 여유가 있는가.</summary>
        public bool CanAfford(int amount) => _remaining >= amount;
    }
}
