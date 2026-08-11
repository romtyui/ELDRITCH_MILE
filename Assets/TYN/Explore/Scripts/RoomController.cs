using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Explore
{
    using EldritchMile.Core;

    /// <summary>
    /// 房間：依 SpawnSlot 隨機填入內容，並追蹤探索進度。
    ///
    /// 【與封存的 RoomController 的差別】
    ///   舊：GetComponentsInChildren 掃已擺好的物件，靜態、每次都一樣
    ///   新：SpawnSlot + RoomContentData 隨機填充（C6），且用 seed 保證可重現
    ///
    /// 【C13】房間清空後不是直接離開，而是問「要探索其他的東西嗎？」
    /// 因此本類別只負責「回報清空了」，是否離開由 ExploreStageController 決定。
    /// </summary>
    public class RoomController : MonoBehaviour
    {
        [Header("內容生成 (C6)")]
        [Tooltip("留空則不生成，只使用場景中已擺好的物件")]
        public RoomContentData contentData;

        [Tooltip("留空會自動抓子物件裡所有的 SpawnSlot")]
        public List<SpawnSlot> slots = new List<SpawnSlot>();

        [Header("敘事")]
        [TextArea(2, 4)] public string entryText = "";

        [Tooltip("所有互動物件都處理完後顯示。留空則不提示")]
        [TextArea(2, 4)] public string clearText = "";

        /// 房間內所有互動物件都處理完了
        public event System.Action OnRoomCleared;

        private readonly List<IInteractable> tracked = new List<IInteractable>();
        private int interactedCount;
        private bool isCleared;

        public int TotalInteractables => tracked.Count;
        public int InteractedCount => interactedCount;
        public bool IsCleared => isCleared;

        /// <summary>
        /// 生成內容。由 ExploreStageController 在房間 Instantiate 之後呼叫。
        /// seed 來自 RunContext，保證同一場 run 重進同一節點看到的擺設一致。
        /// </summary>
        public void Populate(int seed)
        {
            if (slots.Count == 0)
            {
                GetComponentsInChildren(true, slots);
            }

            if (contentData == null || slots.Count == 0)
            {
                Debug.Log($"[房間] {name} 沒有內容表或沒有 SpawnSlot，維持場景既有擺設");
                return;
            }

            var rng = new System.Random(seed);
            var usedCount = new Dictionary<RoomContentData.Entry, int>();

            // 打亂位子順序，避免每次都從同一個角落開始填
            var shuffled = new List<SpawnSlot>(slots);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            int filled = 0;
            int maxFill = contentData.maxFilled > 0 ? contentData.maxFilled : shuffled.Count;

            foreach (SpawnSlot slot in shuffled)
            {
                if (filled >= maxFill) break;
                if (slot == null || slot.IsOccupied) continue;

                // 未達最低數量時忽略 fillChance，確保房間不會空到沒東西可做
                bool mustFill = filled < contentData.minFilled;
                if (!mustFill && rng.NextDouble() > slot.fillChance) continue;

                RoomContentData.Entry entry = contentData.PickFor(slot, rng, usedCount);
                if (entry == null) continue;

                SpawnInto(slot, entry, rng);
                usedCount[entry] = usedCount.TryGetValue(entry, out int c) ? c + 1 : 1;
                filled++;
            }

            Debug.Log($"[房間] {name} 填入 {filled} / {slots.Count} 個位子");
        }

        private void SpawnInto(SpawnSlot slot, RoomContentData.Entry entry, System.Random rng)
        {
            GameObject obj = Instantiate(entry.prefab, slot.transform.position, slot.transform.rotation, slot.transform);

            // C6：以各種角度隨機出現
            float angle = Mathf.Lerp(slot.rotationRange.x, slot.rotationRange.y, (float)rng.NextDouble());
            obj.transform.Rotate(0f, 0f, angle);

            if (slot.scaleRange.x != 1f || slot.scaleRange.y != 1f)
            {
                float s = Mathf.Lerp(slot.scaleRange.x, slot.scaleRange.y, (float)rng.NextDouble());
                obj.transform.localScale *= s;
            }

            // C6：若物件備有多組轉向／樣式的圖，隨機挑一組。
            // 用同一個 rng，所以整個房間的擺設在同一個 seed 下完全可重現。
            foreach (InteractableBase it in obj.GetComponentsInChildren<InteractableBase>(true))
            {
                it.ApplyRandomVariant(rng);
            }

            slot.MarkOccupied();
        }

        // ==========================================
        // 進度追蹤
        // ==========================================
        /// 互動物件在 Start 時自行註冊
        public void Register(IInteractable interactable)
        {
            if (interactable == null || tracked.Contains(interactable)) return;
            tracked.Add(interactable);
        }

        public void ReportInteracted(IInteractable interactable)
        {
            if (isCleared) return;

            interactedCount++;

            if (interactedCount >= tracked.Count)
            {
                isCleared = true;

                if (!string.IsNullOrEmpty(clearText))
                {
                    PopupService.Instance?.QueueText($"【探索完成】\n{clearText}");
                }

                OnRoomCleared?.Invoke();
            }
        }
    }
}
