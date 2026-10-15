using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ItIsYou.Core;
using ItIsYou.Data;
using ItIsYou.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 씬을 코드로 다시 만들어도(메뉴 5) 손으로 고친 UI 가 남게 하는 장치.
    ///
    /// 1. 빌더가 씬(또는 프리팹)을 다 만들면 그 상태를 '기준'으로 적어 둔다 (프로젝트 폴더/UiLayout/Baseline/*.json).
    /// 2. 제작자가 씬에서 고치고 Ctrl+S 하면, 기준과 달라진 값만 골라 UiLayoutOverrides 에셋에 적는다.
    /// 3. 다음에 빌더가 씬을 만들 때 마지막에 그 값을 다시 입힌다.
    ///
    /// 다루는 것: 자리 · 크기 · 회전 · 배율, 이미지(그림 · 색 · 비율 유지), 글자 모양(글꼴 · 크기 · 색 · 정렬 · 스타일 · 자간 · 줄 간격),
    /// 자동 정렬 묶음의 간격 · 여백, 앞뒤 순서. 글 내용 · 켜짐/꺼짐은 다루지 않는다(게임이 정한다).
    /// </summary>
    [InitializeOnLoad]
    public static class UiLayout
    {
        public const string OverridesFolder = "Assets/_Project/UiLayout";
        public const string OverridesPath = OverridesFolder + "/UiLayoutOverrides.asset";
        public const string PrefabRootKey = "(프리팹 맨 위)";

        /// <summary>빌더가 저장하는 중 — 이때는 기록하지 않는다.</summary>
        public static bool Building;

        public struct TargetInfo
        {
            public string key;
            public string label;
            public bool prefab;
            public TargetInfo(string key, string label, bool prefab) { this.key = key; this.label = label; this.prefab = prefab; }
        }

        public static readonly TargetInfo[] Known =
        {
            new TargetInfo("Assets/_Project/Scenes/Title.unity", "타이틀", false),
            new TargetInfo("Assets/_Project/Scenes/Prologue.unity", "인트로 (카툰 위 버튼 · 서류 팝업)", false),
            new TargetInfo("Assets/_Project/Scenes/Tutorial_Chapter.unity", "튜토리얼 챕터 (모든 페이즈)", false),
            new TargetInfo("Assets/_Project/Prefabs/ChoiceButton.prefab", "선택지 버튼", true),
            new TargetInfo("Assets/_Project/Prefabs/ClueSlot.prefab", "단서 칸 (Evidence)", true),
            new TargetInfo("Assets/_Project/Prefabs/LeadSlot.prefab", "정황 칸 (Leads)", true),
            new TargetInfo("Assets/_Project/Prefabs/HypothesisSlot.prefab", "가설 칸", true),
            new TargetInfo("Assets/_Project/Prefabs/PlainButton.prefab", "기본 버튼", true),
        };

        public static string LabelOf(string key)
        {
            foreach (var k in Known) if (k.key == key) return k.label;
            return Path.GetFileNameWithoutExtension(key);
        }

        static UiLayout()
        {
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneOpened += (s, m) => UiPreview.Reset();
            EditorSceneManager.newSceneCreated += (s, setup, mode) => UiPreview.Reset();
            PrefabStage.prefabSaving += OnPrefabSaving;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        // ================================================================ 에셋

        public static UiLayoutOverrides Overrides
        {
            get
            {
                var asset = AssetDatabase.LoadAssetAtPath<UiLayoutOverrides>(OverridesPath);
                if (asset != null) return asset;
                Ed.EnsureFolder(OverridesFolder);
                asset = ScriptableObject.CreateInstance<UiLayoutOverrides>();
                asset.name = "UiLayoutOverrides";
                AssetDatabase.CreateAsset(asset, OverridesPath);
                AssetDatabase.SaveAssets();
                return asset;
            }
        }

        private static void SaveOverrides(UiLayoutOverrides asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        // ================================================================ 기준(빌더가 만든 값)

        [Serializable]
        internal class BaselineNode
        {
            public string path;
            public bool active;
            public bool hasGroup;
            public float groupAlpha = 1f;
            public bool groupInteractable = true;
            public bool groupBlocks = true;
            public UiValues values = new UiValues();
            public List<string> children = new List<string>();
        }

        [Serializable]
        internal class Baseline
        {
            public string key;
            public List<BaselineNode> nodes = new List<BaselineNode>();

            [NonSerialized] private Dictionary<string, BaselineNode> _map;

            public BaselineNode Get(string path)
            {
                if (_map == null)
                {
                    _map = new Dictionary<string, BaselineNode>();
                    foreach (var n in nodes) _map[n.path] = n;
                }
                return _map.TryGetValue(path, out var node) ? node : null;
            }
        }

        private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
        private static string BaselineFolder => Path.Combine(ProjectRoot, "UiLayout", "Baseline");

        private static string BaselineFile(string key)
        {
            var kind = key.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ? "Prefab" : "Scene";
            return Path.Combine(BaselineFolder, kind + "_" + Path.GetFileNameWithoutExtension(key) + ".json");
        }

        private static readonly Dictionary<string, (DateTime time, Baseline data)> _baselineCache =
            new Dictionary<string, (DateTime, Baseline)>();

        internal static Baseline LoadBaseline(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            key = Norm(key);
            var file = BaselineFile(key);
            if (!File.Exists(file)) return null;
            var time = File.GetLastWriteTimeUtc(file);
            if (_baselineCache.TryGetValue(key, out var cached) && cached.time == time) return cached.data;
            var b = new Baseline();
            EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(file), b);
            _baselineCache[key] = (time, b);
            return b;
        }

        public static bool HasBaseline(string key) => File.Exists(BaselineFile(Norm(key)));

        private static void SaveBaseline(Baseline b)
        {
            Directory.CreateDirectory(BaselineFolder);
            File.WriteAllText(BaselineFile(b.key), EditorJsonUtility.ToJson(b, true));
            _baselineCache.Remove(b.key);
        }

        internal static Baseline Capture(string key, IList<GameObject> roots, bool prefab)
        {
            var b = new Baseline { key = Norm(key) };
            foreach (var (path, t) in Walk(roots, prefab))
            {
                var node = new BaselineNode
                {
                    path = path,
                    active = t.gameObject.activeSelf,
                    values = Read(t.gameObject),
                    children = ChildKeys(t)
                };
                var g = t.GetComponent<CanvasGroup>();
                if (g != null)
                {
                    node.hasGroup = true;
                    node.groupAlpha = g.alpha;
                    node.groupInteractable = g.interactable;
                    node.groupBlocks = g.blocksRaycasts;
                }
                b.nodes.Add(node);
            }
            return b;
        }

        // ================================================================ 빌더에서 부르는 곳

        /// <summary>빌더가 씬을 다 만든 뒤 저장할 때. 기준을 적고, 편집기에서 바꾼 값을 입히고, 저장한다.</summary>
        public static void SaveBuiltScene(Scene scene, string path)
        {
            path = Norm(path);
            var roots = scene.GetRootGameObjects();
            Canvas.ForceUpdateCanvases();
            SaveBaseline(Capture(path, roots, false));
            var result = ApplyOverrides(path, roots, false);

            Building = true;
            try
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, path);
            }
            finally { Building = false; }

            Report(path, result);
        }

        /// <summary>빌더가 프리팹(선택지 버튼 · 단서 칸 …)을 저장할 때.</summary>
        public static GameObject SaveBuiltPrefab(GameObject go, string path)
        {
            path = Norm(path);
            var roots = new[] { go };
            SaveBaseline(Capture(path, roots, true));
            var result = ApplyOverrides(path, roots, true);
            Building = true;
            try { return PrefabUtility.SaveAsPrefabAsset(go, path); }
            finally
            {
                Building = false;
                Report(path, result);
            }
        }

        private static void Report(string key, (int applied, List<string> missing) result)
        {
            if (result.applied > 0)
                Debug.Log($"[It is You] UI 편집기 변경 {result.applied}건을 입혔습니다 — {LabelOf(key)}");
            if (result.missing.Count > 0)
                Debug.LogWarning($"[It is You] UI 편집기 변경 중 {result.missing.Count}건은 대상을 찾지 못했습니다 — {LabelOf(key)}\n" +
                                 string.Join("\n", result.missing) +
                                 "\n(오브젝트 이름이 바뀌었거나 없어졌습니다. 메뉴 It is You › UI 편집기 에서 확인하세요.)");
        }

        /// <summary>
        /// 씬을 다시 만들기 직전. 열린 씬에 저장하지 않은 변경이 있으면 기록할지 묻는다 —
        /// 다시 만들면 그 씬이 닫히면서 저장 안 한 변경이 사라지기 때문이다.
        /// </summary>
        public static void RecordBeforeRebuild()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.isLoaded || !s.isDirty) continue;
                var b = LoadBaseline(s.path);
                if (b == null) continue;
                var roots = s.GetRootGameObjects();
                if (CountPending(s.path, b, roots, false) == 0) continue;
                var record = Application.isBatchMode || EditorUtility.DisplayDialog("UI 편집기",
                    $"「{LabelOf(Norm(s.path))}」 씬에 저장하지 않은 UI 변경이 있어요.\n\n" +
                    "다시 만들기 전에 기록해 둘까요? (기록하지 않으면 이번 변경은 사라져요)",
                    "기록하고 계속", "기록하지 않음");
                if (record) Record(Norm(s.path), b, roots, false);
            }
        }

        // ================================================================ 저장할 때 기록

        private static void OnSceneSaving(Scene scene, string path)
        {
            if (Building) return;
            path = Norm(path);
            if (Norm(scene.path) != path) return;   // '다른 이름으로 저장'은 건드리지 않는다
            var b = LoadBaseline(path);
            if (b == null) return;
            var roots = scene.GetRootGameObjects();
            Record(path, b, roots, false);
            // 미리 보기로 켠 화면 · 팝업은 저장하지 않는다 — 게임이 시작될 때의 모습은 빌더가 정한 그대로.
            RestoreStates(b, roots, false);
        }

        private static void OnSceneSaved(Scene scene)
        {
            if (Building) return;
            if (UiPreview.IsOn && Norm(scene.path) == UiPreview.ScenePath)
                UiPreview.Apply(scene, keepClean: true);
        }

        private static void OnPrefabSaving(GameObject root)
        {
            if (Building || root == null) return;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || stage.prefabContentsRoot != root) return;
            var key = Norm(stage.assetPath);
            var b = LoadBaseline(key);
            if (b == null) return;
            Record(key, b, new[] { root }, true);
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) UiPreview.HideForPlay();
            else if (change == PlayModeStateChange.EnteredEditMode) UiPreview.ResumeAfterPlay();
        }

        /// <summary>지금 열린 씬(프리팹)과 기준을 비교해 바뀐 것을 에셋에 다시 적는다. 대상이 없어진 기록은 남겨 둔다.</summary>
        internal static int Record(string key, Baseline b, IList<GameObject> roots, bool prefab)
        {
            var (entries, orders) = Collect(b, roots, prefab);
            var asset = Overrides;
            var target = asset.GetOrAdd(key);
            var present = WalkMap(roots, prefab);

            // 이번에 찾지 못한 대상의 기록은 지우지 않는다(이름이 잠시 바뀐 경우 등) — 편집기에서 '찾을 수 없음'으로 보인다.
            var keptEntries = target.entries.FindAll(e => !present.ContainsKey(e.path));
            keptEntries.AddRange(entries);
            var keptOrders = target.orders.FindAll(o => !present.ContainsKey(o.parentPath));
            keptOrders.AddRange(orders);

            target.entries = keptEntries;
            target.orders = keptOrders;
            if (target.IsEmpty) asset.targets.Remove(target);
            SaveOverrides(asset);
            return entries.Count + orders.Count;
        }

        /// <summary>기록하면 몇 건이 될지(저장 전 미리 세기).</summary>
        internal static int CountPending(string key, Baseline b, IList<GameObject> roots, bool prefab)
        {
            var (entries, orders) = Collect(b, roots, prefab);
            var target = Overrides.Find(Norm(key));
            var before = target == null ? 0 : target.entries.Count + target.orders.Count;
            // 같은 수여도 값이 다를 수 있다 — 대략 '바뀐 게 있다/없다'만 보면 된다.
            return entries.Count + orders.Count == before && SameAsRecorded(target, entries, orders) ? 0 : 1;
        }

        private static bool SameAsRecorded(UiTarget target, List<UiEntry> entries, List<UiOrder> orders)
        {
            if (target == null) return entries.Count == 0 && orders.Count == 0;
            foreach (var e in entries)
            {
                var old = target.entries.Find(x => x.path == e.path);
                if (old == null || old.props != e.props || Diff(old.values, e.values) != UiProp.None) return false;
            }
            foreach (var o in orders)
            {
                var old = target.orders.Find(x => x.parentPath == o.parentPath);
                if (old == null || !SameList(old.children, o.children)) return false;
            }
            return true;
        }

        private static (List<UiEntry>, List<UiOrder>) Collect(Baseline b, IList<GameObject> roots, bool prefab)
        {
            var entries = new List<UiEntry>();
            var orders = new List<UiOrder>();
            foreach (var (path, t) in Walk(roots, prefab))
            {
                var node = b.Get(path);
                if (node == null) continue;

                var cur = Read(t.gameObject);
                var diff = Diff(cur, node.values) & ~Driven(t, prefab && path == PrefabRootKey);
                // 파일로 저장되지 않은 그림 · 글꼴(코드가 실행 중에 만든 것)은 기록할 수 없다 — 기록하면 빈칸이 된다.
                if (cur.sprite != null && !EditorUtility.IsPersistent(cur.sprite)) diff &= ~UiProp.Sprite;
                if (cur.font != null && !EditorUtility.IsPersistent(cur.font)) diff &= ~UiProp.Font;
                if (diff != UiProp.None)
                    entries.Add(new UiEntry { path = path, props = diff, values = cur });

                if (t.childCount > 1)
                {
                    var now = ChildKeys(t);
                    if (!SameRelativeOrder(now, node.children))
                        orders.Add(new UiOrder { parentPath = path, children = now });
                }
            }
            return (entries, orders);
        }

        // ================================================================ 입히기

        /// <summary>기록된 값을 입힌다. (입힌 수, 찾지 못한 경로들)</summary>
        internal static (int applied, List<string> missing) ApplyOverrides(string key, IList<GameObject> roots, bool prefab)
        {
            var missing = new List<string>();
            var target = Overrides.Find(Norm(key));
            if (target == null) return (0, missing);

            var map = WalkMap(roots, prefab);
            int applied = 0;
            foreach (var e in target.entries)
            {
                if (!map.TryGetValue(e.path, out var t)) { missing.Add(e.path); continue; }
                Apply(t, e.props, e.values, prefab && e.path == PrefabRootKey);
                applied++;
            }
            foreach (var o in target.orders)
            {
                if (!map.TryGetValue(o.parentPath, out var parent)) { missing.Add(o.parentPath + " (앞뒤 순서)"); continue; }
                ApplyOrder(parent, o.children);
                applied++;
            }
            return (applied, missing);
        }

        internal static void Apply(Transform t, UiProp props, UiValues v, bool prefabRoot)
        {
            props &= ~Driven(t, prefabRoot);
            if (props == UiProp.None) return;

            if (t is RectTransform rt)
            {
                if (Has(props, UiProp.Anchors)) { rt.anchorMin = v.anchorMin; rt.anchorMax = v.anchorMax; }
                if (Has(props, UiProp.Pivot)) rt.pivot = v.pivot;
                if (Has(props, UiProp.Position)) rt.anchoredPosition = v.anchoredPosition;
                if (Has(props, UiProp.Size)) rt.sizeDelta = v.sizeDelta;
                if (Has(props, UiProp.Scale)) rt.localScale = v.scale;
                if (Has(props, UiProp.Rotation))
                {
                    var e = rt.localEulerAngles;
                    rt.localEulerAngles = new Vector3(e.x, e.y, v.rotationZ);
                }
            }

            var img = t.GetComponent<Image>();
            if (img != null && v.hasImage)
            {
                if (Has(props, UiProp.Sprite)) img.sprite = v.sprite;
                if (Has(props, UiProp.ImageColor)) img.color = v.imageColor;
                if (Has(props, UiProp.PreserveAspect)) img.preserveAspect = v.preserveAspect;
                if (Has(props, UiProp.ImageType)) img.type = v.imageType;
            }

            var tmp = t.GetComponent<TMP_Text>();
            if (tmp != null && v.hasText)
            {
                if (Has(props, UiProp.Font) && v.font != null) tmp.font = v.font;
                if (Has(props, UiProp.FontSize)) tmp.fontSize = v.fontSize;
                if (Has(props, UiProp.TextColor)) tmp.color = v.textColor;
                if (Has(props, UiProp.Alignment)) tmp.alignment = v.alignment;
                if (Has(props, UiProp.FontStyle)) tmp.fontStyle = v.fontStyle;
                if (Has(props, UiProp.CharSpacing)) tmp.characterSpacing = v.characterSpacing;
                if (Has(props, UiProp.LineSpacing)) tmp.lineSpacing = v.lineSpacing;
            }

            var lg = t.GetComponent<LayoutGroup>();
            if (lg != null && v.hasLayoutGroup && Has(props, UiProp.LayoutGroup))
            {
                lg.padding = new RectOffset(Mathf.RoundToInt(v.padding.x), Mathf.RoundToInt(v.padding.y),
                                            Mathf.RoundToInt(v.padding.z), Mathf.RoundToInt(v.padding.w));
                lg.childAlignment = v.childAlignment;
                if (lg is HorizontalOrVerticalLayoutGroup hv) hv.spacing = v.spacing.x;
                else if (lg is GridLayoutGroup grid) { grid.spacing = v.spacing; grid.cellSize = v.cellSize; }
            }

            var le = t.GetComponent<LayoutElement>();
            if (le != null && v.hasLayoutElement && Has(props, UiProp.LayoutElement))
            {
                le.minWidth = v.minSize.x; le.minHeight = v.minSize.y;
                le.preferredWidth = v.preferredSize.x; le.preferredHeight = v.preferredSize.y;
                le.flexibleWidth = v.flexibleSize.x; le.flexibleHeight = v.flexibleSize.y;
            }
        }

        /// <summary>목록에 있는 자식들만 그 순서로 다시 놓는다. 목록에 없는(새로 생긴) 자식은 제자리.</summary>
        internal static void ApplyOrder(Transform parent, List<string> order)
        {
            var current = new List<Transform>();
            var keyed = new Dictionary<string, Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                current.Add(c);
                keyed[ChildKey(parent, i)] = c;
            }

            var wanted = new List<Transform>();
            foreach (var k in order)
                if (keyed.TryGetValue(k, out var c) && !wanted.Contains(c)) wanted.Add(c);
            if (wanted.Count < 2) return;

            var slots = new List<int>();
            foreach (var c in wanted) slots.Add(current.IndexOf(c));
            slots.Sort();

            var final = new List<Transform>(current);
            for (int i = 0; i < wanted.Count; i++) final[slots[i]] = wanted[i];
            for (int i = 0; i < final.Count; i++) final[i].SetSiblingIndex(i);
        }

        /// <summary>켜짐/꺼짐과 투명도(CanvasGroup)를 빌더가 만든 그대로 돌린다 — 미리 보기를 끌 때와 저장할 때.</summary>
        internal static void RestoreStates(Baseline b, IList<GameObject> roots, bool prefab)
        {
            foreach (var (path, t) in Walk(roots, prefab))
            {
                var n = b.Get(path);
                if (n == null) continue;
                if (t.gameObject.activeSelf != n.active) t.gameObject.SetActive(n.active);
                if (!n.hasGroup) continue;
                var g = t.GetComponent<CanvasGroup>();
                if (g == null) continue;
                g.alpha = n.groupAlpha;
                g.interactable = n.groupInteractable;
                g.blocksRaycasts = n.groupBlocks;
            }
        }

        // ================================================================ 되돌리기 (편집기 창)

        /// <summary>항목 하나를 빌더 값으로 되돌린다. 열려 있는 씬/프리팹에도 바로 보인다.</summary>
        public static void Revert(string key, string path, bool values, bool order)
        {
            key = Norm(key);
            var asset = Overrides;
            var target = asset.Find(key);
            if (target == null) return;
            var entry = values ? target.entries.Find(e => e.path == path) : null;
            var childOrder = order ? target.orders.Find(o => o.parentPath == path) : null;
            var b = LoadBaseline(key);

            WithOpenTarget(key, (roots, prefab) =>
            {
                var map = WalkMap(roots, prefab);
                var node = b?.Get(path);
                if (node == null || !map.TryGetValue(path, out var t)) return false;
                if (entry != null)
                {
                    RecordUndo(t, "UI 되돌리기");
                    Apply(t, entry.props, node.values, prefab && path == PrefabRootKey);
                }
                if (childOrder != null) ApplyOrder(t, node.children);
                return true;
            });

            // 씬이 열려 있었다면 위에서 저장하며 다시 기록됐다 — 그래도 남은 것이 있으면 여기서 지운다.
            target = asset.Find(key);
            if (target == null) return;
            if (entry != null) target.entries.RemoveAll(e => e.path == path);
            if (childOrder != null) target.orders.RemoveAll(o => o.parentPath == path);
            if (target.IsEmpty) asset.targets.Remove(target);
            SaveOverrides(asset);
        }

        /// <summary>이 씬(프리팹)에서 바꾼 것을 모두 되돌린다.</summary>
        public static void RevertAll(string key)
        {
            key = Norm(key);
            var asset = Overrides;
            var target = asset.Find(key);
            if (target == null) return;
            var b = LoadBaseline(key);

            WithOpenTarget(key, (roots, prefab) =>
            {
                if (b == null) return false;
                var map = WalkMap(roots, prefab);
                foreach (var e in target.entries)
                {
                    var node = b.Get(e.path);
                    if (node == null || !map.TryGetValue(e.path, out var t)) continue;
                    RecordUndo(t, "UI 모두 되돌리기");
                    Apply(t, e.props, node.values, prefab && e.path == PrefabRootKey);
                }
                foreach (var o in target.orders)
                {
                    var node = b.Get(o.parentPath);
                    if (node != null && map.TryGetValue(o.parentPath, out var t)) ApplyOrder(t, node.children);
                }
                return true;
            });

            asset.targets.Remove(target);
            SaveOverrides(asset);
        }

        /// <summary>열린 씬/프리팹 단계에서 작업하고 저장한다. 열려 있지 않으면 프리팹은 파일을 직접 고치고, 씬은 다음 메뉴 5 때 반영된다.</summary>
        private static void WithOpenTarget(string key, Func<IList<GameObject>, bool, bool> work)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && Norm(stage.assetPath) == key)
            {
                // 프리팹 편집 화면에서는 저장(자동 저장)이 다시 기록하므로 바뀐 것으로 표시만 해 둔다.
                if (work(new[] { stage.prefabContentsRoot }, true))
                    EditorSceneManager.MarkSceneDirty(stage.scene);
                return;
            }

            if (key.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(key) == null) return;
                var root = PrefabUtility.LoadPrefabContents(key);
                try
                {
                    if (work(new[] { root }, true))
                    {
                        Building = true;
                        try { PrefabUtility.SaveAsPrefabAsset(root, key); }
                        finally { Building = false; }
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                return;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.isLoaded || Norm(s.path) != key) continue;
                if (work(s.GetRootGameObjects(), false))
                {
                    // 되돌린 뒤 바로 저장해 둔다. 저장하면서 다시 기록되므로 기록과 씬이 어긋나지 않는다.
                    EditorSceneManager.MarkSceneDirty(s);
                    EditorSceneManager.SaveScene(s);
                }
                return;
            }
        }

        private static void RecordUndo(Transform t, string label)
        {
            var list = new List<UnityEngine.Object> { t };
            foreach (var c in t.GetComponents<Component>())
                if (c is Graphic || c is LayoutGroup || c is LayoutElement) list.Add(c);
            Undo.RecordObjects(list.ToArray(), label);
        }

        // ================================================================ 걷기 · 이름

        /// <summary>씬(또는 프리팹)의 UI 오브젝트를 '경로 → 오브젝트'로 늘어놓는다. 다른 프리팹의 인스턴스(인트로 카툰)는 건너뛴다.</summary>
        internal static List<(string path, Transform t)> Walk(IList<GameObject> roots, bool prefab)
        {
            var list = new List<(string, Transform)>();
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] == null) continue;
                string key;
                if (prefab) key = PrefabRootKey;
                else
                {
                    int k = 0;
                    for (int j = 0; j < i; j++) if (roots[j] != null && roots[j].name == roots[i].name) k++;
                    key = k == 0 ? roots[i].name : roots[i].name + "#" + k;
                }
                Visit(roots[i].transform, key, list, prefab);
            }
            return list;
        }

        internal static Dictionary<string, Transform> WalkMap(IList<GameObject> roots, bool prefab)
        {
            var map = new Dictionary<string, Transform>();
            foreach (var (p, t) in Walk(roots, prefab)) map[p] = t;
            return map;
        }

        private static void Visit(Transform t, string path, List<(string, Transform)> list, bool prefab)
        {
            if (!prefab && PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) return;
            if (t is RectTransform) list.Add((path, t));
            for (int i = 0; i < t.childCount; i++)
                Visit(t.GetChild(i), path + "/" + ChildKey(t, i), list, prefab);
        }

        /// <summary>같은 이름의 형제가 있으면 뒤에 #1, #2 를 붙인다.</summary>
        internal static string ChildKey(Transform parent, int index)
        {
            var name = parent.GetChild(index).name;
            int k = 0;
            for (int j = 0; j < index; j++) if (parent.GetChild(j).name == name) k++;
            return k == 0 ? name : name + "#" + k;
        }

        internal static List<string> ChildKeys(Transform t)
        {
            var list = new List<string>(t.childCount);
            for (int i = 0; i < t.childCount; i++) list.Add(ChildKey(t, i));
            return list;
        }

        /// <summary>오브젝트의 경로. 추적하지 않는(인스턴스 안) 것이면 null.</summary>
        internal static string PathOf(Transform t, bool prefab)
        {
            if (t == null) return null;
            if (!prefab && PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) return null;
            var parts = new List<string>();
            var x = t;
            // 프리팹 편집 화면에서는 UI 프리팹이 임시 캔버스 아래에 놓인다 — 프리팹 맨 위에서 멈춘다.
            var stageRoot = prefab ? PrefabStageUtility.GetCurrentPrefabStage()?.prefabContentsRoot : null;
            while (x.parent != null && (stageRoot == null || x.gameObject != stageRoot))
            {
                parts.Add(ChildKey(x.parent, x.GetSiblingIndex()));
                x = x.parent;
            }
            if (prefab) parts.Add(PrefabRootKey);
            else
            {
                var roots = x.gameObject.scene.GetRootGameObjects();
                int k = 0;
                foreach (var r in roots)
                {
                    if (r == x.gameObject) break;
                    if (r.name == x.name) k++;
                }
                parts.Add(k == 0 ? x.name : x.name + "#" + k);
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        internal static string Norm(string path) => string.IsNullOrEmpty(path) ? path : path.Replace('\\', '/');

        // ================================================================ 값 읽기 · 비교

        internal static UiValues Read(GameObject go)
        {
            var v = new UiValues();
            if (go.transform is RectTransform rt)
            {
                v.anchorMin = rt.anchorMin; v.anchorMax = rt.anchorMax; v.pivot = rt.pivot;
                v.anchoredPosition = rt.anchoredPosition; v.sizeDelta = rt.sizeDelta;
                v.scale = rt.localScale; v.rotationZ = rt.localEulerAngles.z;
            }

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                v.hasImage = true;
                v.sprite = img.sprite; v.imageColor = img.color;
                v.preserveAspect = img.preserveAspect; v.imageType = img.type;
            }

            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                v.hasText = true;
                v.font = tmp.font; v.fontSize = tmp.fontSize; v.textColor = tmp.color;
                v.alignment = tmp.alignment; v.fontStyle = tmp.fontStyle;
                v.characterSpacing = tmp.characterSpacing; v.lineSpacing = tmp.lineSpacing;
            }

            var lg = go.GetComponent<LayoutGroup>();
            if (lg != null)
            {
                v.hasLayoutGroup = true;
                v.padding = new Vector4(lg.padding.left, lg.padding.right, lg.padding.top, lg.padding.bottom);
                v.childAlignment = lg.childAlignment;
                if (lg is HorizontalOrVerticalLayoutGroup hv) v.spacing = new Vector2(hv.spacing, 0f);
                else if (lg is GridLayoutGroup grid) { v.spacing = grid.spacing; v.cellSize = grid.cellSize; }
            }

            var le = go.GetComponent<LayoutElement>();
            if (le != null)
            {
                v.hasLayoutElement = true;
                v.minSize = new Vector2(le.minWidth, le.minHeight);
                v.preferredSize = new Vector2(le.preferredWidth, le.preferredHeight);
                v.flexibleSize = new Vector2(le.flexibleWidth, le.flexibleHeight);
            }
            return v;
        }

        internal static UiProp Diff(UiValues a, UiValues b)
        {
            var d = UiProp.None;
            if (!Same(a.anchorMin, b.anchorMin, 0.0005f) || !Same(a.anchorMax, b.anchorMax, 0.0005f)) d |= UiProp.Anchors;
            if (!Same(a.pivot, b.pivot, 0.0005f)) d |= UiProp.Pivot;
            if (!Same(a.anchoredPosition, b.anchoredPosition)) d |= UiProp.Position;
            if (!Same(a.sizeDelta, b.sizeDelta)) d |= UiProp.Size;
            if ((a.scale - b.scale).sqrMagnitude > 1e-6f) d |= UiProp.Scale;
            if (Mathf.Abs(Mathf.DeltaAngle(a.rotationZ, b.rotationZ)) > 0.01f) d |= UiProp.Rotation;

            if (a.hasImage && b.hasImage)
            {
                if (a.sprite != b.sprite) d |= UiProp.Sprite;
                if (!Same(a.imageColor, b.imageColor)) d |= UiProp.ImageColor;
                if (a.preserveAspect != b.preserveAspect) d |= UiProp.PreserveAspect;
                if (a.imageType != b.imageType) d |= UiProp.ImageType;
            }

            if (a.hasText && b.hasText)
            {
                if (a.font != b.font) d |= UiProp.Font;
                if (Mathf.Abs(a.fontSize - b.fontSize) > 0.01f) d |= UiProp.FontSize;
                if (!Same(a.textColor, b.textColor)) d |= UiProp.TextColor;
                if (a.alignment != b.alignment) d |= UiProp.Alignment;
                if (a.fontStyle != b.fontStyle) d |= UiProp.FontStyle;
                if (Mathf.Abs(a.characterSpacing - b.characterSpacing) > 0.01f) d |= UiProp.CharSpacing;
                if (Mathf.Abs(a.lineSpacing - b.lineSpacing) > 0.01f) d |= UiProp.LineSpacing;
            }

            if (a.hasLayoutGroup && b.hasLayoutGroup)
            {
                if ((a.padding - b.padding).sqrMagnitude > 1e-4f || !Same(a.spacing, b.spacing) ||
                    !Same(a.cellSize, b.cellSize) || a.childAlignment != b.childAlignment)
                    d |= UiProp.LayoutGroup;
            }

            if (a.hasLayoutElement && b.hasLayoutElement)
            {
                if (!Same(a.minSize, b.minSize) || !Same(a.preferredSize, b.preferredSize) || !Same(a.flexibleSize, b.flexibleSize))
                    d |= UiProp.LayoutElement;
            }
            return d;
        }

        /// <summary>
        /// 다른 장치가 대신 정하는 값 — 여기서 바꿔도 소용없으므로 기록하지 않는다.
        /// 자동 정렬 묶음(Layout Group) 안의 자식 자리 · 크기, 크기 맞춤(Content Size Fitter), 추론 판의 균일 맞춤 등.
        /// </summary>
        internal static UiProp Driven(Transform t, bool prefabRoot)
        {
            var d = UiProp.None;
            if (prefabRoot) d |= UiProp.Rect;   // 프리팹 맨 위는 쓰이는 자리(목록 · 격자)가 정한다
            if (t.parent == null && t.GetComponent<Canvas>() != null) d |= UiProp.Rect;
            if (t.GetComponent<UniformFitScaler>() != null) d |= UiProp.Rect;
            if (t.GetComponent<ContentSizeFitter>() != null || t.GetComponent<AspectRatioFitter>() != null) d |= UiProp.Size;
            var p = t.parent;
            if (p != null)
            {
                var lg = p.GetComponent<LayoutGroup>();
                if (lg != null && lg.enabled)
                {
                    var le = t.GetComponent<LayoutElement>();
                    if (le == null || !le.ignoreLayout) d |= UiProp.Anchors | UiProp.Position | UiProp.Size;
                }
            }
            return d;
        }

        /// <summary>'자동 정렬이 정하는 값'인지 — 편집기 창에서 알려 주는 데 쓴다.</summary>
        public static bool IsLayoutDriven(Transform t) => t != null && (Driven(t, false) & (UiProp.Position | UiProp.Size)) != 0;

        private static bool SameRelativeOrder(List<string> now, List<string> baseline)
        {
            // 둘 다에 있는 이름만 놓고 순서를 비교한다(새로 생기거나 빠진 자식은 상관하지 않는다).
            var common = new HashSet<string>(now);
            common.IntersectWith(baseline);
            var a = now.FindAll(common.Contains);
            var b = baseline.FindAll(common.Contains);
            return SameList(a, b);
        }

        private static bool SameList(List<string> a, List<string> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }

        private static bool Same(Vector2 a, Vector2 b, float eps = 0.01f) => Mathf.Abs(a.x - b.x) <= eps && Mathf.Abs(a.y - b.y) <= eps;

        private static bool Same(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) <= 0.002f && Mathf.Abs(a.g - b.g) <= 0.002f &&
            Mathf.Abs(a.b - b.b) <= 0.002f && Mathf.Abs(a.a - b.a) <= 0.002f;

        internal static bool Has(UiProp props, UiProp p) => (props & p) != 0;

        // ================================================================ 이름표 (편집기 창)

        internal static readonly (UiProp prop, string label)[] PropLabels =
        {
            (UiProp.Position, "위치"), (UiProp.Size, "크기"), (UiProp.Anchors, "기준점(앵커)"), (UiProp.Pivot, "중심점"),
            (UiProp.Scale, "배율"), (UiProp.Rotation, "회전"),
            (UiProp.Sprite, "이미지"), (UiProp.ImageColor, "이미지 색"), (UiProp.PreserveAspect, "비율 유지"), (UiProp.ImageType, "이미지 방식"),
            (UiProp.Font, "글꼴"), (UiProp.FontSize, "글자 크기"), (UiProp.TextColor, "글자 색"), (UiProp.Alignment, "정렬"),
            (UiProp.FontStyle, "글자 스타일"), (UiProp.CharSpacing, "자간"), (UiProp.LineSpacing, "줄 간격"),
            (UiProp.LayoutGroup, "자동 정렬(간격 · 여백)"), (UiProp.LayoutElement, "자동 정렬 크기"),
        };

        public static string Describe(UiProp props)
        {
            var parts = new List<string>();
            foreach (var (p, label) in PropLabels) if (Has(props, p)) parts.Add(label);
            return string.Join(" · ", parts);
        }

        /// <summary>자주 손보는 오브젝트의 우리말 이름.</summary>
        public static readonly Dictionary<string, string> Friendly = new Dictionary<string, string>
        {
            // 화면(페이즈)
            { "Screen_Prelude", "기차역 대화 화면" }, { "Screen_Talk", "사건 이전 · 인트로 대화 화면" },
            { "Screen_Cutscene", "컷씬 화면" }, { "Screen_SuspectSelect", "용의자 선택 화면" },
            { "Screen_Collection", "단서 수집 화면" }, { "Screen_Interrogation", "심문 화면" },
            { "Screen_Deduction", "추론(수첩) 화면" }, { "Screen_Accusation", "지목 화면" },
            { "Screen_GameOver", "게임오버 화면" }, { "Screen_Ending", "엔딩 화면" },
            // 늘 있는 것
            { "TopBar", "좌상단 시계 · 장소" }, { "SidePanel", "우측 단서 패널" },
            { "DialoguePanel", "아래 수첩 대사창" }, { "SpeechBubble", "위 말풍선" },
            { "CinematicDialogue", "시네마틱 대사창" }, { "DialogueToolbar", "LOG · AUTO · SKIP 버튼" },
            // 겹쳐 뜨는 것
            { "DialogueLog", "대사 기록(LOG) 창" }, { "RestoreCatcher", "대사창 숨김 해제 버튼" },
            { "CloseUpPopup", "클로즈업 팝업" }, { "ClueReadingWindow", "단서 열람창" },
            { "TutorialPopup", "튜토리얼 팝업" }, { "TitleCard", "심문 시작 제목" },
            { "CoachMark", "수집 가이드(어둡게 덮기)" }, { "ConfirmPopup", "수집 종료 확인 팝업" },
            { "SettingsMenu", "설정 창" }, { "PauseMenu", "일시 정지 메뉴" },
            { "HypothesisPick", "가설 고르기(2차 심문)" }, { "ListPanel", "지목 목록" },
            { "ReplaceOverlay", "가설 교체 안내" }, { "DeleteConfirm", "가설 지우기 확인" },
            // 인트로 · 타이틀
            { "Popup", "서류 팝업" }, { "AdvanceHint", "'다음' 표시" }, { "Skip", "건너뛰기 버튼" },
        };

        public static string FriendlyName(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            var last = path;
            var slash = path.LastIndexOf('/');
            if (slash >= 0) last = path.Substring(slash + 1);
            var hash = last.IndexOf('#');
            var bare = hash >= 0 ? last.Substring(0, hash) : last;
            return Friendly.TryGetValue(bare, out var f) ? $"{f}  ({last})" : last;
        }

        // ================================================================ 기타

        /// <summary>미리 보기로 바꾼 상태를 '바뀌지 않음'으로 표시한다(저장 안 해도 되는 변경이라). 안 되면 그냥 둔다.</summary>
        internal static void TryClearDirty(Scene scene)
        {
            try
            {
                var m = typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Scene) }, null);
                m?.Invoke(null, new object[] { scene });
            }
            catch { /* 내부 API 가 바뀌어도 기능에는 지장 없다 */ }
        }
    }

    /// <summary>
    /// 편집기 창의 '미리 보기' — 페이즈 화면과 팝업을 잠깐 켜 둔다.
    /// 저장할 때와 플레이 직전에는 빌더 상태로 돌리고, 끝나면 다시 켠다. 그래서 게임에는 영향이 없다.
    /// </summary>
    internal static class UiPreview
    {
        public static int Phase = -1;
        public static readonly HashSet<string> Shown = new HashSet<string>();
        public static string ScenePath;
        private static bool _hiddenForPlay;

        public static bool IsOn => Phase >= 0 || Shown.Count > 0;

        public static void Reset()
        {
            Phase = -1;
            Shown.Clear();
            ScenePath = null;
        }

        public static void Apply(Scene scene, bool keepClean)
        {
            var path = UiLayout.Norm(scene.path);
            var b = UiLayout.LoadBaseline(path);
            if (b == null) return;
            var wasDirty = scene.isDirty;
            var roots = scene.GetRootGameObjects();

            UiLayout.RestoreStates(b, roots, false);
            if (Phase >= 0) ApplyPhase(roots, (GamePhase)Phase);
            var map = UiLayout.WalkMap(roots, false);
            foreach (var p in Shown)
                if (map.TryGetValue(p, out var t)) Show(t);

            ScenePath = IsOn ? path : null;
            if (keepClean && !wasDirty) UiLayout.TryClearDirty(scene);
            SceneView.RepaintAll();
        }

        public static void Off(Scene scene)
        {
            Phase = -1;
            Shown.Clear();
            Apply(scene, keepClean: !scene.isDirty);
        }

        public static void HideForPlay()
        {
            if (!IsOn || string.IsNullOrEmpty(ScenePath)) return;
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            var b = UiLayout.LoadBaseline(ScenePath);
            if (b == null) return;
            var wasDirty = scene.isDirty;
            UiLayout.RestoreStates(b, scene.GetRootGameObjects(), false);
            if (!wasDirty) UiLayout.TryClearDirty(scene);
            _hiddenForPlay = true;
        }

        public static void ResumeAfterPlay()
        {
            if (!_hiddenForPlay) return;
            _hiddenForPlay = false;
            if (!IsOn || string.IsNullOrEmpty(ScenePath)) return;
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.IsValid() && scene.isLoaded) Apply(scene, keepClean: true);
        }

        private static void Show(Transform t)
        {
            for (var x = t; x != null; x = x.parent)
            {
                if (!x.gameObject.activeSelf) x.gameObject.SetActive(true);
                var g = x.GetComponent<CanvasGroup>();
                if (g != null && g.alpha < 0.01f) g.alpha = 1f;
            }
        }

        /// <summary>그 페이즈에서 게임이 켜는 화면 묶음과 대사창 모양을 그대로 흉내 낸다.</summary>
        private static void ApplyPhase(IList<GameObject> roots, GamePhase phase)
        {
            var binder = FindIn<PhaseScreenBinder>(roots);
            if (binder != null)
            {
                var so = new SerializedObject(binder);
                var groups = so.FindProperty("groups");
                for (int i = 0; i < groups.arraySize; i++)
                {
                    var g = groups.GetArrayElementAtIndex(i);
                    var root = g.FindPropertyRelative("root").objectReferenceValue as GameObject;
                    if (root == null) continue;
                    root.SetActive(Contains(g.FindPropertyRelative("phases"), phase));
                }
                var always = so.FindProperty("alwaysVisible");
                for (int i = 0; i < always.arraySize; i++)
                    (always.GetArrayElementAtIndex(i).objectReferenceValue as GameObject)?.SetActive(true);
            }

            var presenter = FindIn<DialoguePresenter>(roots);
            if (presenter != null)
            {
                var so = new SerializedObject(presenter);
                var notebook = so.FindProperty("root").objectReferenceValue as CanvasGroup;
                var bubble = so.FindProperty("bubbleRoot").objectReferenceValue as CanvasGroup;
                var cine = so.FindProperty("cinematicRoot").objectReferenceValue as CanvasGroup;

                var cinematic = phase == GamePhase.Prelude || phase == GamePhase.PreIncidentTalk || phase == GamePhase.Cutscene ||
                                phase == GamePhase.Intro || phase == GamePhase.Deduction;
                var split = phase == GamePhase.Interrogation1 || phase == GamePhase.Interrogation2;
                SetGroup(cine, cinematic);
                SetGroup(notebook, !cinematic);
                SetGroup(bubble, split);
            }

            var toolbar = FindIn<DialogueToolbar>(roots);
            if (toolbar != null)
            {
                var so = new SerializedObject(toolbar);
                var root = so.FindProperty("root").objectReferenceValue as GameObject;
                if (root != null) root.SetActive(Contains(so.FindProperty("visiblePhases"), phase));
            }
        }

        private static void SetGroup(CanvasGroup g, bool shown)
        {
            if (g == null) return;
            if (shown && !g.gameObject.activeSelf) g.gameObject.SetActive(true);
            g.alpha = shown ? 1f : 0f;
        }

        private static bool Contains(SerializedProperty list, GamePhase phase)
        {
            if (list == null) return false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).intValue == (int)phase) return true;
            return false;
        }

        internal static T FindIn<T>(IList<GameObject> roots) where T : Component
        {
            foreach (var r in roots)
            {
                if (r == null) continue;
                var c = r.GetComponentInChildren<T>(true);
                if (c != null) return c;
            }
            return null;
        }
    }
}
