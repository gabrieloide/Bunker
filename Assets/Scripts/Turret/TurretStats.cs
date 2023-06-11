using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretStats : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] protected AK.Wwise.Event shoot;
    [Space]
    [Header("Principal Stats")]
    public float damage;
    public float bulletPen;
    
}
