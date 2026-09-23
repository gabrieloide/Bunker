using UnityEngine;
using UnityEngine.Serialization;

// One card in the kill-drop table. Read-only at runtime: LootBag owns all drop state.
[CreateAssetMenu(fileName = "LootTable", menuName = "Loot Table")]
public class Loot : ScriptableObject
{
    public GameObject loots;
    [Tooltip("Relative weight among the drops: chance of this card = weight / sum of all weights")]
    [FormerlySerializedAs("dropChance")]
    [Min(0)] public int weight;
    public int indexCard;
}
