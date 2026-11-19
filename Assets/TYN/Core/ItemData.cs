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
    ///
    /// 【2026-11-06 拆開】這裡只剩共用的部分（id、名字、圖、說明、價格、標籤、武器牌）。
    /// 食物是 <see cref="FoodData"/>、遺物是 <see cref="RelicData"/>，都繼承這個型別；
    /// 鑰匙、補給、武器牌這類沒有專屬欄位的，直接用這個型別。
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

        //11/16 by romtyui
        //For display purposes icon 
        [Tooltip("背包圖標展示")]
        public Sprite displayIcon;

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

        [Header("製作備註（玩家看不到）")]
        [TextArea(2, 8)]
        [Tooltip("給團隊看的：美術還沒畫、效果還沒接、附件與專案的出入、暫定值的理由…\n\n" +
                 "**沒有任何 UI 會讀這一欄**，寫多長都不影響玩家看到的畫面。\n" +
                 "跟 <see cref=\"description\"/> 分開，是因為那一欄會直接出現在快捷欄的說明框裡。")]
        public string notes = "";

        /// <summary>
        /// 點下去有沒有事會發生。**快捷欄用它決定要不要讓那一格可以點** —— 點了沒反應比不能點更糟。
        /// 只有食物（<see cref="FoodData"/>）會是 true。
        /// </summary>
        public virtual bool IsUsable => false;

        /// <summary>
        /// **一件只能有一個，不能疊。** 遺物（<see cref="RelicData"/>）一律是。
        /// 還沒轉成 RelicData 的舊資產靠 `Curio` 標籤判斷（2026-10-13 定案的規則）。
        ///
        /// 會讀這個的地方：<see cref="RunContext.AddItem"/>（已經有就不給）、
        /// <see cref="LootService"/>（已經有的不抽、同一批不重複、數量壓成 1）、
        /// 戰鬥開場灌遺物（舊存檔裡如果有 ×2 也只灌一件）。
        /// </summary>
        public virtual bool IsUnique => HasTag("Curio");

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
