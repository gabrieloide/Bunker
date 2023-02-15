using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootBag : MonoBehaviour
{
    public static LootBag instance;
    public List<Loot> lootList = new List<Loot>();
    
    
    Loot GetDroppedItem()
    {
        List<Loot> possibleItems = new List<Loot>();
        int randomNumber = Random.Range(1, 101);
        foreach (Loot item in lootList)
        {
            if (randomNumber <= item.dropChance )
            {
                possibleItems.Add(item);
            }
        }
        if (possibleItems.Count > 0)
        {
            Loot droppedItem = possibleItems[Random.Range(0, possibleItems.Count)];
            return droppedItem;
        }
        return null;
    }
    public void InstantiateLoot()
    {
        Loot droppItem = GetDroppedItem();

        if(droppItem != null)
        {
            GetComponent<CardDrop>().cardsQueue.Enqueue(droppItem.indexCard);
        }
    }
}
