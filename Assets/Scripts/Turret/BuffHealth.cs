using UnityEngine;

public class BuffHealth : Card
{
    // LEGACY: moved to HealCardDefinition, dropped after migration
    [HideInInspector][SerializeField] float healthRestore = 25;
    public float LegacyHealAmount => healthRestore;

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
