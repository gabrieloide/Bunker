using UnityEngine;

// Presentation of an ally. Stats live in AllyData; movement and melee run in Bunker.Simulation.AllySystem.
public class AllyView : MonoBehaviour
{
    public AllyData Data;

    [Header("Sound")]
    [SerializeField] AK.Wwise.Event attackSound;
    [SerializeField] AK.Wwise.Event deathSound;
    [Space]
    [SerializeField] GameObject deathParticle;

    public void OnAttack()
    {
        if (attackSound != null && attackSound.IsValid())
            attackSound.Post(gameObject);
    }

    public void OnDied()
    {
        if (deathParticle != null)
            Instantiate(deathParticle, transform.position, Quaternion.identity);
        if (deathSound != null && deathSound.IsValid())
            deathSound.Post(gameObject);
    }
}
