using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Card Visual Data Explore")]
public class CardVisualDataExplore : ScriptableObject
{
    [Header("Card Images (探索單圖層)")]
    public Sprite artworkSprite;     // 探索卡主圖
    public Sprite cardFrameSprite;   // 探索卡外框

    // 未來如果探索卡有專屬的外觀屬性 (例如卡背)，可以加在這裡
    // public Sprite cardBackSprite; 

    [Header("Text Colors")]
    public Color nameTextColor = Color.white;
    public Color descriptionTextColor = Color.white;
    public Color costTextColor = Color.white;
}