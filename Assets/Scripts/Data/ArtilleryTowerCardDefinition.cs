using UnityEngine;

// Tower that lobs shells with splash damage. The placed prefab needs an ArtilleryAuthoring
// component for the shell/impact visuals.
[CreateAssetMenu(fileName = "New Artillery Card", menuName = "Bunker/Cards/Artillery Tower")]
public class ArtilleryTowerCardDefinition : TowerCardDefinition
{
    [Header("Artillery")]
    [Min(0f)] public float splashRadius = 1.25f;
    [Min(0.05f)] public float riseTime = 0.45f;
    [Min(0.05f)] public float fallTime = 0.55f;
    [Tooltip("Apex height in world units; keep it above the camera's half height so the apex jump happens off-screen")]
    [Min(0f)] public float height = 9f;
}
