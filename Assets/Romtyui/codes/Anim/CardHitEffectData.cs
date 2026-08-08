using UnityEngine;

[CreateAssetMenu(
    menuName = "CardGame/Card Animation/Card Hit Effect Data"
)]
public class CardHitEffectData : ScriptableObject
{
    [Header("Effect Prefab")]

    [Tooltip("命中目標時生成的特效 Prefab")]
    public GameObject effectPrefab;


    [Header("Position")]

    [Tooltip("相對於目標位置的額外偏移")]
    public Vector2 positionOffset =
        Vector2.zero;


    [Header("Scale")]

    [Tooltip("特效 XY 縮放")]
    public Vector2 scale =
        Vector2.one;


    [Header("Rotation")]

    [Tooltip("特效 Z 軸旋轉")]
    public float rotationZ = 0f;


    [Header("Timing")]

    [Tooltip(
        "特效生成後，等待多久才真正執行卡牌效果。" +
        "例如劍光 0.1 秒後砍到敵人，就填 0.1。"
    )]
    public float impactDelay = 0.1f;

    [Tooltip("特效生成多久後自動刪除")]
    public float lifeTime = 0.6f;


    [Header("Audio")]

    [Tooltip("命中特效播放時的音效")]
    public AudioClip hitSfx;

    [Range(0f, 1f)]
    public float hitSfxVolume = 1f;
}