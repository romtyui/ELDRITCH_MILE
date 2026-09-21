using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUDUI : MonoBehaviour
{
    [Header("Data")]
    public BattleUnit battleUnit;
    public EnergySystem energySystem;

    [Header("Number Animation")]
    public AnimatedNumberTextUI hpNumberAnimator_HP;
    public AnimatedNumberTextUI hpNumberAnimator_SAN;

    [Header("HP UI")]
    public TMP_Text currentHpText;
    public TMP_Text maxHpText;
    public Image hpFillImage;

    [Header("SAN UI")]
    public TMP_Text currentEnergyText;
    public TMP_Text maxEnergyText;
    public Image sanFillImage;

    [Header("Block UI")]
    [Tooltip("整個護盾 UI Root，建議包含 Image、Text 和 Block Bar")]
    public GameObject blockRoot;

    [Tooltip("護盾圖片")]
    public Image blockImage;

    [Tooltip("護盾數值文字")]
    public TMP_Text blockText;

    [Tooltip("護盾 Bar 的 Fill Image")]
    public Image blockFillImage;

    [Tooltip("護盾 Bar 使用的虛擬最大值，只影響 fillAmount，不會限制實際 Block")]
    [Min(1)]
    public int blockBarVirtualMax = 50;

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
            if (currentHpText != null)
                currentHpText.text = "0";

            if (maxHpText != null)
                maxHpText.text = "0";

            if (hpFillImage != null)
                hpFillImage.fillAmount = 0f;

            RefreshBlock();
            return;
        }

        if (hpNumberAnimator_HP != null)
            hpNumberAnimator_HP.SetValue(battleUnit.currentHp);
        else if (currentHpText != null)
            currentHpText.text = battleUnit.currentHp.ToString();

        if (maxHpText != null)
            maxHpText.text = battleUnit.maxHp.ToString();

        if (hpFillImage != null)
        {
            float maxHp = Mathf.Max(1, battleUnit.maxHp);
            hpFillImage.fillAmount = Mathf.Clamp01(battleUnit.currentHp / maxHp);
        }

        RefreshBlock();
    }

    public void RefreshEnergy()
    {
        if (energySystem == null)
        {
            if (currentEnergyText != null)
                currentEnergyText.text = "0";

            if (maxEnergyText != null)
                maxEnergyText.text = "0";

            if (sanFillImage != null)
                sanFillImage.fillAmount = 0f;

            return;
        }

        if (hpNumberAnimator_SAN != null)
            hpNumberAnimator_SAN.SetValue(energySystem.currentEnergy);
        else if (currentEnergyText != null)
            currentEnergyText.text = energySystem.currentEnergy.ToString();

        if (maxEnergyText != null)
            maxEnergyText.text = energySystem.maxEnergy.ToString();

        if (sanFillImage != null)
        {
            float maxSan = Mathf.Max(1, energySystem.maxEnergy);
            sanFillImage.fillAmount = Mathf.Clamp01(energySystem.currentEnergy / maxSan);
        }
    }

    public void RefreshBlock()
    {
        int block = battleUnit != null ? battleUnit.block : 0;
        bool shouldShow = block > 0 || !hideBlockWhenZero;

        if (blockRoot != null)
            blockRoot.SetActive(shouldShow);

        if (blockText != null)
            blockText.text = $"{blockTextPrefix}{block}{blockTextSuffix}";

        if (blockFillImage != null)
        {
            float virtualMax = Mathf.Max(1, blockBarVirtualMax);
            blockFillImage.fillAmount = Mathf.Clamp01(block / virtualMax);
        }
    }
}