using UnityEngine;
public class Enemy : MonoBehaviour
{
    public EnemyData Data;
    public AK.Wwise.Event tank_destroy;
    EnemyMovement enemyMovement;
    SpriteRenderer spriteRenderer;
    [SerializeField] GameObject hitParticle, explosionParticle;
    [SerializeField] float view;
    [SerializeField] LayerMask ally;
    int nextWavePosition;
    [SerializeField] LootBag LootBagCom;
    private float fireRate;
    [HideInInspector]public float Life;
    [SerializeField] GameObject DamageText;
    [SerializeField] LeanTweenType DamageTextAnimTween;
    [SerializeField] float DamageTime;
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
            fireRate = Data.FireRate();
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            //Rebre mal
            Instantiate(hitParticle, transform.position, Quaternion.identity);
            TakeDamage(collision);
            tank_destroy.Post(gameObject);
        }
    }
    void TakeDamage(Collider2D collision)
    {
        var bullet = collision.GetComponent<TowerBullet>();
        float realDamage = bullet.Damage - (bullet.BulletPen - Data.Defense());
        GameObject DamageTxt = Instantiate(DamageText, transform.position, Quaternion.identity);
        Life -= realDamage;
        LeanTween.moveLocalY(DamageTxt, 3, DamageTime).setEase(DamageTextAnimTween);
        Destroy(DamageTxt, DamageTime);
    }
}