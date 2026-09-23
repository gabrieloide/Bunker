using UnityEngine;

// Single source of truth for a card: what it shows, what it spawns, how often it drops and every
// gameplay number it uses. Prefabs only carry presentation (sprites, sounds, particles).
// Edit them all at once from Bunker > Balance.
public abstract class CardDefinition : ScriptableObject
{
    [Header("Info")]
    public string displayName;
    [TextArea(3, 5)] public string description;

    [Header("Prefabs")]
    [Tooltip("Card drawn into the hand (UI prefab with a Card component)")]
    public CardIndex handPrefab;
    [Tooltip("Object placed in the world when the card is played; empty for cards that only apply an effect")]
    public GameObject placedPrefab;

    [Header("Loot")]
    [Tooltip("Relative drop weight: chance of this card = weight / sum of all weights in the catalog. 0 = never drops")]
    [Min(0)] public int dropWeight;
}
