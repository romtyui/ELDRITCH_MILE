using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 一項道具的資料。
    ///
    /// 【為什麼 RunContext 存的還是字串 id，不是這個型別】
    /// 背包的真相要能存檔。字串 id 序列化乾淨、跨版本穩定；把資產引用寫進存檔則很脆
    /// （資產搬移、GUID 變動、資料重整都會讓舊存檔壞掉）。
    /// 而且 `MetaProgressData.legacyItemIds` 早就是字串了，兩邊要一致。
    ///
    /// 所以分工是：**id 是真相，這個型別只負責「長什麼樣」** ——
    /// 顯示的時候才透過 <see cref="ItemDatabase"/> 查表。
    /// 這跟 <see cref="EncounterTargetView"/> 對 <see cref="IProbabilityTarget"/> 的關係是同一個模式。
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "Eldritch/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("識別")]
        [Tooltip("道具的唯一 id。**這是存進背包與存檔的東西，定了就不要改** ——\n" +
                 "改了之後舊存檔、寶箱的 Granted Item Ids、需要鑰匙的門全部會對不上，\n" +
                 "而且不會有任何錯誤訊息，只會靜靜地「找不到道具」。\n\n" +
                 "資產檔名怎麼取都行，這個欄位才是真的")]
        public string id = "";

        [Header("顯示")]
        [Tooltip("玩家看到的名字。**這個可以隨時改**，不影響任何存檔或引用")]
        public string displayName = "";

        [Tooltip("**持有中**的樣子 —— 快捷欄（持有遺物 UI）用的就是這一張。\n\n" +
                 "遺物的美術是**白色**那一張（例：`魚頭遺物白色_方形裁切`）——\n" +
                 "「貪婪的大口」就是這樣掛的，其餘遺物照它。\n\n" +
                 "⚠️ 商店貨架上不是用這一張，是下面的 Shelf Icon（彩色）")]
        public Sprite icon;

        [Tooltip("**商店貨架上**的樣子。彩色那一張（例：`快艇鑰匙`、`釣竿`、`魚叉`）。\n\n" +
                 "留空 = 退回上面的 Icon —— 只有一張圖的道具不必兩欄都填。\n\n" +
                 "【為什麼分兩張】貨架是「商品陳列」，要看得出是什麼東西，所以是彩色；\n" +
                 "持有欄是「我身上有這個」，一整排彩色圖會跟畫面搶注意力，所以是白色剪影。\n" +
                 "這是美術給的兩套素材，不是同一張圖調色調出來的")]
        public Sprite shelfIcon;

        [TextArea(2, 4)]
        [Tooltip("**效果文字**。快捷欄 hover 的說明框只顯示這一欄。\n\n" +
                 "hover 的當下玩家要的是「這個吃下去會怎樣」，不是讀故事 ——\n" +
                 "所以故事文本請寫到下面的 Full Description。\n\n" +
                 "⚠️ 製作備註（美術待補、效果還沒接…）寫到 Notes，兩邊都不要寫。")]
        public string description = "";

        [TextArea(3, 10)]
        [Tooltip("**故事文本**。給日後的圖鑑／收藏冊用，目前沒有 UI 會顯示。\n\n" +
                 "附件《食物》《收藏品》「敘述」欄的那一段就是放這裡。\n" +
                 "要顯示完整內容時用 <see cref=\"FullText\"/>，**不要把效果再抄一份進來** ——\n" +
                 "抄兩份就會有兩個真相，改一邊忘了另一邊是遲早的事。")]
        public string fullDescription = "";

        [Header("商店")]
        [Tooltip("基礎售價。實際售價由商店決定（可能有折扣／加價），這裡是定價")]
        [Min(0)] public int price = 0;

        [Header("分類")]
        [Tooltip(
            "標籤。**這是「這個東西是什麼」，不是「它會在哪裡出現」** ——\n" +
            "出現在哪裡由 LootTable 決定（見 LootTable 的說明）。\n\n" +
            "建議的寫法：類別 + 題材，例如一條魚是 [Consumable, SeaFood]。\n" +
            "不建議把 Tier 寫進來 —— 同一件東西在第一層是大獎、在第四層是雜物，\n" +
            "階級屬於「哪張表抽的」而不屬於物品本身。")]
        public List<string> tags = new List<string>();

        [Header("戰鬥牌")]
        [Tooltip("這件商品買下去要加進戰鬥牌組的哪一張牌。\n\n" +
                 "武器在這個世界會變成卡牌，所以商店賣的「武器」實際上是在賣一張 CardData。\n" +
                 "留空 = 只是普通道具，只進背包。\n\n" +
                 "⚠️ 牌組是 `RunStateManager.savedDeck`（戰鬥端持有），\n" +
                 "加牌走 `PlayerVitals.AddCardToDeck()`，不要自己去碰那個清單")]
        public CardData grantsCard;

        [Header("使用效果（食物／補給）")]
        [Tooltip("使用時回復的 HP。0 = 不回。\n\n" +
                 "⚠️ 這一組是**戰鬥外也有效**的簡易效果，走 PlayerVitals。\n" +
                 "戰鬥內的複雜效果（給力量、加護甲…）是 Romtyui 的 ItemEffectData，\n" +
                 "那一套需要 BattleManager，只能在戰鬥裡跑")]
        [Min(0)] public int hpRestore = 0;

        [Tooltip("使用時回復的 SAN。0 = 不回")]
        [Min(0)] public int sanRestore = 0;

        [Tooltip("使用時**扣掉**的 HP。0 = 不扣。\n\n" +
                 "附件《食物》裡有幾樣東西是有代價的（奢侈的血塊「減少（中等）HP」）——\n" +
                 "沒有這一欄的話那種食物只能寫在敘述裡、實際上不會發生任何事。")]
        [Min(0)] public int hpCost = 0;

        [Tooltip("使用時**扣掉**的 SAN。0 = 不扣。\n\n" +
                 "奇怪的魚／蠕動的生蠔／仰望星空都是「回 HP 但減 SAN」——\n" +
                 "那是這個世界吃東西的代價，不是錯字。")]
        [Min(0)] public int sanCost = 0;

        [Tooltip("使用後是否消耗掉。取消勾選 = 可以重複使用（目前沒有這種道具）")]
        public bool consumeOnUse = true;

        [Header("戰鬥道具效果（戰鬥中主動使用）")]
        [Tooltip("這件道具在**戰鬥裡**的效果（Romtyui 的 `ItemEffectData`）。\n\n" +
                 "戰鬥開始時會由 `BattleStageController` 把身上的道具送進 `ItemInventory`，\n" +
                 "也就是戰鬥畫面左邊那個道具面板（`Props_Panel`）——\n" +
                 "跟遺物走 `RelicsInventory` 是完全對稱的一套。\n\n" +
                 "⛔ **目前全專案一個 `ItemEffectData` 資產都沒有**（那個類別是 abstract，\n" +
                 "還沒有人寫子類別），所以這一欄現在一定是空的，\n" +
                 "戰鬥裡的道具面板也因此永遠是空的。\n\n" +
                 "⚠️ 這跟「快捷欄能不能吃東西」是**兩件事** ——\n" +
                 "快捷欄走的是 `hpRestore` / `sanRestore` 那一組，戰鬥內外都有效，\n" +
                 "不需要這一欄。這一欄是給「只有戰鬥裡才有意義」的效果用的\n" +
                 "（給力量、加護甲、抽牌…那些需要 BattleManager 才跑得動）。")]
        public ItemEffectData battleItemEffect;

        [Header("遺物效果（戰鬥中被動觸發）")]
        [Tooltip("這件收藏品在戰鬥裡的被動效果。\n\n" +
                 "⚠️ 遺物**不是點來用的** —— Romtyui 的設計是在 BattleStart／\n" +
                 "回合開始／回合結束／出牌時自動觸發（見 RelicsTriggerType）。\n" +
                 "戰鬥開始時會由 BattleStageController 把身上的遺物送進 RelicsInventory。\n\n" +
                 "留空 = 這件收藏品目前只是收藏品，沒有效果")]
        public RelicsEffectData relicEffect;

        [Header("製作備註（玩家看不到）")]
        [TextArea(2, 8)]
        [Tooltip("給團隊看的：美術還沒畫、效果還沒接、附件與專案的出入、暫定值的理由…\n\n" +
                 "**沒有任何 UI 會讀這一欄**，寫多長都不影響玩家看到的畫面。\n" +
                 "跟 <see cref=\"description\"/> 分開，是因為那一欄會直接出現在快捷欄的說明框裡。")]
        public string notes = "";

        /// <summary>
        /// 這件道具點下去有沒有事會發生。
        /// **快捷欄用它決定要不要讓那一格可以點** —— 點了沒反應比不能點更糟。
        /// </summary>
        public bool IsUsable => hpRestore > 0 || sanRestore > 0 || hpCost > 0 || sanCost > 0;

        /// <summary>顯示名沒填就退回 id —— 至少畫面上不會是一片空白。</summary>
        public string Label => string.IsNullOrEmpty(displayName) ? id : displayName;

        /// <summary>
        /// 商店貨架上要顯示哪一張。**沒填彩色版就退回持有版**，
        /// 不會變成一個空框 —— 只有一張圖的道具（食物）本來就只填 icon。
        /// </summary>
        public Sprite ShelfIcon => shelfIcon != null ? shelfIcon : icon;

        /// <summary>
        /// 故事 ＋ 效果，接起來的完整說明。**圖鑑那類「要看全部」的畫面用這個。**
        ///
        /// 【為什麼是接起來而不是另存一欄】效果文字只該有一份。
        /// 圖鑑再存一份完整版的話，改了效果就得記得同步兩個地方 ——
        /// 而那種同步遲早會漏，漏了也不會有任何錯誤訊息。
        ///
        /// 兩欄都空就回空字串；只有一欄有東西就只回那一欄，不會留下多餘的空行。
        /// </summary>
        public string FullText
        {
            get
            {
                bool hasStory = !string.IsNullOrEmpty(fullDescription);
                bool hasEffect = !string.IsNullOrEmpty(description);

                if (hasStory && hasEffect) return fullDescription + "\n\n" + description;
                return hasStory ? fullDescription : (hasEffect ? description : "");
            }
        }

        /// <summary>標籤比對。大小寫不敏感 —— 手打標籤很容易大小寫不一致。</summary>
        public bool HasTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || tags == null) return false;

            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], tag, System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
