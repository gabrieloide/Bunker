using UnityEngine;

// Authoring + presentation base for path-walking NPCs. Movement and attacks run in Bunker.Simulation.
public class NPCBehaviour : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] protected AK.Wwise.Event shoot;
    [SerializeField] protected AK.Wwise.Event destroy;
    [Space]

    [Range(3, 20)][SerializeField] float radius;
    [SerializeField] protected float FireRate;
    public float fireRateCountDown;

    public float AttackRadius => radius;
    public float FireInterval => FireRate;

    public void OnFired()
    {
        shoot.Post(gameObject);
    }
}
