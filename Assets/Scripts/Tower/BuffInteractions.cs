using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum BuffType
{
    attackPlus,
    attackSpeed,
}
public class BuffInteractions : MonoBehaviour
{
    BuffType buffType = BuffType.attackPlus;
    int attackBonus;
    float ASBonus;

    private bool overTower = false;
    private GameObject tower;
    private TowerD towerd;

    public void buff()
    {
        switch (buffType)
        {
            case BuffType.attackPlus:
                towerd.towersData.damage *= attackBonus;
                break;
            case BuffType.attackSpeed:
                towerd.towersData.fireRate *= ASBonus;
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

    private void OnMouseDrag()
    {
        gameObject.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition) + new Vector3(0f, 0f, 10f);
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
