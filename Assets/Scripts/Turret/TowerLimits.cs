using System;
using System.Collections.Generic;
using UnityEngine;

// Caps how many towers can stand on the map at once: CardCatalog.maxTowers for all of them and
// TowerCardDefinition.maxOnField per card. Towers still in their card-flip animation count too.
public static class TowerLimits
{
    static readonly List<TurretCard> alive = new List<TurretCard>();
    static readonly Dictionary<TowerCardDefinition, int> pending = new Dictionary<TowerCardDefinition, int>();
    static int pendingTotal;

    public static event Action Changed;

    public static int Count => alive.Count + pendingTotal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        alive.Clear();
        pending.Clear();
        pendingTotal = 0;
        Changed = null;
    }

    public static void Register(TurretCard tower)
    {
        if (alive.Contains(tower)) return;
        alive.Add(tower);
        Changed?.Invoke();
    }

    public static void Unregister(TurretCard tower)
    {
        if (alive.Remove(tower))
            Changed?.Invoke();
    }

    public static int CountOf(TowerCardDefinition card)
    {
        int count = pending.TryGetValue(card, out int p) ? p : 0;
        foreach (var tower in alive)
            if (tower != null && tower.Definition == card) count++;
        return count;
    }

    // 0 in either limit means "no limit"
    public static bool CanPlace(TowerCardDefinition card, CardCatalog catalog)
    {
        if (catalog != null && catalog.maxTowers > 0 && Count >= catalog.maxTowers) return false;
        if (card != null && card.maxOnField > 0 && CountOf(card) >= card.maxOnField) return false;
        return true;
    }

    // Held from the drop until the tower is spawned, so two quick drops can't both slip under the cap
    public static void Reserve(TowerCardDefinition card)
    {
        pending[card] = (pending.TryGetValue(card, out int p) ? p : 0) + 1;
        pendingTotal++;
        Changed?.Invoke();
    }

    public static void Release(TowerCardDefinition card)
    {
        if (!pending.TryGetValue(card, out int p) || p <= 0) return;
        pending[card] = p - 1;
        pendingTotal--;
        Changed?.Invoke();
    }
}
