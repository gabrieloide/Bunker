using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]GameObject[] Enemy;
    [SerializeField] float delayBtwSpawns;
    [SerializeField] int enemyAmount;
    [SerializeField] float WaitForNextEnemy;
    public bool inGame;
    private void Update()
    {
        if (inGame)
        {
            Invoke("EnemyGenerator", delayBtwSpawns);
            inGame = false;
        }
    }
    void EnemyGenerator()
    {
        //spawn 1 enemy every this function is called
         StartCoroutine(Enemyspawn());
    }
    IEnumerator Enemyspawn()
    {
        for (int i = 0; i < enemyAmount; i++)
        {
            int rnd = Random.Range(0, 4);
            Instantiate(Enemy[rnd], transform.position, transform.rotation);
            yield return new WaitForSeconds(0);
        }
    }
}