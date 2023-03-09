using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventAllyCreation : MonoBehaviour
{
    [SerializeField] TowersData Data;
    EnemyMovement enemyMovement;
    int nextWavePosition;
    [SerializeField] LayerMask enemyLayer;
    float life;
    [HideInInspector] public float LifeAlly { get => life; set => life = value; }
    float fireRate
    {
        get => Data.fireRate;
        set
        {
            if (fireRate != Data.fireRate)
            {
                fireRate = Data.fireRate;
            }
        }
    }

    private void Start()
    {
        enemyMovement = FindObjectOfType<EnemyMovement>();
        nextWavePosition = enemyMovement.points.Length - 1;
        transform.position = new Vector3(4, -0.5f, 0);
    }
    private void Update()
    {
        Debug.Log(Data.MoveSpeed);
        RaycastHit2D contact = Physics2D.Raycast(transform.position, Vector2.right, Data.View, enemyLayer);
        if (!contact)
        {
            Move();
            //allyData.flip(enemyMovement.points[nextWavePosition].x, transform.position.x, spriteRenderer);
            if (enemyMovement.points[nextWavePosition].x < transform.position.x)
            {
                transform.localScale = new Vector2(-1, 1);
            }
            else
            {
                transform.localScale = new Vector2(1,1);
            }
        }
        else
        {
            attackEnemy(contact);
        }
        if (life < 0)
        {
            Destroy(gameObject);
        }
    }
    void Move()
    {
        float dis = Vector2.Distance(transform.position, enemyMovement.points[nextWavePosition]);
        if (dis < 0.1f)
        {
            nextWavePosition--;

        }
        transform.position = Vector3.MoveTowards(transform.position, enemyMovement.points[nextWavePosition], Data.MoveSpeed * Time.deltaTime);
    }
    void attackEnemy(RaycastHit2D contact)
    {
        fireRate -= Time.deltaTime;
        if (fireRate < 0)
        {
            contact.collider.GetComponent<Enemy>().Life -= Data.damage;
            fireRate = Data.fireRate;
        }
    }
}
