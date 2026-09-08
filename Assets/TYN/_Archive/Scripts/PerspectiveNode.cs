using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ⚠️ 這一批是封存的舊碼，包進 namespace 是為了**不要污染全域命名空間**。
//    它們宣告的 RunNodeData / MapData 與 EldritchMile.Core 那組同名，
//    放在全域的話會蓋掉正式那組 —— 症狀是新檔案裡「MapData 沒有 GetNode」
//    這種看起來莫名其妙的錯誤，而且完全不會指向這裡。
//    （MapView.cs 第 9 行早就記過這個坑）
namespace EldritchMile.Archive
{

[RequireComponent(typeof(CanvasGroup))]
public class PerspectiveNode : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public RunNodeData runtimeData; // 改為接收純資料層

    [Header("視覺元件")]
    public Image nodeIcon;
    private CanvasGroup canvasGroup;

    [Header("狀態設定 (明暗控制)")]
    public Color colorBright = new Color(1f, 1f, 1f, 1f);       
    public Color colorDim = new Color(0.3f, 0.3f, 0.3f, 1f);    
    public float alphaBright = 1f;                              
    public float alphaDim = 0.4f;                               
    
    private bool isSelectable = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void InitData(RunNodeData data)
    {
        runtimeData = data;
    }

    public void UpdateVisual(bool isCurrent, bool selectable, bool isVisited)
    {
        isSelectable = selectable;

        if (isCurrent)
        {
            nodeIcon.color = colorBright;
            canvasGroup.alpha = alphaBright;
            transform.localScale = Vector3.one * 1.2f; 
        }
        else if (isSelectable)
        {
            nodeIcon.color = colorBright;
            canvasGroup.alpha = 0.9f;
            transform.localScale = Vector3.one;
        }
        else if (isVisited)
        {
            nodeIcon.color = colorDim;
            canvasGroup.alpha = alphaDim;
            transform.localScale = Vector3.one * 0.8f;
        }
        else
        {
            nodeIcon.color = colorDim;
            canvasGroup.alpha = alphaDim;
            transform.localScale = Vector3.one * 0.8f;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isSelectable)
        {
            // 將點擊事件交還給地圖總管處理，解除對 ExplorationManager 的直接依賴
            PerspectiveMapGenerator.Instance.OnNodeClicked(runtimeData);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isSelectable) transform.localScale = Vector3.one * 1.1f; 
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSelectable) transform.localScale = Vector3.one;
    }
}
}
