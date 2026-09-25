using UnityEngine;

<<<<<<< HEAD
=======
public enum TowerStat { Damage, FireRate, BulletPen, Range, Life }

>>>>>>> 1b0f21870329b922747c317aa3561b86f80f1c88
[CreateAssetMenu(fileName = "New Buff Card", menuName = "Bunker/Cards/Tower Buff")]
public class BuffCardDefinition : CardDefinition
{
    [Header("Buff")]
<<<<<<< HEAD
    public BuffType buffType = BuffType.AttackBuff;
    [Tooltip("Attack / Speed: multiplier (e.g. 1.5). Bullet Pen: flat armor penetration added (e.g. 6)")]
    [Min(0f)] public float amount = 1.5f;
=======
    public TowerStat stat = TowerStat.Damage;
    [Tooltip("Multiplier (1.3 = +30%). Bullet Pen: flat armor penetration added (e.g. 6)")]
    [Min(0f)] public float value = 1.5f;
>>>>>>> 1b0f21870329b922747c317aa3561b86f80f1c88
}
