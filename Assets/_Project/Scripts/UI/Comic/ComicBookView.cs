using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>
    /// 인트로 카툰 전체 (IntroComic 프리팹의 맨 위).
    ///
    /// 페이지는 이 오브젝트 아래의 <see cref="ComicPageView"/> 들이고, <b>Hierarchy 순서대로</b> 재생된다.
    /// 페이지를 편집할 때는 그 페이지만 켜 두고(체크박스) 나머지는 꺼 두면 겹치지 않는다 —
    /// 켜고 끈 상태는 재생에 영향이 없다(재생할 때 차례로 켠다).
    /// </summary>
    [AddComponentMenu("It is You/Comic Book View")]
    [DisallowMultipleComponent]
    public class ComicBookView : MonoBehaviour
    {
        [Tooltip("종이(페이지 판). 크기는 1440×1080 — 원본 4000×3000 의 0.36배.")]
        [SerializeField] private RectTransform paper;

        public RectTransform Paper => paper;

        public List<ComicPageView> Pages
        {
            get
            {
                var list = new List<ComicPageView>();
                var root = paper != null ? paper : (RectTransform)transform;
                for (int i = 0; i < root.childCount; i++)
                {
                    var page = root.GetChild(i).GetComponent<ComicPageView>();
                    if (page != null) list.Add(page);
                }
                return list;
            }
        }

        /// <summary>재생 전 — 페이지를 전부 숨긴다.</summary>
        public void HideAll()
        {
            foreach (var p in Pages) p.Hide();
        }
    }
}
