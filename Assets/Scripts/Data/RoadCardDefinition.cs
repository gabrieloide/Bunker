using UnityEngine;

[CreateAssetMenu(fileName = "New Road Card", menuName = "Bunker/Cards/Road")]
public class RoadCardDefinition : CardDefinition
{
    [Header("Road")]
    [Tooltip("Maximum number of grid tiles this card can build in one use")]
    [Min(1)] public int maxTilesPerCard = 5;

    [Tooltip("Seconds the builder takes to construct the road segment")]
    [Min(0f)] public float buildTime = 0.6f;

    [Tooltip("Half-width of the rendered road strip in world units")]
    [Min(0.05f)] public float halfWidth = 0.16f;
}
