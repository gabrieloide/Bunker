using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootBag : MonoBehaviour
{
    public static LootBag instance;
    public List<Loot> lootList = new List<Loot>();

    private readonly List<Loot> possibleItems = new List<Loot>(8);
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

    Loot GetDroppedItem()
    {
        possibleItems.Clear();
        int randomNumber = Random.Range(1, 101);
        for (int i = 0; i < lootList.Count; i++)
        {
            var item = lootList[i];
            if (item != null && randomNumber <= item.dropChance)
            {
                possibleItems.Add(item);
            }
        }

        if (possibleItems.Count > 0)
        {
            return possibleItems[Random.Range(0, possibleItems.Count)];
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
