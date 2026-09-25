using System;
using System.Collections.Generic;
using UnityEngine;

// Template ("molde") for one level: which scene it plays in and everything that makes it different.
// Add a level by duplicating one of these and listing it in the LevelCatalog.
[CreateAssetMenu(fileName = "New Level", menuName = "Bunker/Level")]
public class LevelDefinition : ScriptableObject
{
    [Header("Info")]
    public string displayName;
    [TextArea(2, 4)] public string description;
    [Tooltip("Scene loaded for this level (must be in Build Settings)")]
    public string sceneName = "SceneGame";

    [Header("Enemy base")]
    [Tooltip("Life of the enemy base; destroying it wins the level")]
    [Min(1f)] public float enemyBaseLife = 500f;

    [Header("Waves")]
    [Tooltip("Empty = the scene's own WaveBalanceConfig")]
    public WaveBalanceConfig balance;

    [Header("Allies")]
    [Tooltip("Seconds between two allies leaving the bunker; 0 = no allies")]
    [Min(0f)] public float allySpawnInterval = 6f;
    [Tooltip("Seconds before the first ally")]
    [Min(0f)] public float firstAllyDelay = 4f;

    [Header("Flags")]
    [Tooltip("Flags planted when the level starts; cards can only be played inside their radius")]
    public List<FlagPlacement> flags = new List<FlagPlacement>();

    [Header("Cards")]
    [Tooltip("Empty = the CardCatalog's starting hand")]
    public List<CardDefinition> startingHand = new List<CardDefinition>();
}

[Serializable]
public struct FlagPlacement
{
    public Vector2 position;
    [Min(0.5f)] public float radius;
}
