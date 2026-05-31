using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Card Visual Data")]
public class CardVisualData : ScriptableObject
{
    [Header("Card Images")]
    public Sprite artworkSprite;     // 武器層
    public Sprite cardFaceSprite;    // 卡面層
    public Sprite cardFrameSprite;   // 卡框層
    public Sprite maskSprite;        // 蒙版

    [Header("Text Colors")]
    public Color nameTextColor = Color.white;
    public Color descriptionTextColor = Color.white;
    public Color costTextColor = Color.white;
}