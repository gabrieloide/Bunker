using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCardOnMouse : Card
{
    protected override void CardBehaviour()
    {
        Vector2 Mouseposition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Instantiate(towerData.CardToInstantiate, Mouseposition, Quaternion.identity);
    }
}