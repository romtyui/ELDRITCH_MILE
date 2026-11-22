using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// Stage 自帶「直接畫到螢幕」的相機時，把宿主（EventScene）的畫面相機暫停，離場再還原。
    ///
    /// 規則跟 BattleStageController 那一段一樣（那邊有完整的來龍去脈，這裡不重複）：
    ///   · Stage 底下有一台啟用中、targetTexture 為空的相機 → 宿主所有同類相機暫停
    ///   · 畫到 RenderTexture 的（地圖的 MapCamera）不動 —— 那些不搶螢幕
    ///   · 只關 Camera **元件**，不關物件：宿主的 AudioListener 掛在主相機上，關物件 BGM 會消失
    ///   · 宿主有 AudioListener 時，把 Stage 自己帶的關掉，免得 Console 噴「兩個 AudioListener」
    ///
    /// 2026-11-23 大廳接進來時抽出來的；戰鬥那一份暫時沒換過來（能跑就先不動）。
    /// <see cref="Restore"/> 可以重複呼叫。
    /// </summary>
    public sealed class StageCameraHandover
    {
        private readonly List<Camera> suspended = new List<Camera>();

        public IReadOnlyList<Camera> Suspended => suspended;

        /// <returns>Stage 有自己的畫面相機（因此真的接手了）就是 true</returns>
        public bool Take(Transform stageRoot)
        {
            if (stageRoot == null) return false;

            bool stageOwnsScreen = false;
            foreach (Camera c in stageRoot.GetComponentsInChildren<Camera>(false))
            {
                if (c.enabled && c.targetTexture == null) { stageOwnsScreen = true; break; }
            }
            if (!stageOwnsScreen) return false;

            foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
            {
                if (c.transform.IsChildOf(stageRoot)) continue;
                if (!c.enabled || c.targetTexture != null) continue;

                c.enabled = false;
                suspended.Add(c);
            }

            bool hostHasListener = false;
            foreach (AudioListener l in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
            {
                if (!l.transform.IsChildOf(stageRoot) && l.enabled) { hostHasListener = true; break; }
            }
            if (hostHasListener)
            {
                foreach (AudioListener l in stageRoot.GetComponentsInChildren<AudioListener>(true)) l.enabled = false;
            }

            return true;
        }

        public void Restore()
        {
            for (int i = 0; i < suspended.Count; i++)
            {
                if (suspended[i] != null) suspended[i].enabled = true;
            }
            suspended.Clear();
        }
    }
}
