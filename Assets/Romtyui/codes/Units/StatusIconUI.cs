using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusIconUI : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public TMP_Text stackText;

    public void Set(Sprite icon, int stack)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (stackText != null)
        {
            stackText.text = stack > 1 ? stack.ToString() : "";
            stackText.gameObject.SetActive(stack > 1);
        }

        gameObject.SetActive(true);
    }
}