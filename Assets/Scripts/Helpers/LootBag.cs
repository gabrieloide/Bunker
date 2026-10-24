using Bunker.Simulation;
using UnityEngine;

// Kill drops: first roll whether a card drops at all, then draw one from a bag weighted by the CardCatalog.
// The drop chance rises as the bunker loses life, so a struggling player gets more cards. It is a
// pseudo-random distribution: each kill without a drop raises the next one's chance, so the average
// stays at CurrentDropChance but long droughts and streaks don't happen.
public class LootBag : MonoBehaviour
{
    public static LootBag instance;
    [SerializeField] CardCatalog catalog;

    private CardDrop cardDrop;
    private CardBag dropBag;
    private int killsSinceDrop;

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
        if (catalog == null)
            return;
        if (Random.value >= BalanceMath.PrdChance(CurrentDropChance(), killsSinceDrop))
        {
            killsSinceDrop++;
            return;
        }
        killsSinceDrop = 0;

        // Allies and effect cards; towers come from the wave-clear reward instead
        dropBag ??= new CardBag(catalog, CardCatalog.DropsOnKill);
        CardDefinition dropItem = dropBag.Draw();
        if (dropItem == null)
            return;

        if (cardDrop == null)
            cardDrop = CardDrop.instance != null ? CardDrop.instance : FindAnyObjectByType<CardDrop>();
        if (cardDrop != null)
            cardDrop.EnqueueCard(dropItem);
    }
}
