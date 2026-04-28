using System;
using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    public int maxEnergy = 3;
    public int currentEnergy;

    public event Action OnEnergyChanged;

    private void Awake()
    {
        currentEnergy = maxEnergy;
    }

    public void ResetEnergy()
    {
        currentEnergy = maxEnergy;
        OnEnergyChanged?.Invoke();
    }

    public bool CanSpend(int amount)
    {
        return currentEnergy >= amount;
    }

    public bool Spend(int amount)
    {
        if (currentEnergy < amount)
            return false;

        currentEnergy -= amount;
        OnEnergyChanged?.Invoke();

        return true;
    }

    public void GainEnergy(int amount)
    {
        currentEnergy += amount;

        if (currentEnergy > maxEnergy)
            currentEnergy = maxEnergy;

        OnEnergyChanged?.Invoke();
    }
}