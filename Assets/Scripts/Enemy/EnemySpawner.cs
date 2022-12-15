using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner instance;
    [SerializeField] GameObject[] Enemy;
    [SerializeField] float startDelaySpawns;
    
    [SerializeField] float DelaybtwSpawn;
    public bool inGame;
    private void Start()
    {
        InvokeRepeating("EnemyGenerator", startDelaySpawns, DelaybtwSpawn);
        if (!instance)
        {
            instance = this;
        }
    }
    void EnemyGenerator()
    {
        //spawn 1 enemy every this function is called
        int rnd = Random.Range(0, Enemy.Length);
        Instantiate(Enemy[rnd],WaveManager.instance.SpawnsPositions[WaveManager.instance.NewPositionSpawn], transform.rotation);
    }
    
}