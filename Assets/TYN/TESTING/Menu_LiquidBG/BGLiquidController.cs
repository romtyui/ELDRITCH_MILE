using UnityEngine;
using UnityEngine.UI;

public class BGLiquidController : MonoBehaviour
{
    private Image bgImage;
    private Material bgMaterial;

    [Header("扭曲設定")]
    public float maxDistortion = 0.05f; // 扭曲最大強度 (建議數值小一點，0.03~0.1即可)
    public float duration = 1.5f;       // 每次扭曲(從開始到結束)持續幾秒

    [Header("發作時間間隔")]
    public float minInterval = 3f;
    public float maxInterval = 8f;

    private float timer = 0f;
    private float nextTriggerTime = 0f;
    private bool isDistorting = false;
    private float distortionProgress = 0f;

    void Start()
    {
        bgImage = GetComponent<Image>();
    
        // 1. 先把原本 UI Image 上設定好的圖片（Sprite）的 Texture 拔出來存著
        Texture2D originalTex = null;
        if (bgImage.sprite != null)
        {
            originalTex = bgImage.sprite.texture;
        }
    
        // 2. 複製並產生新材質
        bgMaterial = new Material(bgImage.material);
        bgImage.material = bgMaterial;

        // 3. 強制把剛剛存好的圖片塞進新材質的 _MainTex 裡面，確保不會變不見
        if (originalTex != null)
        {
            bgMaterial.SetTexture("_MainTex", originalTex);
        }

        // 初始化一開始的屬性為 0
        bgMaterial.SetFloat("_DistortionStrength", 0f);
        SetNextTriggerTime();
    }

    void Update()
    {
        if (isDistorting)
        {
            // 計算扭曲的進度 (0 到 1)
            distortionProgress += Time.deltaTime / duration;

            // 使用 Mathf.Sin 來做出平滑的起伏：0 -> 1 -> 0
            // Mathf.PI 代表半個圓周，sin(0) = 0, sin(PI/2) = 1, sin(PI) = 0
            float currentStrength = Mathf.Sin(distortionProgress * Mathf.PI) * maxDistortion;
            
            // 將計算好的強度寫入 Shader 的屬性中
            bgMaterial.SetFloat("_DistortionStrength", currentStrength);

            // 扭曲結束，重置狀態
            if (distortionProgress >= 1f)
            {
                isDistorting = false;
                bgMaterial.SetFloat("_DistortionStrength", 0f);
                SetNextTriggerTime();
            }
        }
        else
        {
            // 計時等待下一次發作
            timer += Time.deltaTime;
            if (timer >= nextTriggerTime)
            {
                isDistorting = true;
                distortionProgress = 0f;
                timer = 0f;
            }
        }
    }

    void SetNextTriggerTime()
    {
        nextTriggerTime = Random.Range(minInterval, maxInterval);
    }
}