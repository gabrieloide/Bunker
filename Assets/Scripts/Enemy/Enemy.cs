using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Enemy : MonoBehaviour
{
    public EnemyData Data;
    public AK.Wwise.Event tank_destroy;
    public float Life;
    EnemyMovement enemyMovement;
    SpriteRenderer spriteRenderer;
    [SerializeField] GameObject hitParticle, explosionParticle;
    public int nextWavePosition;
    GameObject LootBagCom;

    private void Start()
    {
        LootBagCom = GameObject.Find("DropCardManager");
        enemyMovement = FindObjectOfType<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        Life = Data.Life;
    }
    private void Update()
    {
        Move();
        if (Life < 0)
        {
            Data.LifeBehaviour(explosionParticle, transform.position, LootBagCom, gameObject);
        }
    }
    void Move()
    {
        if (nextWavePosition < enemyMovement.points.Length - 1)
        {
            float dis = Vector2.Distance(transform.position, enemyMovement.points[nextWavePosition]);
            if (dis < 0.1f)
            {
                nextWavePosition++;
                if (nextWavePosition == enemyMovement.points.Length - 1)
                {
                    Debug.Log("Disparar");
                }
            }
            transform.position = Vector3.MoveTowards(transform.position, enemyMovement.points[nextWavePosition], Data.MoveSpeed * Time.deltaTime);
            flip();
        }


    }
    void flip()
    {
        bool n = enemyMovement.points[nextWavePosition].x < transform.position.x ? spriteRenderer.flipX = true : spriteRenderer.flipX = false;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Instantiate(hitParticle, transform.position, Quaternion.identity);
            Life -= collision.GetComponent<TowerBullet>().damage;
            collision.GetComponent<TowerBullet>().lifeBullet -= 1;
            tank_destroy.Post(gameObject);
        }
    }
}