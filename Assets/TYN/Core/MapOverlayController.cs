using System.Collections;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// C1：地圖是**常駐覆蓋層**，不是 Stage。
    ///
    /// 企劃流程圖上寫的是「地圖下拉(自動)」，出現 4 次，戰鬥線與事件線都一樣。
    /// 若地圖與探索互斥，就不會用「下拉」這個詞而會用「切換」。
    /// 因此整場 run 只存在一份地圖，反覆下拉收起，不重建、不重新生成。
    ///
    /// 【手法沿用 BookmarkHover】ExitTag 上既有的 BookmarkHover 用
    /// hiddenY(往上藏) / shownY + Mathf.Lerp 到 targetY，正是「下拉」需要的效果。
    /// 差別只在觸發來源：ExitTag 由 hover 觸發，本元件由 GameFlowManager 觸發。
    /// </summary>
    public class MapOverlayController : MonoBehaviour
    {
        [Header("滑動目標")]
        [Tooltip("會被移動的面板。留空則使用自身")]
        public RectTransform panel;

        [Tooltip("收起時的 Y 座標（正數 = 藏在畫面上方）")]
        public float hiddenY = 1200f;

        [Tooltip("下拉完整顯示時的 Y 座標")]
        public float shownY = 0f;

        [Header("動畫")]
        public float slideDuration = 0.5f;

        [Tooltip("下拉的緩動曲線。預設 EaseOut 讓地圖有「掉下來」的份量感")]
        public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("互動")]
        [Tooltip("收起時關閉 raycast，避免玩家在 Stage 進行中還能點到節點")]
        public CanvasGroup canvasGroup;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (panel == null) panel = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            SetOpenImmediate(false);
        }

        public void SetOpenImmediate(bool open)
        {
            IsOpen = open;

            if (panel != null)
            {
                Vector2 pos = panel.anchoredPosition;
                pos.y = open ? shownY : hiddenY;
                panel.anchoredPosition = pos;
            }

            ApplyInteractable(open);
        }

        /// C1：「地圖下拉」
        public IEnumerator SlideDown()
        {
            if (IsOpen) yield break;

            yield return Slide(hiddenY, shownY);

            IsOpen = true;
            ApplyInteractable(true);
        }

        /// 玩家選好節點後收起
        public IEnumerator SlideUp()
        {
            if (!IsOpen) yield break;

            // 先關互動再收，避免收到一半玩家又點到節點
            ApplyInteractable(false);

            yield return Slide(shownY, hiddenY);

            IsOpen = false;
        }

        private IEnumerator Slide(float fromY, float toY)
        {
            if (panel == null) yield break;

            float elapsed = 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);

                Vector2 pos = panel.anchoredPosition;
                pos.y = Mathf.LerpUnclamped(fromY, toY, slideCurve.Evaluate(t));
                panel.anchoredPosition = pos;

                yield return null;
            }

            Vector2 final = panel.anchoredPosition;
            final.y = toY;
            panel.anchoredPosition = final;
        }

        private void ApplyInteractable(bool value)
        {
            if (canvasGroup == null) return;

            canvasGroup.interactable = value;
            canvasGroup.blocksRaycasts = value;
        }

        /// <summary>
        /// 依 RunContext 重畫地圖（節點、連線、走過的路徑、目前位置）。
        /// 在 SlideDown 之前呼叫，此時地圖還在畫面外。
        /// </summary>
        public virtual void Refresh(RunContext run)
        {
        }

        /// <summary>
        /// 地圖下拉完成後呼叫。適合播節點逐層彈出之類的進場動畫。
        /// </summary>
        public virtual IEnumerator OnOpened()
        {
            yield break;
        }
    }
}
