using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 게임 시작부터 기차역 장면까지의 인트로 카툰.
    ///
    /// 컷 그림 · 자리 · 애니메이션은 이 에셋이 아니라 IntroComic 프리팹
    /// (Assets/_Project/Prefabs/IntroComic.prefab) 에 있다. 인트로는 그림을 보며 맞추는 작업이라,
    /// 숫자 칸보다 Scene 뷰에서 직접 끌어 옮기는 쪽이 빠르기 때문이다.
    /// 여기에는 카툰이 끝난 뒤 어디로 가는지만 둔다.
    ///
    /// 챕터와 분리한 이유는 페이즈 규칙이 하나도 적용되지 않기 때문이다.
    /// 여기에는 타임 토큰도, 수집률도, 페널티도 없다. 그림과 클릭만 있다.
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Prologue", fileName = "Prologue_")]
    public class PrologueDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Tooltip("카툰이 끝나면 넘어갈 씬 이름. 비어 있으면 자동 전환하지 않는다.")]
        [SerializeField] private string nextSceneName;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public string NextSceneName => nextSceneName;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }
#endif
    }
}
