using ItIsYou.Core;
using UnityEngine;

namespace ItIsYou.Interrogation
{
    /// <summary>
    /// 무반응 타이머가 차오를 때의 청각 연출.
    ///
    /// 기획: 타이머가 7초를 지나면 배경음 등 나머지 사운드가 줄어들고, 시계 초침 소리가 점진적으로 커진다.
    /// 판정(15초)에 닿는 순간 초침은 최대, 나머지는 바닥.
    ///
    /// 실제 클립은 아직 없다. <see cref="clockTick"/> 에 초침 소리(루프)를 끼우면 그대로 동작한다.
    /// 나머지 사운드는 <see cref="SoundChannel"/> 의 duckable 채널 전체가 같이 줄어든다 —
    /// 배경음·환경음 소스에 SoundChannel 만 붙여 두면 된다.
    /// </summary>
    [AddComponentMenu("It is You/Idle Tension Audio")]
    public class IdleTensionAudio : MonoBehaviour
    {
        [Header("초침")]
        [Tooltip("시계 초침 소리. 루프 클립을 넣는다. SoundChannel(duckable 끔) 이 같이 붙어 있어야 설정 볼륨을 따른다.")]
        [SerializeField] private AudioSource clockTick;

        [Tooltip("판정 직전에 초침이 도달할 볼륨 (설정 배율 전).")]
        [Range(0f, 1f)]
        [SerializeField] private float clockMaxVolume = 0.85f;

        [Header("타이밍")]
        [Tooltip("타이머가 이 시간(초)을 지나면 연출이 시작된다. 기획 7초.")]
        [SerializeField] private float startSeconds = 7f;

        [Tooltip("시작 → 판정 사이의 곡선. 0~1 입력, 0~1 출력. 기본은 뒤로 갈수록 가파른 곡선.")]
        [SerializeField] private AnimationCurve ramp = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0.4f), new Keyframe(1f, 1f, 2.2f, 0f));

        [Header("나머지 사운드")]
        [Tooltip("판정 직전에 나머지 사운드(duckable 채널)가 남는 비율. 0.25 = 1/4.")]
        [Range(0f, 1f)]
        [SerializeField] private float duckFloor = 0.25f;

        [Tooltip("타이머가 초기화되거나 심문이 끝났을 때 원래 소리로 돌아오는 시간(초).")]
        [SerializeField] private float releaseSeconds = 0.6f;

        private SoundChannel _clockChannel;
        private float _level;        // 현재 연출 강도 0~1
        private float _target;       // 타이머가 원하는 강도

        /// <summary>현재 연출 강도 0~1. HUD 의 시각 효과가 같이 쓸 수 있다.</summary>
        public float Level => _level;

        private void Awake()
        {
            if (clockTick != null)
            {
                _clockChannel = clockTick.GetComponent<SoundChannel>();
                clockTick.loop = true;
                clockTick.playOnAwake = false;
            }
        }

        /// <summary>
        /// 매 프레임 타이머 상태를 넘긴다. 타이머가 멈춰 있으면(열람창·일시 정지) 값이 그대로라 연출도 멈춘다.
        /// </summary>
        public void Drive(float elapsedSeconds, float thresholdSeconds)
        {
            var span = Mathf.Max(0.01f, thresholdSeconds - startSeconds);
            var t = Mathf.Clamp01((elapsedSeconds - startSeconds) / span);
            _target = ramp.Evaluate(t);
        }

        /// <summary>연출을 끈다. 심문이 끝났거나 페이즈가 바뀔 때.</summary>
        public void Clear()
        {
            _target = 0f;
        }

        private void Update()
        {
            // 오르는 쪽은 타이머를 그대로 따르고, 내려오는 쪽만 부드럽게.
            if (_target >= _level) _level = _target;
            else _level = releaseSeconds <= 0f ? _target
                : Mathf.MoveTowards(_level, _target, Time.unscaledDeltaTime / releaseSeconds);

            ApplyLevel();
        }

        private void ApplyLevel()
        {
            SoundChannel.SetDuck(Mathf.Lerp(1f, duckFloor, _level));

            if (clockTick == null) return;

            var volume = clockMaxVolume * _level;
            if (_clockChannel != null) _clockChannel.SetBase(volume);
            else clockTick.volume = volume * GameSettings.ChannelVolume(SoundChannelKind.Sfx);

            var shouldPlay = _level > 0.001f && clockTick.clip != null;
            if (shouldPlay && !clockTick.isPlaying) clockTick.Play();
            else if (!shouldPlay && clockTick.isPlaying) clockTick.Stop();
        }

        private void OnDisable()
        {
            _level = 0f;
            _target = 0f;
            SoundChannel.SetDuck(1f);
            if (clockTick != null && clockTick.isPlaying) clockTick.Stop();
        }
    }
}
