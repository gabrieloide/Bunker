using UnityEngine;
using TMPro;

public class DisableTextDamage : MonoBehaviour
{
    [SerializeField] private float floatDistance = 1.2f;
    [SerializeField] private float duration = 0.55f;
    [SerializeField] private TMP_Text DamageText;
    public float DamageTxt;

    public void Animate(Vector3 startPos, float amount)
    {
        DamageTxt = amount;
        if (DamageText == null)
            DamageText = GetComponentInChildren<TMP_Text>();

        if (DamageText != null)
        {
            DamageText.text = Mathf.RoundToInt(amount).ToString();
            DamageText.alpha = 1f;
        }

        // Slight random horizontal scatter so overlapping numbers are readable
        float scatterX = Random.Range(-0.35f, 0.35f);
        Vector3 spawnPos = startPos + new Vector3(scatterX, 0.25f, 0f);
        transform.position = spawnPos;

        // Punch scale pop
        transform.localScale = Vector3.one * 1.5f;
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, Vector3.one, 0.15f).setEaseOutBack();

        // Float upwards
        Vector3 targetPos = spawnPos + new Vector3(0f, floatDistance, 0f);
        LeanTween.move(gameObject, targetPos, duration).setEaseOutQuad().setOnComplete(() =>
        {
            gameObject.SetActive(false);
        });

        // Fade out
        if (DamageText != null)
        {
            LeanTween.value(gameObject, 1f, 0f, 0.22f)
                .setDelay(duration - 0.22f)
                .setOnUpdate((float a) => { if (DamageText != null) DamageText.alpha = a; });
        }
    }
}