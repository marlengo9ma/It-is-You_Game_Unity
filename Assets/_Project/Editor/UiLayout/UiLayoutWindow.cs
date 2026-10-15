using System.Collections.Generic;
using ItIsYou.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// UI 편집기 — 코드를 몰라도 화면 배치를 고칠 수 있게 하는 창.
    ///
    /// 1. 고칠 씬(또는 프리팹)을 연다.
    /// 2. '미리 보기'에서 페이즈를 고르면 그 화면이 켜진다(저장 · 플레이에는 영향 없음).
    /// 3. 씬에서 Rect Tool(T)로 옮기고, 인스펙터에서 이미지 · 글자 모양을 바꾼다.
    /// 4. Ctrl+S — 바뀐 것만 기록된다. 메뉴 5 로 씬을 다시 만들어도 그대로 남는다.
    /// </summary>
    public class UiLayoutWindow : EditorWindow
    {
        private static readonly (GamePhase phase, string label)[] Phases =
        {
            (GamePhase.Prelude, "기차역 대화"), (GamePhase.PreIncidentTalk, "사건 이전 대화"), (GamePhase.Cutscene, "컷씬"),
            (GamePhase.ClueCollection, "단서 수집"), (GamePhase.SuspectSelect, "용의자 선택"), (GamePhase.Interrogation1, "심문"),
            (GamePhase.Deduction, "추론"), (GamePhase.Accusation, "지목"), (GamePhase.Ending, "엔딩"), (GamePhase.GameOver, "게임오버"),
        };

        private Vector2 _scroll;
        private bool _showOtherHidden;
        private string _hiddenFilter = "";
        private bool _showHelp = true;

        [MenuItem("It is You/0. UI 편집기 (위치 · 이미지 · 글자)", priority = 1)]
        public static void Open()
        {
            var w = GetWindow<UiLayoutWindow>();
            w.titleContent = new GUIContent("UI 편집기");
            w.minSize = new Vector2(360f, 420f);
            w.Show();
        }

        private void OnSelectionChange() => Repaint();
        private void OnHierarchyChange() => Repaint();
        private double _nextRepaint;

        // 1초에 한 번만 다시 그린다 — 챕터 씬은 오브젝트가 많다.
        private void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextRepaint) return;
            _nextRepaint = EditorApplication.timeSinceStartup + 1.0;
            Repaint();
        }

        // ------------------------------------------------------------ 지금 편집 중인 대상

        private struct Current
        {
            public string key;
            public bool prefab;
            public Scene scene;
            public IList<GameObject> roots;
            public bool dirty;
        }

        private static bool TryGetCurrent(out Current c)
        {
            c = default;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                c.key = UiLayout.Norm(stage.assetPath);
                c.prefab = true;
                c.scene = stage.scene;
                c.roots = new[] { stage.prefabContentsRoot };
                c.dirty = stage.scene.isDirty;
                return true;
            }
            var s = SceneManager.GetActiveScene();
            if (!s.IsValid() || string.IsNullOrEmpty(s.path)) return false;
            c.key = UiLayout.Norm(s.path);
            c.scene = s;
            c.roots = s.GetRootGameObjects();
            c.dirty = s.isDirty;
            return true;
        }

        // ------------------------------------------------------------ 그리기

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            _showHelp = EditorGUILayout.Foldout(_showHelp, "사용법", true);
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "① 아래에서 고칠 씬(또는 프리팹)을 엽니다.\n" +
                    "② '미리 보기'에서 페이즈를 누르면 그 화면이 켜집니다.\n" +
                    "③ 씬 창에서 Rect Tool(단축키 T)로 옮기고 크기를 바꿉니다.\n" +
                    "    이미지 · 색 · 글자 크기 · 정렬은 인스펙터에서 바꿉니다.\n" +
                    "    앞뒤 순서는 Hierarchy 에서 끌어 옮깁니다(아래일수록 앞).\n" +
                    "④ Ctrl+S — 바뀐 것만 기록됩니다. 메뉴 5 를 돌려도 그대로 남아요.\n\n" +
                    "글 내용(대사 · 버튼 문구)과 켜짐/꺼짐은 게임이 정하므로 기록하지 않아요.",
                    MessageType.Info);
            }

            DrawOpenButtons();

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("플레이 중에는 편집할 수 없어요. 플레이를 멈춘 뒤 고치세요(플레이 중 바꾼 값은 저장되지 않아요).", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (!TryGetCurrent(out var cur))
            {
                EditorGUILayout.HelpBox("열린 씬이 없어요. 위에서 씬을 여세요.", MessageType.None);
                EditorGUILayout.EndScrollView();
                return;
            }

            Section($"지금 편집 중: {UiLayout.LabelOf(cur.key)}");

            if (!UiLayout.HasBaseline(cur.key))
            {
                var isKnown = false;
                foreach (var k in UiLayout.Known) if (k.key == cur.key) isKnown = true;
                if (!isKnown)
                {
                    EditorGUILayout.HelpBox("이 씬(프리팹)은 UI 편집기 대상이 아니에요. 위의 버튼으로 게임 씬을 여세요.\n" +
                                            "(인트로 카툰 컷은 메뉴 9-c 의 IntroComic 프리팹에서 직접 고치면 그대로 남아요.)", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("아직 기준이 없어요. 메뉴 5(전체 다시 만들기)를 한 번 돌리면 그때부터 기록할 수 있어요.\n" +
                                            "(씬에서 직접 고쳐 둔 것이 있으면 다시 만들 때 사라져요.)", MessageType.Warning);
                    if (GUILayout.Button("메뉴 5 — 전체 다시 만들기", GUILayout.Height(28)))
                        EditorApplication.delayCall += GameBuildSetup.RebuildAll;
                }
                EditorGUILayout.EndScrollView();
                return;
            }

            if (cur.dirty)
                EditorGUILayout.HelpBox("저장하지 않은 변경이 있어요 — Ctrl+S 를 누르면 기록됩니다.", MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("저장하고 기록 (Ctrl+S)", GUILayout.Height(26))) SaveCurrent(cur);
                if (!cur.prefab && GUILayout.Button("씬 창을 UI 편집용으로", GUILayout.Height(26))) FrameCanvas(cur);
            }

            if (!cur.prefab) DrawPreview(cur);
            DrawSelection(cur);
            DrawRecorded(cur);

            EditorGUILayout.Space(12);
            EditorGUILayout.EndScrollView();
        }

        private static void Section(string title)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            var r = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.35f));
            EditorGUILayout.Space(4);
        }

        // ------------------------------------------------------------ 열기

        private void DrawOpenButtons()
        {
            Section("열기");
            EditorGUILayout.LabelField("씬", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var k in UiLayout.Known)
                {
                    if (k.prefab) continue;
                    if (GUILayout.Button(ShortLabel(k.label))) OpenScene(k.key);
                }
            }
            EditorGUILayout.LabelField("프리팹 (게임 중에 여러 개 만들어지는 것)", EditorStyles.miniBoldLabel);
            var count = 0;
            EditorGUILayout.BeginHorizontal();
            foreach (var k in UiLayout.Known)
            {
                if (!k.prefab) continue;
                if (count > 0 && count % 3 == 0) { EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal(); }
                if (GUILayout.Button(k.label)) OpenPrefab(k.key);
                count++;
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("인트로 카툰 컷 (IntroComic 프리팹 · 메뉴 9-c)"))
                OpenPrefab(IntroComicImporter.PrefabPath);
        }

        private static string ShortLabel(string label)
        {
            var i = label.IndexOf(" (");
            return i > 0 ? label.Substring(0, i) : label;
        }

        private static void OpenScene(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                EditorUtility.DisplayDialog("UI 편집기", "씬 파일이 없어요. 메뉴 5 를 먼저 돌리세요.", "확인");
                return;
            }
            StageUtility.GoToMainStage();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        private static void OpenPrefab(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null)
            {
                EditorUtility.DisplayDialog("UI 편집기", "프리팹이 없어요. 메뉴 5 를 먼저 돌리세요.", "확인");
                return;
            }
            AssetDatabase.OpenAsset(go);
        }

        private static void SaveCurrent(Current cur)
        {
            if (cur.prefab)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null)
                {
                    EditorSceneManager.MarkSceneDirty(stage.scene);
                    // 프리팹 편집 화면의 저장 — prefabSaving 이 기록한다.
                    const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                    var save = typeof(PrefabStage).GetMethod("Save", F, null, System.Type.EmptyTypes, null)
                               ?? typeof(PrefabStage).GetMethod("SavePrefab", F, null, System.Type.EmptyTypes, null);
                    if (save != null) save.Invoke(stage, null);
                    else EditorUtility.DisplayDialog("UI 편집기", "프리팹 편집 화면 위쪽의 Save 버튼을 눌러 주세요.", "확인");
                }
                return;
            }
            EditorSceneManager.MarkSceneDirty(cur.scene);
            EditorSceneManager.SaveScene(cur.scene);
        }

        private static void FrameCanvas(Current cur)
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null) sv = GetWindow<SceneView>();
            sv.in2DMode = true;
            foreach (var r in cur.roots)
            {
                if (r != null && r.GetComponent<Canvas>() != null)
                {
                    Selection.activeGameObject = r;
                    sv.FrameSelected();
                    break;
                }
            }
            Tools.current = Tool.Rect;
            sv.Focus();
        }

        // ------------------------------------------------------------ 미리 보기

        private void DrawPreview(Current cur)
        {
            Section("미리 보기  (저장 · 플레이에는 영향 없음)");
            var b = UiLayout.LoadBaseline(cur.key);
            var hasPhases = UiPreview.FindIn<ItIsYou.UI.PhaseScreenBinder>(cur.roots) != null;

            if (hasPhases)
            {
                EditorGUILayout.LabelField("페이즈 — 누르면 그 페이즈의 화면이 켜져요", EditorStyles.miniBoldLabel);
                var perRow = Mathf.Max(2, Mathf.FloorToInt((position.width - 24f) / 110f));
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < Phases.Length; i++)
                {
                    if (i > 0 && i % perRow == 0) { EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal(); }
                    var on = UiPreview.Phase == (int)Phases[i].phase && UiPreview.ScenePath == cur.key;
                    var next = GUILayout.Toggle(on, Phases[i].label, "Button");
                    if (next != on)
                    {
                        UiPreview.Phase = next ? (int)Phases[i].phase : -1;
                        UiPreview.Apply(cur.scene, keepClean: !cur.dirty);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            // 평소에 숨어 있는 것들(팝업 · 창 · 안내). 켜 두고 자리를 잡을 수 있다.
            var frequent = new List<string>();
            var others = new List<string>();
            foreach (var n in b.nodes)
            {
                var hidden = !n.active || (n.hasGroup && n.groupAlpha < 0.01f);
                if (!hidden) continue;
                var last = n.path.Substring(n.path.LastIndexOf('/') + 1);
                var bare = last.Split('#')[0];
                if (bare.StartsWith("Screen_")) continue;
                if (UiLayout.Friendly.ContainsKey(bare)) frequent.Add(n.path);
                else others.Add(n.path);
            }

            if (frequent.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("겹쳐 보기 — 평소에 숨어 있는 창 · 팝업", EditorStyles.miniBoldLabel);
                foreach (var p in frequent) HiddenToggle(cur, p, UiLayout.FriendlyName(p));
            }

            _showOtherHidden = EditorGUILayout.Foldout(_showOtherHidden, $"그 밖의 숨겨진 것 ({others.Count})", true);
            if (_showOtherHidden)
            {
                _hiddenFilter = EditorGUILayout.TextField("찾기", _hiddenFilter);
                foreach (var p in others)
                {
                    if (!string.IsNullOrEmpty(_hiddenFilter) && p.IndexOf(_hiddenFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    HiddenToggle(cur, p, p);
                }
            }

            EditorGUI.BeginDisabledGroup(!UiPreview.IsOn);
            if (GUILayout.Button("미리 보기 끄기 (원래대로)")) UiPreview.Off(cur.scene);
            EditorGUI.EndDisabledGroup();
        }

        private static void HiddenToggle(Current cur, string path, string label)
        {
            var on = UiPreview.Shown.Contains(path) && UiPreview.ScenePath == cur.key;
            var next = EditorGUILayout.ToggleLeft(label, on);
            if (next == on) return;
            if (next) UiPreview.Shown.Add(path); else UiPreview.Shown.Remove(path);
            UiPreview.Apply(cur.scene, keepClean: !cur.dirty);
        }

        // ------------------------------------------------------------ 선택한 오브젝트

        private void DrawSelection(Current cur)
        {
            Section("선택한 오브젝트");
            var t = Selection.activeTransform;
            if (t == null)
            {
                EditorGUILayout.LabelField("씬 창이나 Hierarchy 에서 UI 를 하나 고르세요.", EditorStyles.wordWrappedMiniLabel);
                return;
            }

            if (t.gameObject.scene != cur.scene)
            {
                EditorGUILayout.LabelField("지금 편집 중인 씬의 오브젝트가 아니에요.", EditorStyles.wordWrappedMiniLabel);
                return;
            }

            if (!cur.prefab && PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
            {
                EditorGUILayout.HelpBox("인트로 카툰(IntroComic 프리팹) 안의 컷이에요. 위의 '인트로 카툰 컷' 버튼으로 프리팹을 열어 고치면 그대로 남아요.", MessageType.Info);
                return;
            }

            var path = UiLayout.PathOf(t, cur.prefab);
            var b = UiLayout.LoadBaseline(cur.key);
            var node = b?.Get(path);
            EditorGUILayout.LabelField(UiLayout.FriendlyName(path), EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(path, EditorStyles.miniLabel, GUILayout.Height(16));

            if (!(t is RectTransform))
            {
                EditorGUILayout.LabelField("UI 가 아니에요(화면 배치와 상관없음).", EditorStyles.wordWrappedMiniLabel);
                return;
            }
            if (node == null)
            {
                EditorGUILayout.HelpBox("빌더가 만든 오브젝트가 아니에요(새로 추가했거나 이름을 바꿈). 이런 오브젝트는 기록되지 않아 메뉴 5 때 사라져요.\n" +
                                        "새 UI 가 필요하면 Claude 에게 말해 주세요.", MessageType.Warning);
                return;
            }

            if (UiLayout.IsLayoutDriven(t))
                EditorGUILayout.HelpBox("자동 정렬 묶음 안에 있어서 위치 · 크기는 부모(Layout Group)가 정해요. 부모를 선택해 간격 · 여백(Spacing · Padding)을 바꾸세요.", MessageType.Info);

            var target = UiLayout.Overrides.Find(cur.key);
            var entry = target?.entries.Find(e => e.path == path);
            var order = target?.orders.Find(o => o.parentPath == path);

            var pending = UiLayout.Diff(UiLayout.Read(t.gameObject), node.values);
            if (entry == null && order == null && pending == UiProp.None)
            {
                EditorGUILayout.LabelField("바꾼 것 없음 (빌더가 만든 그대로)", EditorStyles.miniLabel);
                return;
            }
            if (entry != null) EditorGUILayout.LabelField("기록됨: " + UiLayout.Describe(entry.props), EditorStyles.wordWrappedMiniLabel);
            if (order != null) EditorGUILayout.LabelField("기록됨: 자식들의 앞뒤 순서", EditorStyles.wordWrappedMiniLabel);
            if (pending != UiProp.None && (entry == null || pending != entry.props || UiLayout.Diff(UiLayout.Read(t.gameObject), entry.values) != UiProp.None))
                EditorGUILayout.LabelField("저장 전: " + UiLayout.Describe(pending) + " — Ctrl+S 로 기록", EditorStyles.wordWrappedMiniLabel);

            if ((entry != null || order != null) && GUILayout.Button("이 오브젝트를 원래대로 되돌리기"))
            {
                UiLayout.Revert(cur.key, path, values: true, order: true);
                GUIUtility.ExitGUI();
            }
        }

        // ------------------------------------------------------------ 기록된 변경

        private void DrawRecorded(Current cur)
        {
            var target = UiLayout.Overrides.Find(cur.key);
            var count = target == null ? 0 : target.entries.Count + target.orders.Count;
            Section($"기록된 변경 ({count}건)");
            if (target == null || count == 0)
            {
                EditorGUILayout.LabelField("아직 없어요.", EditorStyles.miniLabel);
                return;
            }

            var map = UiLayout.WalkMap(cur.roots, cur.prefab);
            foreach (var e in new List<UiEntry>(target.entries))
                Row(cur, map, e.path, UiLayout.Describe(e.props), isOrder: false);
            foreach (var o in new List<UiOrder>(target.orders))
                Row(cur, map, o.parentPath, "자식들의 앞뒤 순서", isOrder: true);

            EditorGUILayout.Space(6);
            if (GUILayout.Button("이 화면의 변경 모두 되돌리기") &&
                EditorUtility.DisplayDialog("UI 편집기", $"「{UiLayout.LabelOf(cur.key)}」에서 바꾼 {count}건을 모두 빌더 값으로 되돌릴까요?\n(되돌린 뒤 씬이 저장됩니다)", "되돌리기", "취소"))
            {
                UiLayout.RevertAll(cur.key);
                GUIUtility.ExitGUI();
            }
        }

        private void Row(Current cur, Dictionary<string, Transform> map, string path, string what, bool isOrder)
        {
            var found = map.TryGetValue(path, out var t);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(UiLayout.FriendlyName(path), EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(found ? what : what + "  — 찾을 수 없음(이름이 바뀌었거나 없어짐)", EditorStyles.wordWrappedMiniLabel);
                }
                if (found && GUILayout.Button("선택", GUILayout.Width(48), GUILayout.Height(32)))
                {
                    if (!t.gameObject.activeInHierarchy && !cur.prefab)
                    {
                        UiPreview.Shown.Add(path);
                        UiPreview.Apply(cur.scene, keepClean: !cur.dirty);
                    }
                    Selection.activeTransform = t;
                    EditorGUIUtility.PingObject(t.gameObject);
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
                if (GUILayout.Button(found ? "되돌리기" : "지우기", GUILayout.Width(64), GUILayout.Height(32)))
                {
                    UiLayout.Revert(cur.key, path, values: !isOrder, order: isOrder);
                    GUIUtility.ExitGUI();
                }
            }
        }
    }
}
