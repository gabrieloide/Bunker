using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DisableTextDamage : MonoBehaviour
{
    [SerializeField] private float offsetDamageTextY;
    [SerializeField] private float damageTextTime;
    [SerializeField] private TMP_Text DamageText;
    public float DamageTxt;

    private void OnEnable()
    {
        StartCoroutine(TextMovement());
    }
    IEnumerator TextMovement()
    {
        DamageText.text = DamageTxt.ToString();
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }
}