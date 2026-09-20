using System.Collections.Generic;
using ItIsYou.Core;
using ItIsYou.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.UI
{
    /// <summary>
    /// 평범한 대화 구간에만 붙는 두 개의 버튼 — 로그, 창 숨기기.
    ///
    /// 증거 수집·심문·단서 조합 페이즈에서는 나타나지 않는다.
    /// 그 세 구간에는 타임 토큰과 무반응 타이머가 돌고 있어서,
    /// 대사창을 치우거나 지난 대사를 들추는 일이 규칙의 일부여야 하기 때문이다.
    /// (심문에서 지난 대사를 보는 수단은 [기억 짐작] 이고, 값을 치른다.)
    /// </summary>
    [AddComponentMenu("It is You/Dialogue Toolbar")]
    public class DialogueToolbar : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private GameFlowController flow;
        [SerializeField] private DialoguePresenter dialogue;
        [SerializeField] private DialogueLogView log;

        [Header("화면")]
        [Tooltip("두 버튼을 담고 있는 묶음. 해당 페이즈가 아니면 통째로 꺼진다.")]
        [SerializeField] private GameObject root;

        [SerializeField] private Button logButton;
        [SerializeField] private Button hideButton;

        [Tooltip("창을 숨긴 동안 화면 전체를 덮는 투명 버튼. 아무 데나 누르면 되돌아온다.")]
        [SerializeField] private GameObject restoreCatcher;

        [SerializeField] private Button restoreButton;

        [Header("표시 조건")]
        [Tooltip("이 페이즈들에서만 버튼이 나타난다.")]
        [SerializeField]
        private List<GamePhase> visiblePhases = new List<GamePhase>
        {
            GamePhase.PreIncidentTalk,
            GamePhase.Cutscene,
            GamePhase.Intro
        };

        private void Awake()
        {
            if (logButton != null) logButton.onClick.AddListener(OnLog);
            if (hideButton != null) hideButton.onClick.AddListener(OnHide);
            if (restoreButton != null) restoreButton.onClick.AddListener(OnRestore);

            if (restoreCatcher != null) restoreCatcher.SetActive(false);

            if (flow != null)
            {
                flow.PhaseChanged += Apply;
                Apply(flow.Phase);
            }
            else
            {
                // 흐름 컨트롤러가 없는 씬(인트로 등)에서는 그냥 켜 둔다.
                if (root != null) root.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (logButton != null) logButton.onClick.RemoveListener(OnLog);
            if (hideButton != null) hideButton.onClick.RemoveListener(OnHide);
            if (restoreButton != null) restoreButton.onClick.RemoveListener(OnRestore);
            if (flow != null) flow.PhaseChanged -= Apply;
        }

        private void Apply(GamePhase phase)
        {
            var show = visiblePhases != null && visiblePhases.Contains(phase);
            if (root != null) root.SetActive(show);

            // 페이즈가 바뀌면 숨김과 로그는 원래대로 되돌린다.
            // 심문이 시작됐는데 대사창이 치워져 있으면 플레이어가 길을 잃는다.
            if (!show)
            {
                OnRestore();
                log?.Close();
            }
        }

        private void OnLog() => log?.Toggle();

        private void OnHide()
        {
            if (dialogue == null) return;
            dialogue.SetHidden(true);
            if (root != null) root.SetActive(false);
            if (restoreCatcher != null) restoreCatcher.SetActive(true);
        }

        private void OnRestore()
        {
            if (dialogue != null) dialogue.SetHidden(false);
            if (restoreCatcher != null) restoreCatcher.SetActive(false);

            var phase = flow != null ? flow.Phase : GamePhase.None;
            var show = flow == null || (visiblePhases != null && visiblePhases.Contains(phase));
            if (root != null) root.SetActive(show);
        }
    }
}
