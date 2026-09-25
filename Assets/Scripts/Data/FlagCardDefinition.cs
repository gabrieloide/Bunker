using UnityEngine;

[CreateAssetMenu(fileName = "New Flag Card", menuName = "Bunker/Cards/Flag")]
public class FlagCardDefinition : CardDefinition
{
    [Header("Flag")]
    [Tooltip("Cards can be played within this distance of the flag")]
    [Min(0.5f)] public float radius = 5f;
}
