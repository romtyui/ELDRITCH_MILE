using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EldritchMile.UI.ProbabilityDialogue
{
    using EldritchMile.Core.ProbabilityDialogue;

    /// <summary>
    /// 一張機率卡。**拖出手牌區 = 使用**（規格 §10）。
    ///
    /// 【為什麼點擊也能出牌】規格只寫了拖曳，但拖曳在觸控／不同解析度下容易失手，
    /// 而且測試時很難操作。點擊當成同一個動作，兩條路走到同一支 `Play()` ——
    /// 不會有「兩種出牌方式行為不一致」的問題。
    /// </summary>
    public class ProbabilityCardUI : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [Header("元件")]
        public Image artwork;
        public Image frame;
        public TextMeshProUGUI valueText;
        public TextMeshProUGUI nameText;
        public CanvasGroup canvasGroup;

        [Header("外觀")]
        public string valueFormat = "+{0}";

        [Tooltip("拖曳時的透明度")]
        [Range(0f, 1f)] public float dragAlpha = 0.6f;

        [Tooltip("往上拖超過這個距離（像素）就算出牌。\n" +
                 "太小會誤觸，太大會覺得拖不動")]
        [Min(0f)] public float playDistance = 90f;

        /// (卡, 這張 UI)
        public event Action<ProbabilityCardUI> OnPlayRequested;

        /// 拖曳／hover 時通知外面「這張是什麼顏色」，用來亮同色回答
        public event Action<ProbabilityCardUI, bool> OnAimChanged;

        /// 滑鼠進出。手牌區靠它決定哪一張要上浮（重排由手牌區負責，卡片自己不動位置）
        public event Action<ProbabilityCardUI, bool> OnHoverChanged;

        /// <summary>
        /// 視覺層。**上浮只動這一層，根物件永遠留在原位。**
        ///
        /// 【為什麼不直接把根物件往上移】游標會被從卡片底下抽走 →
        /// exit → 落下 → 又 enter → 又上浮，在卡片下緣會瘋狂閃爍。
        /// 這是 `ExploreCardDrag.SetLift` 早就踩過並解掉的同一個坑。
        /// </summary>
        private RectTransform visualRoot;

        /// <summary>由手牌區在生成後指定（見 `ProbabilityDialogueView.BuildVisualRoot`）。</summary>
        public void SetVisualRoot(RectTransform root) { visualRoot = root; }

        /// <summary>上浮。0 = 回到原位。</summary>
        public void SetLift(float lift)
        {
            if (visualRoot == null) return;
            visualRoot.anchoredPosition = new Vector2(0f, lift);
        }

        public bool IsDragging { get { return dragging; } }

        public CardDataExplore Data { get; private set; }

        private Vector2 dragStart;
        private Vector3 homePosition;
        private bool dragging;
        private bool spent;

        /// <summary>
        /// 綁一張**探索牌**。機率對話沒有自己的牌型 —— 見 `ProbabilityCardRules`。
        ///
        /// 【牌面是兩層，不是一張圖】美術是這樣分的：
        ///   · `artworkSprite`   —— 牌面（數字 0/20/40/…）。**0~100 的圖面都一樣**，
        ///                          差別只在印的數字
        ///   · `cardFrameSprite` —— 卡框，**屬性就體現在這裡**（本我紅／超我藍／自我綠）
        ///
        /// 所以框**不要染色** —— 顏色已經畫在圖裡了。染下去會把美術蓋掉。
        /// </summary>
        public void Bind(CardDataExplore card)
        {
            Data = card;
            spent = false;

            CardVisualDataExplore vis = card != null ? card.visualData : null;

            if (artwork != null)
            {
                artwork.sprite = vis != null ? vis.artworkSprite : null;
                artwork.enabled = artwork.sprite != null;
            }

            if (frame != null)
            {
                frame.sprite = vis != null ? vis.cardFrameSprite : null;

                // ⚠️ **不要用 frame.enabled 控制顯示。**
                //    frame 綁的是卡片根物件自己的 Image，也就是它的 raycastTarget ——
                //    停用它整張牌就點不到也拖不動了。沒有卡框圖時走 alpha=0。
                //
                // ⚠️ 屬性色是**畫在卡框圖裡**的（本我紅／超我藍／自我綠），
                //    不是用 tint 疊出來的 —— 所以 RGB 保持白色，染色會讓美術偏色
                frame.enabled = true;
                frame.color = new Color(1f, 1f, 1f, frame.sprite != null ? 1f : 0f);
            }

            if (valueText != null)
                valueText.text = string.Format(valueFormat, ProbabilityCardRules.ValueOf(card));

            if (nameText != null && card != null)
                nameText.text = string.IsNullOrEmpty(card.cardName) ? card.cardId : card.cardName;

            // ⚠️ **這裡不能抓 homePosition。**
            //
            // 這張牌住在 HorizontalLayoutGroup 底下，而 Layout 是在**影格結尾**才排的 ——
            // Bind() 當下抓到的還是 prefab 的原始值 (0,0)。
            // 容器如果當下是隱形的（「講完話才出現打牌」那段），排版更是根本沒跑過。
            //
            // 拿那個值當「原位」，拖回去就會飛到容器原點。
            // 改成第一次要用到的時候才抓（EnsureHome），那時排版一定跑完了。
            // 跟 ShortcutSlotUI 修過的是同一個坑。
            homeCaptured = false;
            SetAlpha(1f);
        }

        /// <summary>記住「本來的位置」。**只抓一次** —— 拖到一半再抓會把位移當成原位。</summary>
        private void EnsureHome()
        {
            if (homeCaptured) return;
            homePosition = transform.localPosition;
            homeCaptured = true;
        }

        private bool homeCaptured;

        // ==========================================
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (spent) return;
            OnHoverChanged?.Invoke(this, true);

            // hover 就把同屬性的回答亮起來 —— 跟探索那邊 hover 手牌會顯示成功率同一個意思
            OnAimChanged?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverChanged?.Invoke(this, false);

            // ⚠️ 拖曳中不要熄掉。拖出卡片範圍時會收到 exit，
            //    那時玩家正在瞄準，把亮起的回答熄掉等於把提示收走
            if (!dragging) OnAimChanged?.Invoke(this, false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 拖曳結束也會送一次 click，用 dragging 擋掉避免出兩次牌
            if (dragging || spent) return;
            Play();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (spent) return;
            dragging = true;
            dragStart = eventData.position;
            EnsureHome();
            SetAlpha(dragAlpha);
            OnAimChanged?.Invoke(this, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (spent || !dragging) return;
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (spent) { dragging = false; return; }

            float dy = eventData.position.y - dragStart.y;
            dragging = false;
            OnAimChanged?.Invoke(this, false);

            if (dy >= playDistance)
            {
                Play();
                return;
            }

            // 規格 §10：拖回 hand 視為 Cancel，不使用
            EnsureHome();
            transform.localPosition = homePosition;
            SetAlpha(1f);
        }

        private void Play()
        {
            if (spent) return;
            spent = true;                      // 規格 R7：一張牌只能用一次
            OnPlayRequested?.Invoke(this);
        }

        private void SetAlpha(float a)
        {
            if (canvasGroup != null) canvasGroup.alpha = a;
        }

        /// <summary>出牌被拒絕（例如已經在 Resolving）時退回原位。</summary>
        public void ReturnHome()
        {
            spent = false;
            dragging = false;
            EnsureHome();
            transform.localPosition = homePosition;
            SetAlpha(1f);
        }
    }
}
