using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUnit : BattleUnit
{
    [Header("Enemy Intents")]
    public List<EnemyIntentData> intents = new();

    [Header("Runtime")]
    public int currentIntentIndex = 0;

    [Header("HP UI")]
    public TMP_Text currentHpText;
    public TMP_Text maxHpText;

    [Header("Intent UI")]
    public Image intentImage;
    public TMP_Text intentDamageText;
    [Header("Animation")]
    public EnemyVisualAnimationController visualAnimationController;
    [Header("Battle Manager")]
    public BattleManager battleManager;
    [Header("Intent Tooltip")]
    public TooltipTriggerUI intentTooltipTrigger;
    public event Action OnIntentChanged;

    private bool isDead;

    public bool IsDeathAnimationPlaying
    {
        get
        {
            return isDead && gameObject.activeInHierarchy && currentHp <= 0;
        }
    }

    public void ResetDeathState()
    {
        isDead = false;
    }

    public EnemyIntentData CurrentIntent
    {
        get
        {
            if (intents == null || intents.Count == 0)
                return null;

            if (currentIntentIndex < 0 || currentIntentIndex >= intents.Count)
                currentIntentIndex = 0;

            return intents[currentIntentIndex];
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (string.IsNullOrEmpty(unitName))
            unitName = gameObject.name;

        RefreshAllUI();
    }

    private void OnEnable()
    {
        OnHpChanged += RefreshHpUI;
    }

    private void OnDisable()
    {
        OnHpChanged -= RefreshHpUI;
    }

    private void Start()
    {
        RefreshAllUI();
    }
    
    protected override void OnDamagedButAlive()
    {
        base.OnDamagedButAlive();

        PlayHurtAnimation();
    }
    public void PlayHurtAnimation()
    {
        if (visualAnimationController != null)
            StartCoroutine(visualAnimationController.PlayHurt());
    }
    public IEnumerator PlayActionAnimation(EnemyAnimationType animationType)
    {
        if (visualAnimationController == null)
            yield break;

        switch (animationType)
        {
            case EnemyAnimationType.Attack:
                yield return visualAnimationController.PlayAttack();
                break;

            case EnemyAnimationType.Block:
                yield return visualAnimationController.PlayBlock();
                break;

            case EnemyAnimationType.SpecialAttack:
                yield return visualAnimationController.PlaySpecialAttack();
                break;

            case EnemyAnimationType.Hurt:
                yield return visualAnimationController.PlayHurt();
                break;

            case EnemyAnimationType.Death:
                yield return visualAnimationController.PlayDeath();
                break;
        }
    }
    public EnemyAnimationType GetCurrentIntentAnimationType()
    {
        if (intents == null || intents.Count == 0)
            return EnemyAnimationType.Attack;

        if (currentIntentIndex < 0 || currentIntentIndex >= intents.Count)
            return EnemyAnimationType.Attack;

        return intents[currentIntentIndex].animationType;
    }
    public void ExecuteTurn(BattleUnit player, BattleManager battleManager)
    {
        if (currentHp <= 0)
            return;

        EnemyIntentData intent = CurrentIntent;

        if (intent == null)
        {
            Debug.LogWarning($"[{unitName}] 沒有設定 EnemyIntentData");
            return;
        }

        Debug.Log($"[{unitName}] 執行意圖：{intent.intentName}");

        EnemyActionContext context = new EnemyActionContext(this, player, battleManager);

        for (int i = 0; i < intent.actions.Count; i++)
        {
            EnemyActionData action = intent.actions[i];

            if (action == null)
                continue;

            action.Execute(context);
        }

        AdvanceIntent();
    }

    public void AdvanceIntent()
    {
        if (intents == null || intents.Count == 0)
            return;

        currentIntentIndex++;

        if (currentIntentIndex >= intents.Count)
            currentIntentIndex = 0;

        RefreshIntentUI();

        OnIntentChanged?.Invoke();
    }

    public void RefreshAllUI()
    {
        RefreshHpUI();
        RefreshIntentUI();
    }

    public void RefreshHpUI()
    {
        if (currentHpText != null)
            currentHpText.text = currentHp.ToString();

        if (maxHpText != null)
            maxHpText.text = maxHp.ToString();
    }

    public void RefreshIntentUI()
    {
        EnemyIntentData intent = CurrentIntent;

        if (intent == null)
        {
            if (intentImage != null)
            {
                intentImage.sprite = null;
                intentImage.enabled = false;
            }

            if (intentDamageText != null)
                intentDamageText.text = "";

            return;
        }

        if (intentImage != null)
        {
            intentImage.sprite = intent.intentIcon;
            intentImage.enabled = intent.intentIcon != null;
        }
        if (intentTooltipTrigger != null)
        {
            if (intent == null)
            {
                intentTooltipTrigger.SetEntries(new List<TooltipEntry>());
            }
            else
            {
                List<TooltipEntry> entries = new List<TooltipEntry>();

                string title = intent.intentName;
                string body = $"這名敵人下回合會執行：{intent.intentName}";

                string damageText = intent.GetDamageText();

                if (!string.IsNullOrWhiteSpace(damageText))
                    body += $"\n數值：{damageText}";

                entries.Add(new TooltipEntry(title, body));

                intentTooltipTrigger.SetEntries(entries, TooltipAnchorSide.Left);
            }
        }
        if (intentDamageText != null)
        {
            intentDamageText.text = intent.GetDamageText();
        }
    }

    protected override void Die()
    {
        if (isDead)
            return;

        StartCoroutine(DieRoutine());
    }
    public IEnumerator DieRoutine()
    {
        if (isDead)
            yield break;

        isDead = true;

        yield return PlayActionAnimation(EnemyAnimationType.Death);

        gameObject.SetActive(false);

        if (battleManager != null)
        {
            battleManager.RequestCheckBattleEnd();
        }
        else
        {
            Debug.LogWarning($"[{unitName}] battleManager 沒有指定，死亡動畫結束後無法通知 BattleManager 檢查勝利");
        }
    }
}