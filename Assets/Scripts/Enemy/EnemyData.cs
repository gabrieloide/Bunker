using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    //Escribir mas stats de enemigos
    public int Life;
    public int score;
    public int Damage;
    public int Defense;
    public float MoveSpeed; 
}
