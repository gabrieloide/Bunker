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
    [Tooltip("Enemy territory around the base: no player flag can be planted inside it. It never falls")]
    [Min(0f)] public float enemyBaseRadius = 7f;

    [Header("Music")]
    [Tooltip("Looped while the level plays; empty = the SoundBank's default music")]
    public AudioClip music;

    [Header("Waves")]
    [Tooltip("Empty = the scene's own WaveBalanceConfig")]
    public WaveBalanceConfig balance;

    [Header("Flags")]
    [Tooltip("Flags planted when the level starts; cards can only be played inside their radius")]
    public List<FlagPlacement> flags = new List<FlagPlacement>();
    [Tooltip("White enemy flags: no player flag can be planted inside their radius until an ally walks into it and knocks it down")]
    public List<FlagPlacement> enemyFlags = new List<FlagPlacement>();

    [Header("Ally Roads")]
    [Tooltip("Maximum number of ally road tiles the player can build in this level (0 = unlimited)")]
    [Min(0)] public int maxAllyRoadTiles = 12;
}

[Serializable]
public struct FlagPlacement
{
    public Vector2 position;
    [Min(0.5f)] public float radius;
}
