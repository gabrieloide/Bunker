using System.Collections.Generic;
using UnityEngine;

// Old-school smoke over a damaged building: puffs leave fixed vents on the roof at a steady beat, climb straight up one art
// pixel per tick with a little wind, and play the puff frames at their native size (no scaling, no random spread).
// Lives as a child of the building; the vents come from the parent's sprite bounds.
public class PixelSmoke : MonoBehaviour
{
    const float PixelSize = 1f / 16f;

    [Tooltip("Puff frames, played once over each puff's life")]
    [SerializeField] Sprite[] frames;
    [Tooltip("Frames per second of each puff's animation")]
    [Min(1f)] [SerializeField] float frameRate = 6f;
    [Tooltip("Movement steps per second; each step climbs one art pixel")]
    [Min(1f)] [SerializeField] float stepsPerSecond = 12f;
    [Tooltip("Every this many pixels climbed the puff also moves one pixel downwind")]
    [Min(1)] [SerializeField] int windEvery = 4;
    [Tooltip("Seconds between puffs: damaged / destroyed")]
    [SerializeField] float damagedInterval = 0.7f;
    [SerializeField] float destroyedInterval = 0.25f;
    [Tooltip("Vent positions across the top of the building (0 = left edge, 1 = right edge); damaged uses the first only")]
    [SerializeField] float[] vents = { 0.3f, 0.7f, 0.5f };

    class Puff
    {
        public SpriteRenderer renderer;
        public Vector2 origin;
        public float age;
    }

    readonly List<Puff> puffs = new List<Puff>();
    readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();
    bool destroyedState;
    float timer;
    int nextVent;

    public bool Destroyed
    {
        get => destroyedState;
        set => destroyedState = value;
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer += destroyedState ? destroyedInterval : damagedInterval;
            Emit();
        }

        float life = frames.Length / frameRate;
        for (int i = puffs.Count - 1; i >= 0; i--)
        {
            var puff = puffs[i];
            puff.age += Time.deltaTime;
            if (puff.age >= life)
            {
                puff.renderer.gameObject.SetActive(false);
                pool.Push(puff.renderer);
                puffs.RemoveAt(i);
                continue;
            }
            int steps = Mathf.FloorToInt(puff.age * stepsPerSecond);
            puff.renderer.transform.position = new Vector3(
                puff.origin.x + steps / windEvery * PixelSize,
                puff.origin.y + steps * PixelSize, 0f);
            puff.renderer.sprite = frames[Mathf.Min((int)(puff.age * frameRate), frames.Length - 1)];
        }
    }

    void Emit()
    {
        int ventCount = destroyedState ? vents.Length : 1;
        if (ventCount == 0) return;
        float vent = vents[nextVent++ % ventCount];

        Bounds bounds = BuildingBounds();
        Vector2 origin = new Vector2(Mathf.Lerp(bounds.min.x, bounds.max.x, vent), bounds.max.y - 4f * PixelSize);
        // Snapped to the art pixel grid, like every other sprite
        origin.x = Mathf.Round(origin.x / PixelSize) * PixelSize;
        origin.y = Mathf.Round(origin.y / PixelSize) * PixelSize;

        var renderer = pool.Count > 0 ? pool.Pop() : CreateRenderer();
        renderer.gameObject.SetActive(true);
        renderer.sprite = frames[0];
        renderer.transform.position = origin;
        puffs.Add(new Puff { renderer = renderer, origin = origin });
    }

    SpriteRenderer CreateRenderer()
    {
        var go = new GameObject("Puff");
        go.transform.SetParent(transform, true);
        var renderer = go.AddComponent<SpriteRenderer>();
        // In front of units and effects, like the other one-shot effects
        renderer.sortingLayerID = SortingLayer.NameToID("Entity");
        renderer.sortingOrder = 100;
        return renderer;
    }

    // The building is the biggest sprite under the parent (not its life bar or hit effects)
    Bounds BuildingBounds()
    {
        var best = new Bounds(transform.position, Vector3.one);
        float bestArea = 0f;
        var parent = transform.parent;
        if (parent == null) return best;
        foreach (var r in parent.GetComponentsInChildren<SpriteRenderer>())
        {
            if (r.transform.IsChildOf(transform) || r.sprite == null || !r.enabled) continue;
            float area = r.bounds.size.x * r.bounds.size.y;
            if (area > bestArea) { bestArea = area; best = r.bounds; }
        }
        return best;
    }
}
