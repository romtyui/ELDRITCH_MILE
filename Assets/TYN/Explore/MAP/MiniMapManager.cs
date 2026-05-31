using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniMapManager : MonoBehaviour
{
    [Header("UI 參照")]
    public RectTransform mapContainer; // 小地圖的父物件 (例如一個空白的 UI Panel)
    public GameObject nodePrefab;      // 剛才做的 NodeUI_Prefab
    public GameObject linePrefab;      // 剛才做的 LineUI_Prefab

    [Header("排版設定")]
    public float xSpacing = 150f; // 每一層 (X軸) 的距離
    public float ySpacing = 100f; // 同層分支 (Y軸) 的上下距離

    // 生成小地圖 (範圍：歷史 -2 到 未來 +2)
    public void DrawMap(MapNodeExplore current, List<MapNodeExplore> history)
    {
        // 1. 清空舊的地圖
        foreach (Transform child in mapContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. 生成當前節點 (X=0, Y=0)
        RectTransform currentUI = SpawnNode(current, Vector2.zero, Color.green);

        // 3. 繪製未來節點 (+1 與 +2)
        DrawFutureNodes(current, 1, currentUI, 0f);

        // 4. 繪製過去節點 (-1 與 -2)
        DrawPastNodes(history, currentUI);
    }

    // 繪製未來的分支 (+1, +2)
    private void DrawFutureNodes(MapNodeExplore node, int depth, RectTransform parentUI, float parentY)
    {
        if (depth > 2 || node.nextAvailableNodes == null || node.nextAvailableNodes.Count == 0) return;

        int count = node.nextAvailableNodes.Count;
        // 計算這一層起始的 Y 座標，讓多個分支能以上下對稱置中排列
        float startY = parentY - ((count - 1) * ySpacing) / 2f;

        for (int i = 0; i < count; i++)
        {
            MapNodeExplore nextNode = node.nextAvailableNodes[i];
            if (nextNode == null) continue;

            // 計算座標
            float targetX = depth * xSpacing;
            float targetY = startY + (i * ySpacing);
            Vector2 targetPos = new Vector2(targetX, targetY);

            // 生成節點 (預測未來的顏色設為白色)
            RectTransform childUI = SpawnNode(nextNode, targetPos, Color.white);
            
            // 畫線連接父子
            DrawLine(parentUI, childUI);

            // 遞迴畫下一層 (+2)
            DrawFutureNodes(nextNode, depth + 1, childUI, targetPos.y);
        }
    }

    // 繪製過去的軌跡 (-1, -2)
    private void DrawPastNodes(List<MapNodeExplore> history, RectTransform currentUI)
    {
        RectTransform lastUI = currentUI;
        int maxPast = Mathf.Min(2, history.Count); // 最多往前抓 2 步

        for (int i = 1; i <= maxPast; i++)
        {
            // 從列表最後面往前抓資料
            MapNodeExplore pastNode = history[history.Count - i];
            
            // 過去的節點只呈現一條線，Y 統一為 0
            Vector2 targetPos = new Vector2(-i * xSpacing, 0f);
            
            // 生成節點 (過去走過的設為灰色)
            RectTransform pastUI = SpawnNode(pastNode, targetPos, Color.gray);
            
            // 畫線連接 (注意是從 past 指向 last)
            DrawLine(pastUI, lastUI);
            
            lastUI = pastUI;
        }
    }

    // 輔助函式：生成節點 UI
    private RectTransform SpawnNode(MapNodeExplore nodeData, Vector2 localPos, Color color)
    {
        GameObject obj = Instantiate(nodePrefab, mapContainer);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.localPosition = localPos;

        // 如果你希望節點改變顏色
        Image img = obj.GetComponent<Image>();
        if (img != null) img.color = color;

        return rect;
    }

    // 輔助函式：兩點之間畫 UI 線段
    private void DrawLine(RectTransform startUI, RectTransform endUI)
    {
        GameObject lineObj = Instantiate(linePrefab, mapContainer);
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        
        // 將線段設定在層級最上方，這樣節點圖示才會蓋在線上面
        lineRect.SetAsFirstSibling();

        Vector2 startPos = startUI.localPosition;
        Vector2 endPos = endUI.localPosition;

        // 計算距離 (線的長度)
        Vector2 dir = endPos - startPos;
        float distance = dir.magnitude;

        // 設定線的中心位置與長度
        lineRect.localPosition = startPos + dir / 2f;
        lineRect.sizeDelta = new Vector2(distance, 2f); // 2f 是線的粗細

        // 計算旋轉角度
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
    }
}