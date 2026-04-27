using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    public int maxEnergy = 3;
    public int currentEnergy;

    public void ResetEnergy()
    {
        currentEnergy = maxEnergy;
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
        return true;
    }

    public void GainEnergy(int amount)
    {
        currentEnergy += amount;
    }
}