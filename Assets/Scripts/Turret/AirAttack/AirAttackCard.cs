using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirAttackCard : Card
{
    protected override void CardBehaviour()
    {
        Instantiate(towerData.CardToInstantiate, Vector3.zero, Quaternion.identity);
    }
  
}
