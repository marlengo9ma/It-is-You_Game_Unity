using System;
using UnityEngine;

namespace ItIsYou.Interrogation
{
    /// <summary>
    /// 무반응 판정 타이머.
    ///
    /// 기획서: "무반응 판정은 실제 현실에서의 시간 흐름과 상호 작용되어 판정이 이루어진다.
    /// 플레이어에게 실질적인 스트레스를 주는 요소이자, 선택지 및 오브젝트 선택과는
    /// 또 다른 방향의 심문의 트리거로써 활용된다."
    ///
    /// 그래서 게임 내 시간이 아니라 <see cref="Time.unscaledDeltaTime"/> 으로 센다.
    /// 일시 정지 요청은 겹칠 수 있으므로(툴팁 + 일시정지 메뉴) 카운터로 관리한다.
    /// </summary>
    public class IdleTimer
    {
        private readonly float _threshold;
        private float _elapsed;
        private int _holdCount;
        private bool _fired;
        private bool _running;

        /// <summary>임계 시간에 도달했다. 한 주기에 한 번만 발생한다.</summary>
        public event Action Elapsed;

        /// <summary>0~1 진행도. 시계 초침 소리를 키우고 나머지 사운드를 줄이는 데 쓴다.</summary>
        public float Progress => _threshold <= 0f ? 0f : Mathf.Clamp01(_elapsed / _threshold);

        public bool IsHeld => _holdCount > 0;
        public bool IsRunning => _running;
        public float Remaining => Mathf.Max(0f, _threshold - _elapsed);

        public IdleTimer(float thresholdSeconds)
        {
            _threshold = Mathf.Max(0.1f, thresholdSeconds);
        }

        public void Start()
        {
            _running = true;
            Restart();
        }

        public void Stop()
        {
            _running = false;
            _elapsed = 0f;
            _fired = false;
        }

        /// <summary>타이머를 0으로 되돌린다. 선택지나 오브젝트를 골라 대화가 진행됐을 때.</summary>
        public void Restart()
        {
            _elapsed = 0f;
            _fired = false;
        }

        /// <summary>
        /// 일시 정지를 요청한다. 반드시 <see cref="Release"/> 와 짝을 맞춰야 한다.
        /// 게임 일시 정지, 단서 툴팁 출력 중에 쓴다.
        /// </summary>
        public void Hold() => _holdCount++;

        public void Release() => _holdCount = Mathf.Max(0, _holdCount - 1);

        /// <summary>모든 일시 정지를 푼다. 페이즈 전환처럼 상태를 확실히 정리할 때.</summary>
        public void ReleaseAll() => _holdCount = 0;

        /// <summary>MonoBehaviour 의 Update 에서 매 프레임 호출한다.</summary>
        public void Tick(float unscaledDeltaTime)
        {
            if (!_running || _holdCount > 0 || _fired) return;

            _elapsed += unscaledDeltaTime;
            if (_elapsed < _threshold) return;

            _fired = true;
            Elapsed?.Invoke();
        }
    }
}
