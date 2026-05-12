using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic")]
    public string unitName;
    public int maxHp = 50;

    [Header("Visual")]
    public GameObject normalVisualPrefab;
    public GameObject darkVisualPrefab;

    [Tooltip("怪物 PSB 生成後的位置偏移")]
    public Vector2 visualAnchoredPosition = Vector2.zero;

    [Tooltip("怪物 PSB 生成後的縮放。PSB 如果太小，這裡可以設 50、80、100")]
    public Vector3 visualScale = Vector3.one;

    [Tooltip("怪物 PSB 生成後的旋轉")]
    public Vector3 visualEulerAngles = Vector3.zero;

    [Header("Image Settings")]
    public Color hitBoxColor = new Color(1f, 0f, 1f, 0.25f);
    public Sprite hitBoxSprite;

    [Header("Intents")]
    public List<EnemyIntentData> intents = new();
}