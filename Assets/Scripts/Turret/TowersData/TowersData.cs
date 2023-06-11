using UnityEngine;

[CreateAssetMenu(fileName = "New Tower Data", menuName = "Tower Data")]
public class TowersData : ScriptableObject
{
    public string Name;
    [TextArea(4, 5)] public string Description;
    [Space]
    [Header("Tower")]
    public GameObject CardToInstantiate;

}
