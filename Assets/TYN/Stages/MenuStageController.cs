using UnityEngine;
using EldritchMile.Core;

/// <summary>
/// 主選單 Stage。
///
/// 【命名空間】Core 之所以放 EldritchMile.Core，是為了避開 MapData/RunNodeData 與
/// 尚未改寫的 PerspectiveMapGenerator 撞名。Stage 層沒有這個問題，
/// 因此維持全域命名空間，與專案其餘程式碼一致，在 Inspector 綁定時也少一層阻力。
///
/// 【按鈕怎麼接】本類別只提供 public 方法，實際綁定在 Inspector 的 Button → OnClick。
/// 這是直接取代舊 SceneLoader.LoadUIScene 的位置，一對一替換，不必改動按鈕結構。
/// </summary>
public class MenuStageController : StageController
{
    public override StageType Stage => StageType.Menu;

    [Header("除錯")]
    [Tooltip("勾選後，進場與按鈕點擊都會輸出 log")]
    public bool verboseLog = true;

    /// <summary>
    /// 主選單在「開始新 run」之前就進場，所以 run 一定是 null —— 這是正常的。
    /// </summary>
    public override void OnStageEnter(RunContext run)
    {
        if (verboseLog) Debug.Log("[選單] 進場");
    }

    // ==========================================
    // 按鈕。綁在 Inspector 的 Button → OnClick
    // ==========================================

    /// <summary>
    /// START。取代舊的 SceneLoader.LoadUIScene()。
    ///
    /// 【差別】舊版是 SceneManager.LoadScene("UIScene") 直接換場景；
    /// 新版建立 RunContext（含遺產繼承）後由總管開地圖，全程不換場景。
    /// </summary>
    public void OnStartClicked()
    {
        if (GameFlowManager.Instance == null)
        {
            Debug.LogWarning("[選單] 場上沒有 GameFlowManager");
            return;
        }

        if (verboseLog) Debug.Log("[選單] START");

        // 重複點擊由 GameFlowManager.IsTransitioning 擋掉，這裡不需再防
        GameFlowManager.Instance.StartNewRun();
    }

    /// <summary>
    /// SETTINGS。
    /// 【現況】舊 MenuScene 裡這顆按鈕其實也綁 LoadUIScene，等於沒有實作，只是佔位。
    /// Romtyui 那邊有 OptionMenuUI，日後要接的話從這裡叫。
    /// </summary>
    public void OnSettingsClicked()
    {
        Debug.Log("[選單] SETTINGS —— 尚未實作");
    }

    /// <summary>
    /// RESTART。
    /// 【現況】舊版同樣只綁 LoadUIScene，沒有真的重置任何東西。
    /// 若本意是「清除進度重新開始」，改呼叫 OnClearProgressClicked()。
    /// 因為語意未定，這裡不擅自實作破壞性行為。
    /// </summary>
    public void OnRestartClicked()
    {
        Debug.Log("[選單] RESTART —— 尚未實作（語意待定，見 OnClearProgressClicked）");
    }

    /// <summary>
    /// 清除跨輪迴進度（遺產、解鎖、統計全部歸零）。
    /// ⚠️ 破壞性操作。要綁到按鈕上的話，建議先做二次確認 UI。
    /// </summary>
    public void OnClearProgressClicked()
    {
        MetaProgressData.ClearSave();

        if (GameFlowManager.Instance != null)
        {
            Debug.Log("[選單] 已清除跨輪迴進度。重新啟動遊戲後生效。");
        }
    }
}
