using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ItIsYou.Data
{
    /// <summary>
    /// 대사와 툴팁에 쓰는 인라인 마크업 규칙.
    ///
    /// <list type="bullet">
    /// <item><c>&lt;clue=ID&gt;문구&lt;/clue&gt;</c> — 심문 중 클릭해 증언으로 수집 가능한 구간.
    /// TMP 의 link + underline 으로 변환된다.</item>
    /// <item><c>&lt;hl&gt;문구&lt;/hl&gt;</c> — 단서 툴팁에서 "단서로 쓸 수 있음"을 알리는 강조 구간.</item>
    /// </list>
    ///
    /// 기획 문서를 그대로 옮겨 적을 수 있도록 태그 문법을 단순하게 유지한다.
    /// 실제 TMP 태그는 런타임에서 생성하므로, 데이터에는 게임 규칙만 남는다.
    /// </summary>
    public static class DialogueMarkup
    {
        private static readonly Regex ClueTag =
            new Regex(@"<clue=(?<id>[^>]+)>(?<body>.*?)</clue>", RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex HighlightTag =
            new Regex(@"<hl>(?<body>.*?)</hl>", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>기본 강조 색. 단서로 쓸 수 있는 구간에 쓴다.</summary>
        public const string HighlightColor = "#C9A227";

        /// <summary>본문에 들어 있는 모든 단서 id 를 등장 순서대로 돌려준다.</summary>
        public static IEnumerable<string> ExtractClueIds(string source)
        {
            if (string.IsNullOrEmpty(source)) yield break;
            foreach (Match m in ClueTag.Matches(source))
            {
                var id = m.Groups["id"].Value.Trim();
                if (!string.IsNullOrEmpty(id)) yield return id;
            }
        }

        /// <summary>
        /// TMP 에 넘길 문자열로 변환한다.
        /// <paramref name="collectedClueIds"/> 에 이미 있는 단서는 밑줄을 지우고 수집 완료 색으로 칠한다.
        /// <paramref name="collectable"/> 이 false 면(예: 기억 짐작에서 왜곡된 구간) 링크 자체를 만들지 않는다.
        /// </summary>
        public static string ToRichText(string source, ICollection<string> collectedClueIds, bool collectable = true)
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;

            var result = ClueTag.Replace(source, m =>
            {
                var id = m.Groups["id"].Value.Trim();
                var body = m.Groups["body"].Value;

                if (!collectable)
                    return body;

                var alreadyCollected = collectedClueIds != null && collectedClueIds.Contains(id);
                if (alreadyCollected)
                    return $"<color={HighlightColor}>{body}</color>";

                return $"<link=\"{id}\"><u>{body}</u></link>";
            });

            result = HighlightTag.Replace(result, m => $"<color={HighlightColor}>{m.Groups["body"].Value}</color>");
            return result;
        }

        /// <summary>마크업을 모두 벗긴 평문. 로그나 검색에 쓴다.</summary>
        public static string ToPlainText(string source)
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;
            var result = ClueTag.Replace(source, m => m.Groups["body"].Value);
            result = HighlightTag.Replace(result, m => m.Groups["body"].Value);
            return result;
        }
    }
}
