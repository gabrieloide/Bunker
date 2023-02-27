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
    [SerializeField] float view;
    [SerializeField] LayerMask ally;
    int nextWavePosition;
    GameObject LootBagCom;
    private float fireRate;

    private void Start()
    {
        LootBagCom = GameObject.Find("DropCardManager");
        enemyMovement = FindObjectOfType<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        Life = Data.Life;
        fireRate = Data.fireRate;
    }
    private void Update()
    {

        if (Life < 0)
        {
            Data.LifeBehaviour(explosionParticle, transform.position, LootBagCom, gameObject);
        }
        RaycastHit2D contact = Physics2D.Raycast(transform.position, Vector2.left, view, ally);
        if(!contact)
        {
            Move();
        }
        else
        {
            attackAlly(contact);
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

            }
            transform.position = Vector3.MoveTowards(transform.position, enemyMovement.points[nextWavePosition], Data.MoveSpeed * Time.deltaTime);
            Data.flip(enemyMovement.points[nextWavePosition].x, transform.position.x, spriteRenderer);
        }
    }
    void attackAlly(RaycastHit2D contact)
    {
        fireRate -= Time.deltaTime;
        if (fireRate < 0)
        {
            contact.collider.GetComponent<EventAllyCreation>().life -= Data.Damage;
            fireRate = Data.fireRate;
        }
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