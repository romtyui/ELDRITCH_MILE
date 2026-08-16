using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EldritchMile.UI
{
    /// <summary>
    /// 縮在畫面邊緣、滑鼠靠過去才滑出來的標籤（探索的 ExitTag、商店的 EXIT 都是這個形狀）。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【最重要的一件事：感應區不會動，動的是它的子物件】
    ///
    /// 直覺的寫法是「滑鼠進來 → 把自己移出來」。那會壞掉：
    /// 標籤滑出去之後，游標底下的那一點在標籤上的相對位置就變了，
    /// 很容易變成「已經不在標籤上」→ 送出 exit → 縮回去 → 又碰到 → enter，
    /// **一秒閃好幾次**。這是 HANDOFF §4.6 第五條那個坑，手牌上浮踩過一次。
    ///
    /// 所以這個元件掛在一塊**固定不動的感應區**上，移動的是 <see cref="visual"/>。
    /// 感應區要涵蓋標籤「縮著」與「伸出來」兩個位置，游標在裡面怎麼走都不會抖。
    ///
    /// ⚠️ 感應區身上要有一個 **alpha 0 但 `raycastTarget = true` 的 Image**，
    /// 否則只有標籤本身收得到滑鼠，等於沒有固定感應區，上面那個坑會回來。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【它取代了什麼】`BookmarkHover` 只能動 Y，而且用的是
    /// `Mathf.Lerp(current, target, deltaTime * speed)` ——
    /// 那個寫法**跟幀率有關**（30fps 與 144fps 的手感不一樣），而且永遠到不了終點。
    /// 這裡改成指數平滑，任何幀率下的軌跡都一樣。
    ///
    /// 探索的 ExitTag 還在用舊的那支，可以擇日換過來：
    /// `hiddenY` → `hiddenOffset.y`、`shownY` → `shownOffset.y`。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SlideOutTab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("會滑動的那一層")]
        [Tooltip("實際移動的子物件。**不要指向自己** —— 見類別說明。\n" +
                 "留空會退回移動自己，並在 Console 提醒")]
        public RectTransform visual;

        [Header("位置 (visual 的 anchoredPosition)")]
        [Tooltip("縮著的位置。商店在右下角、往右藏 → 填正的 X，例如 (240, 0)。\n" +
                 "探索的書籤往上藏 → 填正的 Y")]
        public Vector2 hiddenOffset = new Vector2(240f, 0f);

        [Tooltip("滑出來之後的位置")]
        public Vector2 shownOffset = Vector2.zero;

        [Header("動效")]
        [Tooltip("大約多久滑到定位（秒）。0.1～0.2 之間最跟手")]
        [Min(0.01f)] public float smoothSeconds = 0.13f;

        [Tooltip("開場是縮著的")]
        public bool startHidden = true;

        private RectTransform target;
        private Vector2 goal;

        /// <summary>目前是不是伸出來的狀態。Stage 想在別的時機收起來時可以看這個。</summary>
        public bool IsShown { get; private set; }

        private void Awake()
        {
            target = visual != null ? visual : (RectTransform)transform;

            if (visual == null)
            {
                Debug.LogWarning(
                    $"[標籤] 「{name}」沒有指定 Visual，只好移動自己 —— " +
                    "滑出去時游標可能會脫離標籤而反覆進出（見 SlideOutTab 的說明）。" +
                    "請把圖放進子物件並指過來。", this);
            }

            // 感應區沒有東西收 raycast 的話，固定感應區等於不存在
            var g = GetComponent<Graphic>();
            if (g == null || !g.raycastTarget)
            {
                Debug.LogWarning(
                    $"[標籤] 「{name}」身上沒有可接收滑鼠的圖形，感應區不會生效。\n" +
                    "請加一個 Image、alpha 設 0、勾選 Raycast Target。", this);
            }

            IsShown = !startHidden;
            goal = IsShown ? shownOffset : hiddenOffset;
            target.anchoredPosition = goal;
        }

        private void Update()
        {
            Vector2 cur = target.anchoredPosition;
            if (cur == goal) return;

            // 指數平滑：不管幀率多少，同樣的時間走同樣的比例。
            // `Lerp(cur, goal, dt * speed)` 那種寫法在高幀率下會快很多，而且永遠到不了終點。
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / smoothSeconds);
            Vector2 next = Vector2.Lerp(cur, goal, k);

            // 收尾要吸附，否則會無限逼近而一直在算
            if ((next - goal).sqrMagnitude < 0.25f) next = goal;

            target.anchoredPosition = next;
        }

        public void OnPointerEnter(PointerEventData eventData) => SetShown(true);
        public void OnPointerExit(PointerEventData eventData) => SetShown(false);

        /// <summary>直接指定伸出／縮回。確認面板跳出來時可以強制收起。</summary>
        public void SetShown(bool shown)
        {
            IsShown = shown;
            goal = shown ? shownOffset : hiddenOffset;
        }
    }
}
