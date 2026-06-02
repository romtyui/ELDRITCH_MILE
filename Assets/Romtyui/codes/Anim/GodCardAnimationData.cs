using UnityEngine;

[CreateAssetMenu(menuName = "CardGame/God Card/God Card Animation")]
public class GodCardAnimationData : ScriptableObject
{
    [Header("Info")]
    public string animationName;

    [Header("Animator")]
    public RuntimeAnimatorController animatorController;

    [Tooltip("要觸發的 Animator Trigger 名稱")]
    public string triggerName = "PlayGodCorruption";

    [Header("Blackout")]
    [Range(0f, 1f)]
    public float blackoutAlpha = 0.75f;

    [Header("Timing")]
    [Tooltip("等待動畫結束事件的最長時間，避免卡死")]
    public float animationTimeout = 5f;
}