using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 抽 <see cref="LootTable"/> 的引擎。純函式，沒有狀態。
    ///
    /// 【為什麼是 static，不是 MonoBehaviour 單例】它不需要記任何東西 ——
    /// 給一張表、給一個亂數源，就回傳結果。做成單例只會多一個「場上忘了放」的失敗點。
    /// 需要查 id → 道具時才向 <see cref="GameFlowManager"/> 借 <see cref="ItemDatabase"/>。
    ///
    /// 【亂數源一定要外面傳進來】不要用 UnityEngine.Random。
    /// 這場 run 有 `RunContext.runSeed`，同一個種子要能重現同一間商店 ——
    /// 用全域亂數的話，玩家讀檔重進商店會看到不一樣的商品。
    /// </summary>
    public static class LootService
    {
        /// <summary>遞迴深度上限。表指到表、表又指回來的話會無限迴圈。</summary>
        private const int MaxTableDepth = 8;

        /// <summary>
        /// 抽一張表，回傳結果。抽不到東西就是空 List（不會是 null）。
        /// </summary>
        /// <param name="db">
        /// 要查的道具庫。傳 null 就向 <see cref="GameFlowManager"/> 借。
        /// **編輯器工具與測試一定要自己傳** —— GameFlowManager 的 Instance 只有在
        /// 執行時才有值，沒進 Play 模式呼叫的話標籤查詢會一筆都查不到。
        /// </param>
        public static List<ItemStack> Roll(LootTable table, System.Random rng, ItemDatabase db = null)
        {
            var result = new List<ItemStack>();
            RollInto(table, rng, result, 0, Resolve(db), null);
            return Merge(result);
        }

        /// <summary>
        /// 抽到剛好 <paramref name="count"/> 件為止 —— 商店要「填滿八格」用這支。
        ///
        /// 表本身可能一次只產出三件，所以會反覆抽；連續抽不出新東西就放棄，
        /// 免得表裡候選不足時卡在無窮迴圈（例如 distinct 開著但池子只有兩個候選）。
        /// </summary>
        public static List<ItemStack> RollExactly(LootTable table, System.Random rng, int count, ItemDatabase db = null)
        {
            var result = new List<ItemStack>();
            if (table == null || count <= 0) return result;

            db = Resolve(db);

            // ⚠️ 這個集合要**跨批次**活著。
            //
            // 一次 Roll 不一定湊得滿八件（標籤查詢的候選被 distinct 用完時，
            // 那一次抽獎就空手而回），所以會再抽一輪。若每輪各自算 distinct，
            // 第二輪會從頭開始 —— 貨架上就會出現兩份曬乾的海藻。
            // 把集合傳進去，distinct 的池子才知道前面已經拿過什麼。
            var takenAcrossBatches = new HashSet<string>();

            int barren = 0;   // 連續幾輪沒有進帳

            while (result.Count < count && barren < 8)
            {
                int before = result.Count;

                var batch = new List<ItemStack>();
                RollInto(table, rng, batch, 0, db, takenAcrossBatches);

                foreach (ItemStack s in Merge(batch))
                {
                    if (result.Count >= count) break;
                    result.Add(s);
                }

                barren = result.Count > before ? 0 : barren + 1;
            }

            if (result.Count < count)
            {
                Debug.LogWarning(
                    $"[戰利品] 「{table.name}」抽不滿 {count} 件（只有 {result.Count} 件）。\n" +
                    "候選不夠，或條目的 Weight 是 0。畫面上會留下空格。");
            }

            return result;
        }

        // ==========================================
        /// <param name="sharedTaken">
        /// 跨池／跨批次共用的「已經抽過什麼」。傳 null 則每個 distinct 池子各自計算
        /// （同一張表的兩個池子可以抽到同一件東西，那是合理的）。
        /// 商店要填滿格子時會傳一個進來，見 <see cref="RollExactly"/>。
        /// </param>
        private static void RollInto(
            LootTable table, System.Random rng, List<ItemStack> output,
            int depth, ItemDatabase db, HashSet<string> sharedTaken)
        {
            if (table == null || rng == null) return;

            if (depth >= MaxTableDepth)
            {
                Debug.LogWarning($"[戰利品] 「{table.name}」的表層數超過 {MaxTableDepth} 層，可能互相指來指去。中止。");
                return;
            }

            for (int p = 0; p < table.pools.Count; p++)
            {
                LootTable.Pool pool = table.pools[p];
                if (pool == null || pool.entries.Count == 0) continue;

                if (pool.chance < 1f && rng.NextDouble() > pool.chance) continue;

                int rolls = RandomRange(rng, pool.rollsMin, pool.rollsMax);

                // distinct 是「這一個池子這一次執行」之內不重複，
                // 不是跨池 —— 主獎池與雜物池抽到同一件東西是合理的
                var taken = pool.distinct ? (sharedTaken ?? new HashSet<string>()) : null;

                for (int r = 0; r < rolls; r++)
                {
                    LootTable.Entry entry = PickEntry(pool, rng, taken);
                    if (entry == null) break;   // 候選被 distinct 吃光了

                    Resolve(entry, rng, output, taken, depth, db, sharedTaken);
                }
            }
        }

        private static void Resolve(
            LootTable.Entry entry, System.Random rng,
            List<ItemStack> output, HashSet<string> taken, int depth, ItemDatabase db,
            HashSet<string> sharedTaken)
        {
            switch (entry.kind)
            {
                case LootTable.EntryKind.Item:
                    Emit(entry.itemId, entry, rng, output, taken);
                    break;

                case LootTable.EntryKind.TagQuery:
                {
                    List<ItemData> matches = Query(entry.requireTags, entry.excludeTags, db);
                    if (matches.Count == 0)
                    {
                        Debug.LogWarning(
                            $"[戰利品] 標籤查詢沒有任何符合的道具：需要 [{string.Join(", ", entry.requireTags)}]。\n" +
                            "ItemDatabase 裡沒有這種標籤的東西，或標籤打錯字。");
                        return;
                    }

                    // distinct 開著時要避開已抽過的，否則會一直抽到同一條魚然後被丟掉
                    if (taken != null)
                    {
                        matches.RemoveAll(m => taken.Contains(m.id));
                        if (matches.Count == 0) return;
                    }

                    ItemData pickedItem = matches[rng.Next(matches.Count)];
                    Emit(pickedItem.id, entry, rng, output, taken);
                    break;
                }

                case LootTable.EntryKind.Table:
                    // ⚠️ 往下傳的是 `taken`（本池子的不重複集合），不是 sharedTaken。
                    //
                    // 外層寫「抽 3 張卡、不重複」而條目是「轉去卡片表」時，
                    // 若不把集合傳下去，子表每次都從零開始算不重複 —— 三格會出現同一張卡。
                    // 「不重複」是**外層池子的意圖**，委派出去不該把它丟掉。
                    //
                    // 子表的池子要 distinct = true 才會用到它（不然它本來就不在乎重複）。
                    RollInto(entry.table, rng, output, depth + 1, db, taken ?? sharedTaken);
                    break;
            }
        }

        private static void Emit(
            string id, LootTable.Entry entry, System.Random rng,
            List<ItemStack> output, HashSet<string> taken)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (taken != null && !taken.Add(id)) return;

            int count = RandomRange(rng, entry.countMin, entry.countMax);
            if (count <= 0) return;

            output.Add(new ItemStack(id, count));
        }

        /// <summary>
        /// 依權重挑一個條目。<paramref name="taken"/> 只用來略過「指名道具且已經抽過」的條目 ——
        /// 標籤查詢沒辦法在這裡判斷（它代表一整群道具），交給 Resolve 處理。
        /// </summary>
        private static LootTable.Entry PickEntry(LootTable.Pool pool, System.Random rng, HashSet<string> taken)
        {
            float total = 0f;
            int zeroWeight = 0;

            var candidates = new List<LootTable.Entry>();

            for (int i = 0; i < pool.entries.Count; i++)
            {
                LootTable.Entry e = pool.entries[i];
                if (e == null) continue;

                if (e.weight <= 0f) { zeroWeight++; continue; }

                if (taken != null && e.kind == LootTable.EntryKind.Item && taken.Contains(e.itemId)) continue;

                candidates.Add(e);
                total += e.weight;
            }

            if (candidates.Count == 0)
            {
                if (zeroWeight > 0)
                {
                    Debug.LogWarning(
                        $"[戰利品] 池子「{pool.note}」有 {zeroWeight} 筆條目的 Weight 是 0 而被跳過。\n" +
                        "⚠️ Unity 用 Inspector 的 + 新增 List 元素時會零填充 —— 請手動改成 1。");
                }
                return null;
            }

            double roll = rng.NextDouble() * total;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].weight;
                if (roll <= 0) return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }

        /// <summary>
        /// 標籤查詢。回傳的是**新的 List**，呼叫端可以放心修改。
        /// </summary>
        public static List<ItemData> Query(List<string> require, List<string> exclude, ItemDatabase db = null)
        {
            var result = new List<ItemData>();

            db = Resolve(db);
            if (db == null)
            {
                Debug.LogWarning("[戰利品] GameFlowManager 上沒有掛 ItemDatabase，標籤查詢一定查不到東西。");
                return result;
            }

            for (int i = 0; i < db.items.Count; i++)
            {
                ItemData item = db.items[i];
                if (item == null || string.IsNullOrEmpty(item.id)) continue;

                bool ok = true;

                if (require != null)
                {
                    for (int t = 0; t < require.Count && ok; t++)
                    {
                        if (!string.IsNullOrEmpty(require[t]) && !item.HasTag(require[t])) ok = false;
                    }
                }

                if (ok && exclude != null)
                {
                    for (int t = 0; t < exclude.Count && ok; t++)
                    {
                        if (!string.IsNullOrEmpty(exclude[t]) && item.HasTag(exclude[t])) ok = false;
                    }
                }

                if (ok) result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// 把同 id 的疊合併成一疊。「舊麻繩、舊麻繩、舊麻繩」讀起來像壞掉，「舊麻繩 ×3」才對。
        ///
        /// ⚠️ 等 <see cref="ItemStack"/> 加上 per-instance 狀態（每條魚不同的 +HP／−SAN）之後，
        /// 這裡必須改成「同 id **且狀態相同**才合併」，否則兩條不同的魚會被併掉。
        /// 這與 <see cref="RunContext.AddItem"/> 裡的那條註記是同一件事，改要一起改。
        /// </summary>
        private static List<ItemStack> Merge(List<ItemStack> stacks)
        {
            var merged = new List<ItemStack>();

            for (int i = 0; i < stacks.Count; i++)
            {
                ItemStack s = stacks[i];
                if (s == null || string.IsNullOrEmpty(s.id)) continue;

                bool found = false;
                for (int j = 0; j < merged.Count; j++)
                {
                    if (merged[j].id != s.id) continue;

                    merged[j].count += s.count;
                    found = true;
                    break;
                }

                if (!found) merged.Add(new ItemStack(s.id, s.count));
            }

            return merged;
        }

        /// <summary>沒傳道具庫就向 GameFlowManager 借。</summary>
        private static ItemDatabase Resolve(ItemDatabase db)
        {
            if (db != null) return db;
            return GameFlowManager.Instance != null ? GameFlowManager.Instance.itemDatabase : null;
        }

        /// <summary>兩端都含。min &gt; max 時交換，免得資料填反直接回 0。</summary>
        private static int RandomRange(System.Random rng, int min, int max)
        {
            if (min > max) (min, max) = (max, min);
            return rng.Next(min, max + 1);
        }
    }
}
