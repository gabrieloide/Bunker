using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tower Data", menuName = "Tower Data")]
public class TowersData : ScriptableObject
{
    //Definir propiedades de las torres
    public string Name;
    [TextArea(4,5)]public string Description;
    public GameObject CardToInstantiate;
    public float lifeBullet;
    public int damage;
    public float fireRate;
}
