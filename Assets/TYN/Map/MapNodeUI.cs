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
             "【2026-08-21】tooltip 與下面的顏色高亮都上線了，三個 prefab 已全部調回 1。" +
             "要再開的話請先想清楚上一段 —— 那個理由沒有變得比較不成立")]
    public float scaleHover = 1f;

    [Header("Hover 高亮（顏色，不是縮放）")]
    [Tooltip("hover 時把節點變鮮豔。**需要 nodeIcon 的材質是 MapNodeHighlight.mat**，" +
             "不是的話這一整組不會有作用（Awake 會發一次警告，不會安靜地失效）。" +
             "【為什麼是飽和度而不是變亮】明暗已經被「狀態」用掉了" +
             "（亮 = 可前往、暗 = 去不了）。hover 再去動明暗，" +
             "暗節點就會在滑鼠底下假裝自己可以點。" +
             "飽和度是還空著的通道，所以拿它來表達「滑鼠在這裡」。")]
    [Range(1f, 3f)] public float hoverSaturation = 1.7f;

    [Tooltip("往暖光色染多少。**黑筆觸只吃這一項** —— " +
             "純黑的飽和度是 0，上面那個旋鈕對墨線完全沒作用，" +
             "所以要靠這一項才會讓整個圖示（不只紅圈）亮起來。" +
             "調太高會變成一片色塊，0.2~0.35 之間比較像「亮了一下」")]
    [Range(0f, 1f)] public float hoverGlow = 0.28f;

    [Tooltip("暖光的顏色。預設偏紅，配這批圖原本的暗紅圈")]
    public Color hoverGlowColor = new Color(1f, 0.42f, 0.36f, 1f);

    [Tooltip("高亮淡入／淡出的秒數。0 就是立刻切換（會很生硬）")]
    public float hoverFadeSeconds = 0.12f;

    private CanvasGroup canvasGroup;
    private MapView owner;
    private bool isSelectable;

    /// 這個節點專屬的材質副本。**一定要是副本** ——
    /// 直接改 prefab 上那份共用材質的話，滑鼠停在一個節點上會讓**整張地圖**一起發亮。
    private Material iconMaterial;

    /// 現在的高亮程度 0~1，與 hoverTarget 之間由 Update 補間
    private float hoverAmount;
    private float hoverTarget;

    private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
    private static readonly int GlowId = Shader.PropertyToID("_Glow");
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");

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
        SetupHoverMaterial();
    }

    /// <summary>
    /// 複製一份自己的材質。
    ///
    /// ⚠️ 代價是**每個節點各自一個 draw call**（材質不同就併不起來）。
    /// 一張地圖十幾個節點，這個代價可以忽略；但若哪天節點變成上百個，
    /// 要改成共用材質、只有正在 hover 的那一個才用副本。
    /// </summary>
    private void SetupHoverMaterial()
    {
        if (nodeIcon == null) return;

        Material source = nodeIcon.material;
        if (source == null || !source.HasProperty(SaturationId))
        {
            // 這個專案踩過「安靜地失效」的坑，所以寧可吵一點
            Debug.LogWarning(
                $"[地圖] {name} 的 nodeIcon 材質不是 MapNodeHighlight.mat，hover 顏色高亮不會有作用。" +
                "要用的話把 Image 的 Material 指到 Assets/TYN/Map/MapNodeHighlight.mat", this);
            return;
        }

        iconMaterial = new Material(source);
        nodeIcon.material = iconMaterial;
        ApplyHover(0f);
    }

    private void OnDestroy()
    {
        // 執行時 new 出來的材質不會自己被回收
        if (iconMaterial != null) Destroy(iconMaterial);
    }

    /// <summary>
    /// 【為什麼用 unscaledDeltaTime】地圖是 UI。之後若因為選單或事件把
    /// `Time.timeScale` 設成 0，用 deltaTime 的話高亮會整個凍住，
    /// 看起來會像 hover 壞掉。
    /// </summary>
    private void Update()
    {
        if (iconMaterial == null) return;
        if (Mathf.Approximately(hoverAmount, hoverTarget)) return;

        float step = hoverFadeSeconds <= 0f
            ? 1f
            : Time.unscaledDeltaTime / hoverFadeSeconds;

        hoverAmount = Mathf.MoveTowards(hoverAmount, hoverTarget, step);
        ApplyHover(hoverAmount);
    }

    private void ApplyHover(float t)
    {
        if (iconMaterial == null) return;
        iconMaterial.SetFloat(SaturationId, Mathf.Lerp(1f, hoverSaturation, t));
        iconMaterial.SetFloat(GlowId, Mathf.Lerp(0f, hoverGlow, t));
        iconMaterial.SetColor(GlowColorId, hoverGlowColor);
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

        // 走過去之後這個節點就不能點了，但滑鼠可能還停在上面 ——
        // 不收掉的話會留下一個亮著、卻點不動的節點
        if (!selectable) hoverTarget = 0f;

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

        // 高亮只給**可前往**的節點。tooltip 三種狀態都給（那是說明），
        // 但「亮起來」在玩家眼裡等於可以點，去不了的節點亮起來會騙人
        hoverTarget = isSelectable ? 1f : 0f;

        if (isSelectable && !Mathf.Approximately(scaleHover, 1f))
        {
            transform.localScale = Vector3.one * (scaleSelectable * scaleHover);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null) owner.HideNodeTooltip(this);

        hoverTarget = 0f;

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

        // 同理：關地圖時不會送 OnPointerExit，高亮會被凍在補間到一半的地方，
        // 下次打開地圖那個節點就是亮的。所以這裡直接歸零，不補間
        hoverTarget = 0f;
        hoverAmount = 0f;
        ApplyHover(0f);
    }
}
}
