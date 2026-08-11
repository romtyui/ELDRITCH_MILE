using System.Collections;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 每個 Stage prefab 的根物件都掛一個子類別。
    ///
    /// 【鐵則】Stage 內部**不得**出現 SceneManager 呼叫，也不得自行切換到別的 Stage。
    /// 一切流程變更都要經過 GameFlowManager。
    /// </summary>
    public abstract class StageController : MonoBehaviour
    {
        public abstract StageType Stage { get; }

        /// <summary>
        /// 進場。此時畫面仍全黑，適合做初始化與讀取 RunContext。
        ///
        /// 【注意】run 可能為 null —— 主選單是在開始新 run 之前就進場的。
        /// 不需要 run 的 Stage（Menu、Intro）請忽略此參數，需要的請自行判空。
        /// </summary>
        public virtual void OnStageEnter(RunContext run) { }

        /// <summary>
        /// 黑幕淡出完成後呼叫。適合播開場動畫（例如地圖節點逐層彈出）。
        /// </summary>
        public virtual IEnumerator OnStageReady()
        {
            yield break;
        }

        /// <summary>
        /// 退場。此時畫面已全黑，適合把狀態寫回 RunContext 並清理。
        /// </summary>
        public virtual IEnumerator OnStageExit()
        {
            yield break;
        }

        /// <summary>
        /// C2：向流程總管回報本 Stage 已完成，接著地圖會自動下拉。
        ///
        /// 【注意】這是「流程自然結束」的回報，不是「玩家按了返回按鈕」。
        /// 舊架構的 Door.OpenDoor() 是玩家點門就直接走，缺少確認步驟，與 C14 不符。
        /// </summary>
        protected void ReportComplete(StageResult result = StageResult.Completed)
        {
            if (GameFlowManager.Instance == null)
            {
                Debug.LogWarning($"[{Stage}] 找不到 GameFlowManager，無法回報完成");
                return;
            }

            GameFlowManager.Instance.NotifyStageComplete(result);
        }
    }
}
