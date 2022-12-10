using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tower Data", menuName = "Tower Data")]
public class TowersData : ScriptableObject
{
    //Definir propiedades de las torres
    public string Name;
    public string Description;
    public float lifeBullet;
    public float damage;
    public float fireRate;
}
