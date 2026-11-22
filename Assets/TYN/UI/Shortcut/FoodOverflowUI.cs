using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EldritchMile.Core;
using EldritchMile.UI.Shortcut;

namespace EldritchMile.UI
{
    /// <summary>
    /// 食物（藥水）欄滿了又拿到新的 —— 讓玩家決定去留。（規則 2026-10-18 定案；畫面 2026-11-23 照美術《藥水已滿的情況》改版）
    ///
    /// ────────────────────────────────────────────────────────
    /// 【規則】食物同種類不疊加、一個佔一格，最多 <see cref="RunContext.FoodSlots"/> 格（3）。
    /// 滿了再拿到時跳這個畫面，選一個：
    ///   · 右上角每支試管旁的「丟棄」→ 丟掉那個舊的，新的進來
    ///   · 紫色底裡新藥水下面的「丟棄」→ 不要新的，什麼都不變
    ///   · **點試管本身** → 吃掉那個舊的（效果照常發生），新的進來
    /// 一次拿到好幾個（寶箱 ×2）會一個一個問。
    ///
    /// 【畫面】（美術《畫面統整_尺寸》，1pt = 1px，1mm ≈ 2.834px）
    ///   · 背景：截圖模糊壓暗，放在 HUD（400）**底下**的子畫布（390）——
    ///     所以試管、錢、齒輪都清楚，其他全糊掉
    ///   · 黑色橫幅 677.5×54.5mm、白字底 298.6×23.7mm「藥水攜帶已滿，請捨棄一些裝備」
    ///   · 紫色底 360.3×329.7mm：新藥水圖 53×53mm、名稱與效果 27.7pt、丟棄 38.2×11.1mm
    ///   · 每支試管左邊一個丟棄
    ///
    /// 【吃東西走快捷欄那一支】<see cref="ShortcutBarUI.TryUse"/>（respectTiming = false）——
    /// 戰鬥中改走戰鬥單位、付不起代價就不吃、播報實際變化，規則只能有一份。
    ///
    /// 【不能直接關】一定要選一個。關掉的話新的那個要放哪裡沒有答案。
    /// </summary>
    public class FoodOverflowUI : MonoBehaviour
    {
        [Header("整體")]
        public CanvasGroup group;

        [Header("背景（HUD 底下那一層）")]
        [Tooltip("顯示模糊截圖的 RawImage（材質用 blur_M）")]
        public RawImage backdrop;

        [Tooltip("截圖時暫時藏起來的畫面 —— HUD 要清楚地疊在模糊背景上，截進去的話會有一層糊掉的分身")]
        public List<CanvasGroup> hideWhileCapturing = new List<CanvasGroup>();

        [Header("橫幅")]
        public TextMeshProUGUI bannerText;
        public string bannerLine = "藥水攜帶已滿，請捨棄一些裝備";

        [Header("新拿到的那一個（紫色底）")]
        public Image newIcon;
        public TextMeshProUGUI newName;
        public TextMeshProUGUI newDesc;
        public Button newDiscardButton;

        [Header("試管旁的丟棄")]
        [Tooltip("樣板，保持關閉。每支有東西的試管複製一個，貼在試管左邊")]
        public Button tubeDiscardTemplate;

        [Tooltip("丟棄與試管之間的距離（px）")]
        public float tubeDiscardGap = 30f;

        private RunContext run;
        private ShortcutBarUI bar;
        private readonly List<Button> tubeButtons = new List<Button>();
        private readonly List<ItemStack> tubeStacks = new List<ItemStack>();
        private RenderTexture captured;
        private Coroutine captureRoutine;

        public bool IsOpen { get; private set; }

        // 替換模式（2026-10-29 戰鬥獎勵）：獎勵還沒進背包，丟一個舊的換掉，或丟掉這個獎勵（＝不拿）
        private ItemData offer;
        private System.Action<bool> offerDone;

        /// <summary>
        /// 替換模式：<paramref name="newItem"/> 還沒進背包。玩家吃掉／丟掉一個舊的 → 放進去、回呼 true；
        /// 丟掉新的 → 什麼都不變、回呼 false。（BattleFoodBridge.RequestReplace 用）
        /// </summary>
        public void OpenForOffer(RunContext context, ItemData newItem, System.Action<bool> done)
        {
            run = context;
            offer = newItem;
            offerDone = done;
            Rebuild();
        }

        private void Awake()
        {
            RunContext.OnFoodOverflow += HandleOverflow;
            if (tubeDiscardTemplate != null) tubeDiscardTemplate.gameObject.SetActive(false);
            if (newDiscardButton != null) newDiscardButton.onClick.AddListener(DiscardNew);
            if (bannerText != null) bannerText.text = bannerLine;
            SetVisible(false);
        }

        private void OnDestroy()
        {
            RunContext.OnFoodOverflow -= HandleOverflow;
            ReleaseCapture();
        }

        private void HandleOverflow(RunContext context, string newId)
        {
            run = context;
            Rebuild();
        }

        // ==========================================
        private void Rebuild()
        {
            ClearTubeButtons();

            string pending = offer != null ? offer.id : (run != null ? run.PendingFood : null);
            if (pending == null) { SetVisible(false); return; }

            bar = FoodBar();

            // 新的那一個
            ItemData nd = GameFlowManager.Item(pending);
            if (newIcon != null) { newIcon.sprite = nd != null ? nd.icon : null; newIcon.enabled = newIcon.sprite != null; newIcon.preserveAspect = true; }
            if (newName != null) newName.text = nd != null ? nd.Label : pending;
            if (newDesc != null) newDesc.text = nd != null ? nd.description : "";

            // 舊的：快捷欄是照背包順序把食物排進固定格子的，這裡用同一個順序對到每支試管
            var snapshot = new List<ItemStack>(run.inventory);
            for (int i = 0; i < snapshot.Count; i++)
            {
                ItemStack st = snapshot[i];
                if (st == null || st.count <= 0) continue;
                ItemData d = GameFlowManager.Item(st.id);
                if (d == null || !d.HasTag(RunContext.FoodTag)) continue;
                tubeStacks.Add(st);
            }

            if (tubeDiscardTemplate != null)
            {
                for (int i = 0; i < tubeStacks.Count; i++)
                {
                    ItemStack captured = tubeStacks[i];
                    Button b = Instantiate(tubeDiscardTemplate, tubeDiscardTemplate.transform.parent);
                    b.gameObject.SetActive(true);
                    b.onClick.AddListener(() => DiscardOld(captured));
                    tubeButtons.Add(b);
                }
            }

            if (bar != null) bar.slotClickOverride = HandleTubeClicked;

            bool wasOpen = IsOpen;
            SetVisible(true);
            if (!wasOpen) BeginCapture();
            PlaceTubeButtons();
        }

        private void LateUpdate()
        {
            if (IsOpen) PlaceTubeButtons();
        }

        /// <summary>每個丟棄貼在對應試管的左邊、垂直置中。兩邊都是 Overlay 畫布，世界座標就是螢幕座標</summary>
        private void PlaceTubeButtons()
        {
            if (bar == null || !bar.UsingFixedSlots) return;

            var corners = new Vector3[4];
            for (int i = 0; i < tubeButtons.Count; i++)
            {
                Button b = tubeButtons[i];
                if (b == null) continue;

                ShortcutSlotUI slot = i < bar.fixedSlots.Count ? bar.fixedSlots[i] : null;
                bool ok = slot != null && slot.gameObject.activeInHierarchy;
                b.gameObject.SetActive(ok);
                if (!ok) continue;

                ((RectTransform)slot.transform).GetWorldCorners(corners);
                var rt = (RectTransform)b.transform;
                float scale = rt.lossyScale.x;
                float half = rt.rect.width * 0.5f * scale;
                rt.position = new Vector3(corners[0].x - tubeDiscardGap * scale - half, (corners[0].y + corners[1].y) * 0.5f, rt.position.z);
            }
        }

        // ==========================================
        private bool HandleTubeClicked(ItemData d)
        {
            if (!IsOpen) return false;
            Eat(d);
            return true;
        }

        private void Eat(ItemData d)
        {
            ShortcutBarUI b = FoodBar();
            // 吃不下去（付不起代價）時 TryUse 自己會說明原因，畫面留著讓玩家改選
            bool eaten = b != null ? b.TryUse(d, false) : run.ConsumeItem(d.id, 1);
            if (!eaten) return;

            if (offer != null) { FinishOffer(); return; }
            run.AcceptPendingFood();
            Next();
        }

        private void DiscardOld(ItemStack stack)
        {
            run.DiscardStack(stack);
            if (offer != null) { FinishOffer(); return; }
            run.AcceptPendingFood();
            Next();
        }

        private void DiscardNew()
        {
            if (!IsOpen) return;
            if (offer != null) { EndOffer(false); return; }
            run.DiscardPendingFood();
            Next();
        }

        /// <summary>舊的騰出一格了 —— 把獎勵放進去。放不進去（不該發生）就當取消，獎勵保留。</summary>
        private void FinishOffer()
        {
            bool ok = run.FoodCount < run.FoodSlots && run.AddItem(offer.id, 1);
            EndOffer(ok);
        }

        private void EndOffer(bool replaced)
        {
            System.Action<bool> cb = offerDone;
            offer = null;
            offerDone = null;
            ClearTubeButtons();
            SetVisible(false);
            ShortcutBarUI b = FoodBar();
            if (b != null) b.Refresh(false);
            if (cb != null) cb(replaced);
        }

        /// <summary>這一個處理完。佇列裡還有的話接著問下一個。</summary>
        private void Next()
        {
            ShortcutBarUI b = FoodBar();
            if (b != null) b.Refresh(false);

            if (run != null && run.PendingFood != null)
            {
                // 騰出位子之後佇列裡的下一個可能直接放得進去（例如丟了兩個）
                while (run.PendingFood != null && run.AcceptPendingFood()) { }
                if (b != null) b.Refresh(false);
            }

            Rebuild();
        }

        // ==========================================
        private static ShortcutBarUI FoodBar()
        {
            foreach (ShortcutBarUI b in FindObjectsByType<ShortcutBarUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (b.filterTag == RunContext.FoodTag) return b;
            return null;
        }

        private void ClearTubeButtons()
        {
            for (int i = 0; i < tubeButtons.Count; i++) if (tubeButtons[i] != null) Destroy(tubeButtons[i].gameObject);
            tubeButtons.Clear();
            tubeStacks.Clear();
        }

        private void SetVisible(bool on)
        {
            IsOpen = on;
            if (group != null)
            {
                group.alpha = on ? 1f : 0f;
                group.blocksRaycasts = on;
                group.interactable = on;
            }

            if (!on)
            {
                if (bar != null && bar.slotClickOverride == (System.Func<ItemData, bool>)HandleTubeClicked) bar.slotClickOverride = null;
                ReleaseCapture();
            }
        }

        // ==========================================
        // 模糊背景：截一張不含自己、不含 HUD 的畫面（做法同 Romtyui 的 DeckViewerBlur）
        // ==========================================

        private void BeginCapture()
        {
            if (backdrop == null) return;
            if (captureRoutine != null) StopCoroutine(captureRoutine);
            captureRoutine = StartCoroutine(CaptureRoutine());
        }

        // 截圖期間藏起來的畫面原本的透明度。截到一半被關掉也要還回去，不然 HUD 會一直隱形
        private readonly List<float> hiddenAlphas = new List<float>();

        private IEnumerator CaptureRoutine()
        {
            // 這一幀先不要露臉，等截完圖再出來
            if (group != null) group.alpha = 0f;
            HideForCapture();

            yield return new WaitForEndOfFrame();

            RenderTexture shot = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(shot);
            if (captured != null) Destroy(captured);
            captured = new RenderTexture(Screen.width, Screen.height, 0);
            Graphics.Blit(shot, captured, new Vector2(1f, -1f), new Vector2(0f, 1f));
            RenderTexture.ReleaseTemporary(shot);

            RestoreAfterCapture();

            backdrop.texture = captured;
            backdrop.enabled = true;
            if (group != null && IsOpen) group.alpha = 1f;
            captureRoutine = null;
        }

        private void HideForCapture()
        {
            hiddenAlphas.Clear();
            for (int i = 0; i < hideWhileCapturing.Count; i++)
            {
                CanvasGroup g = hideWhileCapturing[i];
                hiddenAlphas.Add(g != null ? g.alpha : 1f);
                if (g != null) g.alpha = 0f;
            }
        }

        private void RestoreAfterCapture()
        {
            for (int i = 0; i < hiddenAlphas.Count && i < hideWhileCapturing.Count; i++)
                if (hideWhileCapturing[i] != null) hideWhileCapturing[i].alpha = hiddenAlphas[i];
            hiddenAlphas.Clear();
        }

        private void ReleaseCapture()
        {
            if (captureRoutine != null) { StopCoroutine(captureRoutine); captureRoutine = null; RestoreAfterCapture(); }
            if (backdrop != null) { backdrop.texture = null; backdrop.enabled = false; }
            if (captured != null) { Destroy(captured); captured = null; }
        }
    }
}
