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
        private readonly HashSet<IInteractable> reported = new HashSet<IInteractable>();
        private int interactedCount;
        private bool isCleared;

        public int TotalInteractables => tracked.Count;
        public int InteractedCount => interactedCount;
        public bool IsCleared => isCleared;

        /// <summary>
        /// 生成內容。由 ExploreStageController 在房間 Instantiate 之後呼叫。
        /// seed 來自 RunContext，保證同一場 run 重進同一節點看到的擺設一致。
        /// </summary>
        [Tooltip("生成物**實際畫出來**大概多大（世界單位）。判定「會不會蓋到畫上去的家具」用的。\n" +
                 "⚠️ 不能用 sprite 的 bounds —— 這批圖是 2048×1448 的畫布、\n" +
                 "真正的寶箱只佔三成，用 bounds 算的話每一個都會「蓋住」每一個。\n" +
                 "量出來：正面寶箱 3.64 × 2.22，碰撞框 3.74 × 2.19，兩者相符。")]
        public Vector2 contentFootprint = new Vector2(3.7f, 2.2f);

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

            // ── 第零段：對號入座的位子先填，而且**不算進 maxFill** ──
            //
            // 【為什麼要有這一段】有 contentTag 的位子是「這一格就是要放那個東西」，
            // 典型是**畫在背景上的家具**（`artIsPainted`）——
            // 那個櫃子本來就畫在牆上，玩家看得到它，卻不一定碰得到，
            // 因為隨機那兩段可能先把 maxFill 用完就輪不到它了。
            // 實測：小屋 5 個 seed 只有 1 次填得到。
            //
            // 【為什麼不算配額】它在畫面上**本來就存在**，不是額外多出來的一件東西。
            // 算進去的話等於「這間房因為牆上有櫃子，就少一個寶箱」——那不合理。
            //
            // 【為什麼忽略 fillChance】同上：畫上去的家具沒有「這次不出現」這種選項。
            foreach (SpawnSlot slot in shuffled)
            {
                if (slot == null || slot.IsOccupied) continue;
                if (string.IsNullOrEmpty(slot.contentTag)) continue;

                RoomContentData.Entry entry = contentData.PickFor(slot, rng, usedCount, null);
                if (entry == null)
                {
                    Debug.LogWarning(
                        $"[房間] {name} 的位子「{slot.name}」指定了 tag「{slot.contentTag}」，" +
                        "但內容表裡沒有同 tag 的條目 —— 這一格會是空的。\n" +
                        "畫在背景上的家具空著的話，玩家會看到一個碰不到的櫃子。", this);
                    continue;
                }

                SpawnInto(slot, entry, rng);
                usedCount[entry] = usedCount.TryGetValue(entry, out int c1) ? c1 + 1 : 1;
            }

            // ── 第零段之後：把被「畫上去的家具」佔住的位子擋掉 ──
            //
            // 【為什麼需要】美術把櫃子畫在牆上，但別的位子就在它前面 ——
            // 隨機的寶箱會直接蓋上去，畫面變成「櫃子前面浮著一個寶箱」，
            // 那正是交接文件 F 節在講的那個矛盾畫面。
            //
            // 【為什麼是擋掉而不是縮小寶箱】寶箱實際畫出來是 3.64 × 2.22，
            // 相機看得到 17.78 × 10 —— 只佔畫面兩成，**它不算大**。
            // 縮小的話會讓**每一間房**的寶箱都變小、點擊區也跟著縮水，
            // 為了一面牆上的櫃子付全域的代價。這是位置問題，不是尺寸問題。
            //
            // 要把被擋掉的位子要回來，**把它們挪開就好** —— 這一段只擋重疊的。
            BlockSlotsCoveringPaintedArt(shuffled);

            // ── 第一段：群組配額先佔位 ──
            //
            // 【順序不能反】先讓家具把位子填滿的話，寶箱就沒地方站了。
            // 這跟 EncounterPlanner 的「保證出現」踩的是同一個坑，做法也一樣。
            var groupQuota = new Dictionary<string, int>();
            var groupFilled = new Dictionary<string, int>();

            foreach (string g in contentData.QuotaGroups)
            {
                if (groupQuota.ContainsKey(g)) continue;

                int want = contentData.RollQuota(g, rng);
                if (want < 0) continue;

                // ⚠️ 配額要被位子數與 maxFilled 夾住。小屋只有幾個位子，
                //    擲到 2 個寶箱又照填的話整間房都是寶箱、沒有場景互動
                int room = Mathf.Min(maxFill, shuffled.Count) - filled;
                if (want > room)
                {
                    Debug.LogWarning(
                        $"[房間] {name} 群組「{g}」配額 {want} 個，但這間房只放得下 {room} 個，已裁掉。\n" +
                        "位子太少或 Max Filled 太小。", this);
                    want = Mathf.Max(0, room);
                }

                groupQuota[g] = want;
                groupFilled[g] = 0;

                int placed = 0;
                foreach (SpawnSlot slot in shuffled)
                {
                    if (placed >= want) break;
                    if (slot == null || slot.IsOccupied) continue;

                    // 配額階段**不看 fillChance** —— 那是「這個位子空著的機率」，
                    // 但配額是「這間房一定要有這麼多」，兩者衝突時配額優先
                    RoomContentData.Entry entry = contentData.PickFor(slot, rng, usedCount, g);
                    if (entry == null) continue;

                    SpawnInto(slot, entry, rng);
                    usedCount[entry] = usedCount.TryGetValue(entry, out int c0) ? c0 + 1 : 1;
                    placed++;
                    filled++;
                }

                groupFilled[g] = placed;

                if (placed < want)
                {
                    // 設定了卻安靜地沒發生，是這個專案最難查的一類問題
                    Debug.LogWarning(
                        $"[房間] {name} 群組「{g}」需要 {want} 個但只放進 {placed} 個。\n" +
                        "可能是沒有條目屬於這個群組，或所有位子的 Placement／Tag 都不接受。", this);
                }
            }

            // ── 第二段：其餘位子照原本的邏輯填，已滿額的群組被排除 ──
            foreach (SpawnSlot slot in shuffled)
            {
                if (filled >= maxFill) break;
                if (slot == null || slot.IsOccupied) continue;

                // 未達最低數量時忽略 fillChance，確保房間不會空到沒東西可做
                bool mustFill = filled < contentData.minFilled;
                if (!mustFill && rng.NextDouble() > slot.fillChance) continue;

                RoomContentData.Entry entry = contentData.PickFor(slot, rng, usedCount, null, groupFilled, groupQuota);
                if (entry == null) continue;

                SpawnInto(slot, entry, rng);
                usedCount[entry] = usedCount.TryGetValue(entry, out int c) ? c + 1 : 1;
                if (!string.IsNullOrEmpty(entry.group))
                    groupFilled[entry.group] = groupFilled.TryGetValue(entry.group, out int gf) ? gf + 1 : 1;
                filled++;
            }

            Debug.Log($"[房間] {name} 填入 {filled} / {slots.Count} 個位子"
                + (groupQuota.Count > 0 ? "（配額：" + string.Join("、", System.Linq.Enumerable.ToArray(
                    System.Linq.Enumerable.Select(groupQuota, kv => kv.Key + " " + groupFilled[kv.Key] + "/" + kv.Value))) + "）" : ""));
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

            // 美術已經把這件家具畫在背景上 —— 生成物只留碰撞框，圖隱形
            if (slot.artIsPainted) HideVisualsKeepRaycast(obj);

            slot.MarkOccupied();
        }

        /// <summary>
        /// 把「會蓋到畫上去的家具」的位子標記成已佔用，這一輪就不會生東西。
        ///
        /// 判定用的是**生成物實際畫出來的大小**（`contentFootprint`），
        /// 不是 sprite 的 bounds —— 這批圖是 2048×1448 的畫布、真正的寶箱只佔三成，
        /// 用 bounds 算的話「每一個都蓋住每一個」，判不出東西來。
        /// </summary>
        private void BlockSlotsCoveringPaintedArt(List<SpawnSlot> all)
        {
            for (int i = 0; i < all.Count; i++)
            {
                SpawnSlot painted = all[i];
                if (painted == null) continue;

                Rect area = painted.ReservedArea;
                if (area.width <= 0f) continue;

                for (int j = 0; j < all.Count; j++)
                {
                    SpawnSlot other = all[j];
                    if (other == null || other == painted || other.IsOccupied) continue;

                    Vector3 c = other.transform.position;
                    var foot = new Rect(c.x - contentFootprint.x * 0.5f,
                                        c.y - contentFootprint.y * 0.5f,
                                        contentFootprint.x, contentFootprint.y);

                    if (!foot.Overlaps(area)) continue;

                    other.MarkOccupied();   // 佔住但不生東西 ＝ 這一輪跳過
                    Debug.Log($"[房間] {name} 的「{other.name}」會蓋到畫上去的家具，這一輪跳過。" +
                              "要用回這個位子就把它挪開。", this);
                }
            }
        }

        /// <summary>
        /// 讓生成物**看不見但仍然碰得到**。
        ///
        /// ⚠️ **一定要調 alpha，不可以 `enabled = false`。**
        /// 停用 Renderer 之後，靠 Collider 的 hover 雖然還在，
        /// 但任何靠 `Image`／`SpriteRenderer` 收事件的東西會整個失效 ——
        /// 這個專案在快捷欄踩過三次（交接文件「坑 1」）。
        /// 而且 `InteractableBase.ApplyRandomVariant` 之後還會去寫那個 Renderer，
        /// 停用的話那一段會安靜地沒有作用。
        ///
        /// 【為什麼不是把 prefab 上的圖拿掉】同一個 prefab 也會被擺在
        /// 「沒有畫上去」的位子上，那時它得看得見。差別是**位子**，不是物件。
        /// </summary>
        private static void HideVisualsKeepRaycast(GameObject obj)
        {
            foreach (SpriteRenderer sr in obj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Color c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, 0f);
            }

            foreach (UnityEngine.UI.Graphic g in obj.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                Color c = g.color;
                g.color = new Color(c.r, c.g, c.b, 0f);
            }
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

            // ⚠️ 同一個物件回報兩次只能算一次。
            // 以前是無條件 interactedCount++，參數收了卻沒用 —— 只要有物件回報兩次
            // （singleUse 關掉的物件、或成功後又走到失敗結案路徑），計數就會超前，
            // 房間會在還有東西沒互動時就宣告清空。
            if (interactable != null && !reported.Add(interactable)) return;

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
