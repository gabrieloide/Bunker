using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]GameObject[] Enemy;
    [SerializeField] float startDelaySpawns;
    [SerializeField] int enemyAmount;
    [SerializeField] float WaitForNextEnemy;
    public GameObject[] Waves;
    public bool inGame;
    private void Start()
    {
        Invoke("EnemyGenerator", startDelaySpawns);
        StartCoroutine(Wave());
        
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
            yield return new WaitForSeconds(WaitForNextEnemy);
        }
    }
    IEnumerator Wave()
    {
        yield return new WaitForSeconds(100f);
        Invoke("EnemyGenerator", 0);
        yield return new WaitForSeconds(170f);
        Invoke("EnemyGenerator", 0);
    }
}