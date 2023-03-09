using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trash : MonoBehaviour
{
    [SerializeField] float widthBox, heightBox;
    [SerializeField] LayerMask CardLayer;
    [HideInInspector]public RaycastHit2D hit2D;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector2(widthBox, heightBox));
    }
    private void LateUpdate()
    {
        hit2D = Physics2D.BoxCast(transform.position, new Vector2(widthBox, heightBox), 360, Vector2.one, 5, CardLayer);
    }
}
