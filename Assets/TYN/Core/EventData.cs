using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 事件的一個效果。選項選下去之後真正發生的事。
    ///
    /// 【種類是照大綱八個事件實際需要的東西開的】不是憑空想像 ——
    /// 每一種下面都註明了它從哪個事件來。
    ///
    /// ⚠️ **有四種還沒有系統可以接**（銷毀武器牌、隨機傳送、進入戰鬥、獲得神牌）。
    /// 它們仍然列在這裡，因為文案現在就要寫得出來；執行時會在 Console 說明並跳過，
    /// **不會靜靜地什麼都沒發生**。等對應的系統做好，只要補上那一個 case。
    /// </summary>
    [Serializable]
    public class EventEffect
    {
        public enum Kind
        {
            /// 獲得道具。《無人的小船》→ 老舊釣竿
            GrantItem = 0,

            /// 消耗道具。《貪吃鬼》→ 給他吃的
            ConsumeItem = 1,

            /// 侵蝕度增減。《暴食之深淵》+5%、《海市蜃樓》+10%
            Corruption = 2,

            /// SAN 增減（**百分比**，對上限算）。《海市蜃樓》san−20%、《損壞的祭壇》恢復 20%
            ChangeSanPercent = 3,

            /// HP 增減（百分比）
            ChangeHpPercent = 4,

            /// 立一個旗標。用來記「這件事發生過」
            SetFlag = 5,

            /// 給錢
            GrantMoney = 6,

            /// <summary>
            /// 消耗**任何**帶某個標籤的東西。《貪吃鬼》→「消耗多少糧食」。
            ///
            /// 跟 <see cref="ConsumeItem"/> 的差別：那個要指名某一條魚，這個只說「糧食」。
            /// 文案不會知道玩家背包裡當下有哪一種，所以大部分「消耗某類資源」都該用這個。
            /// </summary>
            ConsumeItemByTag = 7,

            /// <summary>
            /// 抽一張戰利品表，抽到什麼給什麼。
            /// 《喂米可吃飯》選項 A 的「（可能獲得某些資源）」——
            /// 文案說的是「某些」，本來就不該指名。
            ///
            /// 表填在 <see cref="table"/>，跟寶箱與商店用的是同一種資產。
            /// </summary>
            GrantFromTable = 8,

            /// <summary>
            /// 《好餓好餓的貪吃鬼》選項 B：**插一場戰鬥**，打完接回這一站原本的內容。
            ///
            /// `key` = 對手的 `EnemyData.enemyId`，留空則交給戰鬥組自己抽怪。
            /// ⚠️ 目前五個 EnemyData 的 enemyId 都還是空字串，填了也指定不到。
            /// </summary>
            StartBattle = 102,

            // ── 以下三種還沒有系統可以接 ──

            /// 《喂米可吃飯》永久銷毀 1 張武器牌。**需要牌組編輯，尚未接上**
            DestroyWeaponCard = 100,

            /// 《門扉》隨機傳送到地圖的任意節點。**需要地圖跳轉 API，尚未接上**
            TeleportRandomNode = 101,

            /// 《損壞的祭壇》獲得 1 張神牌。**需要神牌系統，尚未接上**
            GrantGodCard = 103,
        }

        [Tooltip("要發生什麼")]
        public Kind kind = Kind.GrantItem;

        [Tooltip("GrantItem / ConsumeItem：道具 id\n" +
                 "ConsumeItemByTag：道具**標籤**（Food / Weapon / Curio…）\n" +
                 "Corruption：神的 id（深淵 = abyss）\n" +
                 "SetFlag：旗標名稱\n" +
                 "其餘不用填")]
        public string key = "";

        [Tooltip("數量／百分比／增減值。SetFlag 不用填")]
        public int amount = 1;

        [Tooltip("GrantFromTable：要抽的戰利品表。跟寶箱／商店用的是同一種資產")]
        public LootTable table;

        /// <summary>
        /// 套用這個效果，回傳**要給玩家看的一行提示**（沒有就回空字串）。
        ///
        /// 【為什麼回傳字串而不是自己彈視窗】大綱把數值變化直接寫在事件內文裡
        /// （「（【深淵】的侵蝕度+5%）」），所以它本來就是事件文字的一部分。
        /// 由事件 Stage 收集起來一起顯示，才不會一個效果彈一次視窗。
        ///
        /// 這也剛好符合企劃說的「**屬於變動的當下有提示，但遊戲過程不會持續顯示**」。
        /// </summary>
        public string Apply(RunContext run)
        {
            if (run == null) return "";

            switch (kind)
            {
                case Kind.GrantItem:
                {
                    if (string.IsNullOrEmpty(key)) return "";
                    run.AddItem(key, Mathf.Max(1, amount));

                    string n = GameFlowManager.ItemName(key);
                    return amount > 1 ? $"獲得 {n} ×{amount}" : $"獲得 {n}";
                }

                case Kind.ConsumeItem:
                {
                    if (string.IsNullOrEmpty(key)) return "";

                    int want = Mathf.Max(1, amount);
                    string n = GameFlowManager.ItemName(key);

                    // 付不起就不扣 —— 與 RunContext.ConsumeItem 的「全有或全無」一致
                    return run.ConsumeItem(key, want) ? $"失去 {n} ×{want}" : "";
                }

                case Kind.ConsumeItemByTag:
                {
                    if (string.IsNullOrEmpty(key)) return "";

                    int want = Mathf.Max(1, amount);
                    System.Collections.Generic.List<ItemStack> taken = run.ConsumeByTag(key, want);

                    // 扣不起就什麼都沒發生。條件層應該已經擋掉了，
                    // 但條件與效果之間有可能被別的效果插隊（同一個選項先扣再扣）
                    if (taken.Count == 0) return "";

                    var names = new System.Collections.Generic.List<string>();
                    for (int t = 0; t < taken.Count; t++)
                    {
                        string n = GameFlowManager.ItemName(taken[t].id);
                        names.Add(taken[t].count > 1 ? $"{n} ×{taken[t].count}" : n);
                    }

                    return "失去 " + string.Join("、", names.ToArray());
                }

                case Kind.Corruption:
                {
                    int actual = run.AddCorruption(key, amount);
                    if (actual == 0) return "";

                    // 已經 98% 時再 +5，玩家該看到的是 +2 —— 用實際變動量，不是傳入值
                    return $"【{CorruptionLabel(key)}】的侵蝕度 {actual:+#;-#;0}%";
                }

                case Kind.ChangeSanPercent:
                {
                    if (!PlayerVitals.IsReady) return WarnNotReady("SAN");

                    int delta = Mathf.RoundToInt(PlayerVitals.MaxSan * amount / 100f);
                    if (delta == 0) return "";

                    if (delta > 0) { PlayerVitals.RestoreSan(delta); return $"恢復 {delta} 點 SAN"; }

                    return PlayerVitals.SpendSan(-delta) ? $"失去 {-delta} 點 SAN" : "";
                }

                case Kind.ChangeHpPercent:
                {
                    if (!PlayerVitals.IsReady) return WarnNotReady("HP");

                    int delta = Mathf.RoundToInt(PlayerVitals.MaxHp * amount / 100f);
                    if (delta == 0) return "";

                    if (delta > 0) { PlayerVitals.HealHp(delta); return $"恢復 {delta} 點 HP"; }

                    return PlayerVitals.SpendHp(-delta) ? $"失去 {-delta} 點 HP" : "";
                }

                case Kind.GrantFromTable:
                {
                    if (table == null)
                    {
                        Debug.LogWarning("[事件] GrantFromTable 沒有指定戰利品表，這次跳過。");
                        return "";
                    }

                    // ⚠️ 亂數**不綁 run 種子**。
                    //
                    // 商店的「賣什麼」要能重現（玩家離開再進來不該重骰），
                    // 但事件的獎勵是一次性的 —— 綁了種子反而會讓同一場 run 裡
                    // 兩次觸發同一個事件拿到完全一樣的東西。
                    System.Collections.Generic.List<ItemStack> loot =
                        LootService.Roll(table, new System.Random());

                    if (loot.Count == 0) return "";

                    var names = new System.Collections.Generic.List<string>();
                    for (int i = 0; i < loot.Count; i++)
                    {
                        run.AddItem(loot[i].id, loot[i].count);

                        string n = GameFlowManager.ItemName(loot[i].id);
                        names.Add(loot[i].count > 1 ? $"{n} ×{loot[i].count}" : n);
                    }

                    return "獲得 " + string.Join("、", names.ToArray());
                }

                case Kind.SetFlag:
                    run.SetFlag(key);
                    return "";

                case Kind.GrantMoney:
                    run.AddMoney(amount);
                    return $"獲得 {amount} 金幣";

                case Kind.StartBattle:
                    // 戰鬥**插在**事件與這一站原本的內容之間，不是取代它 ——
                    // 理由見 GameFlowManager.InsertBattleBeforeNextStage()。
                    //
                    // key 留空就交給戰鬥組自己抽怪。目前五個 EnemyData 的 enemyId
                    // 都還是空字串，所以填了也指定不到 —— 那一格填了才會生效。
                    if (GameFlowManager.Instance == null)
                    {
                        Debug.LogWarning("[事件] 想開始戰鬥但找不到 GameFlowManager，這次跳過");
                        return "";
                    }

                    GameFlowManager.Instance.InsertBattleBeforeNextStage(key);
                    return "";

                default:
                    Debug.LogWarning(
                        $"[事件] 效果 {kind} **尚未接上**，這次跳過。\n" +
                        "它需要的系統還不存在（牌組編輯／地圖跳轉／神牌）。\n" +
                        "文案可以照寫，等系統做好補上對應的 case 就會生效。");
                    return "";
            }
        }

        private static string WarnNotReady(string what)
        {
            Debug.LogWarning(
                $"[事件] 想改 {what} 但它還沒初始化，這次跳過。\n" +
                "請在 GameFlowManager 設定 Starting Max Hp（見 PlayerVitals）。");
            return "";
        }

        /// <summary>侵蝕度的顯示名。目前只有深淵，之後加神就在這裡加一行。</summary>
        private static string CorruptionLabel(string godId)
        {
            if (godId == CorruptionTracks.Abyss) return "深淵";
            return godId;
        }
    }

    /// <summary>
    /// 一個事件。大綱〈事件〉那一章的每一個表格就是一個這種資產。
    ///
    /// 【觸發時機】「每當前進到下一張地圖，只要滿足對應條件，就會有概率觸發」——
    /// 也就是**進入節點時、Stage 載入之前**。檢查只發生在那一個點，不要散在各處。
    ///
    /// 【對原本節點的影響：前置，不覆蓋】先播事件，播完**照常**進原本的節點。
    /// 覆蓋的話玩家會因為運氣好觸發了事件反而少玩到一間房。
    ///
    /// 【教學是特例，不是另一套】「第一次輪迴（新手教學）」＝
    /// `once = true` ＋ `chance = 1` ＋ 條件是「旗標還沒立」。同一個機制。
    /// </summary>
    [CreateAssetMenu(fileName = "Event_", menuName = "Eldritch/Event")]
    public class EventData : ScriptableObject
    {
        [Header("識別")]
        [Tooltip("唯一 id。**定了就不要改** —— 「觸發過沒有」是靠 event_<id> 這個旗標記的，\n" +
                 "改了會讓已經觸發過的事件重新變成沒觸發過")]
        public string id = "";

        [Tooltip("標題，例如「暴食之深淵」。顯示用")]
        public string title = "";

        [Header("觸發")]
        [Tooltip("全部成立才有機會觸發。留空 = 無條件")]
        public List<GameCondition> conditions = new List<GameCondition>();

        [Tooltip("條件成立後，這次真的觸發的機率。1 = 一定")]
        [Range(0f, 1f)] public float chance = 1f;

        [Tooltip("一整場 run 只會觸發一次。\n" +
                 "⚠️ 大綱寫「從**未觸發過**的事件中隨機」，所以多數事件都要勾")]
        public bool once = true;

        [Tooltip("同時有多個事件可觸發時的權重")]
        [Min(0f)] public float weight = 1f;

        [Header("內容")]
        [TextArea(4, 12)]
        [Tooltip("事件內文。大綱表格裡「事件內容」那一格")]
        public string bodyText = "";

        [Tooltip("事件的圖（CG）。可留空")]
        public Sprite image;

        [Tooltip("這個事件裡會**開口說話**的角色名。\n\n" +
                 "內文與結果文字裡「半魚人：餓...好餓」這種開頭的段落，\n" +
                 "會改用**有名字框**的對話樣式播；其餘走旁白的公版。\n\n" +
                 "⚠️ 一定要在這裡列名字 —— 只看「有沒有冒號」的話，\n" +
                 "旁白裡的冒號會被誤判成人名，名字框就會冒出奇怪的東西。")]
        public List<string> speakerNames = new List<string>();

        [Header("新手教學")]
        [Tooltip("這個事件**開始播的時候**要發哪個教學訊號。留空 = 不發。\n\n" +
                 "《損壞的祭壇》填 AltarOpened —— 教學序列在等這一步。\n" +
                 "沒在跑教學時發了也不會怎樣（沒有人訂閱，訊號就散掉）")]
        public string startSignal = "";

        [Serializable]
        public class Option
        {
            [Tooltip("選項的名字，例如「往上看」。**這是玩家點的那一行**")]
            public string label = "";

            [TextArea(3, 10)]
            [Tooltip("選了之後的內文")]
            public string resultText = "";

            [Tooltip("選了之後發生的事")]
            public List<EventEffect> effects = new List<EventEffect>();

            [Tooltip("**選了這一項**要發哪個教學訊號。留空 = 不發。\n\n" +
                     "《損壞的祭壇》：祈禱填 GodCardObtained、無視填 PrayerDeclined —— \n" +
                     "教學序列靠這兩個分辨玩家走了哪一條")]
            public string chosenSignal = "";
        }

        [Tooltip("選項。**可以是 0 個** —— 《無人的小船》那種只有敘述、沒有選擇的事件\n" +
                 "最多三個（對話框只有三個選項槽）")]
        public List<Option> options = new List<Option>();

        [Header("備註")]
        [TextArea(2, 4)] public string notes = "";

        /// <summary>「觸發過沒有」用的旗標名。</summary>
        public string TriggeredFlag => "event_" + id;
    }
}
