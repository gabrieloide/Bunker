using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootTable", menuName = "Loot Table")]
public class Loot : ScriptableObject
{
    public GameObject loots;
    public string lootname;
    public int dropChance;

    public Loot(string _lootname, int _dropChance)
    {
        lootname = _lootname;
        dropChance = _dropChance;
    }
}