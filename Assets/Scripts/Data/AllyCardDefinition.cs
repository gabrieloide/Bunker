using UnityEngine;

[CreateAssetMenu(fileName = "New Ally Card", menuName = "Bunker/Cards/Ally")]
public class AllyCardDefinition : CardDefinition
{
    [Header("Ally")]
    [Tooltip("Sent out of the bunker when played; its stats live in its AllyData. Must be listed in the scene's AllySpawner")]
    public AllyView ally;
}
