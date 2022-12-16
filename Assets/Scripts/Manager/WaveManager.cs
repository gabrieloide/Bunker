using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;
    public int enemyAmount;
    public int[] newEnemyAmount;
    public Vector3[] SpawnsPositions;
    public int NewPositionSpawn;
    public int Wave;
    public float[] newWaitBtwSpawn;

    public bool changeWave;
    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
    }
    private void Start()
    {
        enemyAmount = newEnemyAmount[0];
    }
    private void Update()
    {
        for (int i = 0; i < 6; i++)
        {
            if (enemyAmount <= 0)
            {
                Wave++;
                enemyAmount = newEnemyAmount[i];
                if (Wave == 3)
                {
                    changeWave = true;
                    Debug.Log("Segunda oleada");
                    NewPositionSpawn++;
                    EnemySpawner.instance.startBtwSpawns = newWaitBtwSpawn[0];
                    
                }

                if (Wave == 5)
                {
                    changeWave = true;
                    Debug.Log("Tercera oleada");
                    NewPositionSpawn++;
                    EnemySpawner.instance.startBtwSpawns = newWaitBtwSpawn[1];
                }
            }
        }
        //1 7 14
    }
}