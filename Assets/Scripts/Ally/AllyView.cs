using UnityEngine;

// Presentation of an ally. Stats live in AllyData; movement and melee run in Bunker.Simulation.AllySystem.
public class AllyView : MonoBehaviour
{
    public AllyData Data;

    [Header("Sound")]
    [SerializeField] Sfx attackSound = Sfx.AllyAttack;
    [SerializeField] Sfx deathSound = Sfx.AllyDeath;
    [Space]
    [SerializeField] GameObject deathParticle;

    public void OnAttack()
    {
        SoundBank.Play(attackSound);
    }

    public void OnDied()
    {
        if (deathParticle != null)
            Instantiate(deathParticle, transform.position, Quaternion.identity);
        SoundBank.Play(deathSound);
    }
}
