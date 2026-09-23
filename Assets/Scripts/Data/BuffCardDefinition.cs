using UnityEngine;

[CreateAssetMenu(fileName = "New Buff Card", menuName = "Bunker/Cards/Tower Buff")]
public class BuffCardDefinition : CardDefinition
{
    [Header("Buff")]
    public BuffType buffType = BuffType.AttackBuff;
    [Tooltip("Attack / Speed: multiplier (e.g. 1.5). Bullet Pen: flat armor penetration added (e.g. 6)")]
    [Min(0f)] public float amount = 1.5f;
}
