using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventAllyCreation : MonoBehaviour
{
    [SerializeField] TowersData Data;
    EnemyMovement enemyMovement;
    int nextWavePosition;
    [SerializeField] LayerMask enemyLayer;
    public float life;
    float fireRate;

    private void Start()
    {
        life = Data.Life;
        fireRate = Data.fireRate;
        enemyMovement = FindObjectOfType<EnemyMovement>();
        nextWavePosition = enemyMovement.points.Length - 1;
        transform.position = new Vector3(4, -0.5f, 0);
    }
    private void Update()
    {

        RaycastHit2D contact = Physics2D.Raycast(transform.position, Vector2.right, Data.View, enemyLayer);
        if (!contact)
        {
            Move();
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
        if (life > 0)
        {
            float dis = Vector2.Distance(transform.position, enemyMovement.points[nextWavePosition]);
            if (dis < 0.1f)
            {
                nextWavePosition--;

            }
            transform.position = Vector3.MoveTowards(transform.position, enemyMovement.points[nextWavePosition], Data.MoveSpeed * Time.deltaTime);
        }
        //allyData.flip(enemyMovement.points[nextWavePosition].x, transform.position.x, spriteRenderer);
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
