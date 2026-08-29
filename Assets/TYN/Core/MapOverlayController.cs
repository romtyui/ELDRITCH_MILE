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

        [Header("背景")]
        [Tooltip("地圖下拉時墊在後面的場景美術。留空會在自己底下找。\n\n" +
                 "**先用戶外那張**（`Art_Village_Outdoor`）——\n" +
                 "地圖是在 `SwitchStageInternal(StageType.None)` 之後才下拉的，\n" +
                 "那時場上一個 Stage 都沒有，不墊東西就直接看到相機的天空底色。\n" +
                 "對話與事件用的是同一支 `StageBackdrop`。")]
        public StageBackdrop backdrop;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (panel == null) panel = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (backdrop == null) backdrop = GetComponentInChildren<StageBackdrop>(true);

            SetOpenImmediate(false);
        }

        public void SetOpenImmediate(bool open)
        {
            if (!open) OnClosing();

            IsOpen = open;

            if (open) backdrop?.Spawn();
            else backdrop?.Despawn();

            if (panel != null)
            {
                Vector2 pos = panel.anchoredPosition;
                pos.y = open ? shownY : hiddenY;
                panel.anchoredPosition = pos;
            }

            ApplyInteractable(open);
        }

        /// <summary>
        /// 覆蓋層即將收起。子類別在這裡清掉「不該留在別的畫面上」的東西。
        ///
        /// 【為什麼需要這個 hook】覆蓋層是**滑出畫面**的，從頭到尾沒有 `SetActive(false)` ——
        /// 所以子物件的 `OnDisable` 永遠不會觸發。任何靠 `OnDisable` 做收尾的東西
        /// 在這裡都會失效，而且不會有錯誤訊息，只會看到殘留物浮在下一個畫面上。
        ///
        /// 【特別注意】若某個 UI 為了「不跟著地圖上下移動」而被放在覆蓋層**外面**，
        /// 那它更不可能被自動收掉 —— 一定要在這裡處理。
        /// </summary>
        protected virtual void OnClosing() { }

        /// C1：「地圖下拉」
        public IEnumerator SlideDown()
        {
            if (IsOpen) yield break;

            // ⚠️ 要在**滑下來之前**生成 —— 晚一步的話玩家會看到地圖在藍色天空底色上
            //    掉下來、落定之後背景才「啪」地出現
            backdrop?.Spawn();

            yield return Slide(hiddenY, shownY);

            IsOpen = true;
            ApplyInteractable(true);

            // 新手教學要等「地圖真的攤開了」才提示玩家點節點。
            // 放在 ApplyInteractable 之後 —— 提示冒出來的時候，地圖必須已經可以點
            TutorialSignal.MapOpened();
        }

        /// 玩家選好節點後收起
        public IEnumerator SlideUp()
        {
            if (!IsOpen) yield break;

            // 先關互動再收，避免收到一半玩家又點到節點
            ApplyInteractable(false);
            OnClosing();

            yield return Slide(shownY, hiddenY);

            IsOpen = false;

            // ⚠️ 要在**滑完之後**才收 —— 這一站的背景是生成出來的，
            //    不收的話它會一路留到下一站，壓在房間美術上
            backdrop?.Despawn();

            TutorialSignal.MapClosed();
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
