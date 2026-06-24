using UnityEngine;

public class BoilFrameEffect : MonoBehaviour
{
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private float randomSeedX;
    private float randomSeedY;

    [Header("平滑漂移設定 (Drift)")]
    public float driftAmount = 3f;  // 漂移的最大距離像素
    public float driftSpeed = 0.5f; // 漂移的速度

    [Header("沸騰抖動設定 (Boiling)")]
    public float boilAmount = 4f;       // 抖動的劇烈程度
    public float boilDuration = 0.2f;   // 每次發作(沸騰)持續幾秒
    public float boilIntervalMin = 2f;  // 兩次發作之間的最短間隔
    public float boilIntervalMax = 6f;  // 兩次發作之間的最長間隔

    private float timer = 0f;
    private float nextBoilTime = 0f;
    private bool isBoiling = false;

    void Start()
    {
        // 取得 UI 的 RectTransform 並記錄最一開始的初始位置
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;

        // 給每個物件不同的雜訊種子，確保三個框框不會往同一個方向漂移
        randomSeedX = Random.Range(0f, 100f);
        randomSeedY = Random.Range(0f, 100f);

        SetNextBoilTime();
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 狀態切換邏輯
        if (!isBoiling && timer >= nextBoilTime)
        {
            isBoiling = true;
            timer = 0f; // 重置計時器給沸騰持續時間使用
        }
        else if (isBoiling && timer >= boilDuration)
        {
            isBoiling = false;
            timer = 0f; // 重置計時器給下次間隔使用
            SetNextBoilTime();
        }

        // 計算新位置
        Vector2 targetPosition = originalPosition;

        if (isBoiling)
        {
            // 狀態：沸騰中 (使用 Random.Range 製造每幀都不一樣的急促毛躁感)
            targetPosition.x += Random.Range(-boilAmount, boilAmount);
            targetPosition.y += Random.Range(-boilAmount, boilAmount);
        }
        else
        {
            // 狀態：平滑漂移 (使用 Mathf.PerlinNoise 製造像呼吸或水波一樣的平滑位移)
            // PerlinNoise 回傳 0~1，減去 0.5 再乘 2 讓範圍變成 -1 ~ 1
            float noiseX = (Mathf.PerlinNoise(Time.time * driftSpeed + randomSeedX, 0f) - 0.5f) * 2f;
            float noiseY = (Mathf.PerlinNoise(0f, Time.time * driftSpeed + randomSeedY) - 0.5f) * 2f;
            
            targetPosition.x += noiseX * driftAmount;
            targetPosition.y += noiseY * driftAmount;
        }

        // 套用新位置
        rectTransform.anchoredPosition = targetPosition;
    }

    // 隨機決定下一次發作的時間
    void SetNextBoilTime()
    {
        nextBoilTime = Random.Range(boilIntervalMin, boilIntervalMax);
    }
}