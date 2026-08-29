using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 滑鼠移上去時，把上層那張圖淡入／淡出，做出「兩張圖平滑替換」的效果。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【商店的 EXIT 就是這個】兩張圖只差左邊那格裡的小人：
    ///   · `EXIT_人`（未互動）—— 小人站在牌子裡
    ///   · `EXIT`　　（hover）—— 小人不見了，跑出去了
    ///
    /// 所以底層永遠是「有人」那張，上層疊「沒人」那張並淡入 ——
    /// 看起來就是小人跑掉了。
    ///
    /// ⚠️ **不要用「換 sprite」實作。** 換 sprite 是瞬間切換，
    /// 在這種只差一個小元素的圖上會變成閃一下，看起來像破圖。
    /// 兩張疊著淡入淡出才會平滑。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【掛在哪】掛在**固定不動的感應區**上（跟 <see cref="SlideOutTab"/> 同一個物件），
    /// 不要掛在會滑動的那一層 —— 理由見 SlideOutTab 的類別說明：
    /// 感應區一動，游標的相對位置就變了，會 enter/exit 狂閃。
    ///
    /// 兩支元件掛同一個物件不會打架：它們都只是**接收** enter/exit，
    /// 沒有人會把事件吃掉。
    /// </summary>
    public class HoverCrossfadeImage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("兩層圖")]
        [Tooltip("底層。**未互動時看到的那張**（商店是 `EXIT_人`）。\n" +
                 "它的透明度不會被動到 —— 一直都在")]
        public Image idleImage;

        [Tooltip("上層。**hover 時淡入的那張**（商店是 `EXIT`，小人已經跑掉）。\n" +
                 "要與底層完全重疊、同尺寸，否則淡入時會看到錯位")]
        public Image hoverImage;

        [Header("動效")]
        [Tooltip("淡入淡出大約多久（秒）。0.12～0.25 之間最自然")]
        [Min(0f)] public float fadeSeconds = 0.18f;

        [Tooltip("開始時是不是 hover 狀態。通常維持取消勾選")]
        public bool startHovered = false;

        private float target;
        private float current;

        private void Reset()
        {
            // 掛上去的當下就把上層抓進來，省得每次都要手動拉
            if (hoverImage == null) hoverImage = GetComponentInChildren<Image>(true);
        }

        private void OnEnable()
        {
            target = startHovered ? 1f : 0f;
            current = target;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData) => target = 1f;
        public void OnPointerExit(PointerEventData eventData) => target = 0f;

        private void Update()
        {
            if (Mathf.Approximately(current, target)) return;

            if (fadeSeconds <= 0f)
            {
                current = target;
            }
            else
            {
                // 指數平滑：**任何幀率下的軌跡都一樣**。
                // `Lerp(current, target, deltaTime * speed)` 那種寫法跟幀率有關，
                // 30fps 與 144fps 的手感會不同 —— SlideOutTab 也踩過同一條
                float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.0001f, fadeSeconds) * 4f);
                current = Mathf.Lerp(current, target, k);

                if (Mathf.Abs(target - current) < 0.002f) current = target;
            }

            Apply();
        }

        private void Apply()
        {
            if (hoverImage == null) return;

            Color c = hoverImage.color;
            c.a = current;
            hoverImage.color = c;

            // ⚠️ 上層不可以吃掉滑鼠事件 —— 吃掉的話感應區就收不到 exit，
            //    淡入之後永遠淡不回去
            hoverImage.raycastTarget = false;
        }
    }
}
