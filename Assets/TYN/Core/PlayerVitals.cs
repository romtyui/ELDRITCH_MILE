using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 探索端存取 HP／SAN 的唯一入口。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【它為什麼是個轉接頭，而不是一份自己的資料】
    ///
    /// HP／SAN 真正存在 `RunStateManager`（`Assets/Romtyui/`，隊友的）。
    /// 戰鬥會讀它、寫它；探索也要讀它、寫它。
    ///
    /// **我方不另存一份。** 兩邊各存一份的話，只要有一條路徑忘了同步，
    /// 玩家就會看到「探索扣了血，進戰鬥又滿血」——而且這種 bug 不會報錯，
    /// 只會讓數值慢慢對不起來，最後沒人知道哪一份才是真的。
    ///
    /// 所以這裡**只呼叫，不持有**，也完全沒有改到隊友的任何檔案。
    /// 搬過來的是「誰負責在 run 開始時初始化」這個責任，不是資料本身。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【⚠️ SAN 在隊友的程式裡叫 Energy】
    ///
    /// `RunStateManager` 沒有任何 sanity 欄位。它的 `savedCurrentEnergy` /
    /// `savedMaxEnergy` **就是 SAN** —— 證據是他自己的 log 印的是
    /// `SAN {savedCurrentEnergy}/{savedMaxEnergy}`，而且 `BattleManager` 裡
    /// 有一行註解寫「因為你要求 SAN 值不重製」（一般的 energy 每回合會重置，SAN 不會）。
    ///
    /// 這個對應**只寫在這裡一個地方**。其他程式一律用 `San`，
    /// 不要自己去碰 `savedCurrentEnergy`，否則哪天命名統一了要改十處。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【沒有 RunStateManager 的時候】
    ///
    /// 全部的存取都會安靜地失敗（讀到 0、扣款回傳 false），不會丟例外。
    /// 主選單、單獨測探索場景時場上本來就沒有它，那不是錯誤。
    /// </summary>
    public static class PlayerVitals
    {
        private static RunStateManager Rs => RunStateManager.Instance;

        /// <summary>場上有沒有 RunStateManager。沒有的話所有數值都是 0。</summary>
        public static bool Exists => Rs != null;

        /// <summary>
        /// 數值有沒有被初始化過。
        ///
        /// 【為什麼要有這個】`hasSavedRunState` 只有在 `SaveFromBattle()` 之後才是 true。
        /// 玩家若在任何戰鬥之前就先進探索房間，HP 讀出來是 **0** ——
        /// 那時候扣血會直接把人扣死。凡是要動 HP／SAN 的地方都要先問這個。
        /// </summary>
        public static bool IsReady => Rs != null && Rs.hasSavedRunState && Rs.savedPlayerMaxHp > 0;

        // ==========================================
        // 讀
        // ==========================================
        public static int Hp => Rs != null ? Rs.savedPlayerCurrentHp : 0;
        public static int MaxHp => Rs != null ? Rs.savedPlayerMaxHp : 0;

        /// <summary>SAN。對應到隊友那邊的 Energy，見類別說明。</summary>
        public static int San => Rs != null ? Rs.savedCurrentEnergy : 0;
        public static int MaxSan => Rs != null ? Rs.savedMaxEnergy : 0;

        // ==========================================
        // run 開始時的初始化
        // ==========================================
        /// <summary>
        /// run 開始時把 HP／SAN 設好。**已經有值就不覆蓋**（讀檔續玩不該被重置）。
        ///
        /// 【這一支會讓 `hasSavedRunState` 變成 true，那是有副作用的】
        /// 那個旗標是隊友的 `ApplyToBattle()` 的開關：true 的時候，戰鬥開始會套用
        /// 這裡設定的數值，而不是戰鬥自己的預設值。**這正是我們要的** ——
        /// 「run 開始就初始化」的意思就是後面所有戰鬥都接續同一條血條。
        ///
        /// 但也因此，傳進來的值不能是垃圾：`maxHp <= 0` 會讓玩家一進戰鬥就死。
        /// 所以下面會擋掉並警告，寧可維持「還沒初始化」也不要寫壞。
        /// </summary>
        public static void EnsureInitialized(int maxHp, int maxSan)
        {
            if (Rs == null)
            {
                Debug.Log("[生命值] 場上沒有 RunStateManager，跳過初始化（主選單或單獨測場景時是正常的）");
                return;
            }

            if (IsReady)
            {
                Debug.Log($"[生命值] 已經有數值了，不覆蓋：HP {Hp}/{MaxHp}、SAN {San}/{MaxSan}");
                return;
            }

            if (maxHp <= 0)
            {
                Debug.LogWarning(
                    "[生命值] 起始 Max HP 是 0，不進行初始化。\n" +
                    "若寫下去，玩家一進戰鬥就是 0 血 —— 這種錯誤在戰鬥開場才會爆，很難查回來。\n" +
                    "請在 GameFlowManager 上把 Starting Max Hp 設成大於 0 的值。");
                return;
            }

            Rs.savedPlayerMaxHp = maxHp;
            Rs.savedPlayerCurrentHp = maxHp;

            if (maxSan > 0)
            {
                Rs.savedMaxEnergy = maxSan;
                Rs.savedCurrentEnergy = maxSan;
            }

            // ⚠️ 這一行會讓隊友的 ApplyToBattle 開始生效，見上方說明
            Rs.hasSavedRunState = true;

            Debug.Log($"[生命值] run 開始，初始化為 HP {maxHp}/{maxHp}、SAN {San}/{MaxSan}");
        }

        // ==========================================
        // 扣 / 補
        // ==========================================
        /// <summary>
        /// 扣血。**付不起就完全不動**，跟 <see cref="RunContext.SpendMoney"/> 同一個規矩。
        ///
        /// 「付不起」的定義是**會扣到 0 或以下** —— 探索的代價不該直接把玩家扣死。
        /// 真的要做「賭上性命」那種設計時再另開一支，不要放寬這一支。
        /// </summary>
        public static bool SpendHp(int amount)
        {
            if (amount <= 0) return true;

            if (!IsReady)
            {
                Debug.LogWarning("[生命值] HP 還沒初始化，扣不了血。見 PlayerVitals.EnsureInitialized 的說明");
                return false;
            }

            if (Rs.savedPlayerCurrentHp - amount <= 0) return false;

            Rs.savedPlayerCurrentHp -= amount;
            Debug.Log($"[生命值] 扣 HP {amount}（剩 {Hp}/{MaxHp}）");
            return true;
        }

        /// <summary>扣 SAN。規矩同上：扣到 0 以下就算付不起。</summary>
        public static bool SpendSan(int amount)
        {
            if (amount <= 0) return true;

            if (!IsReady)
            {
                Debug.LogWarning("[生命值] SAN 還沒初始化，扣不了。見 PlayerVitals.EnsureInitialized 的說明");
                return false;
            }

            if (Rs.savedCurrentEnergy - amount <= 0) return false;

            Rs.savedCurrentEnergy -= amount;
            Debug.Log($"[生命值] 扣 SAN {amount}（剩 {San}/{MaxSan}）");
            return true;
        }

        /// <summary>補血。不會超過上限。漁獲那類道具用這支。</summary>
        public static void HealHp(int amount)
        {
            if (amount <= 0 || !IsReady) return;

            Rs.savedPlayerCurrentHp = Mathf.Min(Rs.savedPlayerCurrentHp + amount, Rs.savedPlayerMaxHp);
            Debug.Log($"[生命值] 回 HP {amount}（現在 {Hp}/{MaxHp}）");
        }

        /// <summary>補 SAN。不會超過上限。</summary>
        public static void RestoreSan(int amount)
        {
            if (amount <= 0 || !IsReady) return;

            Rs.savedCurrentEnergy = Mathf.Min(Rs.savedCurrentEnergy + amount, Rs.savedMaxEnergy);
            Debug.Log($"[生命值] 回 SAN {amount}（現在 {San}/{MaxSan}）");
        }
    }
}
