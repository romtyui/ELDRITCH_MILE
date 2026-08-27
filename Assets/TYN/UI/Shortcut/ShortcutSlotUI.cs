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
                 "美術稿方案 5 是「圖案突出＋顯示文字」，這個值就是突出多少")]
        public float hoverPushX = 10f;

        [Min(0f)] public float pushSeconds = 0.12f;

        public event Action<ShortcutSlotUI> OnHoverChanged;
        public event Action<ShortcutSlotUI> OnClicked;

        public ItemData Item { get; private set; }
        public int Count { get; private set; }
        public bool IsHovered { get; private set; }

        private Vector2 homePos;
        private Coroutine push;

        public void Bind(ItemData item, int count)
        {
            Item = item;
            Count = count;

            if (icon != null)
            {
                icon.sprite = item != null ? item.icon : null;
                icon.enabled = icon.sprite != null;
            }

            if (countText != null)
            {
                bool many = count > 1;
                countText.gameObject.SetActive(many);
                if (many) countText.text = count.ToString();
            }

            if (frame != null) frame.color = normalTint;

            homePos = ((RectTransform)transform).anchoredPosition;
            gameObject.SetActive(true);
        }

        // ==========================================
        public void OnPointerEnter(PointerEventData eventData)
        {
            IsHovered = true;
            if (frame != null) frame.color = hoverTint;
            StartPush(hoverPushX);
            OnHoverChanged?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
            if (frame != null) frame.color = normalTint;
            StartPush(0f);
            OnHoverChanged?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData) => OnClicked?.Invoke(this);

        private void StartPush(float dx)
        {
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
