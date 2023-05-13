using UnityEngine;
public class Enemy : MonoBehaviour, IDamageable
{
    public EnemyData Data;
    public AK.Wwise.Event shoot;
    public AK.Wwise.Event destroy;
    EnemyMovement enemyMovement;
    SpriteRenderer spriteRenderer;
    [SerializeField] GameObject hitParticle, explosionParticle;
    [SerializeField] float view;
    [SerializeField] LayerMask ally;
    int nextWavePosition;
    [SerializeField] LootBag LootBagCom;
    private float fireRate;
    [HideInInspector]public float Life;
    private void Start()
    {
        LootBagCom = FindObjectOfType<LootBag>();
        enemyMovement = FindObjectOfType<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        fireRate = Data.FireRate();
        Life = Data.Life();
    }
    private void Update()
    {
        if (Life > 0)
        {
            RaycastHit2D contact = Physics2D.Raycast(transform.position, Vector2.left, view, ally);
            if (!contact)
            {
                Move();
            }
            else
            {
                attackAlly(contact);
            }
        }
        else
        {
            Data.LifeBehaviour(explosionParticle, transform.position, LootBagCom, gameObject);
        }
    }
    void Move()
    {
        //Movimiento del enemigo
        if (nextWavePosition < enemyMovement.points.Length - 1)
        {
            float dis = Vector2.Distance(transform.position, enemyMovement.points[nextWavePosition]);
            if (dis < 0.1f)
            {
                nextWavePosition++;
            }
            transform.position = Vector3.MoveTowards(transform.position, enemyMovement.points[nextWavePosition], Data.MoveSpeed() * Time.deltaTime);
            Data.flip(enemyMovement.points[nextWavePosition].x, transform.position.x, spriteRenderer);
        }
        else if (nextWavePosition == enemyMovement.points.Length - 1)
        {
            TowerPlayer.instance.DealDamage(Data.Damage());
        }
    }
    void attackAlly(RaycastHit2D contact)
    {
        fireRate -= Time.deltaTime;
        if (fireRate < 0)
        {
            //Atacar aliados del camino
            contact.collider.GetComponent<EventAllyCreation>().LifeAlly -= Data.Damage();
            shoot.Post(gameObject);
            fireRate = Data.FireRate();
            
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