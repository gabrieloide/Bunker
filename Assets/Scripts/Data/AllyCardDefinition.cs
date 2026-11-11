using UnityEngine;

[CreateAssetMenu(fileName = "New Ally Card", menuName = "Bunker/Cards/Ally")]
public class AllyCardDefinition : CardDefinition
{
    [Header("Ally")]
    [Tooltip("Sent out of the bunker when played; its stats live in its AllyData. Must be listed in the scene's AllySpawner")]
    public AllyView ally;
    [Tooltip("Allies sent per play; above 1 the card shows xN")]
    [Min(1)] public int count = 1;
    [Tooltip("Each ally is a random type from the scene's AllySpawner instead of Ally")]
    public bool randomAlly;
}
