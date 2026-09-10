using Unity.Entities;
using UnityEngine;

public class Enemy : NPCBehaviour, IDamageable
{
    public EnemyData Data;

    [SerializeField] GameObject hitParticle, explosionParticle;
    [HideInInspector] public float Life;

    public Entity Entity { get; set; }
    public GameObject ExplosionParticle => explosionParticle;

    SpriteRenderer spriteRenderer;
    Coroutine flashCoroutine;
    Color originalColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        else
            originalColor = Color.white;
    }

    public void OnHit()
    {
        if (hitParticle != null)
            hitParticle.SetActive(true);

        FlashOnHit();
        destroy.Post(gameObject);
    }

    public void FlashOnHit()
    {
        if (spriteRenderer == null) return;
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    System.Collections.IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = new Color(1f, 0.25f, 0.25f, 1f);
        transform.localScale = new Vector3(transform.localScale.x * 1.15f, transform.localScale.y * 0.9f, 1f);
        yield return new WaitForSeconds(0.06f);
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        transform.localScale = Vector3.one;
        flashCoroutine = null;
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
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        if (hitParticle != null)
            hitParticle.SetActive(false);
        if (TryGetComponent<Animator>(out var animator))
            animator.enabled = true;
        transform.localScale = Vector3.one;
    }
}
