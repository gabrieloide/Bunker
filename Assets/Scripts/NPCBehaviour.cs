using UnityEngine;

// Presentation base for path-walking NPCs. Stats live in EnemyData; movement and attacks run in Bunker.Simulation.
public class NPCBehaviour : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] protected AK.Wwise.Event shoot;
    [SerializeField] protected AK.Wwise.Event destroy;

    // LEGACY: moved to EnemyData, dropped after migration
    [HideInInspector][SerializeField] float radius;
    [HideInInspector][SerializeField] protected float FireRate;
    public float LegacyAttackRadius => radius;
    public float LegacyFireInterval => FireRate;

    public void OnFired()
    {
        shoot.Post(gameObject);
    }
}
