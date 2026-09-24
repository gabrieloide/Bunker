using System.Collections.Generic;
using UnityEngine;

// Every level in play order. The level selector builds one button per entry; beating one unlocks the next.
[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Bunker/Level Catalog")]
public class LevelCatalog : ScriptableObject
{
    public List<LevelDefinition> levels = new List<LevelDefinition>();

    public int IndexOf(LevelDefinition level) => levels.IndexOf(level);

    public LevelDefinition At(int index) => index >= 0 && index < levels.Count ? levels[index] : null;
}
