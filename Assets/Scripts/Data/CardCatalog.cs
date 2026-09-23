using System.Collections.Generic;
using UnityEngine;

// Every card in the game plus the kill-drop rules. LootBag rolls from here; GameManager deals the starting hand from here.
[CreateAssetMenu(fileName = "CardCatalog", menuName = "Bunker/Card Catalog")]
public class CardCatalog : ScriptableObject
{
    [Tooltip("Drop pool; each card's own Drop Weight decides how often it comes out")]
    public List<CardDefinition> cards = new List<CardDefinition>();
    [Tooltip("Dealt in this order when a run starts")]
    public List<CardDefinition> startingHand = new List<CardDefinition>();

    [Header("Kill drops")]
    [Tooltip("Chance that a kill drops a card at full bunker life")]
    [Range(0f, 1f)] public float baseDropChance = 0.25f;
    [Tooltip("Extra drop chance per 10 bunker life missing")]
    [Range(0f, 0.2f)] public float dropChancePerMissingTenLife = 0.02f;

    public int TotalWeight()
    {
        int total = 0;
        foreach (var card in cards)
            if (card != null) total += card.dropWeight;
        return total;
    }

    public CardDefinition PickWeighted()
    {
        int total = TotalWeight();
        if (total <= 0) return null;

        int roll = Random.Range(0, total);
        foreach (var card in cards)
        {
            if (card == null) continue;
            roll -= card.dropWeight;
            if (roll < 0) return card;
        }
        return null;
    }
}
