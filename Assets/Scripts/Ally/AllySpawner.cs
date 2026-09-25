using UnityEngine;

// Scene authoring for allies: where they leave from and the roster they cycle through.
// Timing comes from the LevelDefinition; these values are the fallback when no level is loaded.
public class AllySpawner : MonoBehaviour
{
    [Tooltip("Sent out in this order, looping")]
    public AllyView[] Allies;
    [Tooltip("Seconds between two allies when no LevelDefinition overrides it; 0 = no allies")]
    [Min(0f)] public float defaultInterval = 6f;
    [Min(0f)] public float defaultFirstDelay = 4f;

    public float Interval => LevelSetup.Current != null ? LevelSetup.Current.allySpawnInterval : defaultInterval;
    public float FirstDelay => LevelSetup.Current != null ? LevelSetup.Current.firstAllyDelay : defaultFirstDelay;
}
