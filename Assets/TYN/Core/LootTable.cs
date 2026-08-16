using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 一張戰利品表。**商店賣什麼、寶箱開出什麼，都是這個東西。**
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼「階級」在表上，不在物品上】
    ///
    /// 直覺的做法是給每個道具一個 `tier`，然後「Tier 2 寶箱抽 Tier 2 道具」。
    /// 這在第二個區域就會壞掉 —— 同一條鹹魚在漁村是像樣的補給、在後段是雜物。
    /// 把階級寫在物品上，等於宣告它**在整個遊戲裡永遠一樣珍貴**。
    ///
    /// 所以分工是：
    ///   · 物品的 `tags` 說「**它是什麼**」 —— [Consumable, SeaFood]
    ///   · 這張表說「**這個場合會出現什麼、機率多少**」 —— Loot_Village_Chest_T2
    ///
    /// 難度、區域、稀有度全部是「換一張表」，不是「改物品」。
    /// Minecraft 的 loot_tables、Diablo 的 TreasureClass 都是這個形狀。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【三層結構】表 → 池 → 條目
    ///
    ///   Pool（池）  一次「抽獎機會」。有機率、有抽幾次。一張表可以有多個池：
    ///               「保證掉 1 件主獎（100%）」＋「30% 機率再從雜物池抽 2 件」
    ///
    ///   Entry（條目）池裡的一個候選，帶權重。三種寫法：
    ///               · Item      指名某個道具
    ///               · TagQuery  「任何帶 SeaFood 標籤的東西」 —— 加新魚不用改表
    ///               · Table     指向另一張表 —— 共用的雜物池只寫一次
    ///
    /// 漁村商店會長這樣：
    ///   Pool A ── 抽 6 次 ── [TagQuery SeaFood 權重 70] [TagQuery Misc 權重 25] [Item 撬棍 權重 5]
    ///   Pool B ── 抽 2 次 ── [Table Loot_Shared_Equipment]
    /// </summary>
    [CreateAssetMenu(fileName = "Loot_", menuName = "Eldritch/Loot Table")]
    public class LootTable : ScriptableObject
    {
        public enum EntryKind
        {
            /// 指名一個道具 id
            Item = 0,

            /// 任何符合標籤條件的道具。**加新道具不用回頭改表**
            TagQuery = 1,

            /// 轉去抽另一張表。共用的池子只要寫一次
            Table = 2,
        }

        [Serializable]
        public class Entry
        {
            [Tooltip("這一條是「指名道具」「標籤查詢」還是「轉到另一張表」")]
            public EntryKind kind = EntryKind.Item;

            [Tooltip("Item：道具 id。要在 ItemDatabase 登記過")]
            public string itemId = "";

            [Tooltip("TagQuery：必須**全部**具備的標籤。留空 = 不限")]
            public List<string> requireTags = new List<string>();

            [Tooltip("TagQuery：具備**任一個**就排除的標籤")]
            public List<string> excludeTags = new List<string>();

            [Tooltip("Table：要轉去抽的表")]
            public LootTable table;

            [Tooltip("權重。**同一個池子裡的相對值**，不是百分比。\n" +
                     "⚠️ Unity 用 + 新增 List 元素時會零填充 —— 記得改成 1 以上，否則這條永遠抽不到")]
            [Min(0f)] public float weight = 1f;

            [Tooltip("抽中時給幾個。兩個都填 1 就是一個")]
            [Min(1)] public int countMin = 1;
            [Min(1)] public int countMax = 1;
        }

        [Serializable]
        public class Pool
        {
            [Tooltip("給人看的註記，不影響行為。例如「保證主獎」「雜物」")]
            public string note = "";

            [Tooltip("這個池子有多少機率會被執行。1 = 一定會")]
            [Range(0f, 1f)] public float chance = 1f;

            [Tooltip("執行時抽幾次。兩個都填 1 就是一次")]
            [Min(0)] public int rollsMin = 1;
            [Min(0)] public int rollsMax = 1;

            [Tooltip("同一個池子裡不重複抽到同一個道具。\n" +
                     "商店要開（八格全是鹹魚很蠢），寶箱通常不用（三條魚是合理的）")]
            public bool distinct = true;

            public List<Entry> entries = new List<Entry>();
        }

        [Header("池子。由上往下依序執行")]
        public List<Pool> pools = new List<Pool>();
    }
}
