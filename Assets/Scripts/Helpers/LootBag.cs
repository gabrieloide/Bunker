using System.Collections.Generic;
using UnityEngine;

// Kill drops: first roll whether a card drops at all, then pick one by weight from the CardCatalog.
// The drop chance rises as the bunker loses life, so a struggling player gets more cards.
public class LootBag : MonoBehaviour
{
    public static LootBag instance;
    [SerializeField] CardCatalog catalog;

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

        if (catalog == null)
            Debug.LogError("[LootBag] No CardCatalog assigned; kills will never drop cards.", this);

        cardDrop = GetComponentInChildren<CardDrop>();
        if (cardDrop == null)
            cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();
    }

    public float CurrentDropChance()
    {
        if (catalog == null) return 0f;
        float missingTens = Mathf.Floor((1f - SimEventDispatcher.Health01) * 10f);
        return Mathf.Clamp01(catalog.baseDropChance + missingTens * catalog.dropChancePerMissingTenLife);
    }

    public void InstantiateLoot()
    {
        if (catalog == null || Random.value >= CurrentDropChance())
            return;

        CardDefinition dropItem = catalog.PickWeighted();
        if (dropItem == null)
            return;

        if (cardDrop == null)
            cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();
        if (cardDrop != null)
            cardDrop.cardsQueue.Enqueue(dropItem);
    }
}
