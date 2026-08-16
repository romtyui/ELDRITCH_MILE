using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// id → <see cref="ItemData"/> 的查表。
    ///
    /// 【為什麼需要它】背包裡存的是字串 id（見 ItemData 的說明），
    /// 但玩家不該看到 `lockpick` 這種東西。中間需要一層翻譯。
    ///
    /// 【怎麼取用】掛在 `GameFlowManager` 上（它已經持有 `RunContext`，也就是背包本身），
    /// 用 `GameFlowManager.ItemName(id)` 取顯示名。與 `ProbabilityCheck` 持有
    /// `AttributeChartData` 是同一個模式：MonoBehaviour 單例持有 SO 資料。
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Eldritch/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [Tooltip("所有道具。順序不影響行為")]
        public List<ItemData> items = new List<ItemData>();

        private Dictionary<string, ItemData> lookup;

        /// <summary>
        /// 建快取時清單有幾筆。用來偵測「有人在程式裡改了清單」。
        ///
        /// 【為什麼需要】`OnValidate` 只有在 Inspector 編輯、Undo、匯入時才會觸發。
        /// 用程式 `items.Add(...)` 是**不會**觸發的 —— 快取還停在舊的內容，
        /// 剛加進去的道具查不到，而且完全沒有錯誤訊息。
        /// </summary>
        private int cachedCount = -1;

        private void OnEnable()
        {
            // SO 在編輯器裡會隨 domain reload 重新載入，快取要跟著失效
            Invalidate();
        }

        private void OnValidate()
        {
            Invalidate();
        }

        /// <summary>
        /// 讓快取失效。**在程式裡調換既有元素（數量沒變）之後一定要自己呼叫** ——
        /// 只有數量變化偵測得到。
        /// </summary>
        public void Invalidate()
        {
            lookup = null;
            cachedCount = -1;
        }

        private void Rebuild()
        {
            lookup = new Dictionary<string, ItemData>();
            cachedCount = items.Count;

            for (int i = 0; i < items.Count; i++)
            {
                ItemData item = items[i];

                // Unity 的 Inspector 用 + 新增 List 元素時會零填充，
                // 所以空元素是常見的（HANDOFF §4.3），不當成錯誤只跳過
                if (item == null) continue;

                if (string.IsNullOrEmpty(item.id))
                {
                    Debug.LogWarning(
                        $"[道具] 「{item.name}」沒有填 Id，查不到也用不了。", item);
                    continue;
                }

                if (lookup.ContainsKey(item.id))
                {
                    Debug.LogWarning(
                        $"[道具] id 重複：「{item.id}」同時被 {lookup[item.id].name} 與 {item.name} 使用。\n" +
                        "後者會被忽略。id 必須唯一 —— 它是背包與存檔認得的唯一依據。", item);
                    continue;
                }

                lookup.Add(item.id, item);
            }
        }

        public ItemData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (lookup == null || cachedCount != items.Count) Rebuild();

            return lookup.TryGetValue(id, out ItemData item) ? item : null;
        }

        /// <summary>
        /// 取顯示名。**查不到就回傳 id 本身** —— 畫面上出現 `lockpick` 很醜，
        /// 但出現空白會讓玩家以為是 bug，而且更難查。
        /// </summary>
        public string DisplayNameOf(string id)
        {
            ItemData item = GetById(id);
            return item != null ? item.Label : id;
        }
    }
}
