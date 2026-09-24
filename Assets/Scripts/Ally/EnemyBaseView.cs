using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

// Presentation of the enemy base the allies attack. Its life lives in the simulation; destroying it wins.
public class EnemyBaseView : MonoBehaviour
{
    [Tooltip("Used when no LevelDefinition sets the base life")]
    [Min(1f)] [SerializeField] float defaultLife = 500f;
    [SerializeField] Slider lifeBar;
    [SerializeField] GameObject hitParticle;
    [SerializeField] GameObject destroyedParticle;

    public Entity Entity { get; set; }
    public float StartLife => LevelSetup.Current != null ? LevelSetup.Current.enemyBaseLife : defaultLife;

    void Start()
    {
        if (lifeBar == null) return;
        lifeBar.minValue = 0f;
        lifeBar.maxValue = StartLife;
        lifeBar.value = StartLife;
    }

    public void OnHit(float remainingLife)
    {
        if (lifeBar != null) lifeBar.value = remainingLife;
        if (hitParticle != null)
            Instantiate(hitParticle, transform.position, Quaternion.identity);
    }

    public void OnDestroyed()
    {
        if (destroyedParticle != null)
            Instantiate(destroyedParticle, transform.position, Quaternion.identity);
    }
}
