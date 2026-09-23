using System.Collections;
using Unity.Entities;
using UnityEngine;

// Presentation for a tower. Stats come from the TowerCardDefinition that spawned it (set by
// SimulationBridge before Start); combat logic lives in Bunker.Simulation.
public abstract class TurretCard : TurretStats, IDamageable
{
    // Mirrored from the simulation every frame; the life bar reads it
    [System.NonSerialized] public float Life;

    [SerializeField] protected GameObject BulletParticle;
    [SerializeField] GameObject TurretLifeSlider;
    [SerializeField] float TurretLifeSliderOffset;
    [SerializeField] Transform BuffSpritePosition;
    [HideInInspector] public GameObject _BuffSpritePosition;
    [HideInInspector] public bool HaveBuff;

    public Entity Entity { get; set; }
    public TowerCardDefinition Definition { get; set; }
    public float Range => Definition != null ? Definition.range : 0f;
    public virtual Vector3 MuzzlePosition => transform.position;

    protected void Awake() => TowerLimits.Register(this);
    protected void OnDestroy() => TowerLimits.Unregister(this);

    protected void Start()
    {
        if (TurretLifeSlider != null)
            Instantiate(TurretLifeSlider, transform.position + new Vector3(0f, TurretLifeSliderOffset, 0f), Quaternion.identity, transform);
    }

    public abstract void OnFired(Vector3 targetPosition);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Range);
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

    // Plays the buff burst at the tower's feet, then pins the stat icon once the burst ends
    public void ApplyBuff(GameObject buffIcon, GameObject buffEffect, Color effectTint)
    {
        float delay = 0f;
        if (buffEffect != null)
        {
            GameObject fx = Instantiate(buffEffect, FootPosition(), Quaternion.identity, transform);
            if (fx.TryGetComponent(out OneShotEffect shot))
            {
                shot.SetTint(effectTint);
                shot.DrawAbove(GetComponentInChildren<SpriteRenderer>());
                delay = shot.Duration;
            }
        }

        if (buffIcon == null) return;
        if (delay > 0f)
            StartCoroutine(ShowBuffSpriteAfter(buffIcon, delay));
        else
            ShowBuffSprite(buffIcon);
    }

    IEnumerator ShowBuffSpriteAfter(GameObject buffIcon, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowBuffSprite(buffIcon);
    }

    // Bottom edge of the occupied (base) cell; falls back to the pivot when placed off-grid
    Vector3 FootPosition()
    {
        if (PlacementGrid.Available && TryGetComponent(out GridOccupant occupant))
            return PlacementGrid.CellCenter(occupant.Cell) - new Vector3(0f, PlacementGrid.CellSize.y * 0.5f, 0f);
        return transform.position;
    }
}
