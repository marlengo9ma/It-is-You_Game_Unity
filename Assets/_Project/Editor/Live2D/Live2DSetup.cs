using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ItIsYou.Characters;
using ItIsYou.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ItIsYou.EditorTools
{
    /// <summary>
    /// 세피 Live2D 준비 (메뉴 It is You › 10).
    ///
    /// 1. Cubism SDK 를 쓰는 코드를 켠다 (스크립트 정의 LIVE2D_CUBISM — SDK 가 있으면 자동).
    /// 2. 모델(.model3.json)과 모션(.motion3.json)을 'Original Workflow' 로 다시 가져온다 →
    ///    모델 프리팹(Sepi 1.prefab, 모션 컨트롤러 포함) + 모션마다 .anim.
    /// 3. 모션 표(Data/Characters/SepiMotions.asset)를 만든다 — 기본: 기본 → smile, 특수: 충격(shock) + 빈 칸들.
    ///    이미 있으면 빈 칸만 채우고 손으로 바꾼 것은 그대로 둔다.
    /// 4. 아트 세트에 모델 · 모션 표를 넣고, Live2D 전용 레이어 · 렌더러(Cubism URP Renderer)를 준비한다.
    /// 그 뒤 메뉴 5 를 돌리면 대화 · 심문 화면의 세피가 Live2D 로 바뀐다.
    /// </summary>
    [InitializeOnLoad]
    public static class Live2DSetup
    {
        public const string Define = "LIVE2D_CUBISM";
        public const string LayerName = "Live2D";
        public const string ModelFolder = "Assets/_Project/Art/Characters/sepi_model";
        public const string MotionSetPath = "Assets/_Project/Data/Characters/SepiMotions.asset";
        private const string CubismRendererPath = "Assets/Live2D/Cubism/Rendering/URP/CubismURPRenderer.asset";

        /// <summary>
        /// 특수 모션 칸 (2026-10-24 제작자: 최소 8가지 추가 예정). 이름은 대사 데이터의 연출 이름(StageAnimation)과 같다 —
        /// 모션이 오면 같은 이름 칸에 끌어다 넣기만 하면 그 연출이 걸린 대사에서 재생된다.
        /// </summary>
        private static readonly (string id, string note, string file, string[] aliases)[] DefaultSlots =
        {
            ("shock",      "충격 — 깜짝 놀람. 심문 리액션 순간 · 놀란 표정의 줄", "충격", new[] { StageAnimation.Startle }),
            (StageAnimation.Tremble,   "동공이 떨림 · 잔떨림", null, null),
            (StageAnimation.LookAway,  "시선을 피함", null, null),
            (StageAnimation.GlanceCat, "고양이 쪽을 힐끔", null, null),
            (StageAnimation.Sigh,      "작은 한숨", null, null),
            (StageAnimation.Glare,     "째려봄", null, null),
            (StageAnimation.Fume,      "화가 나 부들거림", null, null),
            (StageAnimation.Shrink,    "어깨가 움츠러듦", null, null),
            (StageAnimation.Pale,      "얼굴이 하얗게 질림", null, null),
            (StageAnimation.Bow,       "고개를 숙임", null, null),
            (StageAnimation.Ponder,    "눈을 감고 생각", null, null),
            ("special_1",  "예비 칸 — 새 모션용", null, null),
            ("special_2",  "예비 칸 — 새 모션용", null, null),
        };

        private static readonly string[] IdleFiles = { "기본", "smile" };

        static Live2DSetup()
        {
            EditorApplication.delayCall += SyncDefine;
        }

        // ================================================================ 스크립트 정의

        public static bool HasCubism =>
            AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Live2D.Cubism");

        /// <summary>SDK 가 있으면 LIVE2D_CUBISM 을 켜고, 없으면 끈다. 바뀌면 다시 컴파일된다.</summary>
        public static void SyncDefine()
        {
            var want = HasCubism;
            foreach (var target in new[] { NamedBuildTarget.Standalone })
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
                var list = defines.ToList();
                var has = list.Contains(Define);
                if (want == has) continue;
                if (want) list.Add(Define); else list.Remove(Define);
                PlayerSettings.SetScriptingDefineSymbols(target, list.ToArray());
                Debug.Log($"[It is You] Live2D Cubism SDK {(want ? "발견 — LIVE2D_CUBISM 켬" : "없음 — LIVE2D_CUBISM 끔")} ({target.TargetName})");
            }
        }

        // ================================================================ 메뉴

        [MenuItem("It is You/10. 세피 Live2D 준비 (모델 · 모션 표)", priority = 60)]
        public static void SetupMenu()
        {
            var report = Setup();
            Debug.Log("[It is You] 세피 Live2D 준비\n" + report);
        }

        [MenuItem("It is You/10-b. 세피 모션 표 열기", priority = 61)]
        public static void OpenMotionSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<Live2DMotionSet>(MotionSetPath);
            if (set == null) { SetupMenu(); set = AssetDatabase.LoadAssetAtPath<Live2DMotionSet>(MotionSetPath); }
            Selection.activeObject = set;
            EditorGUIUtility.PingObject(set);
        }

        public static string Setup()
        {
            var log = new List<string>();
            if (!HasCubism) return "Live2D Cubism SDK 가 프로젝트에 없습니다 — live2d.com 에서 Cubism SDK for Unity 를 임포트하세요.";
            SyncDefine();

            // ---- 1. Original Workflow 로 다시 가져오기 ----
            var menuType = Type.GetType("Live2D.Cubism.Editor.CubismUnityEditorMenu, Live2D.Cubism.Editor");
            var setOw = menuType?.GetMethod("SetImportAsOriginalWorkflow", BindingFlags.Public | BindingFlags.Static);
            if (setOw != null) { setOw.Invoke(null, new object[] { true }); log.Add("Original Workflow 켬"); }
            else log.Add("⚠ Original Workflow 설정을 찾지 못함 — Live2D › Cubism › OriginalWorkflow 메뉴에서 켜 주세요");

            var model3 = Directory.GetFiles(ModelFolder, "*.model3.json").Select(p => p.Replace('\\', '/')).FirstOrDefault();
            if (model3 == null) return string.Join("\n", log) + "\n모델(.model3.json)을 찾지 못했습니다: " + ModelFolder;

            // Cubism 은 모션이 모델 폴더 '안의 하위 폴더'에 있다고 보고 모션 목록(fadeMotionList)을 만든다.
            // 모델 폴더에 바로 놓인 모션은 motions/ 로 옮긴다 — 새 모션도 그냥 모델 폴더에 넣고 메뉴 10 을 다시 돌리면 된다.
            MoveMotionsIntoSubfolder(log);

            AssetDatabase.ImportAsset(model3, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            foreach (var m in Directory.GetFiles(ModelFolder, "*.motion3.json", SearchOption.AllDirectories))
                AssetDatabase.ImportAsset(m.Replace('\\', '/'), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();

            var prefabPath = model3.Replace(".model3.json", ".prefab");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return string.Join("\n", log) + "\n모델 프리팹이 만들어지지 않았습니다: " + prefabPath;
            log.Add("모델 프리팹: " + prefabPath);

            // ---- 2. 프리팹: 모션 컨트롤러 · Actor ----
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var mcType = Type.GetType("Live2D.Cubism.Framework.Motion.CubismMotionController, Live2D.Cubism");
                if (mcType != null && root.GetComponent(mcType) == null)
                {
                    root.AddComponent(mcType);   // CubismFadeController 는 RequireComponent 로 함께 붙는다
                    log.Add("CubismMotionController 추가");
                }
                var fcType = Type.GetType("Live2D.Cubism.Framework.MotionFade.CubismFadeController, Live2D.Cubism");
                var fc = fcType != null ? root.GetComponent(fcType) : null;
                if (fc != null)
                {
                    var listField = fcType.GetField("CubismFadeMotionList");
                    if (listField != null)
                    {
                        var list = AssetDatabase.LoadMainAssetAtPath(FadeMotionListPath);
                        if (list == null) log.Add("⚠ fadeMotionList 가 없습니다 — Original Workflow 로 다시 가져와야 합니다: " + FadeMotionListPath);
                        else if (!ReferenceEquals(listField.GetValue(fc), list)) { listField.SetValue(fc, list); log.Add("FadeMotionList 연결"); }
                    }
                }
                var animator = root.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.runtimeAnimatorController = null;   // 모션 컨트롤러가 Animator Controller 와 함께 쓰이면 꺼진다
                    log.Add("Animator Controller 비움 (모션 컨트롤러 사용)");
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            // ---- 3. 모션 표 ----
            var set = Ed.CreateOrLoad<Live2DMotionSet>(Path.GetDirectoryName(MotionSetPath).Replace('\\', '/'), Path.GetFileNameWithoutExtension(MotionSetPath));
            var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { ModelFolder })
                .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null).ToList();
            AnimationClip Clip(string name) => clips.FirstOrDefault(c => string.Equals(c.name, name, StringComparison.OrdinalIgnoreCase));

            if (set.idleSequence == null || set.idleSequence.Count == 0 || set.idleSequence.All(c => c == null))
                set.idleSequence = IdleFiles.Select(Clip).Where(c => c != null).ToList();
            foreach (var d in DefaultSlots)
            {
                var slot = set.slots.FirstOrDefault(s => s != null && s.id == d.id);
                if (slot == null)
                {
                    slot = new Live2DMotionSet.MotionSlot { id = d.id, note = d.note, aliases = new List<string>(d.aliases ?? Array.Empty<string>()) };
                    set.slots.Add(slot);
                }
                if (slot.clip == null && d.file != null) slot.clip = Clip(d.file);
            }
            if (string.IsNullOrEmpty(set.reactionSlot)) set.reactionSlot = "shock";
            if (set.expressionMotions.Count == 0)
                set.expressionMotions.Add(new Live2DMotionSet.ExpressionMotion { expression = ExpressionId.Surprised, slot = "shock" });
            EditorUtility.SetDirty(set);
            log.Add($"모션 표: {MotionSetPath} — 기본 {string.Join(" → ", set.idleSequence.Where(c => c != null).Select(c => c.name))}, " +
                    $"특수 {set.slots.Count}칸 (채워진 칸: {string.Join(", ", set.slots.Where(s => s.clip != null).Select(s => s.id))})");
            if (clips.Count == 0) log.Add("⚠ 모션 클립(.anim)이 없습니다 — .motion3.json 가져오기를 확인하세요");

            // ---- 4. 아트 세트 · 레이어 · 렌더러 ----
            var art = AssetDatabase.LoadAssetAtPath<ArtSet>("Assets/_Project/Art/ArtSet.asset");
            if (art != null)
            {
                art.sepiLive2D = prefab;
                art.sepiLive2DMotions = set;
                EditorUtility.SetDirty(art);
                log.Add("아트 세트 › sepiLive2D · sepiLive2DMotions 연결");
            }

            var layer = EnsureLayer(LayerName);
            log.Add(layer >= 0 ? $"레이어 '{LayerName}' = {layer}" : "⚠ 빈 레이어가 없어 Live2D 레이어를 만들지 못함");
            var rendererIndex = EnsureCubismRenderer(log);
            log.Add($"Cubism URP 렌더러 번호 = {rendererIndex}");

            AssetDatabase.SaveAssets();
            log.Add("이제 메뉴 5(전체 다시 만들기)를 돌리면 씬에 반영됩니다.");
            return string.Join("\n", log);
        }

        // ================================================================ 모션 폴더

        public const string MotionSubfolder = "motions";

        /// <summary>Cubism 이 모션 목록을 두는 자리 — 모델 폴더/폴더이름.fadeMotionList.asset.</summary>
        public static string FadeMotionListPath => $"{ModelFolder}/{Path.GetFileName(ModelFolder)}.fadeMotionList.asset";

        private static void MoveMotionsIntoSubfolder(List<string> log)
        {
            var sub = $"{ModelFolder}/{MotionSubfolder}";
            var loose = Directory.GetFiles(ModelFolder, "*.motion3.json", SearchOption.TopDirectoryOnly).Select(p => p.Replace('\\', '/')).ToList();
            if (loose.Count > 0 && !AssetDatabase.IsValidFolder(sub)) AssetDatabase.CreateFolder(ModelFolder, MotionSubfolder);

            foreach (var json in loose)
            {
                var stem = Path.GetFileName(json).Replace(".motion3.json", "");
                foreach (var ext in new[] { ".motion3.json", ".anim", ".fade.asset" })
                {
                    var from = $"{ModelFolder}/{stem}{ext}";
                    var to = $"{sub}/{stem}{ext}";
                    if (AssetDatabase.LoadMainAssetAtPath(from) == null) continue;
                    if (AssetDatabase.LoadMainAssetAtPath(to) != null) { log.Add($"⚠ {to} 가 이미 있어 {from} 은 그대로 둠"); continue; }
                    var err = AssetDatabase.MoveAsset(from, to);
                    if (!string.IsNullOrEmpty(err)) log.Add($"⚠ {from} 옮기기 실패: {err}");
                }
                log.Add($"모션 '{stem}' → {MotionSubfolder}/");
            }

            // 모션이 모델 폴더에 바로 있었을 때 Cubism 이 한 단계 위에 만든 목록(예: Characters.fadeMotionList)을 제자리로.
            var parent = Path.GetDirectoryName(ModelFolder).Replace('\\', '/');
            var stray = $"{parent}/{Path.GetFileName(parent)}.fadeMotionList.asset";
            if (AssetDatabase.LoadMainAssetAtPath(stray) != null && AssetDatabase.LoadMainAssetAtPath(FadeMotionListPath) == null)
            {
                var err = AssetDatabase.MoveAsset(stray, FadeMotionListPath);
                log.Add(string.IsNullOrEmpty(err) ? $"모션 목록을 제자리로: {FadeMotionListPath}" : $"⚠ 모션 목록 옮기기 실패: {err}");
            }
        }

        // ================================================================ 레이어

        public static int LayerIndex => LayerMask.NameToLayer(LayerName);

        private static int EnsureLayer(string name)
        {
            var existing = LayerMask.NameToLayer(name);
            if (existing >= 0) return existing;
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var p = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(p.stringValue)) continue;
                p.stringValue = name;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
            return -1;
        }

        // ================================================================ 렌더러

        /// <summary>
        /// Cubism 은 URP 에서 전용 렌더 패스(CubismRenderPassFeature)로 그린다. SDK 에 들어 있는 렌더러(CubismURPRenderer)를
        /// 프로젝트의 URP 설정마다 렌더러 목록에 더하고 그 번호를 돌려준다. Live2D 카메라만 이 렌더러를 쓴다.
        /// </summary>
        public static int EnsureCubismRenderer(List<string> log = null)
        {
            var renderer = AssetDatabase.LoadMainAssetAtPath(CubismRendererPath);
            if (renderer == null) { log?.Add("⚠ CubismURPRenderer 를 찾지 못함: " + CubismRendererPath); return -1; }

            var index = -1;
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/Live2D/")) continue;
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                var so = new SerializedObject(asset);
                var list = so.FindProperty("m_RendererDataList");
                if (list == null) continue;
                var found = -1;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == renderer) { found = i; break; }
                if (found < 0)
                {
                    found = list.arraySize;
                    list.arraySize++;
                    list.GetArrayElementAtIndex(found).objectReferenceValue = renderer;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    log?.Add($"{Path.GetFileName(path)} 의 렌더러 목록에 CubismURPRenderer 추가 (#{found})");
                }
                if (index < 0) index = found;
                else if (index != found) log?.Add($"⚠ URP 설정마다 Cubism 렌더러 번호가 다릅니다 ({path}: {found}) — 같은 번호가 되게 맞춰 주세요");
            }
            return index;
        }

        /// <summary>카메라가 쓸 URP 렌더러 번호를 정한다 (UniversalAdditionalCameraData.m_RendererIndex).</summary>
        public static void SetCameraRenderer(Camera cam, int index)
        {
            if (cam == null || index < 0) return;
            var t = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (t == null) return;
            var data = cam.GetComponent(t) ?? cam.gameObject.AddComponent(t);
            var so = new SerializedObject(data);
            var p = so.FindProperty("m_RendererIndex");
            if (p != null) { p.intValue = index; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }
}
