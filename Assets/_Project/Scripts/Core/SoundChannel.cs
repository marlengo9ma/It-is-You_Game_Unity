using System;
using UnityEngine;

namespace ItIsYou.Core
{
    /// <summary>
    /// AudioSource 하나를 소리 채널(배경음 · 효과음 · 음성)에 묶는다.
    /// 설정의 채널 볼륨이 바뀌면 원래 볼륨에 곱해 다시 넣는다.
    ///
    /// 코드에서 볼륨을 직접 움직이는 소스(무반응 초침 등)는 <see cref="SetBase"/> 로 값을 넘긴다.
    ///
    /// 덕킹: 심문의 무반응 연출이 <see cref="SetDuck"/> 로 0~1 을 넣으면
    /// <see cref="duckable"/> 인 채널은 그만큼 줄어든다. 초침 소리처럼 오히려 커져야 하는 소스는
    /// duckable 을 끈다.
    /// </summary>
    [AddComponentMenu("It is You/Sound Channel")]
    [RequireComponent(typeof(AudioSource))]
    public class SoundChannel : MonoBehaviour
    {
        [SerializeField] private SoundChannelKind kind = SoundChannelKind.Sfx;

        [Tooltip("설정을 적용하기 전의 원래 볼륨. 비워 두면 시작 시 AudioSource 의 값을 쓴다.")]
        [SerializeField] private float baseVolume = -1f;

        [Tooltip("켜면 무반응 연출(초침)이 커질 때 같이 줄어든다. 초침 소리 자체는 꺼 둔다.")]
        [SerializeField] private bool duckable = true;

        private static float _duck = 1f;

        /// <summary>덕킹 배율이 바뀌었다.</summary>
        public static event Action DuckChanged;

        /// <summary>현재 덕킹 배율. 1 = 그대로, 0.25 = 1/4 로 줄어듦.</summary>
        public static float DuckLevel => _duck;

        private AudioSource _source;

        public SoundChannelKind Kind => kind;
        public bool Duckable => duckable;

        /// <summary>duckable 채널 전체에 곱할 배율을 정한다. 무반응 연출이 부른다.</summary>
        public static void SetDuck(float multiplier)
        {
            multiplier = Mathf.Clamp01(multiplier);
            if (Mathf.Approximately(_duck, multiplier)) return;
            _duck = multiplier;
            DuckChanged?.Invoke();
        }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            if (baseVolume < 0f) baseVolume = _source.volume;
        }

        private void OnEnable()
        {
            GameSettings.AudioChanged += Apply;
            DuckChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameSettings.AudioChanged -= Apply;
            DuckChanged -= Apply;
        }

        /// <summary>원래 볼륨을 바꾼다. 설정 배율은 여기서 다시 곱한다.</summary>
        public void SetBase(float volume)
        {
            baseVolume = Mathf.Clamp01(volume);
            Apply();
        }

        private void Apply()
        {
            if (_source == null) return;
            var v = Mathf.Clamp01(baseVolume) * GameSettings.ChannelVolume(kind);
            if (duckable) v *= _duck;
            _source.volume = v;
        }
    }
}
