using System.Collections.Generic;
using UnityEngine;

// Kill drops: first roll whether a card drops at all, then pick one by weight.
// The drop chance rises as the bunker loses life, so a struggling player gets more cards.
public class LootBag : MonoBehaviour
{
    public static LootBag instance;
    public List<Loot> lootList = new List<Loot>();

    [Tooltip("Chance that a kill drops a card at full bunker life")]
    [Range(0f, 1f)][SerializeField] float baseDropChance = 0.25f;
    [Tooltip("Extra drop chance per 10 bunker life missing")]
    [Range(0f, 0.2f)][SerializeField] float dropChancePerMissingTenLife = 0.02f;

    private CardDrop cardDrop;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        cardDrop = GetComponentInChildren<CardDrop>();
        if (cardDrop == null)
            cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();
    }

    public float CurrentDropChance()
    {
        float missingTens = Mathf.Floor((1f - SimEventDispatcher.Health01) * 10f);
        return Mathf.Clamp01(baseDropChance + missingTens * dropChancePerMissingTenLife);
    }

    Loot GetDroppedItem()
    {
        if (Random.value >= CurrentDropChance())
            return null;

        int total = 0;
        for (int i = 0; i < lootList.Count; i++)
            if (lootList[i] != null) total += lootList[i].weight;
        if (total <= 0)
            return null;

        int roll = Random.Range(0, total);
        for (int i = 0; i < lootList.Count; i++)
        {
            var item = lootList[i];
            if (item == null) continue;
            roll -= item.weight;
            if (roll < 0)
                return item;
        }
        return null;
    }

    public void InstantiateLoot()
    {
        Loot dropItem = GetDroppedItem();

        if (dropItem != null)
        {
            if (cardDrop == null)
                cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();

            if (cardDrop != null)
            {
                cardDrop.cardsQueue.Enqueue(dropItem.indexCard);
            }
        }
    }
}
