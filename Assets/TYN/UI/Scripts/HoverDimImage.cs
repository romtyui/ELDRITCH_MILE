using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 滑鼠移上去時把一張圖**變暗**，移開再亮回來。（2026-10-19：遺物欄的開啟按鈕「兩個長方形」要有 hover 回饋）
    ///
    /// ────────────────────────────────────────────────────────
    /// 【跟商店貨架同一個手感】貨架的 hover 是乘上 <c>hoverTint</c>（ShopSlotUI），這裡也是乘色、短淡入淡出。
    ///
    /// 【不會吃掉點擊】只實作 enter/exit。點擊照樣往上傳給 ShortcutBarUI（它靠 IPointerClickHandler 開欄位）——
    /// Unity 的點擊只送給往上找到的**第一個**實作 click 的物件，這支沒有實作，所以不會擋。
    ///
    /// ⚠️ 掛在**有 raycastTarget 的那張圖**上（或它的父物件），否則收不到 enter/exit。
    /// </summary>
    public class HoverDimImage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("要變暗的圖。留空 = 自己身上那張")]
        public Graphic target;

        [Tooltip("hover 時乘上的顏色。越暗回饋越明顯")]
        public Color hoverTint = new Color(0.6f, 0.6f, 0.6f, 1f);

        [Tooltip("淡入淡出大約多久（秒）")]
        [Min(0f)] public float fadeSeconds = 0.12f;

        private Color baseColor;
        private bool captured;
        private float current, goal;

        private void Reset()
        {
            target = GetComponent<Graphic>();
        }

        private void Capture()
        {
            if (captured) return;
            if (target == null) target = GetComponent<Graphic>();
            if (target == null) return;
            baseColor = target.color;
            captured = true;
        }

        private void OnDisable()
        {
            // 收起來時歸位 —— 不然下次出現還是暗的（滑鼠離開時物件已經關了，exit 不會來）
            goal = current = 0f;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData) { Capture(); goal = 1f; }
        public void OnPointerExit(PointerEventData eventData) { Capture(); goal = 0f; }

        private void Update()
        {
            if (Mathf.Approximately(current, goal)) return;
            current = fadeSeconds <= 0f ? goal : Mathf.MoveTowards(current, goal, Time.unscaledDeltaTime / fadeSeconds);
            Apply();
        }

        private void Apply()
        {
            if (!captured || target == null) return;
            target.color = Color.Lerp(baseColor, baseColor * hoverTint, current);
        }
    }
}
