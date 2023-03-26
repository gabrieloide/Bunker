using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum BuffType
{
    attackPlus,
    attackSpeed,
    turretLifeBonus,
}
public class BuffInteractions : MonoBehaviour
{
    [SerializeField]BuffType buffType = BuffType.attackPlus;
    [SerializeField]int attackBonus;
    [SerializeField]int lifeBulletAditional;
    [SerializeField]int lifeTurrentAditional;
    [SerializeField]float ASBonus;

    private bool overTower = false;
    private GameObject tower;
    private TowerD towerd;

    public void buff()
    {
        //Buff a las torretas
        switch (buffType)
        {
            case BuffType.attackPlus:
                towerd.damage *= attackBonus;
                break;
            case BuffType.attackSpeed:
                towerd.fireRateCountDown *= ASBonus;
                break;
            case BuffType.turretLifeBonus:
                towerd.timeToDestroy += lifeTurrentAditional;
                break;
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<TowerD>() != null)
        {
            tower = collision.gameObject;
            overTower = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<TowerD>() != null)
        {
            tower = null;
            overTower = false;
        }
    }
    private void OnMouseUp()
    {
        if (overTower)
        {
            towerd = tower.GetComponent<TowerD>();
            buff();
        }
    }
}
