using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ItIsYou.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace ItIsYou.Core
{
    /// <summary>
    /// 빌드 점검용. 실행 인자에 <c>-itisyou-probe</c> 가 있을 때만 움직인다(평소에는 아무 일도 하지 않는다).
    /// 타이틀 → 챕터(기차역 대화) → 1차 심문으로 건너뛰며 화면을 찍고, 심문 화면에서 클릭이 어디에 닿는지 기록한 뒤 끝낸다.
    /// 결과: (persistentDataPath)/probe/ 의 PNG 와 probe.txt.
    /// </summary>
    public class BuildProbe : MonoBehaviour
    {
        private string _dir;
        private readonly List<string> _log = new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!System.Environment.GetCommandLineArgs().Contains("-itisyou-probe")) return;
            var go = new GameObject("BuildProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<BuildProbe>();
        }

        private IEnumerator Start()
        {
            _dir = System.IO.Path.Combine(Application.persistentDataPath, "probe");
            System.IO.Directory.CreateDirectory(_dir);
            Log($"screen {Screen.width}x{Screen.height}");

            yield return new WaitForSecondsRealtime(3f);
            yield return Shot("0_title");

            SceneManager.LoadScene(SceneRoutes.TutorialChapter);
            yield return new WaitForSecondsRealtime(6f);
            LogLive2D();
            yield return Shot("1_prelude");

            var flow = FindFirstObjectByType<GameFlowController>();
            if (flow != null) flow.DebugJumpToInterrogation(null, false);
            for (var i = 0; i < 12; i++)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                ClickPopupButtons();
            }
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("2_interrogation");
            LogLive2D();
            LogHits("Character_Sepi", "Canvas/Screen_Interrogation/Character_Sepi");
            LogHits("SpeechBubble", "Canvas/SpeechBubble/Body");

            System.IO.File.WriteAllLines(System.IO.Path.Combine(_dir, "probe.txt"), _log);
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(_dir, name + ".png"));
            Log("shot " + name);
            yield return null;
        }

        private void ClickPopupButtons()
        {
            var popup = GameObject.Find("TutorialPopup");
            if (popup == null) return;
            var b = popup.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(x => x.isActiveAndEnabled && x.interactable);
            if (b != null) b.onClick.Invoke();
        }

        private void LogLive2D()
        {
            foreach (var raw in Resources.FindObjectsOfTypeAll<UnityEngine.UI.RawImage>().Where(r => r.name == "Live2D" && r.gameObject.scene.IsValid()))
                Log($"live2d {raw.transform.parent.name} active={raw.isActiveAndEnabled} tex={(raw.texture != null ? raw.texture.width + "x" + raw.texture.height : "null")} mat={(raw.material != null ? raw.material.shader.name : "-")} rect={raw.rectTransform.rect}");
            var cam = GameObject.Find("Live2DCamera");
            Log("live2d camera " + (cam != null ? cam.GetComponent<Camera>().enabled + " ortho " + cam.GetComponent<Camera>().orthographicSize + " pos " + cam.transform.position : "missing"));
            var stageGo = GameObject.Find("Live2D_Sepi");
            if (stageGo != null)
            {
                var rs = stageGo.GetComponentsInChildren<Renderer>(true);
                var b = new Bounds(); var has = false; var enabled = 0;
                foreach (var r in rs) { if (!r.enabled) continue; enabled++; if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds); }
                Log($"live2d renderers {rs.Length} enabled {enabled} bounds {b.center} {b.size}");
                var stage = stageGo.GetComponent<ItIsYou.Characters.Live2DStage>();
                var rt = stage != null ? stage.Texture : null;
                if (rt != null)
                {
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = prev;
                    var px = tex.GetPixels32();
                    var opaque = px.Count(c => c.a > 10);
                    Log($"live2d texture opaque pixels {opaque} / {px.Length}");
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(_dir, "live2d_rt_" + Time.frameCount + ".png"), tex.EncodeToPNG());
                    Destroy(tex);
                }
            }
        }

        private void LogHits(string label, string path)
        {
            var go = GameObject.Find(path);
            var es = EventSystem.current;
            if (go == null || es == null) { Log($"hits {label}: missing (go={go != null}, es={es != null})"); return; }
            var rt = (RectTransform)go.transform;
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            var pos = RectTransformUtility.WorldToScreenPoint(null, (c[0] + c[2]) * 0.5f);
            var res = new List<RaycastResult>();
            es.RaycastAll(new PointerEventData(es) { position = pos }, res);
            Log($"hits {label} @{pos}: " + string.Join(" > ", res.Select(r => PathOf(r.gameObject.transform))));
        }

        private static string PathOf(Transform t)
        {
            var s = t.name;
            while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }

        private void Log(string line)
        {
            _log.Add(line);
            Debug.Log("[BuildProbe] " + line);
        }
    }
}
