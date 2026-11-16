using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 食物。**點來吃的**：回 HP／SAN（也可能有代價），戰鬥裡另外可以有 Romtyui 的道具效果。
    ///
    /// 【為什麼從 ItemData 拆出來】（2026-11-06，Romtyui 要求）
    /// 以前遺物與食物是同一個型別，靠 `Food`／`Curio` 標籤分辨，Inspector 上兩邊的欄位全部攤在一起，
    /// 填錯一邊也不會有任何提示。拆成兩個型別之後，每一件只看得到自己該填的欄位，
    /// 程式也能直接用型別判斷（`item is FoodData`），不必再比對標籤字串。
    ///
    /// 共用的東西（id、名字、圖、說明、價格、標籤）還在 <see cref="ItemData"/>，
    /// 所以資料庫、背包、寶箱、商店這些「什麼道具都收」的地方不用改。
    ///
    /// ⚠️ `Food` 標籤先留著：戰利品表與 Romtyui 大廳的倉庫還在用標籤篩選。
    /// </summary>
    [CreateAssetMenu(fileName = "Item_food_", menuName = "Eldritch/Food")]
    public class FoodData : ItemData
    {
        [Header("使用效果")]
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

        [Tooltip("使用後是否消耗掉。取消勾選 = 可以重複使用（目前沒有這種食物）")]
        public bool consumeOnUse = true;

        [Header("戰鬥道具效果（戰鬥中主動使用）")]
        [Tooltip("這件食物在**戰鬥裡**的效果（Romtyui 的 `ItemEffectData`）。\n\n" +
                 "戰鬥開始時由 `BattleStageController` 把身上的食物送進 `ItemInventory`，\n" +
                 "也就是戰鬥畫面左邊那個道具面板（`Props_Panel`）。\n\n" +
                 "⚠️ 這跟「快捷欄能不能吃」是**兩件事** —— 快捷欄走的是上面那組回復值，\n" +
                 "戰鬥內外都有效。這一欄是給「只有戰鬥裡才有意義」的效果用的\n" +
                 "（給力量、加護甲、抽牌…那些需要 BattleManager 才跑得動）。")]
        public ItemEffectData battleItemEffect;

        /// <summary>
        /// 點下去有沒有事會發生。**快捷欄用它決定要不要讓那一格可以點** —— 點了沒反應比不能點更糟。
        /// </summary>
        public override bool IsUsable => hpRestore > 0 || sanRestore > 0 || hpCost > 0 || sanCost > 0;
    }
}
