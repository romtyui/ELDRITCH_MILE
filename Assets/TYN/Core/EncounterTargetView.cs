using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace EldritchMile.Core
{
    /// <summary>
    /// 打牌環節中，互動對象在對話框裡的化身。
    ///
    /// 【為什麼需要它】原本卡片是拖到世界裡的寶箱上。三個問題：
    ///   1. 世界物件可能被對話框或其他東西蓋住，當拖曳目標不可靠
    ///   2. 要讓卡片打得到世界，就得讓 raycast 穿透對話框 —— 那又會變成
    ///      「打牌時還能點到背景」，互動邊界模糊
    ///   3. 機率標籤浮在世界物件頭上，位置受場景擺設影響，時常被遮
    ///
    /// 所以進入打牌環節後，互動主體整個搬進對話框：大圖是拖曳目標、
    /// 機率顯示在大圖頭上、壓黑層負責擋住世界。玩家的注意力與可點範圍一致。
    ///
    /// 【實作】本類別實作 IProbabilityTarget 但**不持有任何狀態** ——
    /// 屬性、衰減進度、判定結果全部轉發給真正的目標（世界裡的寶箱／NPC）。
    /// 它只負責「長什麼樣」與「接收點擊」。
    /// </summary>
    public class EncounterTargetView : MonoBehaviour, IProbabilityTarget,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// 玩家點了這個大圖。兩段式出牌的第二段接這裡。
        ///
        /// 用事件而非直接呼叫，是為了讓 Core 不必反過來認識 Explore ——
        /// 由 ExploreStageController 在生成時訂閱。
        /// </summary>
        public event Action<EncounterTargetView> OnClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;   // 拖曳放開時 Unity 也會送 click，要濾掉
            if (eventData.button != PointerEventData.InputButton.Left) return;   // 右鍵是離開
            OnClicked?.Invoke(this);
        }

        [Header("元件")]
        [Tooltip("顯示對象的大圖")]
        public Image image;

        [Tooltip("機率數字。**常駐顯示**：平常是目前的機率，手上指著一張牌時是打出這張牌的機率")]
        public TextMeshProUGUI previewLabel;

        [Tooltip("圖上方的一行字：容器的描述、這次判定的結果。對話框在打牌時是收起來的，文字放這裡（下方是手牌）")]
        public TextMeshProUGUI resultLabel;

        [Header("尺寸")]
        [Tooltip("大圖能佔用的最大範圍。實際尺寸會依圖的比例縮進這個範圍內，\n" +
                 "不是拉伸填滿 —— 所以寬扁的木箱與細長的立繪都不會變形")]
        public Vector2 maxSize = new Vector2(420f, 520f);

        [Tooltip("勾選則小圖不會被放大，維持原始像素尺寸（適合像素美術）")]
        public bool neverUpscale = false;

        [Header("機率數字")]
        [Tooltip("沒有指著牌時顯示的機率（百分比）。寶箱目前沒有基礎機率，所以是 00")]
        [Range(0, 100)] public int baseRatePercent = 0;

        [Tooltip("數字跳動的秒數。0 ＝ 直接換")]
        [Min(0f)] public float countSeconds = 0.12f;

        public Color normalColor = Color.white;
        public Color immuneColor = new Color(0.5f, 0.5f, 0.5f);

        [Tooltip("屬性完全不合時顯示什麼。不要用「0%」—— 要讓玩家看得出是屬性問題而非運氣差")]
        public string immuneText = "X";

        /// 真正的判定對象。狀態的唯一真相在它身上。
        public IProbabilityTarget Source { get; private set; }

        public void Bind(IProbabilityTarget source, Sprite closeUp)
        {
            Source = source;

            if (closeUp == null)
            {
                // 沒有特寫圖 → Image 沒東西可畫。若順手把 Image 停用，
                // 它連 raycast 都收不到，於是「看不見」又「點不到」，
                // 而且完全不會有錯誤訊息 —— 非常難查，所以在這裡明講。
                Debug.LogWarning(
                    $"[打牌] 「{(source != null ? source.DisplayName : name)}」沒有設定 Close Up Sprite。\n" +
                    "對話框裡不會有圖，卡片也就沒有東西可以打。\n" +
                    "請在該互動物件的 Inspector 設定 Close Up Sprite。",
                    this
                );
            }

            if (image != null)
            {
                image.sprite = closeUp;

                // ⚠️ 大圖是 UI，靠 GraphicRaycaster + raycastTarget 接收拖放，
                //    **不是** Collider2D（那是世界物件用的）。
                //    而且 Image 必須保持 enabled —— 停用的 Graphic 收不到任何 raycast。
                image.enabled = true;
                image.raycastTarget = true;

                // 沒有圖時用全透明避免畫出白方塊，但仍保有可點區域
                image.color = closeUp != null
                    ? new Color(image.color.r, image.color.g, image.color.b, 1f)
                    : new Color(image.color.r, image.color.g, image.color.b, 0f);

                FitToSprite(closeUp);
            }

            ShowResult(null);
            shownPercent = baseRatePercent;
            WriteNumber(shownPercent);
        }

        /// <summary>圖上方那行字。留空 ＝ 收掉。</summary>
        public void ShowResult(string line)
        {
            if (resultLabel == null) return;

            resultLabel.text = line ?? "";
            resultLabel.gameObject.SetActive(!string.IsNullOrEmpty(line));

            if (image != null)
                resultLabel.rectTransform.anchoredPosition = new Vector2(0f, image.rectTransform.sizeDelta.y * 0.5f + resultGap);
        }

        [Tooltip("那行字離圖的上緣多遠")]
        public float resultGap = 44f;

        /// <summary>
        /// 依圖的比例調整 RectTransform，讓它「就是那張圖的大小」。
        ///
        /// 【為什麼不用 Preserve Aspect】那個只是把圖 letterbox 在原本的框裡，
        /// RectTransform 仍是框的尺寸。結果是：
        ///   · 點擊範圍包含左右（或上下）的空白邊
        ///   · 錨在頂端的機率標籤會浮在空白上方，而不是貼著圖
        /// 直接改 RectTransform 就沒有這些落差，寬扁的木箱與細長的立繪都各自合身。
        /// </summary>
        private void FitToSprite(Sprite sprite)
        {
            if (sprite == null) return;

            var rt = image.rectTransform;
            float w = sprite.rect.width;
            float h = sprite.rect.height;
            if (w <= 0f || h <= 0f) return;

            float scale = Mathf.Min(maxSize.x / w, maxSize.y / h);
            if (neverUpscale) scale = Mathf.Min(scale, 1f);

            rt.sizeDelta = new Vector2(w * scale, h * scale);

            // 尺寸是自己算的，preserveAspect 就沒有必要了；
            // 留著也不會錯，但關掉比較不會讓人誤以為是它在負責。
            image.preserveAspect = false;
        }

        // ==========================================
        // IProbabilityTarget —— 全部轉發，不自己存狀態
        // ==========================================
        public string DisplayName => Source != null ? Source.DisplayName : name;

        public ExploreAttribute Attribute =>
            Source != null ? Source.Attribute : ExploreAttribute.None;

        public float CurrentDecayMultiplier =>
            Source != null ? Source.CurrentDecayMultiplier : 1f;

        public void ApplyDecay(float step)
        {
            Source?.ApplyDecay(step);
        }

        /// <summary>轉給真正的目標 —— 衰減的真相在 Source 身上，這裡只是一張臉。</summary>
        public void ResetDecay()
        {
            Source?.ResetDecay();
        }

        public void OnCheckResult(bool success, float usedRate)
        {
            // 不管成敗都彈一下：這張牌確實打在箱子上了
            PlayBounce();

            // 再讓真正的目標處理結果（開箱、給道具、變更狀態）
            Source?.OnCheckResult(success, usedRate);
        }

        public void ShowPreview(float rate, Effectiveness eff)
        {
            if (previewLabel == null) return;

            if (eff == Effectiveness.None)
            {
                StopCount();
                previewLabel.text = immuneText;
                previewLabel.color = immuneColor;
                return;
            }

            CountTo(Mathf.RoundToInt(rate * 100f));
        }

        /// <summary>沒有指著牌了。**數字不收掉**，回到目前的機率（骷髏頭的眼眶裡一直有數字）。</summary>
        public void HidePreview()
        {
            CountTo(baseRatePercent);
        }

        private int shownPercent;
        private Coroutine countRoutine;

        private void StopCount()
        {
            if (countRoutine != null) { StopCoroutine(countRoutine); countRoutine = null; }
        }

        private void WriteNumber(int percent)
        {
            if (previewLabel == null) return;
            previewLabel.text = percent.ToString("00");
            previewLabel.color = normalColor;
        }

        private void CountTo(int percent)
        {
            StopCount();

            if (countSeconds <= 0f || !isActiveAndEnabled)
            {
                shownPercent = percent;
                WriteNumber(percent);
                return;
            }

            countRoutine = StartCoroutine(CountRoutine(shownPercent, percent));
        }

        private IEnumerator CountRoutine(int from, int to)
        {
            for (float t = 0f; t < countSeconds; t += Time.unscaledDeltaTime)
            {
                shownPercent = Mathf.RoundToInt(Mathf.Lerp(from, to, t / countSeconds));
                WriteNumber(shownPercent);
                yield return null;
            }

            shownPercent = to;
            WriteNumber(to);
            countRoutine = null;
        }

        [Header("出牌時的 Q 彈")]
        [Tooltip("整段搖多久")]
        [Min(0.05f)] public float bounceSeconds = 0.6f;

        [Tooltip("每秒壓扁／拉長幾次")]
        public float bounceFrequency = 5f;

        [Tooltip("壓扁的幅度。0.1 ＝ 寬 +10%、高 -10%")]
        public float bounceSquash = 0.1f;

        [Tooltip("左右搖晃的角度")]
        public float bounceTilt = 3.5f;

        private Coroutine bounceRoutine;

        /// <summary>Q 彈地搖一下。動整個化身（圖、數字、那行字一起），轉軸是畫面上圖的中心。</summary>
        public void PlayBounce()
        {
            if (!isActiveAndEnabled) return;
            if (bounceRoutine != null) StopCoroutine(bounceRoutine);
            bounceRoutine = StartCoroutine(BounceRoutine());
        }

        private IEnumerator BounceRoutine()
        {
            for (float t = 0f; t < bounceSeconds; t += Time.unscaledDeltaTime)
            {
                float fade = 1f - t / bounceSeconds;
                fade *= fade;

                float squash = bounceSquash * fade * Mathf.Sin(t * bounceFrequency * Mathf.PI * 2f);
                transform.localScale = new Vector3(1f + squash, 1f - squash, 1f);
                transform.localRotation = Quaternion.Euler(0f, 0f, bounceTilt * fade * Mathf.Sin(t * bounceFrequency * Mathf.PI));
                yield return null;
            }

            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            bounceRoutine = null;
        }

        public void OnAttemptsExhausted()
        {
            Source?.OnAttemptsExhausted();
        }

        [Header("被瞄準時")]
        [Tooltip("卡片瞄準這個目標時整張圖乘上的顏色。預設是稍微變暗。\n" +
                 "不要太搶眼 —— 圖上方還有機率數字要看")]
        public Color targetedTint = new Color(0.72f, 0.72f, 0.72f, 1f);

        private bool baseColorCached;
        private Color baseColor = Color.white;

        public void SetTargeted(bool targeted)
        {
            if (image == null) return;

            // 第一次才記底色。Bind() 會依有沒有圖調 alpha，所以要在那之後才抓
            if (!baseColorCached)
            {
                baseColorCached = true;
                baseColor = image.color;
            }

            image.color = targeted
                ? new Color(baseColor.r * targetedTint.r,
                            baseColor.g * targetedTint.g,
                            baseColor.b * targetedTint.b,
                            baseColor.a)
                : baseColor;
        }

        // ==========================================
        // 兩段式出牌：手上有卡時滑過來也要有回饋
        // ==========================================
        public void OnPointerEnter(PointerEventData eventData)
        {
            DialogueEncounterController e = DialogueEncounterController.Instance;
            if (e != null && e.IsActive && e.HasArmedCard) e.SetAimed(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            DialogueEncounterController e = DialogueEncounterController.Instance;
            if (e != null && ReferenceEquals(e.AimedTarget, this)) e.ClearAimed();
        }
    }
}
