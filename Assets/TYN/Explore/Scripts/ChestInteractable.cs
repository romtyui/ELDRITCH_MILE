using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Explore
{
    using EldritchMile.Core;

    /// <summary>
    /// 寶箱 / 容器。流程圖上有三種型態：
    ///   1. 開寶箱（不須賭博判定）── 直接開
    ///   2. 需要先獲得鑰匙才能開啟（C7）
    ///   3. 開鎖（需機率判定，C4/C18）
    ///
    /// 三者共用同一個元件，靠 openMode 切換。
    /// </summary>
    public class ChestInteractable : InteractableBase, IProbabilityTarget, IRetryableTarget
    {
        /// <summary>手牌用盡後，用什麼付「再來一輪」的代價。</summary>
        public enum RetryPolicy
        {
            /// 不可重複嘗試。手牌用盡即結案
            None = 0,

            /// 消耗特定道具（每次一個）
            RequiresItem = 1,

            /// 扣 HP。**尚未接上** —— 見下方說明
            CostsHealth = 2,

            /// 扣 SAN。**尚未接上** —— 見下方說明
            CostsSanity = 3,
        }

        public enum OpenMode
        {
            /// 直接開，不需判定（流程圖的「不須賭博判定」分支）
            Direct,

            /// 需要鑰匙。沒鑰匙就打不開（C7）
            RequiresKey,

            /// 需要機率判定開鎖（C4/C18）
            RequiresCheck,
        }

        [Header("開啟方式")]
        public OpenMode openMode = OpenMode.Direct;

        [Tooltip("RequiresKey 時需要的道具 id")]
        public string requiredKeyId = "";

        [Tooltip("沒有鑰匙時顯示的文字")]
        [TextArea(2, 3)]
        public string lockedText = "鎖住了。得先找到鑰匙。";

        [Tooltip("開鎖成功後是否消耗鑰匙")]
        public bool consumeKey = false;

        [Tooltip("RequiresCheck 時，開始打牌環節前顯示的提示")]
        [TextArea(2, 3)]
        public string checkPromptText = "上了鎖。也許能用點手段撬開。";

        [Header("內容物")]
        public List<string> lootItems = new List<string>();

        [Tooltip("開啟後加入背包的道具 id（例如鑰匙）。**這是「一定會給」的部分**")]
        public List<string> grantedItemIds = new List<string>();

        [Tooltip("隨機戰利品表。與商店共用同一套抽取邏輯。\n\n" +
                 "**難度與區域的差別做在這裡，不是做在寶箱上** ——\n" +
                 "漁村的普通寶箱指 Loot_Village_Chest_T1、精英的指 T2，\n" +
                 "寶箱這支程式一個字都不用改。留空則只給上面那排固定道具")]
        public LootTable lootTable;

        [Header("屬性判定 (C17/C18，僅 RequiresCheck 時使用)")]
        public ExploreAttribute attribute = ExploreAttribute.None;

        [Header("重試 (手牌用盡後付全域代價再來一輪)")]
        [Tooltip("None ＝ 不可重複嘗試，手牌用盡即結案。\n" +
                 "RequiresItem ＝ 消耗道具，**可以用**。\n" +
                 "CostsHealth / CostsSanity ＝ 扣 HP／SAN，**尚未接上** —— " +
                 "HP／SAN 歸 Romtyui 的 RunStateManager 管，而它的值只有在第一場戰鬥打完才存在。" +
                 "要等「run 開始就初始化」談定後才能接。選了會在 Console 說明並直接結案")]
        public RetryPolicy retryPolicy = RetryPolicy.None;

        [Tooltip("大類型，決定代價的固有倍率。測試用的普通寶箱是 Tier1（×1）")]
        public ObjectTier tier = ObjectTier.Tier1;

        [Tooltip("代價計算規則。留空則不可重試（會在 Console 提醒）")]
        public RetryCostData retryCost;

        [Tooltip("RequiresItem 時需要的道具 id。每次重試消耗一個")]
        public string retryItemId = "";

        [Tooltip("詢問文字。\n" +
                 "{0} = 道具名或代價數值、{1} = 這是第幾次重試、{2} = 身上還剩幾個（數值型資源時是空字串）")]
        [TextArea(2, 3)]
        public string retryPromptFormat = "還能再撬一次，但要用掉一個「{0}」{2}。要試嗎？（第 {1} 次重試）";

        [Tooltip("付不起時顯示的一句話")]
        [TextArea(2, 3)]
        public string cannotAffordText = "手上沒有能用的工具了。";

        /// 這個物件已經重試過幾次。決定遞增代價，且**不會**因為重試而歸零。
        private int retryCount;

        [Header("預覽 UI (C17)")]
        [Tooltip("hover 手牌時顯示成功率的文字。可留空")]
        public TMPro.TextMeshPro previewLabel;

        // ── IProbabilityTarget ──
        ExploreAttribute IProbabilityTarget.Attribute => attribute;

        public float CurrentDecayMultiplier { get; private set; } = 1f;

        public void ApplyDecay(float step)
        {
            // C18：衰減掛在目標上，不是掛在卡片上 —— 這個箱子越試越難，
            // 換去試別的目標則各自獨立計算。
            CurrentDecayMultiplier = Mathf.Max(0f, CurrentDecayMultiplier - step);
        }

        /// <summary>
        /// 回到 1.0。開新環節時由 `DialogueEncounterController.Begin()` 呼叫。
        ///
        /// ⚠️ **只重置衰減，不碰 `retryCount`** —— 那兩個是不同的東西：
        /// 衰減是「這一手牌之內越試越難」，重試次數是「這個箱子被砸過幾次」，
        /// 後者本來就設計成不歸零（遞增代價靠它）。
        /// </summary>
        public void ResetDecay()
        {
            CurrentDecayMultiplier = 1f;
        }

        [Header("判定結果文字 (C18③)")]
        [Tooltip("判定成功時即時顯示的一句話")]
        [TextArea(2, 3)]
        public string successText = "鎖「喀」地一聲鬆開了。";

        [Tooltip("判定失敗時即時顯示的一句話")]
        [TextArea(2, 3)]
        public string failText = "沒能撬開。鎖紋風不動。";

        [Tooltip("手牌用盡仍未成功、這個箱子徹底結案時顯示的一句話。\n" +
                 "與 Fail Text 的差別：那句是「這次沒中，還可以再試」，這句是「結束了」")]
        [TextArea(2, 3)]
        public string exhaustedText = "鎖芯已經被撬爛了。這箱子開不了了。";

        [Tooltip("結案之後又被點擊時顯示的一句話。留空則沉默")]
        [TextArea(2, 3)]
        public string alreadyFailedText = "已經沒救了，別再費工夫。";

        public void OnCheckResult(bool success, float usedRate)
        {
            string body = success ? successText : failText;

            // C12：回合感寫進結果文字（「這是你嘗試的第 3 次。」），第二次起才出現。
            // 成功也附加 —— 這是回合計數不是失敗計數，而且成功後環節不會自動結束（C18⑦），
            // 玩家可能還會繼續打。
            DialogueEncounterController encounter = DialogueEncounterController.Instance;
            if (encounter != null) body = encounter.WithAttemptLine(body);

            // C18③：即時替換對話框正文，不排隊、不需點擊 —— 玩家可以馬上出下一張
            PopupService.Instance?.ShowInstant(body);

            if (success) Open();
        }

        public void ShowPreview(float rate, Effectiveness eff)
        {
            if (previewLabel == null) return;

            previewLabel.gameObject.SetActive(true);
            previewLabel.text = eff == Effectiveness.None
                ? "X"
                : $"{Mathf.RoundToInt(rate * 100f)}";

            previewLabel.color = eff == Effectiveness.None
                ? new Color(0.5f, 0.5f, 0.5f)
                : Color.white;
        }

        public void HidePreview()
        {
            if (previewLabel != null) previewLabel.gameObject.SetActive(false);
        }

        [Header("被瞄準時")]
        [Tooltip("卡片瞄準這個物件時，世界立繪乘上的顏色。預設稍微變暗")]
        public Color targetedTint = new Color(0.72f, 0.72f, 0.72f, 1f);

        private bool baseTintCached;
        private Color baseTint = Color.white;

        /// <summary>
        /// 【多數情況用不到】打牌環節開始後，判定目標會換成對話框裡的
        /// `EncounterTargetView`，世界裡的箱子被壓黑層擋著、也不再是投放對象。
        /// 只有化身生成失敗、目標退回世界物件本身時才會走到這裡。
        /// </summary>
        public void SetTargeted(bool targeted)
        {
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
            if (targetRenderer == null) return;

            if (!baseTintCached)
            {
                baseTintCached = true;
                baseTint = targetRenderer.color;
            }

            targetRenderer.color = targeted
                ? new Color(baseTint.r * targetedTint.r, baseTint.g * targetedTint.g,
                            baseTint.b * targetedTint.b, baseTint.a)
                : baseTint;
        }

        /// <summary>
        /// 手牌用盡仍未撬開 → 這個箱子結案。
        ///
        /// 【為什麼一定要結案】不結案的話：衰減已歸零（保證 0%），但箱子還可以點、
        /// 還會重抽一手牌 —— 玩家會陷入一個永遠跑不完、而且必定失敗的迴圈。
        /// 而且它永遠不回報房間，C13 的「要探索其他的東西嗎？」就永遠不會跳。
        ///
        /// 【重試機制的插入點】日後「付出代價重來」（消耗道具／HP／SAN）要接在這裡：
        /// 先問玩家付不付得起、願不願意，願意就重置衰減並重抽手牌，
        /// 不願意或付不起才走到 MarkFailed()。
        /// </summary>
        public void OnAttemptsExhausted()
        {
            if (hasInteracted) return;   // 最後一張剛好成功，已經 Open() 過了

            if (!string.IsNullOrEmpty(exhaustedText))
            {
                // ⚠️ 用 ShowText（排隊）而不是 ShowInstant（即時替換）。
                //    這一刻對話框上正顯示著最後一張牌的「沒能撬開」，
                //    ShowInstant 會 pending.Clear() 並直接蓋掉它 —— 玩家連自己為什麼
                //    失敗都沒看到就變成「鎖芯已經被撬爛了」。排隊才讀得完兩句。
                PopupService.Instance?.ShowText(exhaustedText);
            }

            MarkFailed();
        }

        // ==========================================
        // IRetryableTarget：付出全域代價再來一輪
        // ==========================================
        public bool CanOfferRetry
        {
            get
            {
                if (hasInteracted) return false;          // 已經開過或已結案
                if (retryPolicy == RetryPolicy.None) return false;

                if (retryCost == null)
                {
                    Debug.LogWarning(
                        $"[打牌] 「{displayName}」設定了重試（{retryPolicy}）但沒有指定 Retry Cost 資產，" +
                        "無法算出代價，這次不提供重試。", this);
                    return false;
                }

                // HP／SAN 還沒接上。這裡明講原因，不要讓它靜靜地當成「不可重試」
                if ((retryPolicy == RetryPolicy.CostsHealth || retryPolicy == RetryPolicy.CostsSanity)
                    && !PlayerVitals.IsReady)
                {
                    Debug.LogWarning(
                        $"[打牌] 「{displayName}」的重試代價是 {retryPolicy}，但 HP／SAN 還沒初始化。\n" +
                        "請在 GameFlowManager 上把 Starting Max Hp 設成大於 0 的值 ——\n" +
                        "沒設的話它們只有在第一場戰鬥打完之後才有值（見 PlayerVitals）。本次直接結案。", this);
                    return false;
                }

                return true;
            }
        }

        /// 這一次重試要付多少（retryCount 從 0 起算，所以第一次重試是 index 0）
        public int NextRetryCost =>
            retryCost != null ? retryCost.Calculate(tier, retryCount) : 0;

        public string BuildRetryPrompt()
        {
            // 道具是「有或沒有」，講數量沒有意義，所以顯示道具名；
            // 數值型資源才顯示金額。兩者共用同一個 format，{0} 各自代入該講的東西。
            //
            // ⚠️ 一定要走 ItemName() 翻譯。直接用 retryItemId 的話玩家會看到
            //    「要用掉一個 lockpick」—— id 是給程式認的，不是給玩家看的。
            string costLabel = retryPolicy == RetryPolicy.RequiresItem
                ? GameFlowManager.ItemName(retryItemId)
                : NextRetryCost.ToString();

            // 玩家要決定划不划算，「還剩幾個」跟「要付多少」一樣重要 ——
            // 最後一根撬棍該不該用掉，跟手上還有五根時是完全不同的決定
            string remainLabel = "";

            if (retryPolicy == RetryPolicy.RequiresItem)
            {
                RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;
                if (run != null) remainLabel = $"（還剩 {run.CountOf(retryItemId)} 個）";
            }
            else if (retryPolicy == RetryPolicy.CostsHealth)
            {
                remainLabel = $"（HP {PlayerVitals.Hp}/{PlayerVitals.MaxHp}）";
            }
            else if (retryPolicy == RetryPolicy.CostsSanity)
            {
                remainLabel = $"（SAN {PlayerVitals.San}/{PlayerVitals.MaxSan}）";
            }

            return string.Format(retryPromptFormat, costLabel, retryCount + 1, remainLabel);
        }

        public bool TryPayForRetry()
        {
            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;

            if (retryPolicy == RetryPolicy.RequiresItem)
            {
                if (run == null || !run.ConsumeItem(retryItemId))
                {
                    if (!string.IsNullOrEmpty(cannotAffordText))
                    {
                        PopupService.Instance?.ShowInstant(cannotAffordText);
                    }
                    Debug.Log($"[打牌] 「{displayName}」重試付不起：缺少道具 {retryItemId}");
                    return false;
                }

                // 道具是「有或沒有」，用不到數值代價。但還是把算出來的金額印出來，
                // 這樣光用道具測就能看到 tier × 遞增的曲線對不對，不必等 HP／SAN 接上
                int wouldCost = NextRetryCost;
                retryCount++;

                Debug.Log(
                    $"[打牌] 「{displayName}」第 {retryCount} 次重試：消耗道具 {retryItemId}" +
                    $"（{tier} 若改用數值資源，本次代價為 {wouldCost}）"
                );
                return true;
            }

            // ── 數值代價：HP / SAN ──
            //
            // ⚠️ 這兩支**不會把玩家扣死**（見 PlayerVitals.SpendHp）。
            //    「賭上性命」那種設計要另外做，不要放寬那一支的判斷 ——
            //    探索途中被一個寶箱扣死，玩家只會覺得是 bug。
            if (retryPolicy == RetryPolicy.CostsHealth || retryPolicy == RetryPolicy.CostsSanity)
            {
                int cost = NextRetryCost;

                bool paid = retryPolicy == RetryPolicy.CostsHealth
                    ? PlayerVitals.SpendHp(cost)
                    : PlayerVitals.SpendSan(cost);

                if (!paid)
                {
                    if (!string.IsNullOrEmpty(cannotAffordText))
                    {
                        PopupService.Instance?.ShowInstant(cannotAffordText);
                    }

                    Debug.Log(
                        $"[打牌] 「{displayName}」重試付不起：需要 {cost}，" +
                        (retryPolicy == RetryPolicy.CostsHealth
                            ? $"目前 HP {PlayerVitals.Hp}"
                            : $"目前 SAN {PlayerVitals.San}"));
                    return false;
                }

                retryCount++;

                Debug.Log(
                    $"[打牌] 「{displayName}」第 {retryCount} 次重試：" +
                    $"{(retryPolicy == RetryPolicy.CostsHealth ? "扣 HP" : "扣 SAN")} {cost}（{tier}）");
                return true;
            }

            return false;
        }

        public void ResetForRetry()
        {
            // 衰減回到初始。若不重置，重來也是保證 0% —— 那是假選擇。
            CurrentDecayMultiplier = 1f;
            Debug.Log($"[打牌] 「{displayName}」衰減已重置回 1.00");
        }

        protected override void OnInteractBlocked()
        {
            // 徹底失敗的箱子還立在那裡，點下去毫無反應的話，
            // 玩家分不出「這結束了」與「遊戲卡住了」
            if (FailedPermanently && !string.IsNullOrEmpty(alreadyFailedText))
            {
                PopupService.Instance?.ShowText(alreadyFailedText);
            }
        }

        // ── 互動 ──
        public override void Interact()
        {
            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;

            switch (openMode)
            {
                case OpenMode.Direct:
                    Open();
                    break;

                case OpenMode.RequiresKey:
                    if (run != null && run.HasItem(requiredKeyId))
                    {
                        if (consumeKey) run.ConsumeItem(requiredKeyId);
                        Open();
                    }
                    else
                    {
                        // 沒鑰匙不算互動完成 —— 拿到鑰匙後還能再來
                        PopupService.Instance?.ShowText(lockedText);
                    }
                    break;

                case OpenMode.RequiresCheck:
                    // C18：點擊只是「開始打牌環節」，不會自動擲骰。
                    // 實際判定要玩家把機率卡拖到這個目標上，而且可以連續嘗試。
                    if (stage != null)
                    {
                        stage.BeginEncounter(this, checkPromptText, closeUpSprite);
                    }
                    else
                    {
                        PopupService.Instance?.ShowText(checkPromptText);
                    }
                    break;
            }
        }

        /// <summary>
        /// 抽戰利品表，直接入袋，回傳給玩家看的清單（「撬棍 ×2」這種）。
        ///
        /// 【種子】綁 run 的種子加上這個寶箱的物件 id —— 同一場 run 裡兩個寶箱不會開出一模一樣的東西。
        /// 不做跨存檔的重現，因為寶箱開過就結案（`MarkDone`），不會有第二次。
        /// </summary>
        private List<string> RollLoot(RunContext run)
        {
            var lines = new List<string>();
            if (lootTable == null) return lines;

            var rng = new System.Random(run.runSeed ^ GetInstanceID());

            foreach (ItemStack stack in LootService.Roll(lootTable, rng))
            {
                if (stack == null || string.IsNullOrEmpty(stack.id)) continue;

                run.AddItem(stack.id, stack.count);

                string name = GameFlowManager.ItemName(stack.id);
                lines.Add(stack.count > 1 ? $"{name} ×{stack.count}" : name);
            }

            return lines;
        }

        private void Open()
        {
            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;

            // 道具立刻入袋（狀態不能延後，否則中途離開會遺失）
            var reported = new List<string>(lootItems);

            if (run != null)
            {
                foreach (string id in grantedItemIds) run.AddItem(id);

                foreach (string line in RollLoot(run)) reported.Add(line);
            }

            // 但「獲得了什麼」的**播報**要等打牌環節結束。
            // 打牌期間畫面焦點在大圖與手牌，中途跳出道具清單會打斷節奏，
            // 而且玩家可能還想繼續出牌（C18⑦）。
            bool duringEncounter = DialogueEncounterController.Instance != null
                                && DialogueEncounterController.Instance.IsActive;

            if (duringEncounter && stage != null)
            {
                stage.DeferLootReport(displayName, reported);
            }
            else
            {
                PopupService.Instance?.ShowLoot(displayName, reported);
            }

            MarkDone();
        }
    }
}
