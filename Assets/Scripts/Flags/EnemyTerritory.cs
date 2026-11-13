using System.Collections.Generic;
using UnityEngine;

// Ground the enemy holds: its base's circle plus every standing white flag. The player can't plant a flag inside it.
public static class EnemyTerritory
{
    const string RingMaterialPath = "EnemyFlagRing";
    // The dark tone of the game's palette
    static readonly Color RingColor = new Color32(34, 35, 35, 255);

    struct BaseZone
    {
        public EnemyBaseView Owner;
        public Vector2 Center;
        public float Radius;
    }

    static readonly List<EnemyFlag> flags = new List<EnemyFlag>();
    static readonly List<BaseZone> bases = new List<BaseZone>();
    static Vector2 baseCenter;
    static float baseRadius;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        flags.Clear();
        bases.Clear();
        baseRadius = 0f;
    }

    public static void Register(EnemyFlag flag)
    {
        if (!flags.Contains(flag)) flags.Add(flag);
    }

    public static void Unregister(EnemyFlag flag) => flags.Remove(flag);

    public static void RegisterBase(EnemyBaseView owner, Vector2 center, float radius)
    {
        for (int i = 0; i < bases.Count; i++)
        {
            if (bases[i].Owner == owner)
            {
                bases[i] = new BaseZone { Owner = owner, Center = center, Radius = radius };
                return;
            }
        }
        bases.Add(new BaseZone { Owner = owner, Center = center, Radius = radius });
    }

    public static void UnregisterBase(EnemyBaseView owner)
    {
        for (int i = bases.Count - 1; i >= 0; i--)
        {
            if (bases[i].Owner == owner || bases[i].Owner == null)
                bases.RemoveAt(i);
        }
    }

    public static void SetBase(Vector2 center, float radius)
    {
        baseCenter = center;
        baseRadius = radius;
    }

    public static void ClearBase() => baseRadius = 0f;

    public static bool Contains(Vector3 worldPosition)
    {
        Vector2 point = worldPosition;
        if (baseRadius > 0f && (point - baseCenter).sqrMagnitude <= baseRadius * baseRadius)
            return true;
        foreach (var b in bases)
            if (b.Radius > 0f && (point - b.Center).sqrMagnitude <= b.Radius * b.Radius)
                return true;
        foreach (var flag in flags)
            if (flag != null && (point - flag.Center).sqrMagnitude <= flag.Radius * flag.Radius)
                return true;
        return false;
    }

    // Enemy rings show only while a card is dragged, like the player's flag rings
    public static DottedRing CreateRing(Transform parent, float radius)
    {
        var material = Resources.Load<Material>(RingMaterialPath);
        if (material == null)
        {
            Debug.LogError($"EnemyTerritory: missing Resources/{RingMaterialPath} material.");
            return null;
        }
        var ringObject = new GameObject("EnemyTerritoryRing", typeof(MeshFilter), typeof(MeshRenderer));
        ringObject.transform.SetParent(parent, false);
        var ring = ringObject.AddComponent<DottedRing>();
        ringObject.GetComponent<MeshRenderer>().sharedMaterial = material;
        ring.SetColor(RingColor);
        ring.SetRadius(radius);
        ringObject.SetActive(false);
        return ring;
    }

    public static void UpdateRing(DottedRing ring)
    {
        bool show = GameManager.instance != null && GameManager.instance.onDrag;
        if (ring.gameObject.activeSelf != show)
            ring.gameObject.SetActive(show);
    }
}
