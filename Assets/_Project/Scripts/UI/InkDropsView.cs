using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 단서 조합의 시도 자원 — 잉크 방울. 남은 만큼 차 있고, 쓴 만큼 말라 있다.
    ///
    /// 숫자로 쓰지 않는 이유: 이 게임은 보이지 않는 수치를 두지 않기로 했다.
    /// 방울은 수치가 아니라 '몇 번 더 적을 수 있는가'의 그림이다.
    /// </summary>
    [AddComponentMenu("It is You/Ink Drops View")]
    public class InkDropsView : MonoBehaviour
    {
        [SerializeField] private RectTransform container;
        [SerializeField] private Image dropPrefab;
        [SerializeField] private Color fullColor = new Color32(0xC4, 0xA7, 0x5C, 0xFF);
        [SerializeField] private Color driedColor = new Color32(0x3A, 0x40, 0x48, 0xFF);

        private readonly List<Image> _drops = new List<Image>();

        public void SetCapacity(int capacity)
        {
            if (container == null || dropPrefab == null) return;
            while (_drops.Count < capacity)
            {
                var drop = Instantiate(dropPrefab, container);
                drop.gameObject.SetActive(true);
                _drops.Add(drop);
            }
            for (int i = 0; i < _drops.Count; i++)
                _drops[i].gameObject.SetActive(i < capacity);
        }

        public void SetRemaining(int remaining)
        {
            for (int i = 0; i < _drops.Count; i++)
            {
                if (!_drops[i].gameObject.activeSelf) continue;
                _drops[i].color = i < remaining ? fullColor : driedColor;
            }
        }
    }
}
