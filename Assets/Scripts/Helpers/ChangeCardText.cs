using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Card detail panel (right click on a hand card). Single reusable instance owned by UIManager.
// 1-bit friendly: opens top-down in discrete pixel steps (RectMask2D clip, no scaling), then types the description.
[RequireComponent(typeof(RectTransform))]
public class ChangeCardText : MonoBehaviour
{
    public TMP_Text CardName;
    public TMP_Text CardDescription;

    [SerializeField] float panelScale = 0.25f;
    [SerializeField] int revealSteps = 4;
    [SerializeField] float stepInterval = 0.035f;
    [SerializeField] float charsPerSecond = 90f;
    [Tooltip("Gap between the card's top edge and the panel, in canvas units")]
    [SerializeField] float gapAboveCard = 3f;

    RectTransform rectTransform;
    RectMask2D mask;
    Vector4 basePadding;
    Vector2 contentMin, contentMax; // union of children, in panel-local units
    Coroutine routine;

    public Card Owner { get; private set; }
    public bool IsOpen => Owner != null;

    void Awake()
    {
        rectTransform = (RectTransform)transform;
        rectTransform.localScale = Vector3.one * panelScale;

        mask = GetComponent<RectMask2D>();
        if (mask == null) mask = gameObject.AddComponent<RectMask2D>();
        CacheContentBounds();

        gameObject.SetActive(false);
    }

    // Mask covers the union of all children (the background image is taller than the root rect)
    void CacheContentBounds()
    {
        Rect root = rectTransform.rect;
        Vector2 min = root.min, max = root.max;
        var corners = new Vector3[4];
        foreach (RectTransform child in rectTransform)
        {
            child.GetWorldCorners(corners);
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = rectTransform.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
        }
        contentMin = min;
        contentMax = max;
        // RectMask2D padding: (left, bottom, right, top), positive shrinks
        basePadding = new Vector4(min.x - root.xMin, min.y - root.yMin, root.xMax - max.x, root.yMax - max.y);
    }

    public void Show(Card owner, string textName, string textDescription, RectTransform anchor)
    {
        Owner = owner;
        CardName.text = textName;
        CardDescription.text = textDescription;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        PlaceAbove(anchor);

        Restart(Open());
    }

    public void Hide(Card owner = null)
    {
        if (!IsOpen || (owner != null && owner != Owner)) return;
        Owner = null;
        if (gameObject.activeInHierarchy)
            Restart(Close());
    }

    void Restart(IEnumerator next)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(next);
    }

    IEnumerator Open()
    {
        CardDescription.ForceMeshUpdate();
        int totalChars = CardDescription.textInfo.characterCount;
        CardDescription.maxVisibleCharacters = 0;
        CardName.enabled = false;

        for (int step = 1; step <= revealSteps; step++)
        {
            SetReveal(step / (float)revealSteps);
            yield return new WaitForSecondsRealtime(stepInterval);
        }

        CardName.enabled = true;
        float visible = 0f;
        while (visible < totalChars)
        {
            visible += charsPerSecond * Time.unscaledDeltaTime;
            CardDescription.maxVisibleCharacters = Mathf.Min(totalChars, Mathf.FloorToInt(visible));
            yield return null;
        }
        routine = null;
    }

    IEnumerator Close()
    {
        CardName.enabled = false;
        CardDescription.maxVisibleCharacters = 0;
        for (int step = revealSteps - 1; step >= 0; step--)
        {
            SetReveal(step / (float)revealSteps);
            yield return new WaitForSecondsRealtime(stepInterval);
        }
        gameObject.SetActive(false);
        routine = null;
    }

    // Reveals the panel from the top edge down; hidden rows are clipped from the bottom, snapped to whole units
    void SetReveal(float t)
    {
        float hiddenCanvasUnits = Mathf.Round((contentMax.y - contentMin.y) * panelScale * (1f - t));
        float hidden = hiddenCanvasUnits / panelScale;
        mask.padding = new Vector4(basePadding.x, basePadding.y + hidden, basePadding.z, basePadding.w);
    }

    // Content bottom sits gapAboveCard over the card's top edge, centred on it and clamped inside the canvas
    void PlaceAbove(RectTransform anchor)
    {
        var parent = rectTransform.parent as RectTransform;
        if (parent == null || anchor == null) return;

        var corners = new Vector3[4];
        anchor.GetWorldCorners(corners);
        // corners[1] = top-left, corners[2] = top-right
        // Keep the card's depth: this canvas sits on the camera, so z = 0 would fall inside the near clip plane
        Vector3 cardTop = parent.InverseTransformPoint((corners[1] + corners[2]) * 0.5f);

        float s = panelScale;
        float x = cardTop.x - (contentMin.x + contentMax.x) * 0.5f * s;
        float y = cardTop.y + gapAboveCard - contentMin.y * s;

        Rect bounds = parent.rect;
        x = Mathf.Clamp(x, bounds.xMin - contentMin.x * s, bounds.xMax - contentMax.x * s);
        y = Mathf.Min(y, bounds.yMax - contentMax.y * s);

        rectTransform.localPosition = new Vector3(Mathf.Round(x), Mathf.Round(y), cardTop.z);
    }
}
