using UnityEngine;

public class TowerPlayer : MonoBehaviour
{
    public static TowerPlayer instance;
    public float life;
    float dealTime;
    [SerializeField] GameObject hitParticle;
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
            //Recibir da;o a la torre
            life -= enemyDamage;
            Instantiate(hitParticle, transform.position + new Vector3(1,0), Quaternion.identity, transform);
            dealTime = DealTime;
        }
    }
}