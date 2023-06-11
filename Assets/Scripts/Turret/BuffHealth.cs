using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffHealth : Card
{
    [SerializeField] float healthRestore = 25;
    protected override void CardBehaviour()
    {
        GameManager.instance.CurrentCardAmount--;
        TowerPlayer.instance.life += healthRestore;
        TowerPlayer.instance.life = Mathf.Clamp(TowerPlayer.instance.life, 0, 100);
    }
}
