using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.U2D.Animation;

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