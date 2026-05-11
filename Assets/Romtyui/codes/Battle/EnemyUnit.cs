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

        if (intentDamageText != null)
        {
            intentDamageText.text = intent.GetDamageText();
        }
    }

    protected override void Die()
    {
        base.Die();
        gameObject.SetActive(false);
    }
}