using System.Collections;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 全專案唯一的黑幕。
    ///
    /// 【為什麼要統一】舊架構有兩份互相搶控制權的黑幕：
    ///   PerspectiveMapGenerator.transitionFade（UIScene）
    ///   ExplorationManager.fadeCanvasGroup（ExploreScene）
    /// 靠「地圖淡到全黑 → 停手 → 探索場景自己從全黑淡出」的默契銜接，
    /// 任何一邊沒接到就永久黑屏。（設計文件 §3 病根 2）
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [Header("預設時長")]
        public float defaultDuration = 0.4f;

        private CanvasGroup canvasGroup;

        /// 轉場中。UI 應該在此期間拒絕輸入。
        public bool IsBlocking { get; private set; }

        public bool IsBlack => canvasGroup != null && canvasGroup.alpha >= 0.99f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            canvasGroup = GetComponent<CanvasGroup>();

            // 開場全黑，由第一個 Stage 負責淡出
            SetBlackImmediate(true);
        }

        public void SetBlackImmediate(bool black)
        {
            if (canvasGroup == null) return;

            canvasGroup.alpha = black ? 1f : 0f;
            canvasGroup.blocksRaycasts = black;
            IsBlocking = black;
        }

        public IEnumerator FadeToBlack(float duration = -1f)
        {
            yield return Fade(1f, duration);
        }

        public IEnumerator FadeFromBlack(float duration = -1f)
        {
            yield return Fade(0f, duration);
        }

        private IEnumerator Fade(float targetAlpha, float duration)
        {
            if (canvasGroup == null) yield break;

            if (duration < 0f) duration = defaultDuration;

            IsBlocking = true;
            canvasGroup.blocksRaycasts = true;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // SmoothStep 讓頭尾比較柔和
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;

            bool nowBlack = targetAlpha >= 0.99f;
            canvasGroup.blocksRaycasts = nowBlack;
            IsBlocking = nowBlack;
        }
    }
}
