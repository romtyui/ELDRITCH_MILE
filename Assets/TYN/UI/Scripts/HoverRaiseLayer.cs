using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 滑鼠進入時把自己（或指定的根物件）提到同層最上面，離開時放回原位。
///
/// 【為什麼需要】手牌與對話框的選項在畫面上會重疊，但兩者**都不能永久蓋住對方**：
///   · 手牌永遠在上 → 選項被卡片擋住，玩家看不到自己要打什麼、也點不到
///   · 選項永遠在上 → 玩家想看手牌細節時被擋住
/// 所以改成「誰底下有滑鼠，誰就在上面」。
///
/// 【只改 sibling 順序，不動座標】同一個 Canvas 內的疊放由 sibling index 決定，
/// 所以提到最上層＝`SetAsLastSibling()`。離開時放回原本的 index 就完全復原。
///
/// 【要有東西接得到滑鼠】本元件靠 `IPointerEnter/Exit`。Unity 的指標事件只往
/// **祖先**傳，不傳給兄弟 —— 所以本元件要掛在「感應區與所有內容的共同祖先」上，
/// 掛在感應區自己身上的話，滑到卡片時完全收不到事件。
/// </summary>
public class HoverRaiseLayer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("要提到最上層的物件。留空則用自身。\n" +
             "通常指到「手牌區的根」，而不是感應區本身 —— 要一起浮上去的是整組 UI")]
    public Transform target;

    [Tooltip("勾選則離開時放回原本的 sibling index；取消則留在最上層")]
    public bool restoreOnExit = true;

    [Header("邊緣防抖")]
    [Tooltip("游標進入後要停留多久才真的提起（秒）。\n\n" +
             "在邊界上游標會反覆進出，每次都立刻切換就會瘋狂閃爍。\n" +
             "要求「停一下」之後，成對出現的 enter/exit 會互相抵消掉")]
    [Min(0f)] public float raiseDelay = 0.06f;

    [Tooltip("游標離開後要等多久才放下（秒）。\n\n" +
             "**刻意比 Raise Delay 長** —— 提起要靈敏、放下要遲鈍。\n" +
             "這樣游標在邊緣抖動時會維持在「已提起」，而不是兩邊來回")]
    [Min(0f)] public float lowerDelay = 0.18f;

    private int originalIndex = -1;
    private bool raised;

    /// <summary>
    /// 鎖住＝維持提起，忽略所有離開事件。
    ///
    /// 【為什麼需要】拖曳卡片時游標一定會離開手牌區（那正是拖出去的意思），
    /// 這時若照常放下，整個手牌區會沉到對話框與立繪後面 —— 玩家正在拖的東西
    /// 突然被蓋住，而且看起來像 bug。
    /// </summary>
    private bool locked;

    public void SetLocked(bool value)
    {
        locked = value;

        if (locked)
        {
            pendingTimer = 0f;
            Raise();
        }
        else if (!pointerInside && restoreOnExit)
        {
            // 解鎖時游標已經不在範圍內 → 照正常延遲放下
            pendingTimer = lowerDelay > 0f ? lowerDelay : 0.0001f;
        }
    }

    /// 目前指標在不在範圍內（事件即時更新），與 raised 分開 —— 中間隔著延遲
    private bool pointerInside;

    /// 還要等多久才套用 pointerInside 的狀態。<= 0 表示沒有待處理的切換
    private float pendingTimer;

    private void Awake()
    {
        if (target == null) target = transform;
    }

    private void OnDisable()
    {
        // 在提起狀態下被停用時，OnPointerExit 不會送達（Unity 的已知行為），
        // 不還原的話下次啟用就永遠停在最上層
        pointerInside = false;
        pendingTimer = 0f;
        Restore();
    }

    private void Update()
    {
        if (pendingTimer <= 0f) return;

        pendingTimer -= Time.unscaledDeltaTime;
        if (pendingTimer > 0f) return;

        if (pointerInside) Raise();
        else if (restoreOnExit) Restore();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;

        // 已經是想要的狀態 → 取消待處理的切換。
        // 這就是防抖的關鍵：邊界上成對出現的 enter/exit 會互相抵消
        if (raised) { pendingTimer = 0f; return; }

        pendingTimer = raiseDelay > 0f ? raiseDelay : 0.0001f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;

        // 拖曳中一律不放下 —— 游標離開手牌區正是「把卡拖出去」的意思
        if (locked) { pendingTimer = 0f; return; }

        if (!raised) { pendingTimer = 0f; return; }

        pendingTimer = lowerDelay > 0f ? lowerDelay : 0.0001f;
    }

    private void Raise()
    {
        if (raised || target == null) return;

        originalIndex = target.GetSiblingIndex();
        target.SetAsLastSibling();
        raised = true;
    }

    private void Restore()
    {
        if (!raised || target == null) return;

        // 期間若有別的東西改動了同層物件數，index 可能超出範圍，夾一下
        if (originalIndex >= 0)
        {
            int max = target.parent != null ? target.parent.childCount - 1 : 0;
            target.SetSiblingIndex(Mathf.Clamp(originalIndex, 0, max));
        }

        raised = false;
    }
}
