using System;
using System.Collections;
using System.Collections.Generic;
using ItIsYou.Clues;
using ItIsYou.Data;
using ItIsYou.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItIsYou.Investigation
{
    /// <summary>
    /// 단서 수집 페이즈의 진행자.
    ///
    /// 씬에 놓인 <see cref="InteractableObject"/> 들을 모아 붙이고,
    /// 클릭할 때마다 대사를 재생하고 단서를 넣는다.
    /// 페이즈를 끝내려 할 때는 <see cref="CollectionGate"/> 로 수집률을 판정한다.
    /// </summary>
    [AddComponentMenu("It is You/Investigation Controller")]
    public class InvestigationController : MonoBehaviour
    {
        [Header("씬")]
        [Tooltip("비워 두면 자식에서 자동으로 모은다.")]
        [SerializeField] private List<InteractableObject> sceneObjects = new List<InteractableObject>();

        [Header("UI")]
        [SerializeField] private DialoguePresenter dialogue;
        [SerializeField] private CursorController cursor;
        [SerializeField] private ClueTrayView clueTray;

        [Tooltip("다음 페이즈로 넘어가는 버튼. 에디터에서 붙인 연결은 저장되지 않으므로 런타임에 직접 잇는다.")]
        [SerializeField] private Button advanceButton;

        [Tooltip("수집 상황 표시. 남은 개수를 직접 세어 주지는 않는다 — 몇 개가 더 있는지 알려주면 탐색이 아니라 체크리스트가 된다.")]
        [SerializeField] private TMP_Text progressLabel;

        private ChapterDefinition _chapter;
        private ClueInventory _inventory;
        private bool _busy;

        /// <summary>페이즈를 끝내도 된다는 판정이 났다.</summary>
        public event Action<CollectionGate> ReadyToAdvance;

        /// <summary>경고 구간이라 플레이어 확인이 필요하다.</summary>
        public event Action<CollectionGate> ConfirmationRequested;

        /// <summary>단서를 새로 얻었다.</summary>
        public event Action<ClueDefinition> ClueCollected;

        public bool IsBusy => _busy;

        public void Begin(ChapterDefinition chapter, ClueInventory inventory)
        {
            _chapter = chapter;
            _inventory = inventory;

            if (sceneObjects == null || sceneObjects.Count == 0)
                sceneObjects = new List<InteractableObject>(GetComponentsInChildren<InteractableObject>(true));

            for (int i = 0; i < sceneObjects.Count; i++)
            {
                var obj = sceneObjects[i];
                if (obj == null) continue;
                obj.HoverChanged += OnHoverChanged;
                obj.Clicked += OnClicked;
                obj.SetEnabled(true);
            }

            if (advanceButton != null)
            {
                advanceButton.onClick.RemoveListener(RequestAdvance);
                advanceButton.onClick.AddListener(RequestAdvance);
            }

            dialogue.SyncCollected(inventory.OwnedIds);
            RefreshTray();
            RefreshProgress();
        }

        private void OnDisable()
        {
            for (int i = 0; i < sceneObjects.Count; i++)
            {
                var obj = sceneObjects[i];
                if (obj == null) continue;
                obj.HoverChanged -= OnHoverChanged;
                obj.Clicked -= OnClicked;
            }
        }

        private void OnHoverChanged(InteractableObject obj, bool hovering)
        {
            if (_busy) return;
            cursor?.SetHoveringInteractable(hovering);
        }

        private void OnClicked(InteractableObject obj)
        {
            if (_busy || obj == null || obj.Definition == null) return;
            StartCoroutine(InspectRoutine(obj));
        }

        private IEnumerator InspectRoutine(InteractableObject obj)
        {
            _busy = true;
            SetObjectsEnabled(false);
            cursor?.ResetState();

            var def = obj.Definition;
            var lines = obj.Inspected ? def.OnReinspect : def.OnInspect;
            obj.MarkInspected();

            if (lines != null && lines.Count > 0)
                yield return dialogue.Play(new DialogueSequence(lines, def.Id));

            // 특수 오브젝트라면 단서를 넣는다. 일반 오브젝트는 대사만 나오고 끝난다.
            if (def.GrantsClue && _inventory.Add(def.GrantedClue))
            {
                dialogue.NotifyCollected(def.GrantedClue.Id);
                RefreshTray();
                ClueCollected?.Invoke(def.GrantedClue);
            }

            RefreshProgress();
            _busy = false;
            SetObjectsEnabled(true);
        }

        /// <summary>플레이어가 다음 페이즈로 넘어가려 한다.</summary>
        public void RequestAdvance()
        {
            if (_busy) return;
            StartCoroutine(AdvanceRoutine());
        }

        private IEnumerator AdvanceRoutine()
        {
            _busy = true;
            SetObjectsEnabled(false);

            var gate = CollectionGate.Evaluate(_chapter, _inventory);
            var speech = gate.PickDialogue(_chapter);

            if (speech != null && !speech.IsEmpty)
                yield return dialogue.Play(speech);

            _busy = false;

            switch (gate.Result)
            {
                case CollectionGateResult.Pass:
                    ReadyToAdvance?.Invoke(gate);
                    break;

                case CollectionGateResult.WarnAndConfirm:
                    // 진행 여부는 플레이어가 고른다. 여기서는 판정만 넘긴다.
                    ConfirmationRequested?.Invoke(gate);
                    SetObjectsEnabled(true);
                    break;

                default:
                    // 진행 불가. 계속 뒤져야 한다.
                    SetObjectsEnabled(true);
                    break;
            }
        }

        /// <summary>경고 팝업에서 "그래도 진행"을 골랐을 때.</summary>
        public void ConfirmAdvanceAnyway()
        {
            var gate = CollectionGate.Evaluate(_chapter, _inventory);
            if (gate.Result == CollectionGateResult.Blocked) return;
            SetObjectsEnabled(false);
            ReadyToAdvance?.Invoke(gate);
        }

        /// <summary>지금 수집 상태를 그대로 판정해 본다. UI 표시용.</summary>
        public CollectionGate PeekGate() => CollectionGate.Evaluate(_chapter, _inventory);

        private void SetObjectsEnabled(bool value)
        {
            for (int i = 0; i < sceneObjects.Count; i++)
                sceneObjects[i]?.SetEnabled(value);
        }

        private void RefreshTray()
        {
            if (clueTray != null) clueTray.Rebuild(_inventory);
            RefreshProgress();
        }

        /// <summary>조사한 곳과 남은 곳을 알린다. 단서 개수는 드러내지 않는다.</summary>
        private void RefreshProgress()
        {
            if (progressLabel == null) return;
            var seen = 0;
            for (int i = 0; i < sceneObjects.Count; i++)
            {
                if (sceneObjects[i] != null && sceneObjects[i].Inspected) seen++;
            }
            progressLabel.text = $"살펴본 곳  {seen} / {sceneObjects.Count}";
        }
    }
}
