using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;
    public int enemyAmount;
    public Vector3[] SpawnsPositions;
    public int NewPositionSpawn;
    EnemySpawner enemySpawner;
    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
    }
    private void Start()
    {
        enemySpawner = FindObjectOfType<EnemySpawner>();
    }
    private void Update()
    {
        //1 7 14
        if (enemyAmount <=0)
        {
            NewPositionSpawn++;
        }
    }
}