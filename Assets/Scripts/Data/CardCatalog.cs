using System.Collections.Generic;
using UnityEngine;

// Every card in the game plus the rules for handing them out. Three pools share the list:
// kills drop everything but towers and flags (allies, buffs, mines...); towers come only from the
// random starting hand and the reward for clearing a wave; flags only from that reward.
// Weights compare within a pool.
[CreateAssetMenu(fileName = "CardCatalog", menuName = "Bunker/Card Catalog")]
public class CardCatalog : ScriptableObject
{
    [Tooltip("Every card that can be handed out; each card's own Drop Weight decides how often it comes out of its pool")]
    public List<CardDefinition> cards = new List<CardDefinition>();

    [Header("Towers")]
    [Tooltip("Flag cards dealt when a level starts (before the towers, so they get a hand slot first)")]
    [Min(0)] public int startingFlags = 1;
    [Tooltip("Random towers dealt when a level starts")]
    [Min(0)] public int startingTowers = 4;
    [Tooltip("Random towers dealt each time a wave is cleared; what doesn't fit in the hand waits in the draw pile")]
    [Min(0)] public int towersPerWave = 3;
    [Tooltip("Flag cards dealt each time a wave is cleared (before the towers, so they get a hand slot first)")]
    [Min(0)] public int flagsPerWave = 1;

    [Header("Placement")]
    [Tooltip("Towers allowed on the map at the same time; 0 = no limit. Each tower card can also set its own Max On Field")]
    [Min(0)] public int maxTowers = 8;

    [Header("Demolition")]
    [Tooltip("Ruins left where a tower was torn down (they stay all match); empty = the tower's own sprite, greyed out")]
    public Sprite ruinsSprite;
    [Tooltip("Marks the free neighbouring cells the ruins block; empty = blocked but unmarked")]
    public Sprite blockedCellSprite;

    [Header("Kill drops")]
    [Tooltip("Average chance that a kill drops a card at full bunker life (each kill without a drop raises the next one's chance)")]
    [Range(0f, 1f)] public float baseDropChance = 0.25f;
    [Tooltip("Extra drop chance per 10 bunker life missing")]
    [Range(0f, 0.2f)] public float dropChancePerMissingTenLife = 0.02f;

    public static bool IsTower(CardDefinition card) => card is TowerCardDefinition;
    public static bool IsFlag(CardDefinition card) => card is FlagCardDefinition;
    public static bool DropsOnKill(CardDefinition card) => !IsTower(card) && !IsFlag(card);

    // The pool a card is rolled from: towers among towers, flags among flags, the rest among kill drops
    public static System.Predicate<CardDefinition> PoolOf(CardDefinition card) =>
        IsTower(card) ? IsTower : IsFlag(card) ? IsFlag : DropsOnKill;

    public int TotalWeight(System.Predicate<CardDefinition> pool)
    {
        int total = 0;
        foreach (var card in cards)
            if (card != null && pool(card)) total += card.dropWeight;
        return total;
    }
}
