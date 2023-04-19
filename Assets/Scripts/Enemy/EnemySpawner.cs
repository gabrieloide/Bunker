using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner instance;
    public float StartEnemySpawner;

    public float EnemyDelay;
    float waitBetweenSpawns;


    public int EnemyAmount = 15;
    int newEnemy = 2;
    public GameObject[] Enemies;
    public int enemiesAlive;

    private void Start()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        StartCoroutine(SpawnEnemies());
        waitBetweenSpawns = EnemyDelay;
    }
    IEnumerator SpawnEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(StartEnemySpawner);
            for (int i = 0; i < EnemyAmount; i++)
            {
                instantiateEnemy();
                yield return new WaitForSeconds(waitBetweenSpawns);
            }
            while (enemiesAlive > 0)
            {
                yield return null;
            }
            waveChanger(WaveManager.instance.Wave);
            WaveManager.instance.GetEnemyBuffed();
            WaveManager.instance.Wave++;
        }
    }
    public void instantiateEnemy()
    {
        int random = Random.Range(0, newEnemy); 
        Instantiate(Enemies[random], transform.position, Quaternion.identity);
        enemiesAlive++;
    }
    void waveChanger(int currentWave)
    {
        if (currentWave % 10 == 0 && newEnemy < 9)
        {
            newEnemy++;
            EnemyAmount += 5;
        }
    }
}