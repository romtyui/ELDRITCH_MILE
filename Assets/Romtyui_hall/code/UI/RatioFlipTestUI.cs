using System;
using UnityEngine;
using UnityEngine.UI;

public class RatioFlipTestUI : MonoBehaviour
{
    [Serializable]
    public class FlipBlock
    {
        [Tooltip("顯示 A／B 圖片的 Image，請指定方塊底下的 Visual。")]
        public Image image;

        [NonSerialized] public Vector3 originalScale;
        [NonSerialized] public Vector3 originalPosition;
        [NonSerialized] public bool displayedB;
        [NonSerialized] public bool targetB;
        [NonSerialized] public bool flipToB;
        [NonSerialized] public bool isFlipping;
        [NonSerialized] public float elapsed;
    }

    [Header("滑條")]
    [SerializeField, Tooltip("控制 B 類型方塊數量的滑條。")]
    private Slider ratioSlider;

    [SerializeField, Min(0), Tooltip("開啟畫面時，初始顯示 B 的方塊數量。")]
    private int initialBCount = 5;

    [Header("圖片")]
    [SerializeField, Tooltip("A 類型使用的圖片。")]
    private Sprite spriteA;

    [SerializeField, Tooltip("B 類型使用的圖片。")]
    private Sprite spriteB;

    [Header("方塊順序")]
    [SerializeField, Tooltip("依照轉換順序放入方塊。滑條為 3 時，前 3 個顯示 B，其餘顯示 A。")]
    private FlipBlock[] blocks = new FlipBlock[10];

    [Header("翻面動畫")]
    [SerializeField, Min(0.01f), Tooltip("一次完整跳起翻面的總時間，包含起跳、換圖和落地。")]
    private float flipDuration = 0.5f;

    [SerializeField, Min(0f), Tooltip("跳起的最大高度。設為 0 時，維持原地翻面。")]
    private float jumpHeight = 60f;

    [SerializeField, Tooltip("勾選時沿 Canvas 上方跳起；取消時沿方塊父物件的本地上方跳起，搭配圓形排列會朝圓外跳。")]
    private bool jumpInCanvasUpDirection = true;

    private void Awake()
    {
        if (blocks == null) blocks = new FlipBlock[0];

        foreach (FlipBlock block in blocks)
        {
            if (block == null || block.image == null) continue;

            block.originalScale = block.image.rectTransform.localScale;
            block.originalPosition = block.image.rectTransform.anchoredPosition3D;
        }

        if (ratioSlider == null) return;

        ratioSlider.minValue = 0;
        ratioSlider.maxValue = blocks.Length;
        ratioSlider.wholeNumbers = true;
        ratioSlider.SetValueWithoutNotify(Mathf.Clamp(initialBCount, 0, blocks.Length));
    }

    private void OnEnable()
    {
        if (ratioSlider == null) return;

        ratioSlider.onValueChanged.AddListener(OnSliderValueChanged);
        ApplyCount(Mathf.RoundToInt(ratioSlider.value), false);
    }

    private void OnDisable()
    {
        if (ratioSlider != null) ratioSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        if (blocks == null) return;

        foreach (FlipBlock block in blocks)
        {
            if (block == null || block.image == null) continue;

            block.isFlipping = false;
            block.elapsed = 0f;
            block.displayedB = block.targetB;
            block.image.sprite = block.displayedB ? spriteB : spriteA;
            block.image.rectTransform.localScale = block.originalScale;
            block.image.rectTransform.anchoredPosition3D = block.originalPosition;
        }
    }

    private void Update()
    {
        foreach (FlipBlock block in blocks)
        {
            if (block == null || block.image == null) continue;

            if (!block.isFlipping)
            {
                if (block.displayedB == block.targetB) continue;

                block.isFlipping = true;
                block.flipToB = block.targetB;
                block.elapsed = 0f;
            }

            block.elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(block.elapsed / Mathf.Max(0.01f, flipDuration));
            float widthRatio;

            if (progress < 0.5f)
            {
                widthRatio = 1f - Mathf.SmoothStep(0f, 1f, progress * 2f);
            }
            else
            {
                block.displayedB = block.flipToB;
                block.image.sprite = block.displayedB ? spriteB : spriteA;
                widthRatio = Mathf.SmoothStep(0f, 1f, (progress - 0.5f) * 2f);
            }

            RectTransform visual = block.image.rectTransform;
            Vector3 scale = block.originalScale;
            scale.x *= widthRatio;
            visual.localScale = scale;

            float height = 4f * progress * (1f - progress) * jumpHeight;
            Vector3 jumpOffset = GetJumpOffset(block, height);
            visual.anchoredPosition3D = block.originalPosition + jumpOffset;

            if (progress >= 1f)
            {
                visual.localScale = block.originalScale;
                visual.anchoredPosition3D = block.originalPosition;
                block.isFlipping = false;
            }
        }
    }

    private Vector3 GetJumpOffset(FlipBlock block, float height)
    {
        Vector3 offset = Vector3.up * height;
        if (!jumpInCanvasUpDirection) return offset;

        Canvas canvas = block.image.canvas;
        Transform parent = block.image.rectTransform.parent;
        if (canvas == null || parent == null) return offset;

        Transform canvasTransform = canvas.rootCanvas.transform;
        Vector3 worldOffset = canvasTransform.TransformVector(offset);
        return parent.InverseTransformVector(worldOffset);
    }

    private void OnSliderValueChanged(float value)
    {
        ApplyCount(Mathf.RoundToInt(value), true);
    }

    private void ApplyCount(int bCount, bool animate)
    {
        bCount = Mathf.Clamp(bCount, 0, blocks.Length);

        for (int i = 0; i < blocks.Length; i++)
        {
            FlipBlock block = blocks[i];
            if (block == null || block.image == null) continue;

            block.targetB = i < bCount;

            if (!animate)
            {
                block.isFlipping = false;
                block.elapsed = 0f;
                block.displayedB = block.targetB;
                block.image.sprite = block.displayedB ? spriteB : spriteA;
                block.image.rectTransform.localScale = block.originalScale;
                block.image.rectTransform.anchoredPosition3D = block.originalPosition;
            }
        }
    }
}