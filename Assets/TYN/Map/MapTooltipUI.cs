using UnityEngine;
using TMPro;

namespace EldritchMile.Map
{
    /// <summary>
    /// 地圖節點的 hover 說明框。
    ///
    /// 【為什麼不直接用 Romtyui 的 TooltipUI】那一份是通用的（`TooltipEntry` 就是
    /// title + body），技術上可以共用 —— 但它不在 EventScene 裡，要用就得把對方的
    /// prefab 搬進我們的場景，等於讓地圖的外觀與生命週期綁在戰鬥那邊。
    /// 依專案的分工慣例（只動 `Assets/TYN/`），這種耦合不逕行建立。
    ///
    /// **若日後決定全遊戲統一一套 tooltip**，換掉的成本很小 ——
    /// `MapNodeUI` 只透過 `MapView.ShowNodeTooltip()` 這一個出口要求顯示，
    /// 改那一個方法的內容即可，節點本身不必動。
    ///
    /// 【定位方式】用 `CalculateRelativeRectTransformBounds` 取節點在 Canvas 座標下的
    /// 實際範圍，再往旁邊擺並夾在畫面內。不用滑鼠座標 —— 那樣框會跟著游標晃，
    /// 而且節點本身有大小，貼著節點比貼著游標穩定。
    /// </summary>
    public class MapTooltipUI : MonoBehaviour
    {
        /// <summary>說明框擺在哪。</summary>
        public enum PlacementMode
        {
            /// 貼在被 hover 的節點旁邊，位置由程式計算
            FollowNode = 0,

            /// 位置由編輯器擺好，程式不碰。適合固定在角落的資訊區
            Fixed = 1,
        }

        [Header("元件")]
        [Tooltip("要移動的整個框。通常就是本物件的 RectTransform")]
        public RectTransform panel;

        [Tooltip("淡入淡出用。留空則直接 SetActive")]
        public CanvasGroup canvasGroup;

        public TextMeshProUGUI titleText;

        [Tooltip("說明內文。留空則只顯示標題")]
        public TextMeshProUGUI bodyText;

        [Header("定位")]
        [Tooltip("Follow Node ＝ 貼在被 hover 的節點旁邊，程式自動算位置並夾在畫面內。\n\n" +
                 "Fixed ＝ **位置完全由你在編輯器擺好，程式不碰它**。\n" +
                 "適合擺在右下角這種空間大、不會擋到地圖的固定資訊區。\n" +
                 "選 Fixed 之後，下面的 Offset 與 Screen Padding 都不會被用到")]
        public PlacementMode placement = PlacementMode.FollowNode;

        [Tooltip("與節點之間的間距。**僅 Follow Node 模式使用**")]
        public Vector2 offset = new Vector2(24f, 0f);

        [Tooltip("與畫面邊緣至少保留多少距離。**僅 Follow Node 模式使用**")]
        public Vector2 screenPadding = new Vector2(16f, 16f);

        [Header("閒置狀態（固定面板適用）")]
        [Tooltip("沒有 hover 任何節點時，**保留框、只換文字**，而不是整個消失。\n\n" +
                 "固定在角落的資訊面板建議勾選 —— 一個時有時無的框會讓那塊區域一直在閃，\n" +
                 "而且玩家不會知道那裡本來有東西。\n\n" +
                 "Follow Node 模式請維持不勾（跟著節點的框當然要跟著消失）")]
        public bool keepFrameWhenIdle = false;

        [Tooltip("閒置時顯示的標題")]
        public string idleTitle = "";

        [TextArea(2, 4)]
        [Tooltip("閒置時顯示的說明，例如「把游標移到節點上查看」")]
        public string idleBody = "把游標移到節點上查看。";

        [Header("動態")]
        [Tooltip("淡入時間。0 = 直接出現。\n" +
                 "勾了 Keep Frame When Idle 時不會用到 —— 框一直在，只有文字在換")]
        [Min(0f)] public float fadeDuration = 0.12f;

        private RectTransform canvasRect;
        private Coroutine fadeRoutine;

        /// <summary>
        /// 總開關。地圖收起時關掉，展開時打開。
        ///
        /// 【為什麼需要它，而不是只呼叫 ForceHide()】地圖是**滑出畫面**的，
        /// 滑走的瞬間節點會離開游標 → Unity 送出 `OnPointerExit` → `Hide()` →
        /// 而 `Keep Frame When Idle` 又把框重新開起來。
        ///
        /// 也就是說「關掉」這個動作會被一個**比它晚到的滑鼠事件**復活。
        /// 所以需要一個狀態，讓關閉之後所有的顯示要求都失效，直到地圖再次展開。
        /// </summary>
        private bool suppressed;

        private void Awake()
        {
            if (panel == null) panel = transform as RectTransform;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvasRect = canvas.GetComponent<RectTransform>();

            // 固定面板要在一開始就把閒置文字擺好，否則玩家開地圖會先看到上一次的殘留內容
            if (keepFrameWhenIdle) ShowIdle();
            else HideImmediate();
        }

        /// <summary>
        /// 地圖展開／收起時呼叫。收起後所有顯示要求都會被忽略，
        /// 直到再次展開 —— 這是「總開關」。
        /// </summary>
        public void SetSuppressed(bool value)
        {
            suppressed = value;

            if (suppressed)
            {
                ForceHide();
            }
            else if (keepFrameWhenIdle)
            {
                ShowIdle();
            }
        }

        public void Show(string title, string body, RectTransform target)
        {
            if (suppressed || panel == null) return;
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body)) return;

            // ⚠️ Fixed 模式不需要 target。Follow Node 沒有 target 就無從定位，直接放棄
            if (placement == PlacementMode.FollowNode && target == null) return;

            SetTexts(title, body);

            panel.gameObject.SetActive(true);

            // 先讓 layout 算出真正的尺寸，再定位 —— 尺寸沒定下來就算位置會用到上一則的大小。
            // Fixed 模式雖然不定位，但若面板掛了 ContentSizeFitter 仍需要這一步。
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

            if (placement == PlacementMode.FollowNode) Reposition(target);

            // 固定面板一直都在，不做淡入 —— 每次 hover 都閃一下比不淡入更吵
            if (canvasGroup != null && !keepFrameWhenIdle)
            {
                if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                fadeRoutine = StartCoroutine(FadeTo(1f));
            }
        }

        /// <summary>
        /// 沒有 hover 任何節點時的狀態。
        /// 固定面板用它換回提示文字（框留著），跟隨式的則整個收掉。
        /// </summary>
        public void ShowIdle()
        {
            // 總開關關著時不可以復活 —— 地圖滑走後那個遲到的 OnPointerExit
            // 就是走這條路把框重新開起來的
            if (suppressed || panel == null) return;

            SetTexts(idleTitle, idleBody);

            panel.gameObject.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void SetTexts(string title, string body)
        {
            if (titleText != null)
            {
                titleText.text = title;
                // 標題是空的就收起來，否則框頂會多出一塊空白
                titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
            }

            if (bodyText != null)
            {
                bodyText.text = body;
                bodyText.gameObject.SetActive(!string.IsNullOrEmpty(body));
            }
        }

        public void Hide()
        {
            if (panel == null) return;

            // 固定面板不消失，只換回閒置文字
            if (keepFrameWhenIdle)
            {
                ShowIdle();
                return;
            }

            if (canvasGroup == null)
            {
                panel.gameObject.SetActive(false);
                return;
            }

            if (!panel.gameObject.activeInHierarchy)
            {
                HideImmediate();
                return;
            }

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeTo(0f));
        }

        /// <summary>
        /// 不走淡出。由 `MapView` 在「選定節點要開始移動」與「重建地圖」時呼叫。
        ///
        /// 固定面板一樣不會消失 —— 那兩個時機只是內容過期了，不是那塊區域要收掉。
        /// </summary>
        public void HideImmediate()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (keepFrameWhenIdle)
            {
                ShowIdle();
                return;
            }

            ForceHide();
        }

        /// <summary>
        /// **真的關掉，連 `Keep Frame When Idle` 也不留。**
        ///
        /// 【什麼時候要用】離開地圖的時候。固定面板的「閒置時保留框」是為了
        /// 玩家還在看地圖的情境 —— 一旦地圖收起來、進了探索房間，
        /// 那個框就會孤零零地浮在別的畫面上。
        ///
        /// 一般的滑鼠移開請用 `Hide()`，那個才會尊重 Keep Frame When Idle。
        /// </summary>
        public void ForceHide()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (panel != null) panel.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator FadeTo(float target)
        {
            float start = canvasGroup.alpha;

            if (fadeDuration > 0f)
            {
                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / fadeDuration));
                    yield return null;
                }
            }

            canvasGroup.alpha = target;
            fadeRoutine = null;

            // 淡出結束才真的關掉，否則淡出中會提前消失
            if (target <= 0f && panel != null) panel.gameObject.SetActive(false);
        }

        /// <summary>擺在節點右邊；右邊放不下就換左邊；再放不下就夾進畫面。</summary>
        private void Reposition(RectTransform target)
        {
            if (canvasRect == null) return;

            Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, target);
            Vector2 size = panel.rect.size;

            // panel 的 pivot 影響 anchoredPosition 的意義，這裡統一以「左上角」推算
            float centerY = (b.min.y + b.max.y) * 0.5f;

            Vector2 right = new Vector2(b.max.x + offset.x, centerY + size.y * 0.5f);
            Vector2 left = new Vector2(b.min.x - size.x - offset.x, centerY + size.y * 0.5f);

            Vector2 pos = Fits(right, size) ? right : left;

            Rect canvas = canvasRect.rect;
            pos.x = Mathf.Clamp(pos.x, canvas.xMin + screenPadding.x, canvas.xMax - size.x - screenPadding.x);
            pos.y = Mathf.Clamp(pos.y, canvas.yMin + size.y + screenPadding.y, canvas.yMax - screenPadding.y);

            panel.anchoredPosition = pos;
        }

        private bool Fits(Vector2 pos, Vector2 size)
        {
            Rect r = canvasRect.rect;
            return pos.x >= r.xMin + screenPadding.x
                && pos.x + size.x <= r.xMax - screenPadding.x;
        }
    }
}
