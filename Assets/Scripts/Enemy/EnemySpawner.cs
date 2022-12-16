using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner instance;
    [SerializeField] GameObject[] Enemy;

    [SerializeField] float DelaybtwSpawn;
    public float startBtwSpawns;

    public float startDelaySpawns;
    public float startCounter;

    private void Start()
    {
        if (!instance)
        {
            instance = this;
        }
    }
    private void FixedUpdate()
    {        
        startDelaySpawns -= Time.deltaTime;
        EnemyGenerator();
    }
    void EnemyGenerator()
    {
        //spawn 1 enemy every this function is called
        if (startDelaySpawns <= 0)
        {
            if (WaveManager.instance.enemyAmount > 0)
            {
                if (WaveManager.instance.changeWave)
                {
                    startDelaySpawns = startCounter;
                    WaveManager.instance.changeWave = false;
                }
                DelaybtwSpawn -= Time.deltaTime;
                if (DelaybtwSpawn < 0)
                {
                    DelaybtwSpawn = startBtwSpawns;
                    //Cada oleada spawnea agarra 1 de mas
                    WaveManager.instance.enemyAmount--;
                    int rnd = Random.Range(0, Enemy.Length);
                    Instantiate(Enemy[rnd],WaveManager.instance.SpawnsPositions[WaveManager.instance.NewPositionSpawn], transform.rotation);
                }
            }
        }
    }
}