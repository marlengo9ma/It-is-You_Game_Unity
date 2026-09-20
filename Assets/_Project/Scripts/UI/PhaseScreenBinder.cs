using System;
using System.Collections.Generic;
using ItIsYou.Core;
using ItIsYou.Data;
using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>
    /// 페이즈에 따라 화면 묶음을 켜고 끈다.
    ///
    /// 페이즈마다 씬을 나누지 않는 이유는, 단서 목록과 대사창이 페이즈를 가로질러
    /// 그대로 살아 있어야 하기 때문이다. 씬을 갈아타면 그때마다 상태를 옮겨야 한다.
    /// </summary>
    [AddComponentMenu("It is You/Phase Screen Binder")]
    public class PhaseScreenBinder : MonoBehaviour
    {
        [Serializable]
        public class ScreenGroup
        {
            [Tooltip("이 묶음이 보일 페이즈들.")]
            public List<GamePhase> phases = new List<GamePhase>();

            public GameObject root;
        }

        [SerializeField] private GameFlowController flow;
        [SerializeField] private List<ScreenGroup> groups = new List<ScreenGroup>();

        [Tooltip("모든 페이즈에서 계속 보이는 것들. 단서 목록, 대사창 등.")]
        [SerializeField] private List<GameObject> alwaysVisible = new List<GameObject>();

        private void Awake()
        {
            if (flow != null) flow.PhaseChanged += Apply;
            // 시작 시점에는 전부 숨긴다. 첫 페이즈 전환이 필요한 것만 켠다.
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i]?.root != null) groups[i].root.SetActive(false);
            }
            for (int i = 0; i < alwaysVisible.Count; i++)
            {
                if (alwaysVisible[i] != null) alwaysVisible[i].SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (flow != null) flow.PhaseChanged -= Apply;
        }

        public void Apply(GamePhase phase)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                if (g?.root == null) continue;
                g.root.SetActive(g.phases != null && g.phases.Contains(phase));
            }
        }
    }
}
