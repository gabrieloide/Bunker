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
    void EnemyGenerator()
    {

    }
    public void Generator(float newEnemyAmount)
    {
        StartEnemySpawn -= Time.fixedDeltaTime;
        if (StartEnemySpawn <= 0)
        {
            StartEnemySpawn = 0;
            EnemyDelay -= Time.fixedDeltaTime;
            if (EnemyDelay <= 0)//Timer para spawnear enemigos.
            {
                if (startEnemySpawn > 0)//Verifica si la cantidad de enemigos es mayor a cero.
                {
                    //Spawnea 1 enemigos cada vez que el contador de startEnemyspawn es menor que 0.
                    startEnemySpawn--;
                    int random = Random.Range(0, Enemies.Length);//Da un numero aleatorio para spawnear un enemigo
                    Instantiate(Enemies[random], transform.position, Quaternion.identity);
                    EnemyDelay = WaitBetweenSpawns;
                }
                else if (startEnemySpawn <= 0)
                {
                    EnemyDelay = 0; //Si no hay enemigos el tiempo se queda en 0.
                    StartEnemySpawn = 15;
                    EnemyAmount += newEnemyAmount;
                    startEnemySpawn = EnemyAmount;
                    WaveManager.instance.Wave++;
                }
            } 
        }
    }
}