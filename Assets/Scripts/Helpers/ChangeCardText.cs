using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// Card detail panel (right click on a hand card), the "card-info" element of UI/Game.uxml. Lives next to GameUIPanel.
// 1-bit friendly: opens top-down in discrete pixel steps (clipped, no scaling), then types the description.
public class ChangeCardText : MonoBehaviour
{
    [SerializeField] int revealSteps = 4;
    [SerializeField] float stepInterval = 0.035f;
    [SerializeField] float charsPerSecond = 90f;
    [Tooltip("Gap between the card's top edge and the panel, in panel units")]
    [SerializeField] float gapAboveCard = 3f;

    VisualElement panel, content;
    Label cardName, cardDescription;
    string description = "";
    Coroutine routine;

    public Card Owner { get; private set; }
    public bool IsOpen => Owner != null;

    void Start()
    {
        var root = GameUIPanel.Instance.Root;
        panel = root.Q("card-info");
        content = panel.Q(className: "card-info__content");
        cardName = panel.Q<Label>("card-name");
        cardDescription = panel.Q<Label>("card-description");
    }

    public void Show(Card owner, string textName, string textDescription, VisualElement anchor)
    {
        if (panel == null) return;
        Owner = owner;
        cardName.text = textName;
        description = textDescription ?? "";

        panel.visible = true;
        panel.BringToFront();
        PlaceAbove(anchor);

        Restart(Open());
    }

    public void Hide(Card owner = null)
    {
        if (!IsOpen || (owner != null && owner != Owner)) return;
        Owner = null;
        Restart(Close());
    }

    void Restart(IEnumerator next)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(next);
    }

    IEnumerator Open()
    {
        SetTyped(0);
        cardName.visible = false;

        for (int step = 1; step <= revealSteps; step++)
        {
            SetReveal(step / (float)revealSteps);
            yield return new WaitForSecondsRealtime(stepInterval);
        }

        cardName.visible = true;
        float visible = 0f;
        while (visible < description.Length)
        {
            visible += charsPerSecond * Time.unscaledDeltaTime;
            SetTyped(Mathf.Min(description.Length, Mathf.FloorToInt(visible)));
            yield return null;
        }
        routine = null;
    }

    IEnumerator Close()
    {
        cardName.visible = false;
        SetTyped(0);
        for (int step = revealSteps - 1; step >= 0; step--)
        {
            SetReveal(step / (float)revealSteps);
            yield return new WaitForSecondsRealtime(stepInterval);
        }
        panel.visible = false;
        routine = null;
    }

    // The untyped rest stays in place but transparent, so the wrapping doesn't jump while typing
    void SetTyped(int chars) =>
        cardDescription.text = chars >= description.Length ? description : description.Substring(0, chars) + "<alpha=#00>" + description.Substring(chars);

    // Reveals the panel from the top edge down; hidden rows are clipped from the bottom, snapped to whole units
    void SetReveal(float t) => panel.style.height = Mathf.Round(content.resolvedStyle.height * t);

    // Content bottom sits gapAboveCard over the card's top edge, centred on it and clamped inside the panel
    void PlaceAbove(VisualElement anchor)
    {
        if (anchor == null) return;
        Rect card = anchor.worldBound;
        Rect bounds = panel.parent.worldBound;
        float width = content.resolvedStyle.width, height = content.resolvedStyle.height;

        float x = Mathf.Clamp(card.center.x - width * 0.5f, bounds.xMin, bounds.xMax - width);
        float y = Mathf.Max(card.yMin - gapAboveCard - height, bounds.yMin);
        panel.style.left = Mathf.Round(x);
        panel.style.top = Mathf.Round(y);
    }
}
