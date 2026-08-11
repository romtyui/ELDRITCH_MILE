using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 1. 定義所有的鼠標狀態
public enum CursorType
{
    Idle,           // 空閒狀態 (靜態)
    HoverChest,     // 懸浮寶箱 (抓握動畫循環)
    HoldChest,      // 點擊/長按寶箱 (固定抓握)
    HoverNPC        // 懸浮NPC (對話氣泡，可靜態可動畫)
}

// 2. 定義每種游標的資料結構
[System.Serializable]
public class CursorData
{
    public CursorType cursorType;
    public Texture2D[] textures;    // 陣列。如果只有1張圖就是靜態，2張以上就會自動播放動畫
    public float frameRate = 0.2f;  // 動畫每幀的切換時間
    public Vector2 hotSpot;         // 鼠標的點擊判定點 (通常是左上角 Vector2.zero，或是中心)
}

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("游標設定資料庫")]
    public List<CursorData> cursorDataList;

    private CursorType currentCursorType;
    private Coroutine cursorAnimationCoroutine;

    private void Awake()
    {
        // 簡單的單例模式設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 遊戲開始時設定為空閒鼠標
        SetCursor(CursorType.Idle);
    }

    // 讓外部呼叫來切換鼠標狀態的核心方法
    public void SetCursor(CursorType newType)
    {
        // 如果狀態沒變，就不重複執行
        if (currentCursorType == newType && cursorAnimationCoroutine != null) return;

        // 尋找對應的游標資料
        CursorData data = cursorDataList.Find(x => x.cursorType == newType);
        if (data == null)
        {
            Debug.LogWarning("找不到對應的游標資料: " + newType);
            return;
        }

        currentCursorType = newType;

        // 停止之前的動畫協程
        if (cursorAnimationCoroutine != null)
        {
            StopCoroutine(cursorAnimationCoroutine);
            cursorAnimationCoroutine = null;
        }

        // 判斷是靜態還是動畫
        if (data.textures.Length == 1)
        {
            // 靜態游標
            Cursor.SetCursor(data.textures[0], data.hotSpot, CursorMode.Auto);
        }
        else if (data.textures.Length > 1)
        {
            // 動畫游標，開啟協程
            cursorAnimationCoroutine = StartCoroutine(AnimateCursor(data));
        }
    }

    // 處理游標動畫的協程
    private IEnumerator AnimateCursor(CursorData data)
    {
        int currentFrame = 0;
        while (true)
        {
            // 設定當前幀的圖片
            Cursor.SetCursor(data.textures[currentFrame], data.hotSpot, CursorMode.Auto);
            
            // 跳下一幀
            currentFrame = (currentFrame + 1) % data.textures.Length;
            
            // 等待設定的時間
            yield return new WaitForSeconds(data.frameRate);
        }
    }
}