using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItIsYou.Core
{
    /// <summary>세이브가 적힌 자리. 페이즈의 시작이거나, 스토리 장면의 대사 한 줄.</summary>
    public enum SavePoint
    {
        /// <summary>스토리 장면(대화 · 컷씬 · 엔딩 · 1챕터 인트로)의 대사 한 줄.</summary>
        Story = 0,
        /// <summary>단서 수집 페이즈의 시작. 게임오버의 '단서 수집부터 다시'도 여기로 돌아온다.</summary>
        ClueCollection = 1,
        /// <summary>1차 심문 — 인물 선택 화면 (심문 페이즈의 시작).</summary>
        Interrogation1 = 2,
        /// <summary>추론 페이즈의 시작.</summary>
        Deduction = 3,
        /// <summary>2차 심문 — 인물 선택 화면.</summary>
        Interrogation2 = 4,
        /// <summary>범인 지목 페이즈의 시작.</summary>
        Accusation = 5,
        /// <summary>데모 종료 화면.</summary>
        DemoEnd = 6
    }

    /// <summary>
    /// 세이브 한 칸의 내용. JsonUtility 로 PlayerPrefs 에 적는다 — 웹 빌드에서도 그대로 남는다(브라우저 저장소).
    /// 단서는 id 로만 적고, 불러올 때 GameDatabase 에서 다시 찾는다.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string chapterId;
        public string savedAt;
        /// <summary>세이브 칸에 보이는 설명. 예: 단서 수집 시작 / 기차역 대화 · 12번째 대사</summary>
        public string label;
        public SavePoint point;

        // ---- 스토리 장면 ----
        /// <summary>prelude · preIncident · cutscene · afterCutscene · flashback0.. · afterFlashback · intro
        /// · ending:엔딩id:단계 · teaser:단계</summary>
        public string storyStep;
        public int storyLine;

        // ---- 판 상태 ----
        public List<string> clues = new List<string>();
        public List<string> readClues = new List<string>();
        public List<string> promotedClues = new List<string>();
        public int trust;
        public int friendly;
        public int aggressive;
        public List<string> seenCutscenes = new List<string>();
        public List<string> craftedRecipes = new List<string>();
        public string accusedId;

        // ---- 1차 심문 · 추론 ----
        public List<string> interrogated = new List<string>();
        public bool selectTalkDone;
        public bool deductionEntered;
        /// <summary>-1 이면 추론 상태 없음(새 잉크로 시작).</summary>
        public int deductionInk = -1;
        public int deductionFails;
        public List<string> deductionTried = new List<string>();

        // ---- 2차 심문 ----
        public List<string> visited2 = new List<string>();

        /// <summary>게임오버의 '단서 수집부터 다시'가 돌아갈 자리(단서 수집 시작의 세이브 JSON).</summary>
        public string retryJson;

        public SaveData Clone() => JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(this));
    }

    /// <summary>
    /// 세이브 칸 — 자동 저장 1칸(0번) + 수동 저장 4칸(1~4번).
    ///
    /// 자동 저장은 페이즈의 시작(단서 수집 · 심문 · 추론 · 지목)과 스토리 장면의 대사 한 줄마다 덮어쓴다.
    /// 수동 저장은 일시 정지 메뉴의 '저장하기'에서, 지금 자리(가장 최근의 자동 저장 지점)를 고른 칸에 적는다.
    /// 나중에 엔딩 수집에서 '분기마다 이어하기'로 넘어갈 때도 같은 자리 정보를 쓴다.
    /// </summary>
    public static class SaveSystem
    {
        public const int AutoSlot = 0;
        public const int ManualSlotCount = 4;
        public const int SlotCount = ManualSlotCount + 1;
        private const string KeyPrefix = "itisyou.save.";

        /// <summary>세이브 칸이 바뀌었다.</summary>
        public static event Action Changed;

        /// <summary>지금 판의 저장할 자리를 돌려주는 곳. 챕터 씬의 GameFlowController 가 연결한다. 없으면 저장할 수 없다.</summary>
        public static Func<SaveData> CurrentProvider;

        /// <summary>다음 챕터 씬이 시작될 때 불러올 세이브.</summary>
        private static SaveData _pending;

        public static string SlotName(int slot) => slot == AutoSlot ? "자동 저장" : $"저장 {slot}";

        private static string Key(int slot) => KeyPrefix + (slot == AutoSlot ? "auto" : "slot" + slot);

        public static bool Has(int slot) => PlayerPrefs.HasKey(Key(slot));

        public static bool AnyExists
        {
            get
            {
                for (int i = 0; i < SlotCount; i++) if (Has(i)) return true;
                return false;
            }
        }

        public static SaveData Read(int slot)
        {
            if (!Has(slot)) return null;
            try { return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key(slot))); }
            catch (Exception e)
            {
                Debug.LogWarning($"[It is You] 세이브 {SlotName(slot)} 을(를) 읽지 못했습니다 — {e.Message}");
                return null;
            }
        }

        public static void Write(int slot, SaveData data)
        {
            if (data == null || slot < 0 || slot >= SlotCount) return;
            data.savedAt = DateTime.Now.ToString("yyyy.MM.dd  HH:mm");
            PlayerPrefs.SetString(Key(slot), JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void Delete(int slot)
        {
            PlayerPrefs.DeleteKey(Key(slot));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>지금 판을 저장할 수 있는가 (챕터 씬에서, 저장할 자리가 있을 때).</summary>
        public static bool CanSaveNow => CurrentProvider != null && CurrentProvider() != null;

        /// <summary>지금 자리를 칸에 저장한다. 저장했으면 true.</summary>
        public static bool SaveCurrent(int slot)
        {
            if (slot == AutoSlot) return false;
            var data = CurrentProvider?.Invoke();
            if (data == null) return false;
            Write(slot, data.Clone());
            return true;
        }

        /// <summary>세이브를 불러온다 — 챕터 씬을 새로 열고, 씬이 시작되면 그 자리부터 이어 간다.</summary>
        public static void LoadAndPlay(SaveData data)
        {
            if (data == null) return;
            _pending = data.Clone();
            UI.PauseMenu.ForceUnpause();
            SceneManager.LoadScene(SceneRoutes.TutorialChapter);
        }

        /// <summary>챕터 씬이 시작될 때 한 번 꺼낸다. 없으면 null — 처음부터.</summary>
        public static SaveData TakePending()
        {
            var p = _pending;
            _pending = null;
            return p;
        }
    }
}
