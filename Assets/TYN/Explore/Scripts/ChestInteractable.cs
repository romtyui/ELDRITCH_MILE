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
    public class ChestInteractable : InteractableBase, IProbabilityTarget
    {
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

        [Tooltip("開啟後加入背包的道具 id（例如鑰匙）")]
        public List<string> grantedItemIds = new List<string>();

        [Header("屬性判定 (C17/C18，僅 RequiresCheck 時使用)")]
        public ExploreAttribute attribute = ExploreAttribute.None;

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

        [Header("判定結果文字 (C18③)")]
        [Tooltip("判定成功時即時顯示的一句話")]
        [TextArea(2, 3)]
        public string successText = "鎖「喀」地一聲鬆開了。";

        [Tooltip("判定失敗時即時顯示的一句話")]
        [TextArea(2, 3)]
        public string failText = "沒能撬開。鎖紋風不動。";

        public void OnCheckResult(bool success, float usedRate)
        {
            // C18③：即時替換對話框正文，不排隊、不需點擊 —— 玩家可以馬上出下一張
            PopupService.Instance?.ShowInstant(success ? successText : failText);

            if (success) Open();
        }

        public void ShowPreview(float rate, Effectiveness eff)
        {
            if (previewLabel == null) return;

            previewLabel.gameObject.SetActive(true);
            previewLabel.text = eff == Effectiveness.None
                ? "✕"
                : $"{Mathf.RoundToInt(rate * 100f)}";

            previewLabel.color = eff == Effectiveness.None
                ? new Color(0.5f, 0.5f, 0.5f)
                : Color.white;
        }

        public void HidePreview()
        {
            if (previewLabel != null) previewLabel.gameObject.SetActive(false);
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

        private void Open()
        {
            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;

            // 道具立刻入袋（狀態不能延後，否則中途離開會遺失）
            if (run != null)
            {
                foreach (string id in grantedItemIds) run.AddItem(id);
            }

            // 但「獲得了什麼」的**播報**要等打牌環節結束。
            // 打牌期間畫面焦點在大圖與手牌，中途跳出道具清單會打斷節奏，
            // 而且玩家可能還想繼續出牌（C18⑦）。
            bool duringEncounter = DialogueEncounterController.Instance != null
                                && DialogueEncounterController.Instance.IsActive;

            if (duringEncounter && stage != null)
            {
                stage.DeferLootReport(displayName, lootItems);
            }
            else
            {
                PopupService.Instance?.ShowLoot(displayName, lootItems);
            }

            MarkDone();
        }
    }
}
