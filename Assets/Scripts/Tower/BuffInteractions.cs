using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum BuffType
{
    attackPlus,
    attackSpeed,
}
public class BuffInteractions : MonoBehaviour
{
    BuffType buffType = BuffType.attackPlus;
    float attackBonus;
    float ASBonus;
    public void buff()
    {
        switch (buffType)
        {
            case BuffType.attackPlus:

                break;
            case BuffType.attackSpeed:
                break;
        }
    }
}
