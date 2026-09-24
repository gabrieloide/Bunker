using System;
using System.Collections.Generic;
using UnityEngine;

// Union of every flag's radius: where the player is allowed to play cards.
public static class FlagTerritory
{
    static readonly List<Flag> flags = new List<Flag>();

    // Every flag circle is handed to the DottedRing shader so each ring hides the stretch that falls
    // inside another flag: together they draw one outline around the whole territory
    public const int MaxShaderCircles = 32;
    static readonly int CirclesId = Shader.PropertyToID("_FlagCircles");
    static readonly int CircleCountId = Shader.PropertyToID("_FlagCircleCount");
    static readonly Vector4[] circles = new Vector4[MaxShaderCircles];

    public static event Action Changed;
    public static IReadOnlyList<Flag> Flags => flags;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        flags.Clear();
        Changed = null;
    }

    static void RaiseChanged()
    {
        int count = 0;
        foreach (var flag in flags)
        {
            if (flag == null || count >= MaxShaderCircles) continue;
            Vector2 c = flag.Center;
            circles[count++] = new Vector4(c.x, c.y, flag.Radius, 0f);
        }
        Shader.SetGlobalVectorArray(CirclesId, circles);
        Shader.SetGlobalFloat(CircleCountId, count);
        Changed?.Invoke();
    }

    public static void Register(Flag flag)
    {
        if (flags.Contains(flag)) return;
        flags.Add(flag);
        RaiseChanged();
    }

    public static void Unregister(Flag flag)
    {
        if (flags.Remove(flag))
            RaiseChanged();
    }

    public static void NotifyChanged() => RaiseChanged();

    // A new flag has to overlap an existing one; a scene without flags accepts it anywhere
    public static bool IsConnected(Vector3 worldPosition, float radius)
    {
        if (flags.Count == 0) return true;

        Vector2 point = worldPosition;
        foreach (var flag in flags)
        {
            if (flag == null) continue;
            float reach = flag.Radius + radius;
            if ((point - flag.Center).sqrMagnitude < reach * reach)
                return true;
        }
        return false;
    }

    // A scene without flags keeps the old free placement instead of locking every card
    public static bool Contains(Vector3 worldPosition)
    {
        if (flags.Count == 0) return true;

        Vector2 point = worldPosition;
        foreach (var flag in flags)
        {
            if (flag == null) continue;
            if ((point - flag.Center).sqrMagnitude <= flag.Radius * flag.Radius)
                return true;
        }
        return false;
    }
}
