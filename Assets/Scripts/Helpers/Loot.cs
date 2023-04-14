using UnityEngine;

[CreateAssetMenu(fileName = "LootTable", menuName = "Loot Table")]
public class Loot : ScriptableObject
{
    public GameObject loots;
    public int dropChance;
    public int indexCard;

    public Loot(int _dropChance, int _indexCard)
    {
        dropChance = _dropChance;
        indexCard = _indexCard;
    }
    public Loot()
    {

    }

}