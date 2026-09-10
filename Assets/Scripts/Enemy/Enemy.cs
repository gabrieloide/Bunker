using Unity.Entities;
using UnityEngine;

public class Enemy : NPCBehaviour, IDamageable
{
    public EnemyData Data;

    [SerializeField] GameObject hitParticle, explosionParticle;
    [HideInInspector] public float Life;

    public Entity Entity { get; set; }
    public GameObject ExplosionParticle => explosionParticle;

    public void OnHit()
    {
        if (hitParticle != null)
            hitParticle.SetActive(true);

        destroy.Post(gameObject);
    }

    public void OnReachedEnd()
    {
        if (TryGetComponent<Animator>(out var animator))
            animator.enabled = false;
    }

    public void OnDied()
    {
        destroy.Post(gameObject);
    }

    public void Damage(float damage, float bulletPen, GameObject deactivateBullet)
    {
        OnHit();
        if (SimulationBridge.Instance != null)
            SimulationBridge.Instance.RequestDamage(Entity, damage, bulletPen);
        if (deactivateBullet != null)
            deactivateBullet.SetActive(false);
    }

    public void ResetForPool()
    {
        if (hitParticle != null)
            hitParticle.SetActive(false);
        if (TryGetComponent<Animator>(out var animator))
            animator.enabled = true;
        transform.localScale = Vector3.one;
    }
}
