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

        public void OnCheckResult(bool success, float usedRate)
        {
            if (success)
            {
                Open();
            }
            else
            {
                PopupService.Instance?.ShowText($"「{displayName}」沒能打開…");
            }
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
                    // 判定由打牌環節驅動（玩家把機率卡拖到這個目標上），
                    // 直接點擊只提示，不自動擲骰。
                    PopupService.Instance?.ShowText("上了鎖。也許能用點手段撬開。");
                    break;
            }
        }

        private void Open()
        {
            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;

            if (run != null)
            {
                foreach (string id in grantedItemIds) run.AddItem(id);
            }

            PopupService.Instance?.ShowLoot(displayName, lootItems);
            MarkDone();
        }
    }
}
