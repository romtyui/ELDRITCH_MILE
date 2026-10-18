using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 戰鬥跟著地區換背景與怪物池的**接口**。（2026-10-18，設計見 Docs/Design_RegionTravel.md 接法 B）
///
/// ────────────────────────────────────────────────────────
/// 【給 Romtyui】這支掛在 Stage_Battle_v3 的根物件上，兩個欄位已經指好：
///   · background = BattleContentRoot/SceneCanvas/SceneRoot/Background
///   · spawner    = monster_Panel 上的 EnemyFormationSpawner
/// **你改了階層或換了物件，只要重新指這兩格**，我們這邊不用動。
/// 之後要加「地區專屬的燈光／音樂／故障強度」也塞在 Apply 裡，介面不變。
///
/// 【誰呼叫】我方的 BattleStageController，在預約敵人、打開 BattleManager **之前**。
/// 傳 null 的那一格 = 這一區沒指定，維持 prefab 原本的設定。
///
/// 【打哪一組】（2026-10-18《怪物組合》表）我們依這一站的級別算出要的 weight
/// （前三場 1、第四場起 2、菁英 3、首領 5，數字在 RegionData），
/// 呼叫 <see cref="ReserveFromPool"/> 從怪物池裡抽一組 **weight 剛好相等** 的、預約給 spawner。
/// spawner 本來就會先用預約的那一組，所以他的抽怪程式不用改。
///   · 為什麼不用 spawner 的「模式二」：它是「weight ≤ 上限」，菁英戰（3）會抽到前期的雜魚組；
///     而且它用 isBoss 分一般／首領，池子裡的勾選目前跟表對不上（見下）
///   · isBoss 在這裡**不參與挑選**，只影響他那邊要不要用首領的 UI —— 所以池子裡的勾要照表打（只有首領打勾）
///
/// 檔案放在 Assets/TYN 是因為分工規則（我方不在 Assets/Romtyui 底下建檔）；
/// 內容歸戰鬥組維護，要搬過去也可以，prefab 上的參照不會斷。
/// </summary>
public class BattleRegionHook : MonoBehaviour
{
    [Tooltip("戰鬥背景那張圖")]
    public Image background;

    [Tooltip("抽怪的那一支。沒有指定對手時他會從自己的 encounterPool 抽")]
    public EnemyFormationSpawner spawner;

    private Sprite defaultBackground;
    private EnemyEncounterPoolData defaultPool;
    private bool captured;

    /// <summary>套用一個地區的戰鬥設定。null = 維持 prefab 原本的。</summary>
    public void Apply(Sprite bg, EnemyEncounterPoolData pool)
    {
        Capture();

        if (background != null) background.sprite = bg != null ? bg : defaultBackground;
        if (spawner != null) spawner.encounterPool = pool != null ? pool : defaultPool;

        Debug.Log($"[戰鬥地區] 背景 = {(background != null && background.sprite != null ? background.sprite.name : "（無）")}、" +
                  $"怪物池 = {(spawner != null && spawner.encounterPool != null ? spawner.encounterPool.name : "（無）")}");
    }

    /// <summary>
    /// 從目前的怪物池抽一組 weight == <paramref name="weight"/> 的組合，預約給 spawner。回傳抽到的那一組，抽不到回 null。
    ///
    /// 這一場 run 出現過的組合先避開（池子自己記的那份）；全出現過了才重複。
    /// 這個 weight 一組都沒有（表還沒填完）就往下找最接近的，再沒有就往上 —— 寧可難度差一階，也不要整場空白。
    /// </summary>
    public EnemyFormationData ReserveFromPool(int weight)
    {
        EnemyEncounterPoolData pool = spawner != null ? spawner.encounterPool : null;
        if (pool == null || pool.entries == null) return null;

        int found = -1;
        List<EnemyFormationData> candidates = null;
        for (int d = 0; d <= 10 && candidates == null; d++)
        {
            candidates = Collect(pool, weight - d);
            if (candidates != null) { found = weight - d; break; }
            if (d > 0) { candidates = Collect(pool, weight + d); if (candidates != null) { found = weight + d; break; } }
        }
        if (candidates == null) return null;

        EnemyFormationData pick = candidates[Random.Range(0, candidates.Count)];
        pool.MarkFormationUsed(pick);
        if (RunStateManager.Instance != null) RunStateManager.Instance.ReserveFormation(pick);

        Debug.Log($"[戰鬥地區] 要 weight {weight}" + (found != weight ? $"（沒有，改用 {found}）" : "") +
                  $"，{candidates.Count} 組候選 → {pick.name}");
        return pick;
    }

    /// <summary>weight 剛好等於 w 的組合。優先沒出現過的；全出現過就全部。一組都沒有回 null。</summary>
    private static List<EnemyFormationData> Collect(EnemyEncounterPoolData pool, int w)
    {
        if (w < 0) return null;
        var fresh = new List<EnemyFormationData>();
        var all = new List<EnemyFormationData>();
        for (int i = 0; i < pool.entries.Count; i++)
        {
            EnemyEncounterPoolEntry e = pool.entries[i];
            if (e == null || e.formation == null || e.weight != w) continue;
            if (!all.Contains(e.formation)) all.Add(e.formation);
            if (!pool.IsFormationUsed(e.formation) && !fresh.Contains(e.formation)) fresh.Add(e.formation);
        }
        if (fresh.Count > 0) return fresh;
        return all.Count > 0 ? all : null;
    }

    private void Capture()
    {
        if (captured) return;
        captured = true;
        if (background != null) defaultBackground = background.sprite;
        if (spawner != null) defaultPool = spawner.encounterPool;
    }
}
