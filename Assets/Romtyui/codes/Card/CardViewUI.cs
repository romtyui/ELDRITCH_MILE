using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardViewUI : MonoBehaviour
{
    [Header("UI Refs")]
    public Image artworkImage;      // 卡面層
    public Image frameImage;        // 卡框層，可先不管
    public TMP_Text nameText;       // Text(TMP) 可拆多個 TMP
    public TMP_Text costText;
    public TMP_Text descriptionText;

    public CardInstance CardInstance { get; private set; }

    public void Bind(CardInstance instance)
    {
        CardInstance = instance;

        if (instance == null || instance.data == null)
            return;

        //nameText.text = instance.data.cardName;
        //costText.text = instance.currentCost.ToString();
        descriptionText.text = instance.data.description;

        if (artworkImage != null)
            artworkImage.sprite = instance.data.artwork;
    }
}