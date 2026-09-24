using UnityEngine;

public enum TowerStat { Damage, FireRate, BulletPen, Range, Life }

[CreateAssetMenu(fileName = "New Buff Card", menuName = "Bunker/Cards/Tower Buff")]
public class BuffCardDefinition : CardDefinition
{
    [Header("Buff")]
    public TowerStat stat = TowerStat.Damage;
    [Tooltip("Multiplier (1.3 = +30%). Bullet Pen: flat armor penetration added (e.g. 6)")]
    [Min(0f)] public float value = 1.5f;
}
