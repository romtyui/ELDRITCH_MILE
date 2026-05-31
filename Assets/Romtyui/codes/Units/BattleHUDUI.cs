using TMPro;
using UnityEngine;

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

    private void OnEnable()
    {
        SubscribeEvents();
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
            return;
        }

        if (currentHpText != null)
            currentHpText.text = battleUnit.currentHp.ToString();

        if (maxHpText != null)
            maxHpText.text = battleUnit.maxHp.ToString();
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
}