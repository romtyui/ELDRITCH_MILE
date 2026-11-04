using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EldritchMile.UI.ProbabilityDialogue
{
    using EldritchMile.Core.ProbabilityDialogue;

    /// <summary>
    /// 一張機率卡。**拖出牌區 = 使用**（規格 §10），範圍見 `HandFanLayout.LeftHandZone`。
    /// （2026-11-02 拿掉點擊出牌：滑上去看機率時手滑點一下，一張牌就沒了）
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

        [Tooltip("牌區的上緣（佔畫面高度的比例）。由手牌區在生成時指定")]
        public float handZoneTopRatio = 0.34f;

        [Tooltip("拖出牌區（放開就會出牌）時放大幾倍")]
        public float armedScale = 1.12f;

        /// 打出去的那一下。由手牌區在生成時指定
        public PlayedCardMotion playedCard = new PlayedCardMotion();

        /// 放開了，要打出去。牌開始停住，這段期間手牌區要鎖住輸入
        public event Action<ProbabilityCardUI> OnPlayStarting;

        /// 停完了，真的結算
        public event Action<ProbabilityCardUI> OnPlayRequested;

        /// 拖曳／hover 時通知外面「這張是什麼顏色」，用來亮同色回答
        public event Action<ProbabilityCardUI, bool> OnAimChanged;

        /// 滑鼠進出。手牌區靠它決定哪一張要上浮（重排由手牌區負責，卡片自己不動位置）
        public event Action<ProbabilityCardUI, bool> OnHoverChanged;

        /// 開始／結束拖曳。拖曳中的牌不參與排列，拖回來要重排
        public event Action<ProbabilityCardUI, bool> OnDragChanged;

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

        /// <summary>被指到時浮起來。只動視覺層。</summary>
        public void SetPose(bool active, float hoverY, float hoverScale)
        {
            EldritchMile.UI.HandFanLayout.PoseVisual(visualRoot, transform as RectTransform, active, hoverY, hoverScale);
        }

        public bool IsDragging { get { return dragging; } }

        public CardDataExplore Data { get; private set; }

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

            SetAlpha(1f);
        }

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

        // 留著這個介面才擋得住點擊往父物件冒泡。點牌沒有效果
        public void OnPointerClick(PointerEventData eventData) { }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (spent) return;
            dragging = true;
            SetAlpha(dragAlpha);
            transform.SetAsLastSibling();
            transform.localRotation = Quaternion.identity;
            SetPose(false, 0f, 1f);
            transform.position = eventData.position;
            OnDragChanged?.Invoke(this, true);
            OnAimChanged?.Invoke(this, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (spent || !dragging) return;
            transform.position = eventData.position;

            // 拖出牌區了：放大、不透明 ——「現在放開就打出去了」
            bool outside = EldritchMile.UI.HandFanLayout.LeftHandZone(eventData.position, handZoneTopRatio);
            transform.localScale = Vector3.one * (outside ? armedScale : 1f);
            SetAlpha(outside ? 1f : dragAlpha);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (spent) { dragging = false; return; }

            dragging = false;

            // 打出去的話亮著的回答與預覽留著，結算那一刻才收
            if (EldritchMile.UI.HandFanLayout.LeftHandZone(eventData.position, handZoneTopRatio))
            {
                Play();
                return;
            }

            OnAimChanged?.Invoke(this, false);

            // 規格 §10：拖回 hand 視為 Cancel，不使用
            transform.localScale = Vector3.one;
            SetAlpha(1f);
            OnDragChanged?.Invoke(this, false);
        }

        private void Play()
        {
            if (spent) return;
            spent = true;                      // 規格 R7：一張牌只能用一次

            OnPlayStarting?.Invoke(this);
            StartCoroutine(PlayRoutine());
        }

        /// <summary>
        /// 打出去：停在放開的位置彈一下、停住 → 結算 → 淡出（順序照戰鬥）。
        /// 機率數字與進度條在「結算」那一刻才跑，玩家先看到牌打出去，才看到它的效果。
        /// </summary>
        private IEnumerator PlayRoutine()
        {
            yield return playedCard.PunchAndHold(transform);

            OnPlayRequested?.Invoke(this);

            if (!spent) yield break;           // 被 ReturnHome 退回來了

            yield return playedCard.FadeAway(transform, canvasGroup);

            Destroy(gameObject);
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
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;
            SetAlpha(1f);
            OnDragChanged?.Invoke(this, false);
        }
    }
}
