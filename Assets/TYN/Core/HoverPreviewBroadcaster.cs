using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// C17：hover 一張手牌 → 畫面上所有選項同時顯示各自的成功率。
    /// 對應企劃草圖裡的三列「A 50 / B 50 / C 50」。
    ///
    /// 【為什麼用廣播】若讓每個選項自己去查，N 個選項就會有 N 次
    /// FindObjectsOfType 或輪詢。改為註冊 + 廣播，只掃一次。
    ///
    /// 【C18① 限制】已選定「主要目標」時不廣播 —— 畫面應聚焦在該目標。
    /// 這個判斷由 DialogueEncounterController 負責，見 Begin() 的說明。
    /// </summary>
    public class HoverPreviewBroadcaster : MonoBehaviour
    {
        public static HoverPreviewBroadcaster Instance { get; private set; }

        private readonly List<IProbabilityTarget> targets = new List<IProbabilityTarget>();
        private bool isPreviewing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Register(IProbabilityTarget target)
        {
            if (target == null || targets.Contains(target)) return;
            targets.Add(target);
        }

        public void Unregister(IProbabilityTarget target)
        {
            if (target == null) return;

            targets.Remove(target);

            // 移除時若正在預覽，避免留下沒人關掉的殘影
            if (isPreviewing) target.HidePreview();
        }

        public void Clear()
        {
            End();
            targets.Clear();
        }

        /// <summary>
        /// 廣播預覽給所有已註冊目標。
        ///
        /// ⚠️ 呼叫端須先確認「尚未選定主要目標」(C18①)。本類別不自己判斷，
        ///    因為主要目標的狀態屬於 DialogueEncounterController。
        /// </summary>
        /// <param name="focusTarget">
        /// 不為 null 時**只顯示這一個目標**的機率，其餘全部收起來。
        ///
        /// 這是 C18① 的正確樣子：「選定後畫面聚焦單一目標」指的是**只剩它有數字**，
        /// 不是「什麼數字都沒有」—— 玩家正要瞄準它，那一刻最需要看到成功率。
        /// </param>
        public void Begin(CardDataExplore hoveredCard, IProbabilityTarget focusTarget = null)
        {
            if (hoveredCard == null) return;
            if (ProbabilityCheck.Instance == null)
            {
                Debug.LogWarning("[預覽] 場上沒有 ProbabilityCheck，無法計算機率");
                return;
            }

            isPreviewing = true;

            for (int i = 0; i < targets.Count; i++)
            {
                IProbabilityTarget t = targets[i];
                if (t == null) continue;

                if (focusTarget != null && !ReferenceEquals(t, focusTarget))
                {
                    t.HidePreview();
                    continue;
                }

                Effectiveness eff;
                float rate = ProbabilityCheck.Instance.CalculateRate(hoveredCard, t, out eff);

                t.ShowPreview(rate, eff);
            }
        }

        public void End()
        {
            if (!isPreviewing) return;

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null) targets[i].HidePreview();
            }

            isPreviewing = false;
        }
    }
}
