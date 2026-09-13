using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.UI
{
    /// <summary>
    /// 讓戰鬥的說明框（`TooltipUI`）能正確貼在**別的座標系**畫布上的圖示旁邊。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【症狀】（2026-09-15 build 回報）怪物的狀態／意圖說明（蓄力…）跑到畫面右上角。
    ///
    /// 【原因】說明框在 `BattleUICanvas`（Screen Space - **Overlay**，單位是像素），
    /// 怪物的圖示在 `monsterCanvas`（Screen Space - **Camera**，單位是世界座標）。
    /// `TooltipUI.Reposition` 用 `CalculateRelativeRectTransformBounds(說明框畫布, 目標)` 量目標位置 ——
    /// 那一支只是做矩陣換算，**不會處理兩種座標系的差異**：世界座標 (-3, 4) 被當成像素，
    /// 等於畫面左下角，四個方向都放不下，最後被夾到右上角。編輯器裡也一樣（不只 build）。
    ///
    /// 【為什麼不改 TooltipUI】那是 Romtyui 的檔案。這裡在我方做一層轉接：
    /// 每個「畫布座標系跟說明框不同」的觸發器，在說明框的畫布上放一個**看不見的替身矩形**，
    /// 每幀把它移到圖示在螢幕上的位置，再把觸發器的 `targetRect` 指到替身。
    /// `TooltipUI` 量到的就是同一個座標系的東西了。
    ///
    /// 兩邊畫布座標系相同時（例如之後都改成 Overlay）什麼都不做 —— 修正了也不會重複換算。
    /// 根治的做法（Reposition 先換成螢幕座標再換回來）請跟 Romtyui 討論。
    ///
    /// 【掛在哪】`BattleStageController` 進場時自動加在戰鬥 Stage 上，掃的是 Stage 底下的觸發器。
    /// </summary>
    public class TooltipCanvasBridge : MonoBehaviour
    {
        [Tooltip("多久重新掃一次觸發器（秒）—— 怪物的狀態圖示是戰鬥中動態生成的")]
        [Min(0.05f)] public float rescanSeconds = 0.25f;

        private class Pair
        {
            public TooltipTriggerUI trigger;
            public RectTransform original;
            public RectTransform proxy;
            public Canvas sourceCanvas;
        }

        private readonly List<Pair> pairs = new List<Pair>();
        private readonly Vector3[] corners = new Vector3[4];
        private float timer;

        private void OnDisable()
        {
            // 還原：觸發器指回原本的目標，替身收掉
            for (int i = 0; i < pairs.Count; i++)
            {
                Pair p = pairs[i];
                if (p.trigger != null && p.trigger.targetRect == p.proxy)
                    p.trigger.targetRect = p.original == (p.trigger.transform as RectTransform) ? null : p.original;
                if (p.proxy != null) Destroy(p.proxy.gameObject);
            }
            pairs.Clear();
        }

        private void LateUpdate()
        {
            TooltipUI tip = TooltipUI.Instance;
            if (tip == null) return;

            Canvas tipCanvas = tip.GetComponentInParent<Canvas>(true);
            if (tipCanvas == null) return;
            tipCanvas = tipCanvas.rootCanvas;

            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f)
            {
                timer = rescanSeconds;
                Rescan(tipCanvas);
            }

            for (int i = pairs.Count - 1; i >= 0; i--)
            {
                Pair p = pairs[i];
                if (p.trigger == null || p.original == null || p.proxy == null)
                {
                    if (p.proxy != null) Destroy(p.proxy.gameObject);
                    pairs.RemoveAt(i);
                    continue;
                }
                Follow(p, tipCanvas);
            }
        }

        private void Rescan(Canvas tipCanvas)
        {
            TooltipTriggerUI[] triggers = GetComponentsInChildren<TooltipTriggerUI>(true);

            for (int i = 0; i < triggers.Length; i++)
            {
                TooltipTriggerUI t = triggers[i];
                if (t == null || IsPaired(t)) continue;

                RectTransform original = t.targetRect != null ? t.targetRect : t.transform as RectTransform;
                if (original == null) continue;

                // ⚠️ 看的是**被量的那個矩形**（targetRect）在哪個畫布，不是觸發器本身在哪個畫布。
                //    兩者可以不同 —— 第一版拿觸發器的畫布相機去換算 targetRect，
                //    結果 Overlay 畫布上的像素座標被丟進正交相機，替身飛到 (610648, 865284)
                Canvas c = original.GetComponentInParent<Canvas>(true);
                if (c == null) continue;
                Canvas root = c.rootCanvas;
                if (root == tipCanvas || SameSpace(root, tipCanvas)) continue;

                GameObject go = new GameObject("TooltipProxy_" + t.name, typeof(RectTransform));
                go.layer = tipCanvas.gameObject.layer;
                RectTransform proxy = (RectTransform)go.transform;
                proxy.SetParent(tipCanvas.transform, false);
                proxy.anchorMin = new Vector2(0.5f, 0.5f);
                proxy.anchorMax = new Vector2(0.5f, 0.5f);
                proxy.pivot = new Vector2(0.5f, 0.5f);

                t.targetRect = proxy;
                pairs.Add(new Pair { trigger = t, original = original, proxy = proxy, sourceCanvas = root });
            }
        }

        private bool IsPaired(TooltipTriggerUI t)
        {
            for (int i = 0; i < pairs.Count; i++) if (pairs[i].trigger == t) return true;
            return false;
        }

        /// 兩個畫布量出來的座標可以直接互通嗎
        private static bool SameSpace(Canvas a, Canvas b)
        {
            bool aOverlay = a.renderMode == RenderMode.ScreenSpaceOverlay;
            bool bOverlay = b.renderMode == RenderMode.ScreenSpaceOverlay;
            if (aOverlay && bOverlay) return true;
            if (!aOverlay && !bOverlay) return a.worldCamera == b.worldCamera;
            return false;
        }

        /// <summary>把替身擺到原圖示在說明框畫布上的位置與大小。原圖示 → 螢幕座標 → 說明框畫布座標。</summary>
        private void Follow(Pair p, Canvas tipCanvas)
        {
            Camera srcCam = p.sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : p.sourceCanvas.worldCamera;
            Camera dstCam = tipCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : tipCanvas.worldCamera;
            RectTransform canvasRt = (RectTransform)tipCanvas.transform;

            p.original.GetWorldCorners(corners);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < 4; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(srcCam, corners[i]);
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, dstCam, out local);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            p.proxy.anchoredPosition = (min + max) * 0.5f;
            p.proxy.sizeDelta = max - min;
        }
    }
}
