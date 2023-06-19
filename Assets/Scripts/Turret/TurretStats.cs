using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretStats : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] protected AK.Wwise.Event shoot;
    [SerializeField] protected AK.Wwise.Event destroy;
    [Space]
    [Header("Principal Stats")]
    public float damage;
    public float bulletPen;
    
}
