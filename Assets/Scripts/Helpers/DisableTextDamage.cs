using UnityEngine;
using UnityEngine.UIElements;

// Floating damage number. LeanTween moves, scales and fades this pooled object; its label on the game UI panel follows it.
public class DisableTextDamage : MonoBehaviour
{
    [SerializeField] private float floatDistance = 1.2f;
    [SerializeField] private float duration = 0.55f;
    [Tooltip("Text height in world units")]
    [SerializeField] private float fontSize = 0.8f;
    public float DamageTxt;

    Label label;
    float alpha = 1f;

    public void Animate(Vector3 startPos, float amount)
    {
        DamageTxt = amount;
        if (label == null && GameUIPanel.Instance != null)
        {
            label = new Label { pickingMode = PickingMode.Ignore };
            label.AddToClassList("damage-text");
            GameUIPanel.Instance.World.Add(label);
        }
        if (label != null)
        {
            label.text = Mathf.RoundToInt(amount).ToString();
            label.style.display = DisplayStyle.Flex;
        }
        alpha = 1f;

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
        LeanTween.value(gameObject, 1f, 0f, 0.22f)
            .setDelay(duration - 0.22f)
            .setOnUpdate((float a) => alpha = a);
    }

    void LateUpdate()
    {
        if (label == null) return;
        var panel = GameUIPanel.Instance;
        Vector2 centre = panel.WorldToPanel(transform.position);
        label.style.left = centre.x;
        label.style.top = centre.y;
        label.style.fontSize = fontSize * panel.UnitsPerWorld;
        label.style.scale = new Scale(transform.localScale);
        label.style.opacity = alpha;
    }

    void OnDisable()
    {
        if (label != null) label.style.display = DisplayStyle.None;
    }

    void OnDestroy() => label?.RemoveFromHierarchy();
}
