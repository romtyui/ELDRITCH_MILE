using System;
using System.Collections.Generic;
using UnityEngine;
using EldritchMile.Core;

/// <summary>
/// 戰鬥裡的食物欄（Romtyui 的 <see cref="ItemInventory"/>）跟旅程的背包（<see cref="RunContext.inventory"/>）保持一致。
/// （2026-10-29：他的戰鬥獎勵加了食物／藥水，要接上正式流程）
///
/// ────────────────────────────────────────────────────────
/// 【約定（給 Romtyui）】
///   1. **真相是旅程的背包**（RunContext）。ItemInventory 是戰鬥裡的鏡像，格數 = 旅程的食物格數（3）。
///   2. 鏡像裡**一律存 ItemData**（保留物品身分）。你存效果資產（battleItemEffect）也可以，
///      我們會用資料庫反查回 ItemData；但同一個效果被兩種食物共用時會對不準，所以建議存 ItemData。
///   3. 你那邊照常用 <see cref="ItemInventory.AddItem"/> / <see cref="ItemInventory.RemoveItemAt"/>，
///      發出 OnInventoryChanged 之後**我們負責寫回旅程背包**。你不用碰 RunContext，所以不會重複加入。
///   4. 反過來，旅程背包變了（快捷欄吃掉、丟掉、替換）我們會重建鏡像，同樣會發 OnInventoryChanged ——
///      你的獎勵畫面訂閱它重新判斷「收下／裝備已滿」就好。
///   5. 要「選一個舊的換掉」：呼叫 <see cref="RequestReplace"/>。只有換成功才回 true；
///      取消時舊的與獎勵都保留（獎勵不要標成已領取）。
///   6. 領取前請以 AddItem 的回傳值為準（滿了它會回 false）。
///
/// 【道具使用端】我們的流程裡，戰鬥中吃東西走的是 HUD 的快捷欄（ShortcutBarUI.TryUse），它讀旅程背包，
/// 所以鏡像存 ItemData 不影響使用。你 prefab 裡的 Props_Panel 在我們的流程是關著的。
/// </summary>
public class BattleFoodBridge : MonoBehaviour
{
    public static BattleFoodBridge Instance { get; private set; }

    [Tooltip("多久比對一次旅程背包（秒）。快捷欄吃東西、丟東西不會通知這裡，用輪詢")]
    [Min(0f)] public float pollSeconds = 0.15f;

    private ItemInventory mirror;
    private RunContext run;
    private bool writing;           // 我們自己在改鏡像時，不要把 OnInventoryChanged 當成他那邊的改動
    private int runSignature = int.MinValue;
    private float pollTimer;

    public bool IsFull => run != null && run.FoodCount >= run.FoodSlots;

    public void Bind(RunContext context, ItemInventory inventory)
    {
        Unhook();
        run = context;
        mirror = inventory;
        Instance = this;
        if (mirror == null || run == null) return;

        mirror.maxItemCount = Mathf.Max(1, run.FoodSlots);
        mirror.OnInventoryChanged += HandleMirrorChanged;
        RebuildMirror();
    }

    private void OnDestroy()
    {
        Unhook();
        if (Instance == this) Instance = null;
    }

    private void Unhook()
    {
        if (mirror != null) mirror.OnInventoryChanged -= HandleMirrorChanged;
    }

    private void Update()
    {
        if (run == null || mirror == null) return;
        pollTimer += Time.unscaledDeltaTime;
        if (pollTimer < pollSeconds) return;
        pollTimer = 0f;

        if (FoodSignature() != runSignature) RebuildMirror();
    }

    // ==========================================
    // 旅程 → 鏡像
    // ==========================================

    /// <summary>照旅程背包重建鏡像。每一個食物佔一格（同種類不疊加），沒有戰鬥效果的也算。</summary>
    public void RebuildMirror()
    {
        if (run == null || mirror == null) return;

        writing = true;
        try
        {
            mirror.maxItemCount = Mathf.Max(1, run.FoodSlots);
            mirror.Clear();
            List<ItemData> foods = RunFoods();
            for (int i = 0; i < foods.Count; i++) mirror.AddItem(foods[i]);
        }
        finally { writing = false; }

        runSignature = FoodSignature();
    }

    // ==========================================
    // 鏡像 → 旅程
    // ==========================================

    private void HandleMirrorChanged()
    {
        if (writing || run == null || mirror == null) return;

        List<ItemData> want = MirrorFoods();
        List<ItemData> have = RunFoods();

        // 多出來的（他加進去的）→ 寫進旅程。鏡像有格數上限，所以旅程這邊一定放得下，不會觸發「滿了」視窗
        var leftover = new List<ItemData>(have);
        for (int i = 0; i < want.Count; i++)
        {
            if (leftover.Remove(want[i])) continue;
            run.AddItem(want[i].id, 1);
            Debug.Log($"[戰鬥食物] 戰鬥那邊加入「{want[i].Label}」→ 寫回旅程背包");
        }

        // 少掉的（他移除的）→ 從旅程拿掉一個
        for (int i = 0; i < leftover.Count; i++)
        {
            ItemStack st = FindStack(leftover[i].id);
            if (st != null) run.DiscardStack(st);
            Debug.Log($"[戰鬥食物] 戰鬥那邊移除「{leftover[i].Label}」→ 從旅程背包拿掉");
        }

        runSignature = FoodSignature();
    }

    // ==========================================
    // 替換
    // ==========================================

    /// <summary>
    /// 食物欄滿了，要用 <paramref name="offer"/> 換掉一個舊的。跳出選擇視窗（吃掉或丟掉一個舊的、或取消）。
    /// <paramref name="done"/>(true) = 換成功、offer 已經放進背包；(false) = 取消，什麼都沒變。
    /// 沒有滿的話直接放進去，回 true。
    /// </summary>
    public void RequestReplace(ItemData offer, Action<bool> done)
    {
        if (offer == null || run == null) { done?.Invoke(false); return; }

        if (!IsFull)
        {
            bool ok = run.AddItem(offer.id, 1);
            RebuildMirror();
            done?.Invoke(ok);
            return;
        }

        EldritchMile.UI.FoodOverflowUI ui = FindFirstObjectByType<EldritchMile.UI.FoodOverflowUI>(FindObjectsInactive.Include);
        if (ui == null) { Debug.LogWarning("[戰鬥食物] 場上沒有 FoodOverflowUI，沒辦法替換"); done?.Invoke(false); return; }

        ui.OpenForOffer(run, offer, replaced =>
        {
            RebuildMirror();
            done?.Invoke(replaced);
        });
    }

    // ==========================================

    private List<ItemData> RunFoods()
    {
        var list = new List<ItemData>();
        for (int i = 0; i < run.inventory.Count; i++)
        {
            ItemStack st = run.inventory[i];
            if (st == null || st.count <= 0) continue;
            ItemData d = GameFlowManager.Item(st.id);
            if (d == null || !d.HasTag(RunContext.FoodTag)) continue;
            for (int n = 0; n < st.count; n++) list.Add(d);
        }
        return list;
    }

    private List<ItemData> MirrorFoods()
    {
        var list = new List<ItemData>();
        for (int i = 0; i < mirror.CurrentItems.Count; i++)
        {
            ItemData d = ToItemData(mirror.CurrentItems[i]);
            if (d != null) list.Add(d);
        }
        return list;
    }

    /// <summary>鏡像裡的東西 → ItemData。存的是效果資產的話用資料庫反查（取第一個用這個效果的食物）。</summary>
    private static ItemData ToItemData(ScriptableObject o)
    {
        ItemData d = o as ItemData;
        if (d != null) return d;
        if (o == null) return null;

        ItemDatabase db = GameFlowManager.Instance != null ? GameFlowManager.Instance.itemDatabase : null;
        if (db == null) return null;
        for (int i = 0; i < db.items.Count; i++)
            if (db.items[i] != null && db.items[i].battleItemEffect == o && db.items[i].HasTag(RunContext.FoodTag)) return db.items[i];
        return null;
    }

    private ItemStack FindStack(string id)
    {
        for (int i = run.inventory.Count - 1; i >= 0; i--)
            if (run.inventory[i] != null && run.inventory[i].id == id && run.inventory[i].count > 0) return run.inventory[i];
        return null;
    }

    private int FoodSignature()
    {
        unchecked
        {
            int h = 17;
            List<ItemData> foods = RunFoods();
            for (int i = 0; i < foods.Count; i++) h = h * 31 + foods[i].id.GetHashCode();
            return h * 31 + foods.Count;
        }
    }
}
