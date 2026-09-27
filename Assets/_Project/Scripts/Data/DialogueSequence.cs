using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItIsYou.Data
{
    /// <summary>
    /// 대사 묶음 하나. 선택지 한 번 선택 분량, 오브젝트 조사 한 번 분량 같은
    /// "한 호흡"에 해당한다.
    /// </summary>
    [Serializable]
    public class DialogueSequence
    {
        [Tooltip("에디터에서 알아보기 위한 메모. 게임에는 쓰이지 않는다.")]
        [SerializeField] private string note;

        [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

        [Tooltip("이 묶음이 시작될 때 화면의 캐릭터에 거는 연출 이름. 비우면 아무 일도 없다. (StageAnimation 참고)")]
        [SerializeField] private string animationId;

        [Tooltip("이 묶음을 끝까지 본 뒤 신뢰도 변화량. 음수면 하락.")]
        [SerializeField] private int trustDelta = 0;

        [Tooltip("이 묶음을 끝까지 보면 곧바로 심문이 끝난다. (스토리 진행상 강제 종료)")]
        [SerializeField] private bool endsInterrogation = false;

        public string Note => note;
        public IReadOnlyList<DialogueLine> Lines => lines;
        public string AnimationId => animationId;
        public int TrustDelta => trustDelta;
        public bool EndsInterrogation => endsInterrogation;

        public int Count => lines?.Count ?? 0;
        public bool IsEmpty => Count == 0;

        public DialogueSequence() { }

        public DialogueSequence(IEnumerable<DialogueLine> source, string note = null)
        {
            this.note = note;
            lines = new List<DialogueLine>(source);
        }
    }
}
