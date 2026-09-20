using UnityEngine;

namespace ItIsYou.Interrogation
{
    /// <summary>
    /// 기억 짐작 화면의 왜곡 정도. 회차가 올라갈수록 커진다.
    /// 셰이더 파라미터와 텍스트 왜곡 확률에 그대로 넘긴다.
    /// </summary>
    public readonly struct RecallDistortion
    {
        /// <summary>몇 번째 기억 짐작인가. 1부터 시작한다.</summary>
        public readonly int Depth;

        /// <summary>화면 블러 세기 0~1.</summary>
        public readonly float Blur;

        /// <summary>글리치 세기 0~1.</summary>
        public readonly float Glitch;

        /// <summary>인영이 흐려진 정도 0~1. 1이면 실루엣만 남는다.</summary>
        public readonly float SilhouetteFade;

        /// <summary>텍스트가 왜곡돼 읽을 수 없게 될 확률 0~1.</summary>
        public readonly float TextCorruption;

        public RecallDistortion(int depth, float blur, float glitch, float silhouetteFade, float textCorruption)
        {
            Depth = depth;
            Blur = blur;
            Glitch = glitch;
            SilhouetteFade = silhouetteFade;
            TextCorruption = textCorruption;
        }

        /// <summary>왜곡이 없는 상태 — 지금 눈앞의 심문 화면.</summary>
        public static RecallDistortion None => new RecallDistortion(0, 0f, 0f, 0f, 0f);
    }

    /// <summary>
    /// 기억 짐작.
    ///
    /// 기획서: "기억 짐작은 심문 페이즈 중에 여러 번 진행 할 수 있으나,
    /// 횟수에 따라 더욱 불분명한 내용과 인영, 왜곡된 내용들로 보여집니다."
    /// 그리고 조수의 도움으로 흐려진 부분을 복원할 수 있으나 타임 토큰을 더 쓴다.
    ///
    /// 횟수 제한을 두지 않고 왜곡만 키우는 것이 기획 의도라, 여기서도 상한을 두지 않는다.
    /// 대신 <see cref="MaxMeaningfulDepth"/> 이후로는 사실상 아무것도 못 읽게 된다.
    /// </summary>
    public class MemoryRecall
    {
        /// <summary>이 이상 들어가면 왜곡이 최대치로 고정된다.</summary>
        public const int MaxMeaningfulDepth = 4;

        private int _depth;
        private bool _assistedThisRecall;

        /// <summary>현재 회차. 기억 짐작에 들어가 있지 않으면 0.</summary>
        public int Depth => _depth;

        public bool IsActive => _depth > 0;

        /// <summary>이번 회차에서 조수의 도움을 이미 썼는가.</summary>
        public bool AssistedThisRecall => _assistedThisRecall;

        /// <summary>기억 짐작에 한 단계 더 들어간다. 새 회차 번호를 돌려준다.</summary>
        public int Enter()
        {
            _depth++;
            _assistedThisRecall = false;
            return _depth;
        }

        /// <summary>기억 짐작에서 빠져나온다. 누적 회차는 유지된다.</summary>
        public void Exit()
        {
            _assistedThisRecall = false;
        }

        /// <summary>
        /// 조수의 도움을 쓴다. 이번 회차의 왜곡이 한 단계 걷힌다.
        /// 이미 썼으면 false — 타임 토큰을 중복으로 태우지 않기 위해서다.
        /// </summary>
        public bool UseAssist()
        {
            if (_assistedThisRecall) return false;
            _assistedThisRecall = true;
            return true;
        }

        /// <summary>지금 화면에 걸 왜곡 값.</summary>
        public RecallDistortion CurrentDistortion()
        {
            if (_depth <= 0) return RecallDistortion.None;

            // 조수의 도움을 쓰면 체감 회차가 한 단계 낮아진다.
            var effective = _assistedThisRecall ? _depth - 1 : _depth;
            return DistortionAt(effective);
        }

        /// <summary>단서 수집이 가능한 체감 회차. 조수의 도움을 반영한다.</summary>
        public int EffectiveDepth => _assistedThisRecall ? Mathf.Max(0, _depth - 1) : _depth;

        public static RecallDistortion DistortionAt(int depth)
        {
            if (depth <= 0) return RecallDistortion.None;

            var t = Mathf.Clamp01((float)depth / MaxMeaningfulDepth);
            // 회차가 올라갈수록 가파르게 흐려지도록 살짝 가속을 준다.
            var eased = t * t;

            return new RecallDistortion(
                depth,
                blur: Mathf.Lerp(0.15f, 1f, eased),
                glitch: Mathf.Lerp(0.05f, 0.8f, eased),
                silhouetteFade: Mathf.Lerp(0.1f, 0.95f, eased),
                textCorruption: Mathf.Lerp(0f, 0.7f, eased));
        }

        /// <summary>용의자가 바뀌면 처음부터 다시 센다.</summary>
        public void Reset()
        {
            _depth = 0;
            _assistedThisRecall = false;
        }
    }
}
