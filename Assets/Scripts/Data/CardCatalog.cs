using System.Collections.Generic;
using UnityEngine;

// Every card in the game plus the rules for handing them out. Four pools share the list:
// towers, flags, roads, and support cards (allies, buffs, mines, helicopters).
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

    [Header("Roads")]
    [Tooltip("Road cards dealt when a level starts")]
    [Min(0)] public int startingRoads = 1;
    [Tooltip("Road cards dealt every Waves Per Road cleared waves")]
    [Min(0)] public int roadsPerInterval = 1;
    [Tooltip("How many cleared waves between each road card reward (0 = none after start)")]
    [Min(0)] public int wavesPerRoad = 2;

    [Header("Placement")]
    [Tooltip("Towers allowed on the map at the same time; 0 = no limit. Each tower card can also set its own Max On Field")]
    [Min(0)] public int maxTowers = 8;

    [Header("Demolition")]
    [Tooltip("Ruins left where a tower was torn down (they stay all match); empty = the tower's own sprite, greyed out")]
    public Sprite ruinsSprite;
    [Tooltip("Marks the free neighbouring cells the ruins block; empty = blocked but unmarked")]
    public Sprite blockedCellSprite;

    [Header("Support")]
    [Tooltip("Support cards (allies, buffs, mines, helicopters) dealt when a level starts")]
    [Min(0)] public int startingSupport = 2;
    [Tooltip("Support cards dealt each time a wave is cleared")]
    [Min(0)] public int supportPerWave = 1;

    public static bool IsTower(CardDefinition card) => card is TowerCardDefinition;
    public static bool IsFlag(CardDefinition card) => card is FlagCardDefinition;
    public static bool IsRoad(CardDefinition card) => card is RoadCardDefinition;
    public static bool IsSupport(CardDefinition card) => !IsTower(card) && !IsFlag(card) && !IsRoad(card);

    // The pool a card is rolled from: towers among towers, flags among flags, roads among roads, the rest among support
    public static System.Predicate<CardDefinition> PoolOf(CardDefinition card) =>
        IsTower(card) ? IsTower : IsFlag(card) ? IsFlag : IsRoad(card) ? IsRoad : IsSupport;

    public int TotalWeight(System.Predicate<CardDefinition> pool)
    {
        int total = 0;
        foreach (var card in cards)
            if (card != null && pool(card)) total += card.dropWeight;
        return total;
    }
}
