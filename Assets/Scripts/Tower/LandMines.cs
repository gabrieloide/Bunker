using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandMines : MonoBehaviour
{
    public TowersData LandMineData;
    int damage;

    private void Start()
    {
        damage = LandMineData.damage;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            collision.GetComponent<Enemy>().Life -= damage;
            Destroy(gameObject, 3);
        }
    }
}
