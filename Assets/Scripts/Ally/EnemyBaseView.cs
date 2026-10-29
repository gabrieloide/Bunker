using Unity.Entities;
using UnityEngine;

// Presentation of the enemy base the allies attack. Its life lives in the simulation; destroying it wins.
public class EnemyBaseView : MonoBehaviour
{
    [Tooltip("Used when no LevelDefinition sets the base life")]
    [Min(1f)] [SerializeField] float defaultLife = 500f;
    [SerializeField] WorldLifeBar lifeBar;
    [SerializeField] GameObject hitParticle;
    [SerializeField] GameObject destroyedParticle;

    public Entity Entity { get; set; }
    public float StartLife => LevelSetup.Current != null ? LevelSetup.Current.enemyBaseLife : defaultLife;

    void Start()
    {
        if (lifeBar != null) lifeBar.SetValue(StartLife, StartLife);
    }

    public void OnHit(float remainingLife)
    {
        if (lifeBar != null) lifeBar.SetValue(remainingLife, StartLife);
        if (hitParticle != null)
            Instantiate(hitParticle, transform.position, Quaternion.identity);
    }

    public void OnDestroyed()
    {
        if (destroyedParticle != null)
            Instantiate(destroyedParticle, transform.position, Quaternion.identity);
    }
}
