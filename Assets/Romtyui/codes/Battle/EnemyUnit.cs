using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EnemyStatusDebugEntry
{
    public StatusType statusType;
    public int amount;
}

public class EnemyUnit : BattleUnit
{
    [Header("Enemy Intents")]
    public List<EnemyIntentData> intents = new();

    public bool isCharging;
    public int chargeValue;
    public int chargeTurnsLeft;

    [Header("Debug - Current Statuses")]
    [SerializeField]
    private List<EnemyStatusDebugEntry> inspectorStatuses = new List<EnemyStatusDebugEntry>();
    [Header("Enemy Status Icon UI")]
    [Tooltip("怪物狀態 Icon 生成位置。建議這個物件上放 Horizontal Layout Group 或 Vertical Layout Group")]
    public Transform statusIconRoot;

    [Tooltip("狀態圖示資料庫，和玩家狀態 UI 使用同一個 StatusIconDatabase")]
    public StatusIconDatabase statusIconDatabase;

    [Tooltip("狀態 Icon 預置物，直接使用玩家的 StatusIconUI Prefab")]
    public StatusIconUI statusIconPrefab;

    [Tooltip("沒有任何狀態時是否隱藏 StatusIconRoot")]
    public bool hideStatusRootWhenEmpty = true;

    private readonly List<StatusIconUI> spawnedStatusIcons = new List<StatusIconUI>();

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

    [Header("Damage Popup")]
    public DamagePopupUI damagePopupPrefab;

    [Tooltip("這隻怪物專屬的跳字生成 Root，建議放在 EnemySlot 底下")]
    public RectTransform damagePopupRoot;

    [Tooltip("跳字錨點。通常是 visualRoot 或 MonsterVisualRoot")]
    public RectTransform damagePopupAnchor;

    [Tooltip("這隻怪物的跳字偏移")]
    public Vector2 damagePopupOffset = new Vector2(0f, 80f);

    [Tooltip("跳字隨機散開範圍")]
    public Vector2 damagePopupRandomRange = new Vector2(40f, 20f);

    [Header("Enemy Block UI")]
    [Tooltip("整個護盾 UI Root。建議是包含 Image、Text、Animator 的父物件")]
    public GameObject blockRoot;

    [Tooltip("護盾圖片")]
    public Image blockImage;

    [Tooltip("護盾數值文字")]
    public TMP_Text blockText;

    [Tooltip("護盾 UI Animator。可以不指定，沒有就不播放動畫")]
    public Animator blockAnimator;

    [Header("Enemy Block UI Text")]
    public string blockTextPrefix = "";
    public string blockTextSuffix = "";

    [Header("Enemy Block UI Animation")]
    [Tooltip("是否使用護盾生成動畫")]
    public bool useBlockAppearAnimation = true;

    [Tooltip("是否使用護盾常態動畫")]
    public bool useBlockIdleAnimation = true;

    [Tooltip("是否使用護盾消失動畫")]
    public bool useBlockDisappearAnimation = true;

    [Tooltip("生成動畫 Trigger 名稱")]
    public string blockAppearTrigger = "Block_Appear";

    [Tooltip("常態動畫 Trigger 名稱")]
    public string blockIdleTrigger = "Block_Idle";

    [Tooltip("消失動畫 Trigger 名稱")]
    public string blockDisappearTrigger = "Block_Disappear";

    [Tooltip("生成動畫大約秒數。播放完後會嘗試切到常態動畫")]
    public float blockAppearDuration = 0.25f;

    [Tooltip("消失動畫大約秒數。播放完後才隱藏護盾 UI")]
    public float blockDisappearDuration = 0.25f;

    private bool isBlockUIVisible;
    private Coroutine blockAnimationCoroutine;

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
        RefreshInspectorStatuses();

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

        ClearStatusIconUI();
        RefreshInspectorStatuses();
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
        OnHpChanged += RefreshBlockUI;
        OnStatusChanged += RefreshInspectorStatuses;
        OnStatusChanged += RefreshStatusIconUI;

        RefreshBlockUI();
        RefreshInspectorStatuses();
        RefreshStatusIconUI();
    }

    private void OnDisable()
    {
        OnHpChanged -= RefreshHpUI;
        OnHpChanged -= RefreshBlockUI;
        OnStatusChanged -= RefreshInspectorStatuses;
        OnStatusChanged -= RefreshStatusIconUI;

        if (blockAnimationCoroutine != null)
        {
            StopCoroutine(blockAnimationCoroutine);
            blockAnimationCoroutine = null;
        }
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
        RefreshBlockUI();
        RefreshInspectorStatuses();
        RefreshStatusIconUI();
    }

    public void RefreshHpUI()
    {
        if (currentHpText != null)
            currentHpText.text = currentHp.ToString();

        if (maxHpText != null)
            maxHpText.text = maxHp.ToString();
    }

    [ContextMenu("Refresh Inspector Statuses")]
    public void RefreshInspectorStatuses()
    {
        inspectorStatuses.Clear();

        Array statusValues = Enum.GetValues(typeof(StatusType));

        for (int i = 0; i < statusValues.Length; i++)
        {
            StatusType statusType = (StatusType)statusValues.GetValue(i);
            int amount = GetStatus(statusType);

            if (amount <= 0)
                continue;

            inspectorStatuses.Add(new EnemyStatusDebugEntry
            {
                statusType = statusType,
                amount = amount
            });
        }
    }
    public void RefreshStatusIconUI()
    {
        ClearStatusIconUI();

        if (statusIconRoot == null)
            return;

        if (statusIconDatabase == null)
        {
            Debug.LogWarning($"[EnemyUnit] {unitName} 的 statusIconDatabase 沒有指定", gameObject);
            SetStatusIconRootVisible(false);
            return;
        }

        if (statusIconPrefab == null)
        {
            Debug.LogWarning($"[EnemyUnit] {unitName} 的 statusIconPrefab 沒有指定", gameObject);
            SetStatusIconRootVisible(false);
            return;
        }

        Dictionary<StatusType, int> currentStatuses = GetAllStatuses();

        foreach (var pair in currentStatuses)
        {
            StatusType statusType = pair.Key;
            int amount = pair.Value;

            if (amount <= 0)
                continue;

            StatusIconUI iconUI = Instantiate(statusIconPrefab, statusIconRoot);

            if (iconUI == null)
                continue;

            SetupStatusIconVisual(iconUI, statusType, amount);

            spawnedStatusIcons.Add(iconUI);
        }

        SetStatusIconRootVisible(spawnedStatusIcons.Count > 0 || !hideStatusRootWhenEmpty);
    }

    public void ClearStatusIconUI()
    {
        for (int i = 0; i < spawnedStatusIcons.Count; i++)
        {
            if (spawnedStatusIcons[i] != null)
                Destroy(spawnedStatusIcons[i].gameObject);
        }

        spawnedStatusIcons.Clear();

        if (statusIconRoot != null)
        {
            for (int i = statusIconRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(statusIconRoot.GetChild(i).gameObject);
            }
        }

        SetStatusIconRootVisible(false);
    }

    private void SetupStatusIconVisual(StatusIconUI iconUI, StatusType statusType, int amount)
    {
        if (iconUI == null)
            return;

        Sprite icon = statusIconDatabase.GetIcon(statusType);

        if (iconUI.iconImage != null)
        {
            iconUI.iconImage.sprite = icon;
            iconUI.iconImage.enabled = icon != null;
        }

        if (iconUI.stackText != null)
        {
            iconUI.stackText.text = amount > 1 ? amount.ToString() : "";
            iconUI.stackText.gameObject.SetActive(amount > 1);
        }

        DisableStatusIconTooltip(iconUI);

        iconUI.gameObject.SetActive(true);
    }

    private void DisableStatusIconTooltip(StatusIconUI iconUI)
    {
        if (iconUI == null)
            return;

        if (iconUI.tooltipTrigger != null)
            iconUI.tooltipTrigger.enabled = false;

        TooltipTriggerUI[] triggers = iconUI.GetComponentsInChildren<TooltipTriggerUI>(true);

        for (int i = 0; i < triggers.Length; i++)
        {
            TooltipTriggerUI trigger = triggers[i];

            if (trigger == null)
                continue;

            trigger.enabled = false;
        }
    }

    private void SetStatusIconRootVisible(bool visible)
    {
        if (statusIconRoot == null)
            return;

        if (hideStatusRootWhenEmpty)
            statusIconRoot.gameObject.SetActive(visible);
        else
            statusIconRoot.gameObject.SetActive(true);
    }

    public void RefreshBlockUI()
    {
        int currentBlock = block;

        if (blockText != null)
            blockText.text = $"{blockTextPrefix}{currentBlock}{blockTextSuffix}";

        if (currentBlock > 0)
        {
            ShowBlockUI();
        }
        else
        {
            HideBlockUI();
        }
    }

    private void ShowBlockUI()
    {
        if (blockRoot == null)
            return;

        if (blockAnimationCoroutine != null)
        {
            StopCoroutine(blockAnimationCoroutine);
            blockAnimationCoroutine = null;
        }

        if (!isBlockUIVisible)
        {
            blockRoot.SetActive(true);
            isBlockUIVisible = true;

            if (CanPlayBlockAnimation(blockAppearTrigger, useBlockAppearAnimation))
            {
                blockAnimationCoroutine = StartCoroutine(PlayBlockAppearThenIdleRoutine());
            }
            else
            {
                PlayBlockIdleAnimation();
            }

            return;
        }

        if (!blockRoot.activeSelf)
            blockRoot.SetActive(true);

        PlayBlockIdleAnimation();
    }

    private void HideBlockUI()
    {
        if (blockRoot == null)
            return;

        if (!isBlockUIVisible && !blockRoot.activeSelf)
            return;

        if (blockAnimationCoroutine != null)
        {
            StopCoroutine(blockAnimationCoroutine);
            blockAnimationCoroutine = null;
        }

        if (CanPlayBlockAnimation(blockDisappearTrigger, useBlockDisappearAnimation))
        {
            blockAnimationCoroutine = StartCoroutine(PlayBlockDisappearRoutine());
        }
        else
        {
            blockRoot.SetActive(false);
            isBlockUIVisible = false;
        }
    }

    private IEnumerator PlayBlockAppearThenIdleRoutine()
    {
        PlayBlockAnimation(blockAppearTrigger);

        yield return new WaitForSeconds(blockAppearDuration);

        PlayBlockIdleAnimation();

        blockAnimationCoroutine = null;
    }

    private IEnumerator PlayBlockDisappearRoutine()
    {
        PlayBlockAnimation(blockDisappearTrigger);

        yield return new WaitForSeconds(blockDisappearDuration);

        if (blockRoot != null)
            blockRoot.SetActive(false);

        isBlockUIVisible = false;
        blockAnimationCoroutine = null;
    }

    private void PlayBlockIdleAnimation()
    {
        if (!CanPlayBlockAnimation(blockIdleTrigger, useBlockIdleAnimation))
            return;

        PlayBlockAnimation(blockIdleTrigger);
    }

    private bool CanPlayBlockAnimation(string triggerName, bool useAnimation)
    {
        if (!useAnimation)
            return false;

        if (blockAnimator == null)
            return false;

        if (string.IsNullOrWhiteSpace(triggerName))
            return false;

        return AnimatorHasTrigger(blockAnimator, triggerName);
    }

    private void PlayBlockAnimation(string triggerName)
    {
        if (blockAnimator == null)
            return;

        if (string.IsNullOrWhiteSpace(triggerName))
            return;

        if (!AnimatorHasTrigger(blockAnimator, triggerName))
            return;

        ResetBlockTriggerIfExists(blockAppearTrigger);
        ResetBlockTriggerIfExists(blockIdleTrigger);
        ResetBlockTriggerIfExists(blockDisappearTrigger);

        blockAnimator.SetTrigger(triggerName);
    }

    private void ResetBlockTriggerIfExists(string triggerName)
    {
        if (blockAnimator == null)
            return;

        if (string.IsNullOrWhiteSpace(triggerName))
            return;

        if (!AnimatorHasTrigger(blockAnimator, triggerName))
            return;

        blockAnimator.ResetTrigger(triggerName);
    }

    private bool AnimatorHasTrigger(Animator animator, string triggerName)
    {
        if (animator == null)
            return false;

        if (string.IsNullOrWhiteSpace(triggerName))
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];

            if (parameter == null)
                continue;

            if (parameter.type != AnimatorControllerParameterType.Trigger)
                continue;

            if (parameter.name == triggerName)
                return true;
        }

        return false;
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

        ClearStatusIconUI();

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

    public void ShowDamagePopup(int damage)
    {
        if (damage <= 0)
            return;

        if (damagePopupPrefab == null)
        {
            Debug.LogWarning($"[EnemyDamagePopup] {unitName} 的 damagePopupPrefab 沒有指定", gameObject);
            return;
        }

        if (damagePopupRoot == null)
        {
            Debug.LogWarning($"[EnemyDamagePopup] {unitName} 的 damagePopupRoot 沒有指定", gameObject);
            return;
        }

        RectTransform anchor = damagePopupAnchor != null
            ? damagePopupAnchor
            : transform as RectTransform;

        if (anchor == null)
        {
            Debug.LogWarning($"[EnemyDamagePopup] {unitName} 找不到跳字 anchor", gameObject);
            return;
        }

        DamagePopupUI popup = Instantiate(damagePopupPrefab, damagePopupRoot);

        RectTransform popupRect = popup.transform as RectTransform;

        if (popupRect == null)
        {
            popup.Setup(damage, Vector2.zero);
            return;
        }

        Vector2 localPosition = GetLocalPositionInPopupRoot(anchor);

        Vector2 randomOffset = new Vector2(
            UnityEngine.Random.Range(-damagePopupRandomRange.x, damagePopupRandomRange.x),
            UnityEngine.Random.Range(-damagePopupRandomRange.y, damagePopupRandomRange.y)
        );

        popupRect.anchoredPosition = localPosition + damagePopupOffset + randomOffset;

        popup.SetupLocal(damage, popupRect.anchoredPosition);

        Debug.Log(
            $"[EnemyDamagePopup] {unitName} damage = {damage}, root = {damagePopupRoot.name}, anchor = {anchor.name}, local = {popupRect.anchoredPosition}",
            gameObject
        );
    }

    private Vector2 GetLocalPositionInPopupRoot(RectTransform anchor)
    {
        Canvas rootCanvas = damagePopupRoot.GetComponentInParent<Canvas>();

        Camera uiCamera = null;

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = rootCanvas.worldCamera;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
            uiCamera,
            anchor.position
        );

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            damagePopupRoot,
            screenPosition,
            uiCamera,
            out localPoint
        );

        return localPoint;
    }
}