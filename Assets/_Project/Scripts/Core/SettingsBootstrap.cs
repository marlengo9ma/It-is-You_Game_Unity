using UnityEngine;

namespace ItIsYou.Core
{
    /// <summary>
    /// 첫 씬이 뜨기 전에 저장된 설정을 읽어 화면과 소리에 적용한다.
    /// 씬마다 따로 부르지 않아도 되도록 런타임 초기화 훅에 걸어 둔다.
    /// </summary>
    public static class SettingsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (!GameSettings.Loaded) GameSettings.Load();
            GameSettings.ApplyAudio();

            // 에디터의 게임 뷰는 창 크기를 바꿀 수 없다. 빌드에서만 적용한다.
#if !UNITY_EDITOR
            GameSettings.ApplyDisplay();
#endif
        }
    }
}
