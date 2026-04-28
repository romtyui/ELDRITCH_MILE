using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Intent")]
public class EnemyIntentData : ScriptableObject
{
    [Header("Display")]
    public string intentName;

    [TextArea]
    public string description;

    [Header("Actions")]
    public List<EnemyActionData> actions = new();
}