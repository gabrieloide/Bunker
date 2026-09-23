using UnityEngine;

[CreateAssetMenu(fileName = "New Land Mine Card", menuName = "Bunker/Cards/Land Mine")]
public class LandMineCardDefinition : CardDefinition
{
    [Header("Land Mine")]
    [Min(0f)] public float damage = 45f;
    [Min(0f)] public float bulletPen = 4f;
}
