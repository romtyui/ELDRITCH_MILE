using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EldritchMile.Core;

namespace EldritchMile.UI
{
    /// <summary>
    /// 祭壇右手（NO）給的東西：**Romtyui 戰鬥結算那個「挑你要的獎勵」畫面**。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【畫面是他的、邏輯也是他的】（2026-10-13）`AltarUI/Rewards` 是從 Stage_Battle_v3
    /// 複製過來的 <see cref="BattleRewardUI"/> 整組。他那支原本只能從戰鬥裡開
    /// （一定要有 BattleManager），這次在他的檔裡加了 `ShowOutsideBattle()`
    /// （標註 `//新增時間26/10/13 by TYN`，戰鬥裡的行為不變）。
    /// 這一支只做兩件「戰鬥外才會缺」的事：
    ///
    ///   1. **背景模糊**。他畫面裡三張 RawImage 顯示的是 `BattleCaptureRT` ——
    ///      那是戰鬥專用的 BlurCaptureCamera 拍的。戰鬥外沒有那台相機，
    ///      直接打開會看到**上一場戰鬥殘留的畫面**被模糊在背後。
    ///      這裡改成打開前當場截一張螢幕，換給那幾張 RawImage（材質照用他的 blur_M）。
    ///   2. **獎勵內容**。他的獎勵讀 `EnemyFormationData` 上那幾格（金幣／武器／遺物的列數與範圍），
    ///      所以祭壇也用一個 EnemyFormationData 資產當「獎勵設定」——
    ///      不新增型別，他那邊要調獎勵規則時兩邊是同一套欄位。
    /// </summary>
    public class AltarRewardBridge : MonoBehaviour
    {
        [Tooltip("複製過來的 BattleRewardUI。留空會在子物件裡找")]
        public BattleRewardUI rewardUI;

        [Tooltip("給什麼獎勵。只讀金幣／武器／遺物那幾格，敵人清單用不到")]
        public EnemyFormationData rewardSpec;

        [Tooltip("戰鬥那台模糊相機的 RT。畫面裡用到它的 RawImage 會被換成當場截圖")]
        public Texture battleCaptureTexture;

        private readonly List<RawImage> blurTargets = new List<RawImage>();
        private RenderTexture captured;

        private void Awake()
        {
            if (rewardUI == null) rewardUI = GetComponentInChildren<BattleRewardUI>(true);
            if (rewardUI == null) return;

            foreach (RawImage ri in rewardUI.GetComponentsInChildren<RawImage>(true))
            {
                if (battleCaptureTexture == null || ri.texture == battleCaptureTexture) blurTargets.Add(ri);
            }

            // 一開始就把戰鬥的 RT 拿掉，萬一有人提早打開也不會看到殘影
            for (int i = 0; i < blurTargets.Count; i++) blurTargets[i].texture = null;
        }

        private void OnDestroy()
        {
            if (captured != null) { captured.Release(); Destroy(captured); }
        }

        /// <summary>
        /// 打開獎勵畫面。玩家領完或放棄之後呼叫 <paramref name="onDone"/>。
        /// 打不開（資料或引用缺）時也會呼叫 —— 祭壇不能卡在一隻伸出來的手上。
        /// </summary>
        public void Open(RunContext run, Action onDone)
        {
            StartCoroutine(OpenRoutine(run, onDone));
        }

        private IEnumerator OpenRoutine(RunContext run, Action onDone)
        {
            // 截圖要等這一幀畫完
            yield return new WaitForEndOfFrame();

            if (captured == null || captured.width != Screen.width || captured.height != Screen.height)
            {
                if (captured != null) { captured.Release(); Destroy(captured); }
                captured = new RenderTexture(Screen.width, Screen.height, 0) { name = "_AltarRewardBackdrop" };
            }
            // 跟他的 DeckViewerBlur 同一個做法：截進暫存 RT，再上下翻轉 blit 一次 ——
            // CaptureScreenshotIntoRenderTexture 在 DX 底下拿到的是倒過來的
            RenderTexture shot = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(shot);
            Graphics.Blit(shot, captured, new Vector2(1f, -1f), new Vector2(0f, 1f));
            RenderTexture.ReleaseTemporary(shot);

            for (int i = 0; i < blurTargets.Count; i++) blurTargets[i].texture = captured;

            bool opened = rewardUI != null && rewardSpec != null && run != null
                          && rewardUI.ShowOutsideBattle(run, rewardSpec, () => { if (onDone != null) onDone(); });

            if (!opened)
            {
                Debug.LogWarning("[祭壇] 獎勵畫面打不開（" +
                                 (rewardUI == null ? "沒有 BattleRewardUI" : rewardSpec == null ? "沒有獎勵設定" : run == null ? "沒有 RunContext" : "BattleRewardUI 拒絕開啟") +
                                 "），直接結束", this);
                if (onDone != null) onDone();
            }
        }
    }
}
