using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 대사 줄마다 골라 둔 '말풍선' 모양을 메뉴 5(데이터 다시 만들기) 뒤에도 남긴다.
    ///
    /// 데이터 빌더는 대사 에셋을 대본(TutorialScript)으로 다시 채우므로, 인스펙터에서 바꾼 값이 지워진다.
    /// 그래서 다시 만들기 전에 '자동'이 아닌 말풍선을 글 내용 기준으로 적어 두었다가, 다 만든 뒤 같은 글의 줄에 돌려 넣는다.
    /// 글이 바뀐 줄은 짝을 못 찾아 '자동'으로 돌아간다.
    /// </summary>
    public static class BubbleKeeper
    {
        private const string DataRoot = "Assets/_Project/Data";

        /// <summary>지금 데이터 에셋들에서 (글 → 말풍선) 표를 만든다.</summary>
        public static Dictionary<string, int> Capture()
        {
            var map = new Dictionary<string, int>();
            foreach (var so in Each())
            {
                var it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.name != "bubble" || it.propertyType != SerializedPropertyType.Enum) continue;
                    if (it.intValue == 0) continue;   // 자동
                    var text = Text(so, it.propertyPath);
                    if (!string.IsNullOrEmpty(text)) map[text] = it.intValue;
                }
            }
            return map;
        }

        /// <summary>다시 만든 데이터 에셋들에 표의 값을 돌려 넣는다. 돌려 넣은 줄 수를 돌려준다.</summary>
        public static int Restore(Dictionary<string, int> map)
        {
            if (map == null || map.Count == 0) return 0;
            var count = 0;
            foreach (var so in Each())
            {
                var changed = false;
                var it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.name != "bubble" || it.propertyType != SerializedPropertyType.Enum) continue;
                    var text = Text(so, it.propertyPath);
                    if (string.IsNullOrEmpty(text) || !map.TryGetValue(text, out var v) || it.intValue == v) continue;
                    it.intValue = v;
                    changed = true;
                    count++;
                }
                if (changed)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(so.targetObject);
                }
            }
            if (count > 0) AssetDatabase.SaveAssets();
            return count;
        }

        private static string Text(SerializedObject so, string bubblePath)
        {
            var textPath = bubblePath.Substring(0, bubblePath.Length - "bubble".Length) + "text";
            var p = so.FindProperty(textPath);
            return p != null && p.propertyType == SerializedPropertyType.String ? p.stringValue : null;
        }

        private static IEnumerable<SerializedObject> Each()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { DataRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (obj is ScriptableObject) yield return new SerializedObject(obj);
            }
        }
    }
}
