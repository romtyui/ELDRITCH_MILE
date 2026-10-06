using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 遊玩紀錄。**每一場 run 一個 JSON 檔**，從開局寫到結束，中途每一站都存。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【要解決什麼】（2026-09-15 企劃）
    ///   · 這是會輪迴的 Rogue-Like，之後會往多輪發展 —— 遊玩資料要盡可能留下來
    ///   · 修 bug 時要能知道「那一場發生了什麼」：地圖種子、走過哪些站、每站之後的數值
    ///
    /// 【它不是存檔】存檔是「讀回來繼續玩」，這是「事後查」。
    /// 兩者分開，是因為存檔的格式會跟著玩法一直變，紀錄則要能讀舊的。
    /// 所以這裡只寫**字串與數字**，不存任何 Unity 物件參照。
    ///
    /// 【存在哪】`Application.persistentDataPath/RunHistory/run_*.json`
    ///   · Windows：`%USERPROFILE%/AppData/LocalLow/LightCat/ELDRITCH_MILE/RunHistory`
    ///   · WebGL：瀏覽器的 IndexedDB，**不保證寫得進去** —— 所以另外在
    ///     `MetaProgressData.runHistory` 留一份精簡摘要（走 PlayerPrefs，WebGL 也會保留）
    ///
    /// 【有地圖種子就能重現地圖】`seed` 丟回 `MapGenerator.Generate` 會得到同一張圖。
    /// </summary>
    public static class RunRecorder
    {
        public const string FolderName = "RunHistory";

        /// <summary>摘要最多留幾筆。完整紀錄在檔案裡，摘要只是 WebGL 的保底</summary>
        public const int MaxSummaries = 200;

        private static RunRecord current;
        private static MetaProgressData currentMeta;
        private static string currentPath;

        public static string Folder
        {
            get { return Path.Combine(Application.persistentDataPath, FolderName); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // 編輯器關掉 Domain Reload 時 static 會殘留到下一次 Play
            current = null;
            currentMeta = null;
            currentPath = null;

            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        // ==========================================
        // 流程入口（由 GameFlowManager 呼叫）
        // ==========================================

        /// <summary>新的一場開始。上一場還沒結束的話標成放棄。</summary>
        public static void BeginRun(RunContext run, MetaProgressData meta)
        {
            if (run == null) return;

            FinishCurrent(meta, "Abandoned_NewRun");

            current = new RunRecord
            {
                gameVersion = Application.version,
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                runId = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + run.runSeed,
                seed = run.runSeed,
                startedAt = Now(),
                result = "InProgress",
                mapNodeCount = run.mapData != null ? run.mapData.allNodes.Count : 0,
                mapMaxLayer = run.mapData != null ? run.mapData.MaxLayer : 0,
                start = Snapshot(run),
            };
            currentMeta = meta;
            currentPath = Path.Combine(Folder, "run_" + current.runId + ".json");

            Save();
            Debug.Log("[紀錄] 這一場的紀錄寫在 " + currentPath);
        }

        /// <summary>選了一個節點。插播的事件也記在這一筆裡。</summary>
        public static void NodeEntered(RunContext run, RunNodeData node, EventData insertedEvent)
        {
            if (current == null || run == null || node == null) return;

            current.steps.Add(new RunStep
            {
                time = Now(),
                runSeconds = run.ElapsedSeconds,
                what = "EnterNode",
                nodeId = node.nodeId,
                nodeKind = node.kind.ToString(),
                layer = node.layer,
                enemyId = node.enemyId ?? "",
                enemyTier = node.enemyTier.ToString(),
                eventId = insertedEvent != null ? insertedEvent.name : "",
                eventTitle = insertedEvent != null ? insertedEvent.title : "",
                state = Snapshot(run),
            });

            Save();
        }

        /// <summary>一個環節結束（戰鬥、事件、商店…）。數值是「結束之後」的。</summary>
        public static void StageCompleted(RunContext run, StageType stage, StageResult result)
        {
            if (current == null || run == null) return;

            current.steps.Add(new RunStep
            {
                time = Now(),
                runSeconds = run.ElapsedSeconds,
                what = "StageComplete",
                stage = stage.ToString(),
                result = result.ToString(),
                nodeId = run.pendingNode != null ? run.pendingNode.nodeId : "",
                state = Snapshot(run),
            });

            Save();
        }

        /// <summary>這一場結束了（死亡或打完 Boss）。</summary>
        public static void EndRun(RunContext run, MetaProgressData meta, StageResult result)
        {
            if (current == null) return;
            if (run != null) current.final = Snapshot(run);
            FinishCurrent(meta, result.ToString());
        }

        // ==========================================

        private static void OnQuitting()
        {
            FinishCurrent(currentMeta, "Abandoned_Quit");
        }

        private static void FinishCurrent(MetaProgressData meta, string result)
        {
            if (current == null) return;

            current.result = result;
            current.endedAt = Now();
            Save();

            MetaProgressData m = meta != null ? meta : currentMeta;
            if (m != null)
            {
                m.runHistory.Add(new RunSummary
                {
                    runId = current.runId,
                    seed = current.seed,
                    result = current.result,
                    startedAt = current.startedAt,
                    endedAt = current.endedAt,
                    steps = current.steps.Count,
                    deepestLayer = DeepestLayer(current),
                    gameVersion = current.gameVersion,
                });

                while (m.runHistory.Count > MaxSummaries) m.runHistory.RemoveAt(0);
                m.Save();
            }

            current = null;
            currentPath = null;
        }

        private static int DeepestLayer(RunRecord r)
        {
            int deepest = 0;
            for (int i = 0; i < r.steps.Count; i++)
                if (r.steps[i].what == "EnterNode" && r.steps[i].layer > deepest) deepest = r.steps[i].layer;
            return deepest;
        }

        private static void Save()
        {
            if (current == null || string.IsNullOrEmpty(currentPath)) return;

            // ⚠️ 紀錄寫失敗**絕對不能**讓遊戲出錯 —— 這是給事後查的，不是遊戲的一部分
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(currentPath, JsonUtility.ToJson(current, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[紀錄] 寫不進去（不影響遊戲）：" + e.Message);
            }
        }

        private static string Now()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 當下的狀態。**只存字串與數字**（見類別說明）。
        /// HP／SAN／戰鬥牌組讀的是 RunStateManager 的存檔值 —— 戰鬥中途不會即時反映，
        /// 但戰鬥結束時已經回存過了，所以「StageComplete」那一筆是準的。
        /// </summary>
        private static RunSnapshot Snapshot(RunContext run)
        {
            var s = new RunSnapshot
            {
                hp = PlayerVitals.Hp,
                maxHp = PlayerVitals.MaxHp,
                san = PlayerVitals.San,
                maxSan = PlayerVitals.MaxSan,
                money = run.money,
            };

            for (int i = 0; i < run.inventory.Count; i++)
            {
                ItemStack st = run.inventory[i];
                if (st != null) s.items.Add(st.id + " x" + st.count);
            }

            RunStateManager rs = RunStateManager.Instance;
            if (rs != null)
                for (int i = 0; i < rs.savedDeck.Count; i++)
                    if (rs.savedDeck[i] != null) s.battleDeck.Add(rs.savedDeck[i].cardId);

            for (int i = 0; i < run.exploreDeck.Count; i++)
                if (run.exploreDeck[i] != null) s.exploreDeck.Add(run.exploreDeck[i].name);

            s.flags.AddRange(run.flags);

            for (int i = 0; i < run.corruption.Count; i++)
                if (run.corruption[i] != null) s.corruption.Add(run.corruption[i].godId + " " + run.corruption[i].value);

            return s;
        }
    }

    // ==========================================
    // 紀錄的格式。**只加欄位，不改名、不刪** —— 舊紀錄要讀得回來
    // ==========================================

    [Serializable]
    public class RunRecord
    {
        public int recordVersion = 1;
        public string gameVersion = "";
        public string unityVersion = "";
        public string platform = "";

        public string runId = "";
        public int seed;
        public string startedAt = "";
        public string endedAt = "";

        /// InProgress / PlayerDied / RunFinished / Abandoned_NewRun / Abandoned_Quit
        public string result = "";

        public int mapNodeCount;
        public int mapMaxLayer;

        public RunSnapshot start;
        public List<RunStep> steps = new List<RunStep>();
        public RunSnapshot final;
    }

    [Serializable]
    public class RunStep
    {
        public string time = "";
        public float runSeconds;

        /// EnterNode / StageComplete
        public string what = "";

        public string nodeId = "";
        public string nodeKind = "";
        public int layer;
        public string enemyId = "";
        public string enemyTier = "";
        public string eventId = "";
        public string eventTitle = "";

        public string stage = "";
        public string result = "";

        public RunSnapshot state;
    }

    [Serializable]
    public class RunSnapshot
    {
        public int hp, maxHp, san, maxSan, money;
        public List<string> items = new List<string>();
        public List<string> battleDeck = new List<string>();
        public List<string> exploreDeck = new List<string>();
        public List<string> flags = new List<string>();
        public List<string> corruption = new List<string>();
    }

    /// <summary>精簡摘要，存在 MetaProgressData 裡（WebGL 的保底）。</summary>
    [Serializable]
    public class RunSummary
    {
        public string runId = "";
        public int seed;
        public string result = "";
        public string startedAt = "";
        public string endedAt = "";
        public int steps;
        public int deepestLayer;
        public string gameVersion = "";
    }
}
