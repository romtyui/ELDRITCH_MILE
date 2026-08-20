using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 一場 run 的**起始戰鬥牌組**。大綱裡是坎貝爾幫你準備的那一套。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼要有這個資產，不直接用戰鬥場景上的那一份】
    ///
    /// 隊友的起始牌組填在 `SampleScene` 裡 `BattleDeck` 物件的 `startingDeck` 上。
    /// 那份不能當真相，有三個理由：
    ///
    ///   1. **它在別的場景裡。** 我方的流程住在 `EventScene`，run 開始的那一刻
    ///      `BattleDeck` 根本還沒被載入，讀不到。
    ///   2. **它會被覆蓋。** `RunStateManager.ApplyDeck()` 進戰鬥時會
    ///      `startingDeck.Clear()` 再倒入 `savedDeck` —— 那份 Inspector 資料
    ///      只是「沒有 run 狀態時的後備」。
    ///   3. **牌組屬於 run，不屬於戰鬥。** 「這場輪迴開局給你什麼牌」是
    ///      遺產／難度會動的東西，那是流程端的事。
    ///
    /// 所以我方持有一份，run 開始時倒進 `RunStateManager.savedDeck`。
    /// **沒有改到隊友的任何檔案** —— 跟 `PlayerVitals` 是同一個處理方式。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【⚠️ 這份不能是空的】
    ///
    /// `hasSavedRunState` 一旦是 true 而 `savedDeck` 是空的，
    /// 玩家會**帶著空牌組進戰鬥**，而且不會有任何錯誤訊息。
    /// `PlayerVitals.EnsureInitialized()` 會擋住這個情況，見那邊的說明。
    /// </summary>
    [CreateAssetMenu(fileName = "StartingDeck_", menuName = "Eldritch/Starting Deck")]
    public class StartingDeckData : ScriptableObject
    {
        [Tooltip("開局的牌。**同一張牌要放幾張就列幾次** —— 盾 ×4 就是四行。\n\n" +
                 "卡片資產在 Assets/Romtyui/Data/Card Vusil Data/ 底下（戰鬥組的）")]
        public List<CardData> cards = new List<CardData>();

        [TextArea(2, 4)]
        [Tooltip("給企劃看的。這副牌想教玩家什麼，不影響行為")]
        public string notes = "";

        /// <summary>實際可用的牌（濾掉 Inspector 零填充留下的空格）。</summary>
        public List<CardData> Resolve()
        {
            var result = new List<CardData>();

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null) result.Add(cards[i]);
            }

            if (result.Count == 0)
            {
                Debug.LogWarning(
                    $"[起始牌組] 「{name}」一張牌都沒有。這場 run 的 HP／SAN 會因此不初始化 ——" +
                    "空牌組進戰鬥是打不動的，所以寧可退回舊行為。", this);
            }

            return result;
        }
    }
}
