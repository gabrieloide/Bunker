using System.Collections.Generic;
using UnityEngine;

// Presentation of an ally. Stats live in AllyData; movement and melee run in Bunker.Simulation.AllySystem.
public class AllyView : MonoBehaviour
{
    public AllyData Data;

    // Allies on the map, for the enemy flags they knock down
    static readonly List<AllyView> active = new List<AllyView>();
    public static IReadOnlyList<AllyView> Active => active;

    [Header("Sound")]
    [SerializeField] Sfx attackSound = Sfx.AllyAttack;
    [SerializeField] Sfx deathSound = Sfx.AllyDeath;
    [Space]
    [SerializeField] GameObject deathParticle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => active.Clear();

    void OnEnable() => active.Add(this);
    void OnDisable() => active.Remove(this);

    public void OnAttack()
    {
        SoundBank.Play(attackSound);
    }

    public void OnDied()
    {
        if (deathParticle != null)
            OneShotEffect.EnsureFrontSorting(Instantiate(deathParticle, transform.position, Quaternion.identity));
        SoundBank.Play(deathSound);
    }
}
