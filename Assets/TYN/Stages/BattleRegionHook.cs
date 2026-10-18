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

    private void Capture()
    {
        if (captured) return;
        captured = true;
        if (background != null) defaultBackground = background.sprite;
        if (spawner != null) defaultPool = spawner.encounterPool;
    }
}
