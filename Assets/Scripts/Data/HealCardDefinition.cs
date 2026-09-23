using UnityEngine;

[CreateAssetMenu(fileName = "New Heal Card", menuName = "Bunker/Cards/Bunker Heal")]
public class HealCardDefinition : CardDefinition
{
    [Header("Heal")]
    [Tooltip("Bunker life restored")]
    [Min(0f)] public float healAmount = 25f;
}
