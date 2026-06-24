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

    public bool isCharging;
    public int chargeValue;
    public int chargeTurnsLeft;

    private bool chargeBrokenStunQueued;
    private bool stayOnCurrentIntentThisTurn;

    [Header("Runtime")]
    public int currentIntentIndex = 0;

    [Header("HP UI")]
    public TMP_Text currentHpText;
    public TMP_Text maxHpText;
    [Header("Special Intents")]
    public EnemyIntentData stunIntent;
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
    public bool TryGetChargeTooltip(out TooltipEntry entry)
    {
        entry = null;

        if (chargeBrokenStunQueued)
        {
            entry = new TooltipEntry(
                "暈眩",
                "蓄力被打斷，這次行動會空過一回合。"
            );

            return true;
        }

        if (!isCharging)
            return false;

        entry = new TooltipEntry(
            "蓄力",
            $"目前蓄力值：{chargeValue}\n" +
            $"剩餘倒數：{chargeTurnsLeft} 回合\n" +
            $"倒數結束時，造成 {chargeValue} 點傷害。\n" +
            $"受到傷害會降低蓄力值。\n" +
            $"蓄力值歸零時，下一次行動會暈眩並空過一回合。"
        );

        return true;
    }
    public void StartCharge(int startValue, int turnCount)
    {
        isCharging = true;
        chargeValue = Mathf.Max(0, startValue);
        chargeTurnsLeft = Mathf.Max(1, turnCount);

        chargeBrokenStunQueued = false;
        stayOnCurrentIntentThisTurn = false;

        RefreshIntentUI();
        RefreshIntentTooltip();

        Debug.Log($"[{unitName}] 開始蓄力，蓄力值 = {chargeValue}，倒數 = {chargeTurnsLeft}");
    }
    public void RequestStayOnCurrentIntent()
    {
        stayOnCurrentIntentThisTurn = true;
    }
    public bool HasChargeBrokenStunQueued()
    {
        return chargeBrokenStunQueued;
    }
    public void ConsumeChargeBrokenStun()
    {
        chargeBrokenStunQueued = false;
        isCharging = false;
        chargeValue = 0;
        chargeTurnsLeft = 0;

        Debug.Log($"[{unitName}] 因蓄力被打破而暈眩，空過一回合");
    }
    public int TickChargeCountdown()
    {
        if (!isCharging)
            return 0;

        chargeTurnsLeft--;

        if (chargeTurnsLeft < 0)
            chargeTurnsLeft = 0;

        RefreshIntentUI();

        return chargeTurnsLeft;
    }
    public int GetChargeDamage()
    {
        if (!isCharging)
            return 0;

        return Mathf.Max(0, chargeValue);
    }
    public void ClearCharge()
    {
        isCharging = false;
        chargeValue = 0;
        chargeTurnsLeft = 0;
        chargeBrokenStunQueued = false;
        stayOnCurrentIntentThisTurn = false;

        RefreshIntentUI();
    }
    protected override void OnAfterHpDamageTaken(int realHpDamage)
    {
        base.OnAfterHpDamageTaken(realHpDamage);

        if (!isCharging)
            return;

        if (realHpDamage <= 0)
            return;

        chargeValue -= realHpDamage;

        if (chargeValue < 0)
            chargeValue = 0;

        Debug.Log($"[{unitName}] 蓄力受到干擾，扣除 {realHpDamage}，剩餘蓄力值 = {chargeValue}");

        if (chargeValue <= 0)
        {
            isCharging = false;
            chargeBrokenStunQueued = true;
            chargeTurnsLeft = 0;

            Debug.Log($"[{unitName}] 蓄力被打破，下一次行動將暈眩");
        }

        RefreshIntentUI();
    }
    public void ResetDeathState()
    {
        isDead = false;
    }

    public EnemyIntentData CurrentIntent
    {
        get
        {
            if (chargeBrokenStunQueued && stunIntent != null)
                return stunIntent;

            if (intents == null || intents.Count == 0)
                return null;

            if (currentIntentIndex < 0 || currentIntentIndex >= intents.Count)
                currentIntentIndex = 0;

            return intents[currentIntentIndex];
        }
    }
    public void RefreshIntentTooltip()
    {
        if (intentTooltipTrigger == null)
            return;

        List<TooltipEntry> entries = new List<TooltipEntry>();

        TooltipEntry chargeEntry;
        if (TryGetChargeTooltip(out chargeEntry))
        {
            entries.Add(chargeEntry);
            intentTooltipTrigger.SetEntries(entries, TooltipAnchorSide.Left);
            return;
        }

        EnemyIntentData intent = CurrentIntent;

        if (intent != null)
        {
            string title = string.IsNullOrWhiteSpace(intent.intentName)
                ? "意圖"
                : intent.intentName;

            string body = intent.description;

            if (string.IsNullOrWhiteSpace(body))
                body = "這個敵人即將執行此意圖。";

            entries.Add(new TooltipEntry(title, body));
        }

        intentTooltipTrigger.SetEntries(entries, TooltipAnchorSide.Left);
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

        stayOnCurrentIntentThisTurn = false;

        if (chargeBrokenStunQueued)
        {
            ConsumeChargeBrokenStun();
            AdvanceIntent();
            return;
        }

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

        if (stayOnCurrentIntentThisTurn)
        {
            RefreshIntentUI();
            return;
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
            if (chargeBrokenStunQueued)
            {
                intentDamageText.text = "暈";
            }
            else if (isCharging)
            {
                intentDamageText.text = $"{chargeValue}\n{chargeTurnsLeft}";
            }
            else
            {
                intentDamageText.text = intent.GetDamageText();
            }
        }
        RefreshIntentTooltip();
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