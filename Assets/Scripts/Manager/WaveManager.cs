using UnityEngine;

public enum BuffEnemyType
{
    NormalEnemy,
    BuffAttack,
    BuffDefense,
    BuffVelocity,
    BuffLife,
    BuffFireRate
}

// Read-only mirror of Bunker.Simulation.WaveState for UI; updated by SimulationBridge.
public class WaveManager : MonoBehaviour
{
    public BuffEnemyType buffEnemyType = BuffEnemyType.NormalEnemy;
    public static WaveManager instance;
    public int Wave;
    public int MinPorcent, MaxPorcent;
    public GameObject winScreen;

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);
    }
}
