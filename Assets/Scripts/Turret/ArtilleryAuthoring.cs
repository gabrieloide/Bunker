using UnityEngine;

// Shell and impact visuals for an artillery tower. The trajectory and splash numbers live in its
// ArtilleryTowerCardDefinition; SimulationBridge reads both when spawning the tower.
public class ArtilleryAuthoring : MonoBehaviour
{
    [Header("Presentation")]
    public ArtilleryShellView shellPrefab;
    public GameObject impactEffect;
    public AK.Wwise.Event impactSound;

    // LEGACY: moved to ArtilleryTowerCardDefinition, dropped after migration
    [HideInInspector] public float riseTime = 0.45f;
    [HideInInspector] public float fallTime = 0.55f;
    [HideInInspector] public float height = 9f;
    [HideInInspector] public float splashRadius = 1.25f;
}
