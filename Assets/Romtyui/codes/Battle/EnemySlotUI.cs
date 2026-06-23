using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;
using static EnemyData;

public class EnemySlotUI : MonoBehaviour
{
    [Header("Slot Refs")]
    public Image slotImage;
    public EnemyUnit enemyUnit;
    public BattleTargetUI battleTargetUI;

    [Header("Light Reveal")]
    public PSBMonsterLightReveal lightReveal;

    [Header("Visual Root")]
    public RectTransform visualRoot;

    [Header("Slot Visual Settings")]
    [Tooltip("這個站位的視覺縮放倍率。中間前景可以設大一點，後方位置設小一點。")]
    public float slotVisualScaleMultiplier = 1f;

    [Tooltip("這個站位額外的位置偏移。")]
    public Vector2 slotVisualPositionOffset = Vector2.zero;

    [SerializeField]private GameObject currentNormalVisual;
    [SerializeField] private GameObject currentDarkVisual;

    [Header("World Visual Root Test")]
    public Transform worldVisualRoot;

    [Header("Animation")]
    public EnemyVisualAnimationController visualAnimationController;

    [Header("Tooltip")]
    public TooltipTriggerUI intentTooltipTrigger;
    public TooltipTriggerUI statusTooltipTrigger;

    private void Awake()
    {
        AutoFindRefs();
    }

    private void Reset()
    {
        AutoFindRefs();
    }

    private void AutoFindRefs()
    {
        if (slotImage == null)
            slotImage = GetComponent<Image>();

        if (enemyUnit == null)
            enemyUnit = GetComponent<EnemyUnit>();

        if (battleTargetUI == null)
            battleTargetUI = GetComponent<BattleTargetUI>();
        if (lightReveal == null)
            lightReveal = FindFirstObjectByType<PSBMonsterLightReveal>();
        if (visualAnimationController == null)
            visualAnimationController = gameObject.AddComponent<EnemyVisualAnimationController>();
        if (intentTooltipTrigger != null)
        {
            intentTooltipTrigger.openMode = TooltipOpenMode.Click;
            intentTooltipTrigger.preferredSide = TooltipAnchorSide.Left;
        }

        if (statusTooltipTrigger != null)
        {
            statusTooltipTrigger.openMode = TooltipOpenMode.Click;
            statusTooltipTrigger.preferredSide = TooltipAnchorSide.Left;
        }
    }

    public EnemyUnit SpawnEnemy(EnemyData enemyData)
    {
        if (enemyData == null)
        {
            Debug.LogWarning("[EnemySlotUI] enemyData 是 null");
            return null;
        }

        AutoFindRefs();

        gameObject.SetActive(true);

        ClearVisual();

        ApplyDataToImage(enemyData);
        ApplyDataToEnemyUnit(enemyData);
        SpawnVisual(enemyData);
        RefreshIntentTooltip();
        RefreshStatusTooltip();
        return enemyUnit;
    }
    private void RefreshIntentTooltip()
    {
        if (intentTooltipTrigger == null || enemyUnit == null)
            return;

        List<TooltipEntry> entries = new List<TooltipEntry>();

        EnemyIntentData intent = enemyUnit.CurrentIntent;

        if (intent == null)
        {
            intentTooltipTrigger.SetEntries(entries, TooltipAnchorSide.Left);
            return;
        }

        if (intent.actions != null)
        {
            for (int i = 0; i < intent.actions.Count; i++)
            {
                EnemyActionData action = intent.actions[i];

                if (action == null)
                    continue;

                TooltipEntry actionEntry = action.GetTooltipEntry();

                if (actionEntry == null)
                    continue;

                if (string.IsNullOrWhiteSpace(actionEntry.title) &&
                    string.IsNullOrWhiteSpace(actionEntry.body))
                    continue;

                entries.Add(actionEntry);
            }
        }

        //if (intent.tooltipKeywords != null)
        //{
        //    for (int i = 0; i < intent.tooltipKeywords.Count; i++)
        //    {
        //        var keyword = intent.tooltipKeywords[i];

        //        if (keyword == null)
        //            continue;

        //        if (string.IsNullOrWhiteSpace(keyword.title) &&
        //            string.IsNullOrWhiteSpace(keyword.description))
        //            continue;

        //        entries.Add(new TooltipEntry(keyword.title, keyword.description));
        //    }
        //}

        if (entries.Count == 0)
        {
            string title = string.IsNullOrWhiteSpace(intent.intentName)
                ? "意圖"
                : intent.intentName;

            string body = string.IsNullOrWhiteSpace(intent.description)
                ? "這名敵人將要行動。"
                : intent.description;

            entries.Add(new TooltipEntry(title, body));
        }

        intentTooltipTrigger.SetEntries(entries, TooltipAnchorSide.Left);
    }
    private void RefreshStatusTooltip()
    {
        if (statusTooltipTrigger == null || enemyUnit == null)
            return;

        List<TooltipEntry> entries = new List<TooltipEntry>();

        Dictionary<StatusType, int> statuses = enemyUnit.GetAllStatuses();

        foreach (var pair in statuses)
        {
            StatusType statusType = pair.Key;
            int amount = pair.Value;

            if (amount <= 0)
                continue;

            entries.Add(BuildStatusTooltipEntry(statusType, amount));
        }

        statusTooltipTrigger.SetEntries(entries, TooltipAnchorSide.Left);
    }
    private TooltipEntry BuildStatusTooltipEntry(StatusType statusType, int amount)
    {
        string title = GetStatusTitle(statusType);
        string body = GetStatusDescription(statusType, amount);

        return new TooltipEntry(title, body);
    }

    private string GetStatusTitle(StatusType statusType)
    {
        switch (statusType)
        {
            case StatusType.Strength:
                return "力量";

            case StatusType.TemporaryStrength:
                return "臨時力量";

            case StatusType.Weak:
                return "虛弱";

            case StatusType.Vulnerable:
                return "易傷";

            case StatusType.Frail:
                return "脆弱";

            case StatusType.Poison:
                return "中毒";

            case StatusType.Harden:
                return "硬化";

            default:
                return statusType.ToString();
        }
    }

    private string GetStatusDescription(StatusType statusType, int amount)
    {
        switch (statusType)
        {
            case StatusType.Strength:
                return $"造成的攻擊傷害增加 {amount} 點。";

            case StatusType.TemporaryStrength:
                return $"本回合造成的攻擊傷害增加 {amount} 點，回合結束後移除。";

            case StatusType.Weak:
                return $"造成的傷害降低。目前剩餘 {amount} 層。";

            case StatusType.Vulnerable:
                return $"受到的傷害增加。目前剩餘 {amount} 層。";

            case StatusType.Frail:
                return $"獲得的格擋降低。目前剩餘 {amount} 層。";

            case StatusType.Poison:
                return $"回合開始時受到 {amount} 點傷害，之後中毒層數減少。";
            case StatusType.Harden: 
                return $"每回合開始時，獲得 {amount} 點護盾。";

            default:
                return $"目前層數：{amount}";
        }
    }
    //private void RefreshTooltip()
    //{
    //    if (tooltipTrigger == null || enemyUnit == null)
    //        return;

    //    List<TooltipEntry> entries = new List<TooltipEntry>();

    //    EnemyIntentData intent = enemyUnit.CurrentIntent;
    //    if (intent != null)
    //    {
    //        entries.Add(new TooltipEntry(
    //            intent.intentName,
    //            $"這名敵人將要{intent.intentName}\n{intent.GetDamageText()}"
    //        ));

    //        if (intent.tooltipKeywords != null)
    //        {
    //            for (int i = 0; i < intent.tooltipKeywords.Count; i++)
    //            {
    //                var k = intent.tooltipKeywords[i];
    //                if (k == null)
    //                    continue;

    //                entries.Add(new TooltipEntry(k.title, k.description));
    //            }
    //        }
    //    }

    //    tooltipTrigger.SetEntries(entries, TooltipAnchorSide.Left);
    //}
    public void ClearSlot()
    {
        if (enemyUnit != null)
        {
            enemyUnit.OnStatusChanged -= RefreshStatusTooltip;
            enemyUnit.OnIntentChanged -= RefreshIntentTooltip;
        }

        ClearVisual();

        if (enemyUnit != null)
        {
            enemyUnit.currentHp = 0;
            enemyUnit.RefreshAllUI();
        }

        gameObject.SetActive(false);
    }

    private void ApplyDataToImage(EnemyData enemyData)
    {
        if (slotImage == null)
            return;

        slotImage.color = enemyData.hitBoxColor;
        slotImage.sprite = enemyData.hitBoxSprite;
        slotImage.raycastTarget = true;
    }

    private void ApplyDataToEnemyUnit(EnemyData enemyData)
    {
        if (enemyUnit == null)
        {
            Debug.LogWarning("[EnemySlotUI] enemyUnit 沒有指定");
            return;
        }

        enemyUnit.ResetDeathState();

        enemyUnit.unitName = enemyData.unitName;
        enemyUnit.maxHp = enemyData.maxHp;
        enemyUnit.currentHp = enemyData.maxHp;
        enemyUnit.block = 0;

        enemyUnit.intentTooltipTrigger = intentTooltipTrigger;
        enemyUnit.stunIntent = enemyData.stunIntent;
        enemyUnit.ClearAllStatuses();

        if (enemyData.startingStatuses != null)
        {
            for (int i = 0; i < enemyData.startingStatuses.Count; i++)
            {
                StartingStatusEntry entry = enemyData.startingStatuses[i];

                if (entry == null)
                    continue;

                enemyUnit.ApplyStatus(entry.statusType, entry.amount);
            }
        }

        if (enemyUnit.battleManager == null)
            enemyUnit.battleManager = FindFirstObjectByType<BattleManager>();

        enemyUnit.intents.Clear();

        if (enemyData.intents != null)
        {
            for (int i = 0; i < enemyData.intents.Count; i++)
            {
                enemyUnit.intents.Add(enemyData.intents[i]);
            }
        }

        enemyUnit.currentIntentIndex = 0;

        if (battleTargetUI != null)
            battleTargetUI.battleUnit = enemyUnit;

        enemyUnit.RefreshAllUI();

        enemyUnit.OnStatusChanged -= RefreshStatusTooltip;
        enemyUnit.OnStatusChanged += RefreshStatusTooltip;

        enemyUnit.OnIntentChanged -= RefreshIntentTooltip;
        enemyUnit.OnIntentChanged += RefreshIntentTooltip;

        if (enemyUnit != null)
            enemyUnit.visualAnimationController = visualAnimationController;
    }

    private void SpawnVisual(EnemyData enemyData)
    {
        Transform parent = visualRoot != null ? visualRoot : transform;

        currentNormalVisual = SpawnOneVisual(enemyData.normalVisualPrefab, parent, enemyData);
        currentDarkVisual = SpawnOneVisual(enemyData.darkVisualPrefab, parent, enemyData);
        if (lightReveal != null)
        {
            Transform normalRoot = currentNormalVisual != null ? currentNormalVisual.transform : null;
            Transform darkRoot = currentDarkVisual != null ? currentDarkVisual.transform : null;

            lightReveal.RegisterMonster(normalRoot, darkRoot);
        }
        if (visualAnimationController != null)
        {
            visualAnimationController.Bind(
                currentNormalVisual,
                currentDarkVisual,
                enemyData
            );
        }
        //if (lightReveal != null)
        //{
        //    Transform normalRoot = currentNormalVisual != null ? currentNormalVisual.transform : null;
        //    Transform darkRoot = currentDarkVisual != null ? currentDarkVisual.transform : null;

        //    lightReveal.RegisterMonster(normalRoot, darkRoot);
        //}
        //Transform parent = worldVisualRoot != null ? worldVisualRoot : transform;

        //currentNormalVisual = SpawnOneVisual(enemyData.normalVisualPrefab, parent, enemyData);
        //currentDarkVisual = SpawnOneVisual(enemyData.darkVisualPrefab, parent, enemyData);

        //if (lightReveal != null)
        //{
        //    Transform normalRoot = currentNormalVisual != null ? currentNormalVisual.transform : null;
        //    Transform darkRoot = currentDarkVisual != null ? currentDarkVisual.transform : null;

        //    lightReveal.RegisterMonster(normalRoot, darkRoot);
        //}
    }

    private GameObject SpawnOneVisual(GameObject prefab, Transform parent, EnemyData enemyData)
    {
        if (prefab == null)
            return null;

        GameObject visual = Instantiate(prefab);
        

        visual.transform.SetParent(parent, false);
        visual.name = prefab.name + "_Runtime";
        visual.SetActive(true);

        Vector3 finalScale = enemyData.visualScale * slotVisualScaleMultiplier;
        Vector2 finalPosition = enemyData.visualAnchoredPosition + slotVisualPositionOffset;
        Quaternion finalRotation = Quaternion.Euler(enemyData.visualEulerAngles);

        RectTransform rect = visual.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            rect.anchoredPosition = finalPosition;
            rect.localScale = finalScale;
            rect.localRotation = finalRotation;
        }
        else
        {
            visual.transform.localPosition = finalPosition;
            visual.transform.localScale = finalScale;
            visual.transform.localRotation = finalRotation;
        }

        Debug.Log($"[EnemySlotUI] 生成視覺：{prefab.name} -> {visual.name}", visual);
        StartCoroutine(RebindSpriteSkinsNextFrame(visual));
        return visual;
    }
    private IEnumerator RebindSpriteSkinsNextFrame(GameObject root)
    {
        yield return null;

        RebindSpriteSkins(root);

        yield return null;

        RebindSpriteSkins(root);
    }

    private void RebindSpriteSkins(GameObject root)
    {
        if (root == null)
            return;

        SpriteSkin[] skins = root.GetComponentsInChildren<SpriteSkin>(true);

        for (int i = 0; i < skins.Length; i++)
        {
            SpriteSkin skin = skins[i];

            if (skin == null)
                continue;

            skin.autoRebind = true;
            skin.enabled = false;
            skin.enabled = true;
        }

        Debug.Log($"[EnemySlotUI] Rebind SpriteSkin：{root.name}, count = {skins.Length}", root);
    }
    private void ClearVisual()
    {
        Transform normalRoot = currentNormalVisual != null ? currentNormalVisual.transform : null;
        Transform darkRoot = currentDarkVisual != null ? currentDarkVisual.transform : null;

        if (lightReveal != null)
            lightReveal.UnregisterMonster(normalRoot, darkRoot);

        if (currentNormalVisual != null)
        {
            Destroy(currentNormalVisual);
            currentNormalVisual = null;
        }

        if (currentDarkVisual != null)
        {
            Destroy(currentDarkVisual);
            currentDarkVisual = null;
        }

        if (visualRoot != null)
        {
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(visualRoot.GetChild(i).gameObject);
            }
        }
    }
}