using System.Collections.Generic;
using UnityEngine;

// Every card in the game plus the rules for handing them out. Two pools share the list:
// kills drop everything but towers (allies, buffs, mines...), and towers come only from the
// random starting hand and the reward for clearing a wave. Weights compare within a pool.
[CreateAssetMenu(fileName = "CardCatalog", menuName = "Bunker/Card Catalog")]
public class CardCatalog : ScriptableObject
{
    [Tooltip("Every card that can be handed out; each card's own Drop Weight decides how often it comes out of its pool")]
    public List<CardDefinition> cards = new List<CardDefinition>();

    [Header("Towers")]
    [Tooltip("Random towers dealt when a level starts")]
    [Min(0)] public int startingTowers = 3;
    [Tooltip("Random towers dealt each time a wave is cleared; what doesn't fit in the hand waits in the draw pile")]
    [Min(0)] public int towersPerWave = 3;

    [Header("Placement")]
    [Tooltip("Towers allowed on the map at the same time; 0 = no limit. Each tower card can also set its own Max On Field")]
    [Min(0)] public int maxTowers = 8;

    [Header("Kill drops")]
    [Tooltip("Chance that a kill drops a card at full bunker life")]
    [Range(0f, 1f)] public float baseDropChance = 0.25f;
    [Tooltip("Extra drop chance per 10 bunker life missing")]
    [Range(0f, 0.2f)] public float dropChancePerMissingTenLife = 0.02f;

    public static bool IsTower(CardDefinition card) => card is TowerCardDefinition;
    public static bool DropsOnKill(CardDefinition card) => !IsTower(card);

    // The pool a card is rolled from: towers among towers, the rest among kill drops
    public static System.Predicate<CardDefinition> PoolOf(CardDefinition card) => IsTower(card) ? IsTower : DropsOnKill;

    public CardDefinition PickKillDrop() => PickWeighted(DropsOnKill);
    public CardDefinition PickTower() => PickWeighted(IsTower);

    public int TotalWeight(System.Predicate<CardDefinition> pool)
    {
        int total = 0;
        foreach (var card in cards)
            if (card != null && pool(card)) total += card.dropWeight;
        return total;
    }

    CardDefinition PickWeighted(System.Predicate<CardDefinition> pool)
    {
        int total = TotalWeight(pool);
        if (total <= 0) return null;

        int roll = Random.Range(0, total);
        foreach (var card in cards)
        {
            if (card == null || !pool(card)) continue;
            roll -= card.dropWeight;
            if (roll < 0) return card;
        }
        return null;
    }
}
