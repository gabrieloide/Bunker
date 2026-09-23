using UnityEngine;

public class BuffHealth : Card
{
    protected override void CardBehaviour()
    {
        float healAmount = definition is HealCardDefinition heal ? heal.healAmount : 0f;
        if (SimulationBridge.Instance != null)
        {
            SimulationBridge.Instance.RequestBunkerHeal(healAmount);
        }
        else if (TowerPlayer.instance != null)
        {
            TowerPlayer.instance.life = Mathf.Clamp(TowerPlayer.instance.life + healAmount, 0, 100);
            SimEventDispatcher.RecordHeal(healAmount);
        }
    }
}
