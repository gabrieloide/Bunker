using System.Collections.Generic;
using UnityEngine;

// Queues air support cards played during preparation and launches them when the wave starts.
public static class AirSupportQueue
{
    struct Entry
    {
        public CardDefinition Definition;
        public Vector3 Site;
        public GameObject Marker;
    }

    static readonly List<Entry> entries = new List<Entry>();
    static readonly Color MarkerColor = new Color(0x22 / 255f, 0x23 / 255f, 0x23 / 255f, 0.85f);
    static Sprite markerSprite;

    public static int Count => entries.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Clear();
        markerSprite = null;
    }

    public static void Enqueue(CardDefinition def, Vector3 site)
    {
        if (def == null)
            return;

        entries.Add(new Entry
        {
            Definition = def,
            Site = site,
            Marker = CreateMarker(site)
        });
    }

    public static void LaunchAll()
    {
        if (entries.Count == 0)
            return;

        try
        {
            if (SimulationBridge.Instance != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    SimulationBridge.SpawnFromCard(entry.Definition, entry.Site);
                    if (entry.Definition is HelicopterCardDefinition)
                        SoundBank.Play(Sfx.HelicopterLaunch);
                }
            }
        }
        finally
        {
            Clear();
        }
    }

    public static void Clear()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var marker = entries[i].Marker;
            if (marker == null)
                continue;

            if (Application.isPlaying)
                Object.Destroy(marker);
            else
                Object.DestroyImmediate(marker);
        }
        entries.Clear();
    }

    static GameObject CreateMarker(Vector3 site)
    {
        if (!Application.isPlaying)
            return null;

        var go = new GameObject("AirSupportMarker");
        go.transform.position = new Vector3(site.x, site.y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetMarkerSprite();
        sr.color = MarkerColor;
        sr.sortingOrder = 5;
        return go;
    }

    static Sprite GetMarkerSprite()
    {
        if (markerSprite != null)
            return markerSprite;

        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (size - 1) * 0.5f;
        float outerR = size * 0.45f;
        float innerR = size * 0.2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                bool inside = dist <= outerR && (dist >= innerR || Mathf.Abs(dx) < 1f || Mathf.Abs(dy) < 1f);
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }
        tex.Apply();

        markerSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        return markerSprite;
    }
}
