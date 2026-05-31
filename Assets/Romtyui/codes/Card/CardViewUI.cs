using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardViewUI : MonoBehaviour
{
    [Header("Image Refs")]
    public Image artworkImage;      // 武器層
    public Image cardFaceImage;     // 卡面層
    public Image cardFrameImage;    // 卡框層
    public Image maskImage;         // 蒙版

    [Header("Text Refs")]
    public TMP_Text nameText;
    public TMP_Text costText;
    public TMP_Text descriptionText;

    [Header("Fallback Visual")]
    public CardVisualData defaultVisualData;

    public CardInstance CardInstance { get; private set; }

    public void Bind(CardInstance instance)
    {
        CardInstance = instance;

        if (instance == null || instance.data == null)
            return;

        CardData data = instance.data;

        if (nameText != null)
            nameText.text = data.cardName;

        if (costText != null)
            costText.text = instance.currentCost.ToString();

        if (descriptionText != null)
            descriptionText.text = data.description;

        CardVisualData visual = data.visualData != null
            ? data.visualData
            : defaultVisualData;

        ApplyVisual(visual);
    }

    private void ApplyVisual(CardVisualData visual)
    {
        if (visual == null)
        {
            Debug.LogWarning($"[{nameof(CardViewUI)}] CardData 沒有 visualData，CardViewUI 也沒有 defaultVisualData");
            return;
        }

        if (artworkImage != null)
            artworkImage.sprite = visual.artworkSprite;

        if (cardFaceImage != null)
            cardFaceImage.sprite = visual.cardFaceSprite;

        if (cardFrameImage != null)
            cardFrameImage.sprite = visual.cardFrameSprite;

        if (maskImage != null)
            maskImage.sprite = visual.maskSprite;

        if (nameText != null)
            nameText.color = visual.nameTextColor;

        if (descriptionText != null)
            descriptionText.color = visual.descriptionTextColor;

        if (costText != null)
            costText.color = visual.costTextColor;
    }
}