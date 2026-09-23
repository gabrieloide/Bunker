using UnityEngine;

// Shell and impact visuals for an artillery tower. The trajectory and splash numbers live in its
// ArtilleryTowerCardDefinition; SimulationBridge reads both when spawning the tower.
public class ArtilleryAuthoring : MonoBehaviour
{
    [Header("Presentation")]
    public ArtilleryShellView shellPrefab;
    public GameObject impactEffect;
    public AK.Wwise.Event impactSound;
}
