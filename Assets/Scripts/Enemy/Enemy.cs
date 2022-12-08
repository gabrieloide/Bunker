using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Enemy1,
    Enemy2,
    Enemy3,
    Enemy4
}
public class Enemy : MonoBehaviour
{
    public EnemyType enemyType = EnemyType.Enemy1;
//---------------------------------------------------------
    public float Life;
    [HideInInspector]public float Damage;
    float Defense;
    float MoveSpeed;
    //---------------------------------------------------------
    EnemySpawner enemySpawner;
    EnemyMovement enemyMovement;
    TowerD towerD;
    int i;

    private void Start()
    {
        enemySpawner = FindObjectOfType<EnemySpawner>();
        enemyMovement = FindObjectOfType<EnemyMovement>();
    }
    private void Update()
    {
        Move();
        EnemyDamage();
        LifeBehaviour();
        if (Input.GetKeyDown(KeyCode.A))
        {
            for (int i = 0; i < enemySpawner._Life.Length; i++)
            {
                enemySpawner._Life[i] -= 100f;
            }
            Debug.Log("234");
        }
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
    void EnemyDamage()
    {
        switch (enemyType)
        {
            //Stats de Enemigo 1
            case EnemyType.Enemy1:
                Damage = enemySpawner._Damage[0];
                Life = enemySpawner._Life[0];
                Defense = enemySpawner._Defense[0];
                MoveSpeed = enemySpawner._MoveSpeed[0];
                break;
                //Stats de Enemigo 2
            case EnemyType.Enemy2:
                Damage = enemySpawner._Damage[1];
                Life = enemySpawner._Life[1];
                Defense = enemySpawner._Defense[1];
                MoveSpeed = enemySpawner._MoveSpeed[1];
                break;
            //Stats de Enemigo 3
            case EnemyType.Enemy3:
                Damage = enemySpawner._Damage[2];
                Life = enemySpawner._Life[2];
                Defense = enemySpawner._Defense[2];
                MoveSpeed = enemySpawner._MoveSpeed[2];
                break;
            //Stats de Enemigo 4
            case EnemyType.Enemy4:
                Damage = enemySpawner._Damage[3];
                Life = enemySpawner._Life[3];
                Defense = enemySpawner._Defense[3];
                MoveSpeed = enemySpawner._MoveSpeed[3];
                break;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            TowerPlayer.instance.life = TowerPlayer.instance.life - Damage;
        }
    }
}
