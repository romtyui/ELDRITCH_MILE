using UnityEngine;
using UnityEngine.UI;

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

    private GameObject currentNormalVisual;
    private GameObject currentDarkVisual;

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
    }

    public EnemyUnit SpawnEnemy(EnemyData enemyData)
    {
        if (enemyData == null)
        {
            Debug.LogWarning("[EnemySlotUI] enemyData 是 null");
            return null;
        }

        AutoFindRefs();

        ClearVisual();

        ApplyDataToImage(enemyData);
        ApplyDataToEnemyUnit(enemyData);
        SpawnVisual(enemyData);

        gameObject.SetActive(true);

        return enemyUnit;
    }

    public void ClearSlot()
    {
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

        enemyUnit.unitName = enemyData.unitName;
        enemyUnit.maxHp = enemyData.maxHp;
        enemyUnit.currentHp = enemyData.maxHp;
        enemyUnit.block = 0;

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
    }

    private GameObject SpawnOneVisual(GameObject prefab, Transform parent, EnemyData enemyData)
    {
        if (prefab == null)
            return null;

        GameObject visual = Instantiate(prefab, parent);
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

        return visual;
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