using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EldritchMile.Map
{
    // ⚠️ 必須寫在 namespace 內部，理由見 MapView.cs 的說明
    using EldritchMile.Core;

/// <summary>
/// 地圖上單一節點的 UI。取代舊的 PerspectiveNode。
///
/// 【與舊版的差別】
///   · 資料型別改用 EldritchMile.Core.RunNodeData（純資料，不含 ScriptableObject 引用）
///   · 點擊不再直呼 PerspectiveMapGenerator.Instance，改回報給所屬的 MapView，
///     避免節點對「整張地圖的單例」產生依賴
///
/// 【為什麼要放命名空間】尚未封存的舊 PerspectiveMapGenerator.cs 在**全域**命名空間
/// 也定義了 MapData / RunNodeData。C# 名稱解析會先看所在命名空間的宣告，
/// 全域的舊型別因此會贏過 using 匯入的 Core 版本 —— 結果是編譯得過但綁錯型別。
/// 放進命名空間後，using 在同一層就先被採用，永遠指向 Core。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MapNodeUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public RunNodeData Data { get; private set; }

    [Header("視覺元件")]
    public Image nodeIcon;

    [Header("狀態顏色")]
    public Color colorBright = Color.white;
    public Color colorDim = new Color(0.3f, 0.3f, 0.3f, 1f);
    public float alphaBright = 1f;
    public float alphaDim = 0.4f;

    [Header("縮放")]
    [Tooltip("縮放只表達「狀態」，不表達「滑鼠在哪」——\n" +
             "hover 的回饋改由 tooltip 負責（2026-08-16）")]
    public float scaleCurrent = 1.2f;
    public float scaleSelectable = 1f;
    public float scaleInactive = 0.8f;

    [Tooltip("hover 時在 Scale Selectable 之上再乘的倍率。**預設 1 ＝ 不縮放**。\n\n" +
             "【為什麼預設關掉】縮放在這張地圖上已經被「狀態」用掉了" +
             "（當前 1.2 / 可前往 1.0 / 去不了 0.8）。再讓 hover 也改縮放，" +
             "玩家會分不出「這個比較大」是因為它是當前位置，還是滑鼠剛好在上面。\n\n" +
             "【什麼時候該開】tooltip 還沒設定好、但又需要 hover 有回饋時，" +
             "暫時設 1.1 頂著。tooltip 上線後建議調回 1")]
    public float scaleHover = 1f;

    private CanvasGroup canvasGroup;
    private MapView owner;
    private bool isSelectable;

    /// tooltip 要講的三種狀態。與 UpdateVisual 的參數一致。
    public enum NodeState { Current, Selectable, Unreachable, Visited }

    public NodeState State { get; private set; } = NodeState.Unreachable;

    /// 狀態決定的目標透明度（由 UpdateVisual 設定）
    private float stateAlpha = 1f;

    /// 進場淡入的進度 0~1。與 stateAlpha 相乘，兩者互不干擾。
    private float introAlpha = 1f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// 進場淡入用。與 UpdateVisual 設定的狀態透明度相乘，
    /// 所以淡入過程中節點的明暗差異依然正確。
    /// </summary>
    public void SetIntroAlpha(float value)
    {
        introAlpha = Mathf.Clamp01(value);
        ApplyAlpha();
    }

    private void ApplyAlpha()
    {
        if (canvasGroup != null) canvasGroup.alpha = stateAlpha * introAlpha;
    }

    public void Init(RunNodeData data, MapView view)
    {
        Data = data;
        owner = view;
    }

    public void UpdateVisual(bool isCurrent, bool selectable, bool isVisited)
    {
        isSelectable = selectable;

        if (isCurrent)
        {
            State = NodeState.Current;
            Apply(colorBright, alphaBright, scaleCurrent);
        }
        else if (selectable)
        {
            State = NodeState.Selectable;
            Apply(colorBright, 0.9f, scaleSelectable);
        }
        else
        {
            // 走過的與還去不了的都是暗的。差別留給日後的美術（例如走過的加個勾），
            // 但 tooltip 現在就分得出來，所以狀態要記正確
            State = isVisited ? NodeState.Visited : NodeState.Unreachable;
            Apply(colorDim, alphaDim, scaleInactive);
        }
    }

    private void Apply(Color color, float alpha, float scale)
    {
        if (nodeIcon != null) nodeIcon.color = color;

        stateAlpha = alpha;
        ApplyAlpha();

        transform.localScale = Vector3.one * scale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isSelectable || owner == null) return;
        owner.OnNodeClicked(Data);
    }

    /// <summary>
    /// hover 顯示 tooltip。
    ///
    /// 【為什麼拿掉縮放】縮放在這張地圖上已經被「狀態」用掉了
    /// （當前 1.2 / 可前往 1.0 / 去不了 0.8）。再讓 hover 也改縮放，
    /// 就變成同一個視覺通道要表達兩件事 —— 玩家分不出「這個節點比較大」
    /// 是因為它是當前位置，還是因為滑鼠剛好在上面。
    ///
    /// 【為什麼不可前往的節點也顯示】那正是玩家最想知道的資訊
    /// （這是什麼？我為什麼去不了？）。可前往與否只影響點擊，不影響說明。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null) owner.ShowNodeTooltip(this);

        if (isSelectable && !Mathf.Approximately(scaleHover, 1f))
        {
            transform.localScale = Vector3.one * (scaleSelectable * scaleHover);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null) owner.HideNodeTooltip(this);

        if (isSelectable && !Mathf.Approximately(scaleHover, 1f))
        {
            transform.localScale = Vector3.one * scaleSelectable;
        }
    }

    /// <summary>
    /// ⚠️ 物件在**滑鼠還停在上面**時被停用，Unity **不會**送 OnPointerExit ——
    /// 關掉地圖覆蓋層、或重建地圖時都會發生，結果是說明框留在畫面上關不掉。
    ///
    /// 【為什麼用 `owner != null` 而不是 `owner?.`】`?.` 走的是 C# 的 null 判斷，
    /// 認不出「已 Destroy 但參考還在」的 Unity 物件；`!= null` 才會走 Unity 覆寫的比較。
    /// 場景結束時這兩者的差別就是有沒有 NullReferenceException。
    /// </summary>
    private void OnDisable()
    {
        // 節點被停用 ＝ 地圖收起來或重建了。這時要**真的關掉**說明框 ——
        // 用一般的 Hide()，固定面板只會換成閒置文字，那個框會孤零零留在探索畫面上
        if (owner != null) owner.ForceHideNodeTooltip();
    }
}
}
