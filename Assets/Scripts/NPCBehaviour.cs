using UnityEngine;

// Presentation base for path-walking NPCs. Stats live in EnemyData; movement and attacks run in Bunker.Simulation.
public class NPCBehaviour : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] protected Sfx shoot = Sfx.EnemyShot;
    [SerializeField] protected Sfx destroy = Sfx.EnemyDeath;

    public void OnFired()
    {
        SoundBank.Play(shoot);
    }
}
