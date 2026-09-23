using UnityEngine;

// Turns a tower into artillery: SimulationBridge reads this when spawning the tower and the
// simulation fires lobbed shells with splash damage instead of straight bullets.
public class ArtilleryAuthoring : MonoBehaviour
{
    [Header("Trajectory")]
    [Min(0.05f)] public float riseTime = 0.45f;
    [Min(0.05f)] public float fallTime = 0.55f;
    [Tooltip("Apex height in world units; keep it above the camera's half height so the apex jump happens off-screen")]
    [Min(0f)] public float height = 9f;

    [Header("Impact")]
    [Min(0f)] public float splashRadius = 1.25f;

    [Header("Presentation")]
    public ArtilleryShellView shellPrefab;
    public GameObject impactEffect;
    public AK.Wwise.Event impactSound;
}
