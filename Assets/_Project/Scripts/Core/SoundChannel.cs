using UnityEngine;

namespace ItIsYou.Core
{
    /// <summary>
    /// AudioSource 하나를 소리 채널(배경음 · 효과음 · 음성)에 묶는다.
    /// 설정의 채널 볼륨이 바뀌면 원래 볼륨에 곱해 다시 넣는다.
    ///
    /// 코드에서 볼륨을 직접 움직이는 소스(무반응 초침 등)는 <see cref="SetBase"/> 로 값을 넘긴다.
    /// </summary>
    [AddComponentMenu("It is You/Sound Channel")]
    [RequireComponent(typeof(AudioSource))]
    public class SoundChannel : MonoBehaviour
    {
        [SerializeField] private SoundChannelKind kind = SoundChannelKind.Sfx;

        [Tooltip("설정을 적용하기 전의 원래 볼륨. 비워 두면 시작 시 AudioSource 의 값을 쓴다.")]
        [SerializeField] private float baseVolume = -1f;

        private AudioSource _source;

        public SoundChannelKind Kind => kind;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            if (baseVolume < 0f) baseVolume = _source.volume;
        }

        private void OnEnable()
        {
            GameSettings.AudioChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameSettings.AudioChanged -= Apply;
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
            _source.volume = Mathf.Clamp01(baseVolume) * GameSettings.ChannelVolume(kind);
        }
    }
}
