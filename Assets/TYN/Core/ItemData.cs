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

        /// <summary>顯示名沒填就退回 id —— 至少畫面上不會是一片空白。</summary>
        public string Label => string.IsNullOrEmpty(displayName) ? id : displayName;
    }
}
