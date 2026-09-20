using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 에디터 도구용 공통 헬퍼.
    ///
    /// 런타임 데이터 클래스의 필드는 전부 private + [SerializeField] 다.
    /// 게임 코드가 값을 못 바꾸게 막아 둔 건 의도한 것이고,
    /// 대신 에디터 도구(데이터 빌더, TSV 임포터)만 리플렉션으로 채운다.
    /// 런타임에 setter 를 열어 주는 것보다 경계가 분명하다.
    /// </summary>
    public static class Ed
    {
        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>private 직렬화 필드에 값을 넣는다.</summary>
        public static T Set<T>(this T target, string fieldName, object value) where T : class
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, Flags);
                type = type.BaseType;
            }

            if (field == null)
                throw new ArgumentException($"{target.GetType().Name} 에 '{fieldName}' 필드가 없습니다.");

            field.SetValue(target, value);
            return target;
        }

        /// <summary>폴더가 없으면 중간 경로까지 만든다. 경로는 Assets/ 로 시작한다.</summary>
        public static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            var parts = assetFolder.Split('/');
            var path = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = path + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(path, parts[i]);
                path = next;
            }
        }

        /// <summary>같은 경로에 에셋이 있으면 그것을 쓰고, 없으면 새로 만든다.</summary>
        public static T CreateOrLoad<T>(string folder, string fileName) where T : ScriptableObject
        {
            EnsureFolder(folder);
            var path = $"{folder}/{fileName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var created = ScriptableObject.CreateInstance<T>();
            created.name = fileName;
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        public static void Save(params UnityEngine.Object[] dirty)
        {
            for (int i = 0; i < dirty.Length; i++)
            {
                if (dirty[i] != null) EditorUtility.SetDirty(dirty[i]);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>리스트를 만들어 주는 짧은 표기.</summary>
        public static List<T> L<T>(params T[] items) => new List<T>(items);
    }
}
