using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner instance;
    public float StartEnemySpawner;

    public float EnemyDelay;
    float waitBetweenSpawns;


    public float EnemyAmount = 15;
    float startEnemySpawn;
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
        waitBetweenSpawns = EnemyDelay;
    }
    public void Generator(int currentWave)
    {
        StartEnemySpawner -= Time.deltaTime;
        if (StartEnemySpawner <= 0)
        {
            StartEnemySpawner = 0;
            EnemyDelay -= Time.deltaTime;
            if (EnemyDelay <= 0)
            {
                Debug.Log("aaaaaa");
                if (EnemyAmount > 0)
                {
                    EnemyAmount--;
                    Debug.Log("asdf");
                    instantiateEnemy();
                    EnemyDelay = waitBetweenSpawns;
                }
                else if (EnemyAmount <= 0)
                {
                    EnemyDelay = 0;
                    waveChanger(currentWave);
                    WaveManager.instance.GetEnemyBuffed();
                    WaveManager.instance.Wave++;
                }
            } 
        }
    }
    public void instantiateEnemy()
    {
        int random = Random.Range(0, newEnemy); 

        Debug.Log("Crear enemigo");
        Instantiate(Enemies[random], transform.position, Quaternion.identity);
        enemiesAlive++;
    }
    void waveChanger(int currentWave)
    {
        if (currentWave % 10 == 0 && newEnemy < 9)
        {
            EnemyAmount += 5;
            newEnemy++;
        }
    }
}