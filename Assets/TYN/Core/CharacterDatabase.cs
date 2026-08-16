using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// id → <see cref="CharacterData"/> 的查表。與 <see cref="ItemDatabase"/> 完全同一個形狀，
    /// 同樣掛在 <see cref="GameFlowManager"/> 上。
    ///
    /// 兩個庫沒有合併成一個「萬用資料庫」，因為道具與角色的查詢條件不一樣
    /// （道具會依標籤抽獎，角色是指名），硬合併只會讓兩邊都多一層轉型。
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Eldritch/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        public List<CharacterData> characters = new List<CharacterData>();

        private Dictionary<string, CharacterData> lookup;

        /// <summary>
        /// 建快取時清單有幾筆。用來偵測「有人在程式裡改了清單」。
        ///
        /// 【為什麼需要】`OnValidate` 只有在 Inspector 編輯、Undo、匯入時才會觸發。
        /// 用程式 `characters.Add(...)` 是**不會**觸發的 —— 快取還停在舊的內容，
        /// 於是剛加進去的角色查不到，而且完全沒有錯誤訊息。
        /// 這個坑實際發生過：用編輯器腳本補了一個商人進去，`GetById` 一直回 null。
        /// </summary>
        private int cachedCount = -1;

        private void OnEnable() { Invalidate(); }
        private void OnValidate() { Invalidate(); }

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
            lookup = new Dictionary<string, CharacterData>();
            cachedCount = characters.Count;

            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData c = characters[i];

                // Inspector 用 + 新增會零填充，空元素是常見的（HANDOFF §4.3）
                if (c == null) continue;

                if (string.IsNullOrEmpty(c.id))
                {
                    Debug.LogWarning($"[角色] 「{c.name}」沒有填 Id，查不到也用不了。", c);
                    continue;
                }

                if (lookup.ContainsKey(c.id))
                {
                    Debug.LogWarning(
                        $"[角色] id 重複：「{c.id}」同時被 {lookup[c.id].name} 與 {c.name} 使用。後者會被忽略。", c);
                    continue;
                }

                lookup.Add(c.id, c);
            }
        }

        public CharacterData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (lookup == null || cachedCount != characters.Count) Rebuild();

            return lookup.TryGetValue(id, out CharacterData c) ? c : null;
        }

        /// <summary>查不到就回傳 id 本身 —— 跟 ItemDatabase 一樣的理由。</summary>
        public string DisplayNameOf(string id)
        {
            CharacterData c = GetById(id);
            return c != null ? c.Label : id;
        }
    }
}
