using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffHealth : Card
{
    [SerializeField] float healthRestore = 25;
    protected override void CardBehaviour()
    {
        if (SimulationBridge.Instance != null)
        {
            SimulationBridge.Instance.RequestBunkerHeal(healthRestore);
        }
        else if (TowerPlayer.instance != null)
        {
            TowerPlayer.instance.life = Mathf.Clamp(TowerPlayer.instance.life + healthRestore, 0, 100);
            SimEventDispatcher.RecordHeal(healthRestore);
        }
    }
}
