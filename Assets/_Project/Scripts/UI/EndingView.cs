using ItIsYou.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 챕터 엔딩 자리. 엔딩 컷씬과 이벤트 컷씬은 아트 대기 — 지금은 지목 결과 한 줄과 '타이틀로' 만 있다.
    /// </summary>
    [AddComponentMenu("It is You/Ending View")]
    public class EndingView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private Button titleButton;

        private void Awake()
        {
            if (titleButton != null) titleButton.onClick.AddListener(() => SceneManager.LoadScene(SceneRoutes.Title));
            if (root != null) root.SetActive(false);
        }

        public void Show(string accusedName, int clueCount)
        {
            if (root != null) root.SetActive(true);
            if (title != null) title.text = "엔딩 컷씬 (아트 대기)";
            if (body != null) body.text = $"지목: {accusedName}\n수집한 단서 {clueCount}개\n\n이벤트 컷씬 · 엔딩 컷씬 자리 — 챕터 종료";
        }
    }
}
