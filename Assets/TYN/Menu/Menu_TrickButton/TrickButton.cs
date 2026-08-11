using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// 實作 IPointerEnterHandler 與 IPointerExitHandler 來偵測滑鼠進入與離開
public class TrickButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI buttonText;
    private string originalText;
    private Color originalColor;
    public Color hoverColor = new Color(1f, 0.08f, 0.58f); // 預設的鮮粉色 (#FF1493)

    void Start()
    {
        // 抓取子物件的 TextMeshPro 元件並記錄原本的文字與顏色
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            originalText = buttonText.text;
            originalColor = buttonText.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 滑鼠移入時：文字變成 START，顏色變成鮮粉色
        if (buttonText != null)
        {
            buttonText.text = "START";
            buttonText.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 滑鼠移出時：恢復原本的文字與顏色
        if (buttonText != null)
        {
            buttonText.text = originalText;
            buttonText.color = originalColor;
        }
    }
}