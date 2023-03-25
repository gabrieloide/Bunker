using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BuffEnemyType
{
    NormalEnemy,
    BuffAttack,
    BuffDefense,
    BuffVelocity,
    BuffLife,
    BuffFireRate
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
        float r = ((15 * 100) / ranPorcent);
        return Mathf.Ceil(r);
    }
    Dictionary<int, BuffEnemyType> BuffEnemy = new Dictionary<int, BuffEnemyType>();
    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        BuffEnemy.Add(60, BuffEnemyType.NormalEnemy);
        BuffEnemy.Add(13, BuffEnemyType.BuffAttack);
        BuffEnemy.Add(7, BuffEnemyType.BuffDefense);
        BuffEnemy.Add(5, BuffEnemyType.BuffLife);
        BuffEnemy.Add(6, BuffEnemyType.BuffVelocity);
        BuffEnemy.Add(8, BuffEnemyType.BuffFireRate);
        GetEnemyBuffed();
    }
    private void Update()
    {
        EnemySpawner.instance.Generator(IncreaseEnemyAmount(MinPorcent, MaxPorcent));
    }
    public void GetEnemyBuffed()
    {
        foreach (var item in BuffEnemy)
        {
            int i = Random.Range(1, 101);
            if (i < item.Key)
            {
                //Dar buff aleatorio dependiendo de la oleada
                buffEnemyType = item.Value;
            }
        }
    }
}