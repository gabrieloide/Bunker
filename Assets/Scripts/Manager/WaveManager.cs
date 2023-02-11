using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BuffEnemyType
{
    NormalEnemy,
    BuffAttack,
    BuffDefense,
    BuffVelocity,
    BuffLife
}

public class WaveManager : MonoBehaviour
{
    public BuffEnemyType buffEnemyType = BuffEnemyType.NormalEnemy;
    public static WaveManager instance;
    public int Wave;
    public int MinPorcent, MaxPorcent;
    public GameObject winScreen;
    public float IncreaseEnemyAmount(float MinPorcent, float MaxPorcent)
    {
        float ranPorcent = Random.Range(MinPorcent, MaxPorcent);
        float r =( (15 * 100) / ranPorcent);
        return Mathf.Ceil(r);
    }
    Dictionary<int, BuffEnemyType> BuffEnemy;
    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
    }
    private void Update()
    {
        EnemySpawner.instance.Generator(IncreaseEnemyAmount(MinPorcent, MaxPorcent));
    }
    
}