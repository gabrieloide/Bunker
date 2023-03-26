using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner instance;
    public float StartEnemySpawn;

    public float EnemyDelay;
    public float WaitBetweenSpawns;
    public float EnemyAmount;
    float startEnemySpawn;
    public GameObject[] Enemies;

    private void Start()
    {
        startEnemySpawn = EnemyAmount;
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void Generator(float newEnemyAmount)
    {
        StartEnemySpawn -= Time.fixedDeltaTime;
        if (StartEnemySpawn <= 0)
        {
            StartEnemySpawn = 0;
            EnemyDelay -= Time.fixedDeltaTime;
            if (EnemyDelay <= 0)
            {
                if (startEnemySpawn > 0)
                {
                    startEnemySpawn--;
                    //Spawnear enemigo
                    int random = Random.Range(0, Enemies.Length);
                    Instantiate(Enemies[random], transform.position, Quaternion.identity);
                    EnemyDelay = WaitBetweenSpawns;
                }
                else if (startEnemySpawn <= 0)
                {
                    //Aumentar oleada
                    EnemyDelay = 0;
                    StartEnemySpawn = 15;
                    EnemyAmount += newEnemyAmount;
                    startEnemySpawn = EnemyAmount;
                    WaveManager.instance.GetEnemyBuffed();
                    WaveManager.instance.Wave++;
                }
            } 
        }
    }
}