using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/Enemy Intent")]
public class EnemyIntentData : ScriptableObject
{
    [Header("Display")]
    public string intentName;

    [TextArea]
    public string description;

    [Header("Intent UI")]
    public Sprite intentIcon;

    [Tooltip("如果不想自動計算傷害，可以手動填顯示文字。例如：6、6x2、?")]
    public string damageTextOverride;

    [Header("Actions")]
    public List<EnemyActionData> actions = new();

    public string GetDamageText()
    {
        if (!string.IsNullOrEmpty(damageTextOverride))
            return damageTextOverride;

        int totalDamage = 0;

        if (actions != null)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i] is EnemyDamageActionData damageAction)
                {
                    totalDamage += damageAction.amount;
                }
            }
        }

        if (totalDamage <= 0)
            return "";

        return totalDamage.ToString();
    }
}