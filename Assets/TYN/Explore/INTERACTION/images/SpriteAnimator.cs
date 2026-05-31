using UnityEngine;

// 強制要求掛載此腳本的物件必須要有 SpriteRenderer 元件
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    [Header("動畫設定")]
    [Tooltip("請將水面的序列圖依序拖曳到這裡")]
    public Sprite[] frames; 
    
    [Tooltip("每秒播放幾張圖片 (FPS)")]
    public float framesPerSecond = 10f; 

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int currentFrameIndex;

    void Start()
    {
        // 取得物件身上的 SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 確保陣列有圖片才設定初始畫面
        if (frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
        }
    }

    void Update()
    {
        // 如果沒有圖片、只有一張圖片，或 FPS 小於等於 0，就不需要執行運算
        if (frames == null || frames.Length <= 1 || framesPerSecond <= 0) return;

        // 累加時間
        timer += Time.deltaTime;
        
        // 計算每張圖片應該停留的時間
        float timePerFrame = 1f / framesPerSecond;

        // 當時間超過停留時間時，切換到下一張
        if (timer >= timePerFrame)
        {
            // 扣除掉已經用掉的時間，保留餘數讓計時更精準
            timer -= timePerFrame;

            // 推進到下一張，如果到底了就回到第 0 張 (使用餘數運算)
            currentFrameIndex = (currentFrameIndex + 1) % frames.Length;

            // 更新顯示的圖片
            spriteRenderer.sprite = frames[currentFrameIndex];
        }
    }
}