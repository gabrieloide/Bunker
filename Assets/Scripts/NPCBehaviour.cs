using UnityEngine;

// Presentation base for path-walking NPCs. Stats live in EnemyData; movement and attacks run in Bunker.Simulation.
public class NPCBehaviour : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] protected AK.Wwise.Event shoot;
    [SerializeField] protected AK.Wwise.Event destroy;

    public void OnFired()
    {
        shoot.Post(gameObject);
    }
}
