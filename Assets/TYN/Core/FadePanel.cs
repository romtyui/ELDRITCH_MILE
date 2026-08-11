using System.Collections;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 通用的淡入淡出面板。
    ///
    /// 【為什麼要有這個】MapBannerUI 已經在做同一件事（CanvasGroup 淡入淡出），
    /// 但那是地圖橫幅專用、還綁著標題與結束按鈕的邏輯。
    /// 「要探索其他的東西嗎？」這種確認視窗要跟 MapBanner 外觀一致，
    /// 但語意完全不同 —— 所以抽出共用的淡入行為，各自組合。
    ///
    /// 直接 SetActive 會硬切，跟地圖橫幅的柔和淡入放在一起會很突兀。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FadePanel : MonoBehaviour
    {
        [Header("時間")]
        public float fadeInDuration = 0.2f;
        public float fadeOutDuration = 0.2f;

        [Header("啟動狀態")]
        [Tooltip("勾選則遊戲開始時就是隱藏的")]
        public bool hiddenOnAwake = true;

        private CanvasGroup canvasGroup;
        private Coroutine current;

        public bool IsVisible { get; private set; }

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (hiddenOnAwake) HideImmediate();
        }

        public void HideImmediate()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            IsVisible = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// ⚠️ SetActive 必須在 StartCoroutine 之前，不能寫在協程裡面 ——
        /// 物件 inactive 時 StartCoroutine 會直接失敗，協程第一行根本跑不到。
        /// 而「面板是隱藏的」正是唯一會呼叫 Show() 的情況，寫反了就等於這個方法永遠沒用。
        /// </summary>
        public void Show()
        {
            // 物件在編輯器裡就設為停用時 Awake 不會執行，canvasGroup 仍是 null。
            // SetActive(true) 會同步觸發 Awake，所以補抓一次即可。
            gameObject.SetActive(true);
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            if (current != null) StopCoroutine(current);
            current = StartCoroutine(ShowRoutine());
        }

        public void Hide()
        {
            // 已經是隱藏的就不必再跑一次淡出協程 —— 在 inactive 物件上
            // StartCoroutine 會噴 "Coroutine couldn't be started"。
            if (!gameObject.activeInHierarchy)
            {
                HideImmediate();
                return;
            }

            if (current != null) StopCoroutine(current);
            current = StartCoroutine(HideRoutine());
        }

        public IEnumerator ShowRoutine()
        {
            gameObject.SetActive(true);
            IsVisible = true;

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            yield return FadeTo(1f, fadeInDuration);
        }

        public IEnumerator HideRoutine()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            yield return FadeTo(0f, fadeOutDuration);

            IsVisible = false;
            gameObject.SetActive(false);
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            float start = canvasGroup.alpha;
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                yield return null;
            }

            canvasGroup.alpha = target;
        }
    }
}
