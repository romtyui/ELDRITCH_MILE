using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 地圖生成完之後，把「每個戰鬥節點要打誰」一次安排好。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼是「生成後安排」而不是「打的時候才抽」】
    ///
    ///   1. **保證出現做得到。**《螺湮的祝福》要打倒半魚人祭司才觸發。
    ///      現場才抽的話只能寄望運氣，那個事件等於機率再乘一次。
    ///      看得到整張地圖，才有辦法說「這一區一定有一站是他」。
    ///
    ///   2. **同一個節點重進是同一隻怪。**現場抽的話玩家離開再進來就換人，
    ///      而且會變成可以重骰到好打的對手。
    ///
    ///   3. 之後地圖 tooltip 想顯示「這一站是什麼怪」時，資料已經在那裡了。
    ///
    /// 這也是 Slay the Spire 對精英與 Boss 的做法：**預先安排，不抽**。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【順序很重要】保證項**先**佔位，剩下的節點才從權重池抽。
    /// 反過來的話，池子可能把每一站都填滿，保證項就沒有位置了。
    /// </summary>
    public static class EncounterPlanner
    {
        /// <summary>
        /// 安排整張地圖的敵人。**一場 run 只呼叫一次**（地圖生成之後）。
        /// </summary>
        /// <param name="rng">
        /// 亂數源要外面傳，而且應該綁 run 種子 ——
        /// 同一場 run 重算要得到同一張安排表。
        /// </param>
        public static void AssignEnemies(
            MapData map, EncounterPool pool, RunContext run,
            System.Random rng, ItemDatabase itemDb = null)
        {
            if (map == null || pool == null || rng == null)
            {
                if (pool == null)
                    Debug.Log("[敵人安排] 沒有指定 EncounterPool，所有戰鬥都交給戰鬥組自己抽怪");
                return;
            }

            // ── Boss 節點：固定，不抽 ──
            var bossNodes = new List<RunNodeData>();
            var combatNodes = new List<RunNodeData>();

            for (int i = 0; i < map.allNodes.Count; i++)
            {
                RunNodeData n = map.allNodes[i];
                if (n == null) continue;

                if (n.kind == MapNodeKind.Boss) bossNodes.Add(n);
                else if (n.kind == MapNodeKind.Combat) combatNodes.Add(n);
            }

            if (!string.IsNullOrEmpty(pool.bossEnemyId))
            {
                for (int i = 0; i < bossNodes.Count; i++)
                {
                    bossNodes[i].enemyId = pool.bossEnemyId;
                    bossNodes[i].enemyTier = EncounterPool.Tier.Boss;
                }
            }

            // ── 保證出現：先佔位 ──
            // 還沒被安排的戰鬥節點。保證項從這裡挑，挑走就不再是候選
            var free = new List<RunNodeData>(combatNodes);

            for (int g = 0; g < pool.guaranteed.Count; g++)
            {
                EncounterPool.Guaranteed req = pool.guaranteed[g];
                if (req == null || string.IsNullOrEmpty(req.enemyId)) continue;

                int placed = 0;

                for (int c = 0; c < req.count; c++)
                {
                    // 合格 = 還沒被安排 ＋ 層數在範圍內
                    var eligible = new List<RunNodeData>();
                    for (int i = 0; i < free.Count; i++)
                    {
                        RunNodeData n = free[i];
                        if (n.layer < req.minLayer) continue;
                        if (req.maxLayer >= 0 && n.layer > req.maxLayer) continue;
                        eligible.Add(n);
                    }

                    if (eligible.Count == 0) break;

                    RunNodeData chosen = eligible[rng.Next(eligible.Count)];
                    chosen.enemyId = req.enemyId;
                    chosen.enemyTier = req.tier;
                    free.Remove(chosen);
                    placed++;
                }

                if (placed < req.count)
                {
                    // ⚠️ 這個一定要吵。保證項沒排進去的話，等它的事件會安靜地永遠不觸發 ——
                    //    那是最難查的一種問題：功能都在，就是不會發生
                    Debug.LogWarning(
                        $"[敵人安排] 保證出現「{req.enemyId}」需要 {req.count} 次，" +
                        $"但只排進去 {placed} 次。\n" +
                        $"戰鬥節點總共 {combatNodes.Count} 個，可排的層數是 " +
                        $"{req.minLayer} ~ {(req.maxLayer < 0 ? "不限" : req.maxLayer.ToString())}。\n" +
                        "地圖太小、戰鬥節點太少、或層數範圍太窄。等這隻怪的事件會因此永遠不觸發。");
                }
            }

            // ── 剩下的從權重池抽 ──
            int filled = 0;
            for (int i = 0; i < free.Count; i++)
            {
                EncounterPool.Tier tier;
                string id = pool.Pick(run, rng, out tier, itemDb);
                if (string.IsNullOrEmpty(id)) continue;   // 抽不到就留空 = 交給戰鬥組
                free[i].enemyId = id;
                free[i].enemyTier = tier;
                filled++;
            }

            Debug.Log(
                $"[敵人安排] 戰鬥節點 {combatNodes.Count} 個：" +
                $"保證項佔了 {combatNodes.Count - free.Count} 個、池子填了 {filled} 個" +
                $"{(bossNodes.Count > 0 && !string.IsNullOrEmpty(pool.bossEnemyId) ? $"；Boss ×{bossNodes.Count} = {pool.bossEnemyId}" : "")}");
        }
    }
}
