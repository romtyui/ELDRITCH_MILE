using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EldritchMile.UI.Shortcut
{
    using EldritchMile.Core;

    /// <summary>
    /// 快捷欄裡的一格。滑鼠移上去顯示說明（美術稿的方案 4／5）。
    /// </summary>
    public class ShortcutSlotUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("元件")]
        public Image icon;
        public Image frame;

        [Tooltip("同一個道具有多個時顯示數量。只有 1 個時會自動隱藏")]
        public TextMeshProUGUI countText;

        [Header("外觀")]
        public Color normalTint = new Color(1f, 1f, 1f, 0.75f);

        [Tooltip("滑鼠移上去時。美術稿的「圖案突出」")]
        public Color hoverTint = Color.white;

        [Tooltip("hover 時往外推幾像素。0 = 不推。\n" +
                 "⚠️ 快捷欄貼在畫面**右側**，所以往外＝往左＝**負值**。\n" +
                 "美術稿方案 5 是「圖案突出＋顯示文字」，這個值就是突出多少")]
        public float hoverPushX = -14f;

        [Min(0f)] public float pushSeconds = 0.12f;

        public event Action<ShortcutSlotUI> OnHoverChanged;
        public event Action<ShortcutSlotUI> OnClicked;

        public ItemData Item { get; private set; }
        public int Count { get; private set; }
        public bool IsHovered { get; private set; }

        private Vector2 homePos;
        private bool homeCaptured;
        private Coroutine push;

        /// <summary>這一格有沒有外框圖。沒有的話外框要保持透明但仍然收 raycast。</summary>
        private bool frameVisible;

        /// <param name="fallbackIcon">
        /// 道具自己沒有圖時退而用這張。
        ///
        /// ⚠️ **目前 26 個道具沒有一個填了 icon**，而下面是 `icon.enabled = 有圖`——
        /// 沒有這個退路的話整條欄就是一排看不見的空框，
        /// 看起來會像「UI 壞了」而不是「道具還沒有圖」。
        ///
        /// 食物共用針管、遺物共用卡冊**本來就是美術稿的分類做法**，不是將就：
        /// 那兩張圖畫的是「容器」，不是某一個特定道具。
        /// </param>
        /// <param name="frameSprite">格子外框。留空就沿用 prefab 上原本的。</param>
        public void Bind(ItemData item, int count, Sprite fallbackIcon = null, Sprite frameSprite = null)
        {
            Item = item;
            Count = count;

            // ⚠️ **不要用 frame.enabled = false 讓外框隱形。**
            //
            // `frame` 綁的是這一格**根物件自己的 Image**，而那個 Image 就是
            // 這一格的 raycastTarget —— 停用它整格就點不到了（我們修過同一個坑）。
            // 要隱形就把 alpha 調 0：畫面上看不見，但事件照收。
            frameVisible = frameSprite != null;
            if (frame != null) frame.sprite = frameSprite;

            if (icon != null)
            {
                Sprite s = item != null && item.icon != null ? item.icon : fallbackIcon;
                icon.sprite = s;
                icon.enabled = s != null;
            }

            if (countText != null)
            {
                bool many = count > 1;
                countText.gameObject.SetActive(many);
                if (many) countText.text = count.ToString();
            }

            ApplyTint(normalTint);

            // ⚠️ **這裡不能抓 homePos。**
            //
            // 這一格住在 VerticalLayoutGroup 底下，而 Layout 是在**影格結尾**才排的 ——
            // Bind() 當下抓到的還是 prefab 的原始值（通常是 0,0）。
            // 拿那個值當「原位」，hover 離開時就會飛到容器頂端回不來。
            //
            // 改成第一次 hover 時才抓（那時排版一定跑完了），見 EnsureHome()。
            homeCaptured = false;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 記住「本來的位置」。**只在還沒被推出去的時候抓** ——
        /// 推出去之後再抓就會把推出的位移當成原位，於是每次 hover 都往外跑一點。
        /// </summary>
        private void EnsureHome()
        {
            if (homeCaptured) return;
            homePos = ((RectTransform)transform).anchoredPosition;
            homeCaptured = true;
        }

        // ==========================================
        public void OnPointerEnter(PointerEventData eventData)
        {
            EnsureHome();          // ⚠️ 一定要在推出去之前
            IsHovered = true;
            ApplyTint(hoverTint);
            StartPush(hoverPushX);
            OnHoverChanged?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
            ApplyTint(normalTint);
            StartPush(0f);
            OnHoverChanged?.Invoke(this);
        }

        /// <summary>
        /// 套用 normal／hover 的染色。
        ///
        /// 【為什麼圖示也要染】原本只染外框 —— 那在沒有外框的欄（食物）
        /// 等於 **hover 完全沒有視覺回饋**，只剩位移。
        ///
        /// 【為什麼外框要另外算 alpha】沒有外框圖的時候，根物件的 Image
        /// 必須留著收 raycast（見 Bind 的說明），所以只能靠 alpha=0 隱形；
        /// 但 tint 會整個蓋掉 color，不在這裡補就會又冒出白方塊。
        /// </summary>
        private void ApplyTint(Color tint)
        {
            if (icon != null) icon.color = tint;

            if (frame == null) return;

            Color c = tint;
            if (!frameVisible) c.a = 0f;
            frame.color = c;
        }

        public void OnPointerClick(PointerEventData eventData) => OnClicked?.Invoke(this);

        private void StartPush(float dx)
        {
            EnsureHome();

            if (push != null) StopCoroutine(push);
            if (pushSeconds <= 0f)
            {
                ((RectTransform)transform).anchoredPosition = homePos + new Vector2(dx, 0f);
                return;
            }
            push = StartCoroutine(PushRoutine(dx));
        }

        private System.Collections.IEnumerator PushRoutine(float dx)
        {
            Vector2 from = ((RectTransform)transform).anchoredPosition;
            Vector2 to = homePos + new Vector2(dx, 0f);
            float t = 0f;
            while (t < pushSeconds)
            {
                t += Time.unscaledDeltaTime;
                ((RectTransform)transform).anchoredPosition =
                    Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / pushSeconds));
                yield return null;
            }
            ((RectTransform)transform).anchoredPosition = to;
            push = null;
        }
    }
}
