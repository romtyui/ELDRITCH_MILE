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

        [Tooltip("背包／商店用的圖示。目前還沒有 UI 會用到，先留著")]
        public Sprite icon;

        [TextArea(2, 4)]
        [Tooltip("道具說明。同樣是給日後的背包／商店用")]
        public string description = "";

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

        /// <summary>顯示名沒填就退回 id —— 至少畫面上不會是一片空白。</summary>
        public string Label => string.IsNullOrEmpty(displayName) ? id : displayName;

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
