using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tower Data", menuName = "Tower Data")]
public class TowersData : ScriptableObject
{
    //Definir propiedades de las torres
    public string Name;
    [TextArea(4, 5)] public string Description;
    [Space]
    [Header("Tower")]
    public GameObject CardToInstantiate;
    public float Life;
    public float Defense;
    public float bulletPen;
    public float damage;
    public float fireRate;
}
