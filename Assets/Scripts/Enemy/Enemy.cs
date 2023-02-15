using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Enemy : MonoBehaviour
{
    public AK.Wwise.Event tank_destroy;
    //-------------------------STATS--------------------------------
    [SerializeField]EnemyData enemyData;
    public float Life;
    float Damage;
    float Defense;
    float MoveSpeed;
    //--------------------------------------------------------------
    EnemyMovement enemyMovement;
    SpriteRenderer spriteRenderer;
    [SerializeField] GameObject hitParticle, explosionParticle;
    public int nextWavePosition;
    GameObject c;

    private void Start()
    {
        c = GameObject.Find("DropCardManager");
        enemyMovement = FindObjectOfType<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initializeStats();
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
            Instantiate(explosionParticle, transform.position, transform.rotation);
            
            UIManager.instance.score += enemyData.score;
            EnemySpawner.instance.EnemyAmount--;
            c.GetComponent<LootBag>().InstantiateLoot();
            Destroy(gameObject);
        }
    }
    public void initializeStats()
    {
        Life = enemyData.Life;
        Damage = enemyData.Damage;
        Defense= enemyData.Defense;
        MoveSpeed= enemyData.MoveSpeed;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
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
        bool n = enemyMovement.points[nextWavePosition].x < transform.position.x ? spriteRenderer.flipX = true : spriteRenderer.flipX = false;
    }
}
