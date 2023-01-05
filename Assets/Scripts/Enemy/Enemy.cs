using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Enemy : MonoBehaviour
{
    public static Enemy instance;
    public AK.Wwise.Event tank_destroy;
    //-------------------------STATS--------------------------------
    [SerializeField]EnemyData enemyData;
    public float Life;
    float Damage;
    float Defense;
    float MoveSpeed;
    //--------------------------------------------------------------
    EnemyMovement enemyMovement;
    CardDrop cardDrop;
    SpriteRenderer spriteRenderer;
    [SerializeField] GameObject hitParticle;
    public int nextWavePosition;

    private void Start()
    {
        if (!instance) 
        {
            instance = this;
        }
        enemyMovement = FindObjectOfType<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initializeStats();
        if (WaveManager.instance.Wave == 3 || WaveManager.instance.Wave == 4)
        {
            nextWavePosition = 7;
        }
        else if (WaveManager.instance.Wave == 5 || WaveManager.instance.Wave == 6)
        {
            nextWavePosition = 1;
        }
    }
    private void Update()
    {
        Move();
        LifeBehaviour();
    }
    void Move()
    {
        float dis = Vector2.Distance(transform.position, enemyMovement.points[nextWavePosition]);
        if (dis < 0.1f)
        {
            nextWavePosition++;
            if (nextWavePosition == enemyMovement.points.Length)
            {
                Destroy(gameObject);
            }
        }
        if (nextWavePosition < enemyMovement.points.Length)
        {   
            transform.position = Vector3.MoveTowards(transform.position, enemyMovement.points[nextWavePosition], MoveSpeed * Time.deltaTime);
            flip();
        }
    }
    void LifeBehaviour()
    {
        if (Life <= 0)
        {
            GetComponent<LootBag>().InstantiateLoot(transform.position);
            UIManager.instance.score += enemyData.score;
            WaveManager.instance.enemyAmount--;
            Destroy(gameObject);
        }
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
            Instantiate(hitParticle, transform.position, Quaternion.identity);
            Life -= collision.GetComponent<TowerBullet>().damage;
            collision.GetComponent<TowerBullet>().lifeBullet -= 1;
           // tank_destroy.Post(gameObject);
        }
    }
    void flip()
    {
        if (enemyMovement.points[nextWavePosition].x < transform.position.x)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }
}
