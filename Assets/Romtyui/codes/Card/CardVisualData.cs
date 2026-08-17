using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Card Visual Data")]
public class CardVisualData : ScriptableObject
{
    [Header("Card Images")]
    public Sprite artworkSprite;     // ªZ¾¹¼h
    public Sprite cardFaceSprite;    // ¥d­±¼h
    public Sprite cardFrameSprite;   // ¥d®Ø¼h
    public Sprite maskSprite;        // »Xª©

    [Header("Text Colors")]
    public Color nameTextColor = Color.white;
    public Color descriptionTextColor = Color.white;
    public Color costTextColor = Color.white;
}