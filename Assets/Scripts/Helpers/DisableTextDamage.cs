using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableTextDamage : MonoBehaviour
{
    public Vector3 initialPos;
    [SerializeField] private float offsetDamageTextY;
    [SerializeField] private float damageTextTime;

    private void OnEnable()
    {
        StartCoroutine(TextMovement());
    }
    IEnumerator TextMovement()
    {
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }
}