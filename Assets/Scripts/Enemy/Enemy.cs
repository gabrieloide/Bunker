using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Enemy : MonoBehaviour
{
    //-------------------------STATS--------------------------------
    [SerializeField]EnemyData enemyData;
    float Life;
    float Damage;
    float Defense;
    float MoveSpeed;
    //--------------------------------------------------------------
    public static Enemy instance;
    EnemyMovement enemyMovement;
    CardDrop cardDrop;
    int i;
    public int route;
    bool changeWave;

    private void Start()
    {
        if (!instance)
        {
            instance = this;
        }
        cardDrop = FindObjectOfType<CardDrop>();
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
        if (i < enemyMovement.points.Length)
        {   
            transform.position = Vector3.MoveTowards(transform.position, enemyMovement.points[i], MoveSpeed * Time.deltaTime);
        }
        // para las oledas, cambiar el primer movetowars hacia la primera posicion que se quiere llegar, despues cambiar el i por la siguiente oleada del mapa
        //if (changeWave)
        //{
        //    i -= 3;
        //    changeWave = false;
        //}
    }
    void LifeBehaviour()
    {
        if (Life <= 0)
        {
            ChanceToDrop();
            Destroy(gameObject);
        }
    }
    void ChanceToDrop()
    {
        float item = Random.Range(0, 1001);

        if (item <= 190.67f)
        {
            Instantiate(cardDrop.Cards[0], transform.position, transform.rotation);
        }
        else if (item >580.8f && item < 590.7f)
        {
            Instantiate(cardDrop.Cards[1], transform.position, transform.rotation);
        }
        else if(item > 930.7f)
        {
            Instantiate(cardDrop.Cards[2], transform.position, transform.rotation);
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
}
