using Unity.Entities;
using UnityEngine;

// Authoring + presentation for a tower. Stats are read once by SimulationBridge when the
// entity is created; combat logic lives in Bunker.Simulation.
public abstract class TurretCard : TurretStats, IDamageable
{
    public float Life;

    [Space]
    [SerializeField] public float fireRateCountDown = 0f;
    [SerializeField] float fireRate = 0;

    [Range(3, 20)][SerializeField] protected float range = 3f;
    [SerializeField] protected GameObject BulletParticle;
    [SerializeField] GameObject TurretLifeSlider;
    [SerializeField] float TurretLifeSliderOffset;
    [SerializeField] Transform BuffSpritePosition;
    [HideInInspector] public GameObject _BuffSpritePosition;
    [HideInInspector] public bool HaveBuff;

    public Entity Entity { get; set; }
    public float FireRate => fireRate;
    public float Range => range;
    public virtual Vector3 MuzzlePosition => transform.position;

    protected void Start()
    {
        if (TurretLifeSlider != null)
            Instantiate(TurretLifeSlider, transform.position + new Vector3(0f, TurretLifeSliderOffset, 0f), Quaternion.identity, transform);
    }

    public abstract void OnFired(Vector3 targetPosition);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }

    public void Damage(float damage, float bulletPen, GameObject deactivateBullet)
    {
        if (SimulationBridge.Instance != null)
            SimulationBridge.Instance.RequestDamage(Entity, damage, bulletPen);
        if (deactivateBullet != null)
            deactivateBullet.SetActive(false);
    }

    public void ShowBuffSprite(GameObject BuffSprite)
    {
        _BuffSpritePosition = Instantiate(BuffSprite, BuffSpritePosition.position, Quaternion.identity, BuffSpritePosition);
    }
}
