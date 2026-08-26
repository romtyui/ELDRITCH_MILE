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
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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

        public ProbabilityCardData Data { get; private set; }

        private Vector2 dragStart;
        private Vector3 homePosition;
        private bool dragging;
        private bool spent;

        public void Bind(ProbabilityCardData card)
        {
            Data = card;
            spent = false;

            if (artwork != null && card != null && card.visual != null) artwork.sprite = card.visual;
            if (frame != null && card != null) frame.color = card.displayColor;
            if (valueText != null && card != null) valueText.text = string.Format(valueFormat, card.value);
            if (nameText != null && card != null)
                nameText.text = string.IsNullOrEmpty(card.displayName) ? card.cardId : card.displayName;

            homePosition = transform.localPosition;
            SetAlpha(1f);
        }

        // ==========================================
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
            homePosition = transform.localPosition;
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
            transform.localPosition = homePosition;
            SetAlpha(1f);
        }
    }
}
