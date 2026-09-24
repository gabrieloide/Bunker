using System;
using System.Collections.Generic;
using UnityEngine;

// Union of every flag's radius: where the player is allowed to play cards.
public static class FlagTerritory
{
    static readonly List<Flag> flags = new List<Flag>();

    public static event Action Changed;
    public static IReadOnlyList<Flag> Flags => flags;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        flags.Clear();
        Changed = null;
    }

    public static void Register(Flag flag)
    {
        if (flags.Contains(flag)) return;
        flags.Add(flag);
        Changed?.Invoke();
    }

    public static void Unregister(Flag flag)
    {
        if (flags.Remove(flag))
            Changed?.Invoke();
    }

    public static void NotifyChanged() => Changed?.Invoke();

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
