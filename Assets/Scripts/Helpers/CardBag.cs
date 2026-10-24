using System.Collections.Generic;
using UnityEngine;

// Weighted draws without replacement. The bag holds a few copies of each card of a pool in proportion
// to its Drop Weight and refills once empty, so every card turns up in its share within each cycle
// instead of leaving streaks and droughts to luck. Weight changes take effect on the next refill.
public class CardBag
{
    // Average copies per card: more follows the weights more exactly, fewer gives shorter cycles
    const int CopiesPerCard = 3;

    readonly CardCatalog catalog;
    readonly System.Predicate<CardDefinition> pool;
    readonly List<CardDefinition> bag = new List<CardDefinition>();
    readonly List<int> candidates = new List<int>();

    public CardBag(CardCatalog catalog, System.Predicate<CardDefinition> pool)
    {
        this.catalog = catalog;
        this.pool = pool;
    }

    // `avoid`: cards to skip while the bag can offer anything else (e.g. no repeats within one reward)
    public CardDefinition Draw(ICollection<CardDefinition> avoid = null)
    {
        if (bag.Count == 0) Refill();
        int index = PickIndex(avoid);
        if (index < 0 && avoid != null)
        {
            // What's left is all avoided: the next cycle may hold something else
            Refill();
            index = PickIndex(avoid);
        }
        // Pool too small to avoid a repeat
        if (index < 0 && bag.Count > 0)
            index = Random.Range(0, bag.Count);
        if (index < 0) return null;

        CardDefinition card = bag[index];
        bag[index] = bag[bag.Count - 1];
        bag.RemoveAt(bag.Count - 1);
        return card;
    }

    int PickIndex(ICollection<CardDefinition> avoid)
    {
        candidates.Clear();
        for (int i = 0; i < bag.Count; i++)
            if (avoid == null || !avoid.Contains(bag[i]))
                candidates.Add(i);
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : -1;
    }

    void Refill()
    {
        if (catalog == null) return;
        var cards = new HashSet<CardDefinition>();
        int total = 0;
        foreach (var card in catalog.cards)
            if (card != null && card.dropWeight > 0 && pool(card) && cards.Add(card))
                total += card.dropWeight;
        if (total <= 0) return;

        foreach (var card in cards)
        {
            int copies = Mathf.Max(1, Mathf.RoundToInt((float)card.dropWeight / total * cards.Count * CopiesPerCard));
            for (int i = 0; i < copies; i++)
                bag.Add(card);
        }
    }
}
