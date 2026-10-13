using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 컷씬 화면(Screen_Cutscene)의 그림과 이름판. 일러스트 컷씬#1 뒤에 회상씬(E#1, E#2)이 같은 화면을 빌려 쓴다.
    /// 그림이 있으면 그림을, 없으면 이름판(아트 대기)을 보인다.
    /// </summary>
    [AddComponentMenu("It is You/Cutscene Screen View")]
    public class CutsceneScreenView : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color placeholderColor = new Color32(0x15, 0x18, 0x1C, 0xFF);

        public void Show(string name, Sprite sprite)
        {
            if (image != null)
            {
                image.sprite = sprite;
                image.color = sprite != null ? Color.white : placeholderColor;
                image.preserveAspect = sprite != null;
            }
            if (label != null)
            {
                label.text = name ?? string.Empty;
                label.gameObject.SetActive(sprite == null);
            }
        }
    }
}
