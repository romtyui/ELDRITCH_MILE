using TMPro;
using UnityEngine;

public class TooltipBlockUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text bodyText;

    public void SetData(TooltipEntry entry)
    {
        if (titleText != null)
            titleText.text = entry != null ? entry.title : "";

        if (bodyText != null)
            bodyText.text = entry != null ? entry.body : "";
    }
}