using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

namespace EldritchMile.Shop
{
    using EldritchMile.Core;

    /// <summary>
    /// 點了貨架上的商品之後跳出來的「要買嗎？」視窗。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼要有這一步】（2026-09-23 美術改版）
    /// 舊流程是**點一下就成交** —— 扣錢、進背包，中間沒有任何確認。
    /// 誤觸就是直接花錢，而且玩家沒機會看清楚自己買的是什麼。
    /// 新流程照美術的示意圖：點商品 → 跳視窗看清楚 → YES 才扣錢。
    ///
    /// 【它自己有一層畫布】`Canvas` + `overrideSorting`，排序 550。
    /// 比 Canvas_HUD（400）高，所以快捷欄、EXIT 都會被蓋住 ——
    /// 這是故意的：視窗開著的時候不應該還能按到別的東西。
    /// 滿版的 Dimmer 也會吃掉所有點擊，貨架點不到。
    ///
    /// 【商品長什麼樣】分兩種：
    ///   · 武器（`grantsCard` 有東西）→ 疊出卡牌（卡面／武器／卡框三層）
    ///   · 其他（遺物、食物）→ 直接顯示彩色圖（`ShelfIcon`）
    /// 卡牌三層的相對比例抄自 Romtyui 的 `card_template`，但**這裡不壓扁** ——
    /// 戰鬥裡那個 prefab 把 768x1166 的卡框顯示成接近正方形，
    /// 美術的示意圖是照卡框原本的長寬比畫的，所以這裡用原比例。
    ///
    /// 【X 為什麼沒有自己的圖】美術給的 `確認框.png` 把黑框與 X **畫在同一張**，
    /// 而且 X 的圓環下半壓在黑框上 —— 切成矩形一定會帶到框的像素，接縫看得出來。
    /// 所以整張當成一個背景，X 的位置只疊一塊**透明的點擊區**（`closeButton`）。
    /// 代價是 X 沒有 hover／按下的回饋；同樣的動作 NO 有圖，可以接受。
    /// 之後美術若另外給一張獨立的 X，把那塊點擊區換成 Image 就好，位置不用動。
    /// </summary>
    public class ShopPurchasePanelUI : MonoBehaviour
    {
        [Header("元件")]
        public CanvasGroup group;

        [Tooltip("滿版的吃點擊層。**不能拿掉** —— 沒有它玩家可以在視窗開著時點到貨架")]
        public Image dimmer;

        [Header("商品外觀")]
        [Tooltip("武器卡的三層疊在這個節點底下。不是武器時整個關掉")]
        public RectTransform cardRoot;

        public Image cardFaceImage;
        public Image cardArtworkImage;
        public Image cardFrameImage;

        [Tooltip("收藏品／遺物擺成一張「牌」（底圖＋遺物圖＋外框）。留空則退回下面那張單圖")]
        public EldritchMile.UI.RelicCardView relicCard;

        [Tooltip("既不是武器也不是遺物時顯示的彩色圖（食物、補給）。用 ShelfIcon，不是持有欄的白線版")]
        public Image plainImage;

        [Header("文字")]
        [Tooltip("商品名。**這裡以前放的是店主的名字** —— 2026-09-30 改成品名：\n\n"
                 + "視窗的主角是商品，玩家要先看到自己在買什麼；店主是誰旁邊的氣泡已經在講了")]
        [FormerlySerializedAs("speakerText")]
        public TextMeshProUGUI titleText;

        public TextMeshProUGUI priceText;

        [Tooltip("商品說明。內容跟貨架 hover 的說明框同一份（ItemData.description）——\n"
                 + "兩邊講不一樣的話就會有兩個真相，改一邊忘了另一邊是遲早的事")]
        public TextMeshProUGUI bodyText;

        [Header("按鈕")]
        public Button yesButton;
        public Button noButton;

        [Tooltip("右上角的 X。跟 NO 是同一件事，只是位置不同")]
        public Button closeButton;

        [Header("動作")]
        [Min(0f)] public float fadeSeconds = 0.12f;

        [Tooltip("自己這層畫布的排序。要比 Canvas_HUD（400）高，視窗才蓋得住快捷欄與 EXIT")]
        public int sortingOrder = 550;

        public bool IsOpen { get; private set; }

        private Action onYes;
        private Action onNo;
        private Coroutine fade;

        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();

            // ⚠️ `overrideSorting` 要在**執行時**開。
            //    prefab 資產裡沒有上層畫布，Unity 會把它自動關掉 ——
            //    存檔時看起來設好了，載進場景還是 false。
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = sortingOrder;
            }

            if (yesButton != null) yesButton.onClick.AddListener(HandleYes);
            if (noButton != null) noButton.onClick.AddListener(HandleNo);
            if (closeButton != null) closeButton.onClick.AddListener(HandleNo);

            HideImmediate();
        }

        private void OnDestroy()
        {
            if (yesButton != null) yesButton.onClick.RemoveListener(HandleYes);
            if (noButton != null) noButton.onClick.RemoveListener(HandleNo);
            if (closeButton != null) closeButton.onClick.RemoveListener(HandleNo);
        }

        /// <summary>
        /// 打開視窗。
        /// </summary>
        /// <param name="canAfford">
        /// 付不起的時候 YES 會變灰且按不下去 —— **視窗照樣打開**。
        /// 直接什麼都不發生的話，玩家會以為是點壞了。
        /// </param>
        public void Open(ItemData data, int price, string title, string body, bool canAfford,
                         Action yes, Action no)
        {
            onYes = yes;
            onNo = no;

            if (titleText != null) titleText.text = title ?? "";
            if (priceText != null) priceText.text = price.ToString();
            if (bodyText != null) bodyText.text = body ?? "";

            ShowVisual(data);

            if (yesButton != null)
            {
                yesButton.interactable = canAfford;
                var g = yesButton.GetComponent<CanvasGroup>();
                if (g == null) g = yesButton.gameObject.AddComponent<CanvasGroup>();
                g.alpha = canAfford ? 1f : 0.4f;
            }

            IsOpen = true;
            gameObject.SetActive(true);
            StartFade(1f, true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            onYes = null;
            onNo = null;
            StartFade(0f, false);
        }

        /// <summary>不播動畫直接收起來。轉場用 —— 動畫播不完會把殘影帶到下一個畫面。</summary>
        public void HideImmediate()
        {
            IsOpen = false;
            onYes = null;
            onNo = null;
            if (fade != null) { StopCoroutine(fade); fade = null; }
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            gameObject.SetActive(false);
        }

        // ==========================================
        private void HandleYes()
        {
            Action a = onYes;
            Close();
            if (a != null) a();
        }

        private void HandleNo()
        {
            Action a = onNo;
            Close();
            if (a != null) a();
        }

        /// <summary>
        /// 三種長相：武器疊戰鬥卡、收藏品疊遺物牌、其餘（食物、補給）單張彩圖。
        /// </summary>
        private void ShowVisual(ItemData data)
        {
            CardData card = data != null ? data.grantsCard : null;
            CardVisualData vis = card != null ? card.visualData : null;

            bool asCard = vis != null;
            bool asRelic = !asCard && relicCard != null && data != null && data.HasTag("Curio");

            if (cardRoot != null) cardRoot.gameObject.SetActive(asCard);
            if (relicCard != null && !asRelic) relicCard.Hide();
            if (plainImage != null) plainImage.gameObject.SetActive(!asCard && !asRelic);

            if (asCard)
            {
                Apply(cardFaceImage, vis.cardFaceSprite);
                Apply(cardArtworkImage, vis.artworkSprite);
                Apply(cardFrameImage, vis.cardFrameSprite);
                return;
            }

            if (asRelic)
            {
                relicCard.Show(data.ShelfIcon);
                return;
            }

            if (plainImage != null)
            {
                Sprite s = data != null ? data.ShelfIcon : null;
                plainImage.sprite = s;
                plainImage.enabled = s != null;
            }
        }

        private static void Apply(Image img, Sprite sprite)
        {
            if (img == null) return;
            img.sprite = sprite;
            img.enabled = sprite != null;
        }

        // ==========================================
        private void StartFade(float target, bool interactive)
        {
            if (fade != null) StopCoroutine(fade);

            if (group == null)
            {
                gameObject.SetActive(target > 0f);
                return;
            }

            group.blocksRaycasts = interactive;
            group.interactable = interactive;
            fade = StartCoroutine(FadeTo(target));
        }

        private IEnumerator FadeTo(float target)
        {
            float from = group.alpha;

            if (fadeSeconds > 0f)
            {
                float t = 0f;
                while (t < fadeSeconds)
                {
                    // 不吃 timeScale：商店不暫停，但這個視窗的節奏不該被別人影響
                    t += Time.unscaledDeltaTime;
                    group.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / fadeSeconds));
                    yield return null;
                }
            }

            group.alpha = target;
            fade = null;

            // 關完才真的停用，不然淡出會被 SetActive(false) 切斷
            if (target <= 0f) gameObject.SetActive(false);
        }
    }
}
