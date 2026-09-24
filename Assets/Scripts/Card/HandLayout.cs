using System.Collections.Generic;
using UnityEngine;

// Lays the hand out left to right, centred on this RectTransform. Cards close ranks when one leaves and
// squeeze together when they no longer fit the width. Dragged cards keep their spot until they are spent.
[RequireComponent(typeof(RectTransform))]
public class HandLayout : MonoBehaviour
{
    [Tooltip("Gap between two cards, in canvas units")]
    [SerializeField] float spacing = 10f;
    [Tooltip("Card width used for spacing; 0 = read it from the first card")]
    [SerializeField] float cardWidth = 0f;
    [Tooltip("Seconds a card takes to slide to its new spot")]
    [SerializeField] float slideTime = 0.2f;

    readonly List<Card> cards = new List<Card>();
    RectTransform rectTransform;

    public int Count => cards.Count;

    void Awake() => rectTransform = (RectTransform)transform;

    public int IndexOf(Card card) => cards.IndexOf(card);

    public void Add(Card card)
    {
        if (card == null || cards.Contains(card)) return;
        cards.Add(card);
        card.transform.SetParent(transform, false);
        card.transform.localRotation = Quaternion.identity;
        card.transform.localScale = Vector3.one;
        // The newcomer appears straight in its spot (it plays its own deal-in); the rest slide over
        Refresh(card);
    }

    public void Remove(Card card)
    {
        if (cards.Remove(card))
            Refresh(null);
    }

    void Refresh(Card placedInstantly)
    {
        cards.RemoveAll(c => c == null);
        int count = cards.Count;
        if (count == 0) return;

        float width = cardWidth > 0f ? cardWidth : ((RectTransform)cards[0].transform).rect.width;
        float step = width + spacing;
        // Squeeze (cards overlap) instead of spilling past the hand area
        float available = rectTransform.rect.width - width;
        if (count > 1 && step * (count - 1) > available)
            step = Mathf.Max(0f, available) / (count - 1);

        float start = -step * (count - 1) * 0.5f;
        for (int i = 0; i < count; i++)
        {
            var card = cards[i];
            card.SetHandHome(new Vector2(start + step * i, 0f), card == placedInstantly ? 0f : slideTime);
            if (card.transform.parent == transform)
                card.transform.SetSiblingIndex(i);
        }
    }
}
