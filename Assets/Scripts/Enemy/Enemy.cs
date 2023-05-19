using UnityEngine;
public class Enemy : NPCBehaviour, IDamageable
{
    public EnemyData Data;
    public AK.Wwise.Event shoot;
    public AK.Wwise.Event destroy;
    EnemyMovement enemyMovement;
    [SerializeField] GameObject hitParticle, explosionParticle;
    [SerializeField] float view;
    [SerializeField] LayerMask ally;
    [SerializeField] LootBag LootBagCom;
    [HideInInspector] public float Life;
    private float fireRate;
    private void Start()
    {
        LootBagCom = FindObjectOfType<LootBag>();
        enemyMovement = FindObjectOfType<EnemyMovement>();
        fireRate = Data.FireRate();
        StartCoroutine(MoveAlongPath(enemyMovement.points, false, gameObject, Data.MoveSpeed(), transform));
        Life = Data.Life();
    }
    private void Update()
    {
        if (Life < 0)
        {
            Data.LifeBehaviour(explosionParticle, transform.position, LootBagCom, gameObject);
        }
    }
    public void Damage(float damage, float bulletPen, GameObject deactivateBullet)
    {
        //Rebre mal
        float realDamage = damage - (bulletPen - Data.Defense());
        Instantiate(hitParticle, transform.position, Quaternion.identity);
        destroy.Post(gameObject);
        Life -= realDamage;
        deactivateBullet.SetActive(false);
    }
}