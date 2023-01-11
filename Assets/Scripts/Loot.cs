using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootTable", menuName = "Loot Table")]
public class Loot : ScriptableObject
{
    public GameObject loots;
    public int dropChance;

    public Loot(int _dropChance)
    {
        dropChance = _dropChance;
    }
}