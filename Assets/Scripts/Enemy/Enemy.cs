using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Enemy : MonoBehaviour
{
    //-------------------------STATS--------------------------------
    [SerializeField]EnemyData enemyData;
    public float Life;
    public float Damage;
    float Defense;
    float MoveSpeed;
    //---------------------------------------------------------
    EnemyMovement enemyMovement;
    TowerD towerD;
    int i;

    private void Start()
    {
        enemyMovement = FindObjectOfType<EnemyMovement>();
        initializeStats();
    }
    private void Update()
    {
        Move();
        LifeBehaviour();
    }
    void Move()
    {
        float dis = Vector2.Distance(transform.position, enemyMovement.points[i]);
        if (dis < 0.1f)
        {
            i++;
            if (i == enemyMovement.points.Length)
            {
                Destroy(gameObject);
            }
        }
        if(i< enemyMovement.points.Length) transform.position = Vector3.MoveTowards(transform.position, enemyMovement.Points[i], MoveSpeed * Time.deltaTime);
    }
    void LifeBehaviour()
    {
        if (Life <= 0)
        {
            Destroy(gameObject);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            TowerPlayer.instance.life -= Damage;
        }
        if (collision.CompareTag("Bullet"))
        {
            DealDamage();
        }
    }
    void DealDamage()
    {
       Life--;
    }
    void initializeStats() 
    {
        Life = enemyData.Life;
        Damage = enemyData.Damage;
        Defense = enemyData.Defense;
        MoveSpeed = enemyData.MoveSpeed;
    }
}
