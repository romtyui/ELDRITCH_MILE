using UnityEngine;

namespace EldritchMile.Explore
{
    using EldritchMile.Core;

    /// <summary>
    /// 可調查物件：看一眼給世界觀文字，或直接撿走一樣東西。
    /// 取代封存的 InspectableObject。
    /// </summary>
    public class InspectableInteractable : InteractableBase
    {
        public enum Kind
        {
            /// 只顯示描述文字
            Information,

            /// 撿取，會進背包
            Pickup,
        }

        [Header("類型")]
        public Kind kind = Kind.Information;

        [Tooltip("Information：描述文字；Pickup：獲得的道具名稱")]
        [TextArea(2, 5)]
        public string contentText = "一疊舊報紙…";

        [Tooltip("Pickup 時實際加入背包的道具 id。留空則只是敘述、不影響狀態")]
        public string grantedItemId = "";

        public override void Interact()
        {
            if (kind == Kind.Information)
            {
                PopupService.Instance?.ShowText(contentText);
            }
            else
            {
                RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;
                if (run != null && !string.IsNullOrEmpty(grantedItemId))
                {
                    run.AddItem(grantedItemId);
                }

                PopupService.Instance?.ShowText($"獲得：{contentText}");
            }

            MarkDone();
        }
    }
}
