using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu([CreateAssetMenu(fileName = "New Tower Data", menuName = "Tower Data")])]
public class EnemyData : ScriptableObject
{
    public float Life;
    public float Damage;
    public float Defense;
    public float MoveSpeed;
}
