using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 인트로 한 장면. 기획서의 인트로 카툰이 파츠 분할로 만들어지므로,
    /// 데이터 쪽도 "배경 한 장 + 그 위에서 오가는 대사" 단위로 끊어 둔다.
    /// </summary>
    [Serializable]
    public class PrologueBeat
    {
        [Tooltip("이 장면의 이름. 아트가 들어오기 전에는 화면에 그대로 표시된다.")]
        [SerializeField] private string backdropName;

        [Tooltip("아트가 준비되면 여기에 배경을 넣는다. 비어 있으면 이름만 띄운다.")]
        [SerializeField] private Sprite backdrop;

        [SerializeField] private DialogueSequence lines = new DialogueSequence();

        [Tooltip("이 장면이 끝나기 전에 플레이어가 눌러야 하는 상호작용. 없으면 비워 둔다.")]
        [SerializeField] private PrologueProp interaction;

        public string BackdropName => backdropName;
        public Sprite Backdrop => backdrop;
        public DialogueSequence Lines => lines;
        public PrologueProp Interaction => interaction;

        public bool HasInteraction => interaction != null && interaction.IsValid;
    }

    /// <summary>
    /// 인트로 중 클릭해서 들여다보는 물건.
    ///
    /// 기획서 인트로 흐름의 "전달받은 서류 내용 팝업(상호작용)" 이 이것이다.
    /// 읽지 않고는 넘어갈 수 없게 해서, 플레이어가 조수의 이력을 먼저 알고
    /// 본편의 의심에 공감할 수 있도록 한다.
    /// </summary>
    [Serializable]
    public class PrologueProp
    {
        [Tooltip("화면에 뜨는 버튼 문구. 예: 서류를 들여다본다")]
        [SerializeField] private string prompt;

        [SerializeField] private string documentTitle;

        [TextArea(4, 14)]
        [Tooltip("팝업 본문. <hl>…</hl> 로 감싼 부분이 강조된다.")]
        [SerializeField] private string documentBody;

        [Tooltip("팝업을 닫은 뒤 이어지는 대사.")]
        [SerializeField] private DialogueSequence afterReading = new DialogueSequence();

        public string Prompt => string.IsNullOrEmpty(prompt) ? "들여다본다" : prompt;
        public string DocumentTitle => documentTitle;
        public string DocumentBody => documentBody;
        public DialogueSequence AfterReading => afterReading;

        public bool IsValid => !string.IsNullOrEmpty(documentBody);
    }

    /// <summary>
    /// 게임 시작부터 사건 발생 직전까지의 도입부.
    ///
    /// 챕터와 분리한 이유는 페이즈 규칙이 하나도 적용되지 않기 때문이다.
    /// 여기에는 타임 토큰도, 수집률도, 페널티도 없다. 읽는 시간만 있다.
    /// </summary>
    [CreateAssetMenu(menuName = "It is You/Prologue", fileName = "Prologue_")]
    public class PrologueDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [SerializeField] private List<PrologueBeat> beats = new List<PrologueBeat>();

        [Tooltip("인트로가 끝나면 넘어갈 씬 이름. 비어 있으면 자동 전환하지 않는다.")]
        [SerializeField] private string nextSceneName;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public IReadOnlyList<PrologueBeat> Beats => beats;
        public string NextSceneName => nextSceneName;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
        }
#endif
    }
}
