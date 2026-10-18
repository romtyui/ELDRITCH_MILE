using System.Collections.Generic;
using UnityEngine;
public enum EnemyAnimationType
{
    Idle,
    Attack,
    Hurt,
    Death,
    Block,
    SpecialAttack
}
[CreateAssetMenu(menuName = "CardGame/Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic")]
    public string unitName;
    public int maxHp = 50;
    [Header("Save ID")]
    public string enemyId;

    [Header("Visual")]
    public GameObject normalVisualPrefab;
    public GameObject darkVisualPrefab;

    [Tooltip("怪物 PSB 生成後的位置偏移")]
    public Vector2 visualAnchoredPosition = Vector2.zero;

    [Tooltip("怪物 PSB 生成後的縮放。PSB 如果太小，這裡可以設 50、80、100")]
    public Vector3 visualScale = Vector3.one;

    [Tooltip("怪物 PSB 生成後的旋轉")]
    public Vector3 visualEulerAngles = Vector3.zero;
    [Header("Damage Popup")]
    public Vector2 damagePopupOffset = new Vector2(0f, 80f);

    [Header("Block UI")]
    [Tooltip("額外加在 Slot 護盾基準位置上的偏移。")]
    public Vector2 blockPositionOffset = Vector2.zero;

    [Tooltip("乘上 Slot 護盾基準寬、高的倍率。(1, 1) 表示使用 Slot 基準大小。")]
    public Vector2 blockSizeMultiplier = Vector2.one;
    [Header("Image Settings")]
    public Color hitBoxColor = new Color(1f, 0f, 1f, 0.25f);
    public Sprite hitBoxSprite;
    [Header("Target Frame Follow")]
    [Tooltip("鎖定框要跟隨的骨骼名稱，例如 Body、Chest、Head")]
    public string targetFrameFollowBoneName = "Body";
    [Header("Target Frame Settings")]
    [Tooltip("鎖定框相對位置")]
    public Vector2 targetFrameAnchoredPosition = Vector2.zero;

    [Tooltip("鎖定框大小")]
    public Vector2 targetFrameSize = new Vector2(300f, 300f);
    [Header("Special Intents")]
    public EnemyIntentData stunIntent;
    [System.Serializable]
    public class StartingStatusEntry
    {
        public StatusType statusType;
        public int amount = 1;

        [Tooltip("具有額外行為的狀態資料。寄居狩獵需要指定；力量、中毒、初始護盾、護盾保留等既有狀態可留空。")]
        public BattleStatusBehaviourData statusBehaviour;
    }

    [Header("Starting Statuses")]
    public List<StartingStatusEntry> startingStatuses = new();

    [Header("Intents")]
    public List<EnemyIntentData> intents = new();

    [Header("Animation Triggers")]
    public string idleTrigger = "idle";
    public string attackTrigger = "atk";
    public string hurtTrigger = "hurt";
    public string deathTrigger = "death";
    public string blockTrigger = "block";
    public string specialAttackTrigger = "special";

    [Header("Animation Durations")]
    public float attackAnimDuration = 0.6f;
    public float hurtAnimDuration = 0.35f;
    public float deathAnimDuration = 1.0f;
    public float blockAnimDuration = 0.5f;
    public float specialAttackAnimDuration = 0.8f;

    [Header("Animation SFX")]
    public AudioClip attackSfx;
    public AudioClip hurtSfx;
    public AudioClip deathSfx;
    public AudioClip blockSfx;
    public AudioClip specialAttackSfx;

    [Header("Animation SFX Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
}