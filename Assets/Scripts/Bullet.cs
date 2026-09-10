using UnityEngine;

// Pooled projectile view. Movement and collision run in Bunker.Simulation.ProjectileSystem.
public class Bullet : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] AK.Wwise.Event HitSound;
    [Space]

    [SerializeField] private float speed;
    [HideInInspector] public Vector3 target;
    [SerializeField] protected float timeToDestroy = 5;

    public float Speed => speed;
    public float Lifetime => timeToDestroy;

    public void PlayHitSound()
    {
        HitSound.Post(gameObject);
    }
}
