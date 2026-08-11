using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 【遺產機制的切分點】跨輪迴保存的資料。
    ///
    /// A4：戰敗＝死亡＝進入下個輪迴，且有規劃「遺產機制」讓部分內容延續。
    /// 為此把狀態切成兩層：
    ///
    ///   MetaProgressData ── 跨 run，死亡不會清空。存在磁碟。   ← 本類別
    ///   RunContext       ── 單場 run，死亡即整個丟棄。純記憶體。
    ///
    /// 目前遺產機制尚未設計，所以本類別只是骨架 —— 但**切分點已經在這裡了**。
    /// 日後要做遺產，只需要：
    ///   1. 在此新增欄位
    ///   2. 在 RunContext.CreateNew() 決定「新 run 從遺產繼承什麼」
    ///   3. 在 RunContext.ContributeToMeta() 決定「這場 run 留下什麼」
    /// 不需要動 GameFlowManager 或任何 Stage。
    /// </summary>
    [Serializable]
    public class MetaProgressData
    {
        [Header("統計")]
        public int totalRuns;
        public int deaths;
        public int bestLayerReached;

        [Header("遺產（跨輪迴保留）")]
        /// 死亡後仍保留的道具 id。目前沒有任何東西會寫入 —— 待遺產機制設計。
        public List<string> legacyItemIds = new List<string>();

        /// 已解鎖、之後每場 run 都能出現的卡牌 id。
        public List<string> unlockedCardIds = new List<string>();

        public bool HasLegacyItem(string id)
        {
            return !string.IsNullOrEmpty(id) && legacyItemIds.Contains(id);
        }

        public void AddLegacyItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (legacyItemIds.Contains(id)) return;

            legacyItemIds.Add(id);
            Debug.Log($"[Meta] 獲得遺產：{id}");
        }

        // ==========================================
        // 存檔。沿用專案既有的 PlayerPrefs 慣例（RunStateManager 也是這樣做）。
        // 之後要換成檔案存檔，只需改這兩支。
        // ==========================================
        private const string SaveKey = "EldritchMile_MetaProgress";

        public void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public static MetaProgressData Load()
        {
            string raw = PlayerPrefs.GetString(SaveKey, "");

            if (string.IsNullOrWhiteSpace(raw))
            {
                return new MetaProgressData();
            }

            try
            {
                return JsonUtility.FromJson<MetaProgressData>(raw) ?? new MetaProgressData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Meta] 讀取進度失敗，改用全新資料：{e.Message}");
                return new MetaProgressData();
            }
        }

        public static void ClearSave()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            Debug.Log("[Meta] 已清除跨輪迴進度");
        }
    }
}
