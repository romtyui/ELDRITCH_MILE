using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUDUI : MonoBehaviour
{
    [Header("Data")]
    public BattleUnit battleUnit;
    public EnergySystem energySystem;

    [Header("HP TMP")]
    public TMP_Text currentHpText;
    public TMP_Text maxHpText;

    [Header("Energy TMP")]
    public TMP_Text currentEnergyText;
    public TMP_Text maxEnergyText;

    [Header("Block UI")]
    [Tooltip("整個護盾 UI Root，建議是包含 Image 和 Text 的父物件")]
    public GameObject blockRoot;

    [Tooltip("護盾圖片")]
    public Image blockImage;

    [Tooltip("護盾數值文字")]
    public TMP_Text blockText;

    [Tooltip("沒有護盾時是否隱藏整個護盾 UI")]
    public bool hideBlockWhenZero = true;

    [Tooltip("護盾文字前綴，例如空字串、盾、Block")]
    public string blockTextPrefix = "";

    [Tooltip("護盾文字後綴")]
    public string blockTextSuffix = "";

    private void OnEnable()
    {
        SubscribeEvents();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void Start()
    {
        Refresh();
    }

    private void SubscribeEvents()
    {
        if (battleUnit != null)
            battleUnit.OnHpChanged += RefreshHp;

        if (energySystem != null)
            energySystem.OnEnergyChanged += RefreshEnergy;
    }

    private void UnsubscribeEvents()
    {
        if (battleUnit != null)
            battleUnit.OnHpChanged -= RefreshHp;

        if (energySystem != null)
            energySystem.OnEnergyChanged -= RefreshEnergy;
    }

    public void Bind(BattleUnit unit, EnergySystem energy)
    {
        UnsubscribeEvents();

        battleUnit = unit;
        energySystem = energy;

        SubscribeEvents();
        Refresh();
    }

    public void Refresh()
    {
        RefreshHp();
        RefreshEnergy();
    }

    public void RefreshHp()
    {
        if (battleUnit == null)
        {
            Debug.LogWarning("[BattleHUDUI] battleUnit 沒有指定");
            RefreshBlock();
            return;
        }

        if (currentHpText != null)
            currentHpText.text = battleUnit.currentHp.ToString();

        if (maxHpText != null)
            maxHpText.text = battleUnit.maxHp.ToString();

        RefreshBlock();
    }

    public void RefreshEnergy()
    {
        if (energySystem == null)
        {
            Debug.LogWarning("[BattleHUDUI] energySystem 沒有指定");
            return;
        }

        if (currentEnergyText != null)
            currentEnergyText.text = energySystem.currentEnergy.ToString();

        if (maxEnergyText != null)
            maxEnergyText.text = energySystem.maxEnergy.ToString();
    }

    public void RefreshBlock()
    {
        int block = 0;

        if (battleUnit != null)
            block = battleUnit.block;

        bool shouldShow = block > 0 || !hideBlockWhenZero;

        if (blockRoot != null)
            blockRoot.SetActive(shouldShow);

        if (blockText != null)
            blockText.text = $"{blockTextPrefix}{block}{blockTextSuffix}";
    }
}