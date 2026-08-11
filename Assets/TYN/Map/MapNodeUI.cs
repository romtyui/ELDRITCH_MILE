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
    public float scaleCurrent = 1.2f;
    public float scaleSelectable = 1f;
    public float scaleInactive = 0.8f;
    public float scaleHover = 1.1f;

    private CanvasGroup canvasGroup;
    private MapView owner;
    private bool isSelectable;

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
            Apply(colorBright, alphaBright, scaleCurrent);
        }
        else if (selectable)
        {
            Apply(colorBright, 0.9f, scaleSelectable);
        }
        else
        {
            // 走過的與還去不了的都是暗的。差別留給日後的美術（例如走過的加個勾）
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isSelectable) transform.localScale = Vector3.one * scaleHover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSelectable) transform.localScale = Vector3.one * scaleSelectable;
    }
}
}
