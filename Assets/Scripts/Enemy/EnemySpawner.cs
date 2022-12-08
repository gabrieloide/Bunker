using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] int enemyCount = 10;
    [SerializeField]GameObject[] Enemy;
    Enemy enemy;
    [SerializeField] float delayBtwSpawns;
    [SerializeField] float WaitForNextEnemy;
    [Space]
    [Header("Enemy stats")]
    public float[] _Life;
    public float[] _Damage;
    public float[] _Defense;
    public float[] _MoveSpeed;
    public bool inGame;
    private void Start()
    {
        enemy = FindObjectOfType<Enemy>();
        
    }
    private void Update()
    {
        if (inGame)
        {
            Invoke("EnemyGenerator", delayBtwSpawns);
            inGame = false;
        }
    }
    void EnemyGenerator()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            StartCoroutine(Enemyspawn(WaitForNextEnemy));
        }
    }
    IEnumerator Enemyspawn(float nextEnemy)
    {
        int rnd = Random.Range(0, 4);
        Instantiate(Enemy[rnd]);
        yield return new WaitForSeconds(nextEnemy);
    }

}