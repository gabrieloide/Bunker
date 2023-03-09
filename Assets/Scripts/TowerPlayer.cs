using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerPlayer : MonoBehaviour
{
    public static TowerPlayer instance;
    public float life;
    float dealTime;
    public float DealTime;
    [SerializeField] LayerMask EnemyLayer;
    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start() => dealTime = DealTime;
    public void DealDamage(float enemyDamage)
    {
        dealTime -= Time.deltaTime;
        if (dealTime < 0)
        {
            life -= enemyDamage;
            dealTime = DealTime;
        }
    }
}