using System;

namespace EldritchMile.Core
{
    /// <summary>
    /// 背包裡的一疊道具。
    ///
    /// 【為什麼不是 `List&lt;string&gt;` 的重複清單】
    /// 重複清單（三根撬棍＝三筆 "lockpick"）其實可以運作，業界也認可那種
    /// 「在顯示層才堆疊」的做法。但它有一個表達不了的東西：**每一份各自不同的狀態**。
    ///
    /// 這個遊戲是肉鴿，而劇情設定裡漁夫的能力是「**隨機**獲取漁獲」——
    /// 如果每條魚的 +HP／−SAN 數值不同，那漁獲就不是可互換的東西，
    /// 重複清單與單純的 count 都裝不下它。
    ///
    /// 所以選這個結構：**它能表達「三根一樣的撬棍」，反過來不行。**
    /// 現在換的成本是零（`RunContext` 目前沒有任何持久化），等商店與漁獲都接上去之後
    /// 呼叫點會多很多。
    ///
    /// 【per-instance 狀態日後加在這裡】
    /// 例如 `public int hpGain; public int sanCost;`。加了之後
    /// **`RunContext.AddItem` 的合併規則要跟著改** —— 目前它是「同 id 就併」，
    /// 有狀態之後必須是「同 id **且狀態相同**才併」，否則兩條不同的魚會被併成一疊。
    /// </summary>
    [Serializable]
    public class ItemStack
    {
        /// <summary>道具 id。對應 <see cref="ItemData.id"/>，是背包與存檔認得的唯一依據。</summary>
        public string id;

        /// <summary>這一疊有幾個。恆 &gt;= 1 —— 歸零的疊會被 RunContext 直接移除。</summary>
        public int count = 1;

        public ItemStack() { }

        public ItemStack(string id, int count = 1)
        {
            this.id = id;
            this.count = count;
        }
    }
}
