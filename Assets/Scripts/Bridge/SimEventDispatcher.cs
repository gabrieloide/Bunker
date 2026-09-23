using System;
using Bunker.Simulation;
using Unity.Entities;
using UnityEngine;

public static class SimEventDispatcher
{
    // --- Current Simulation State (Single Source of Truth for Presentation) ---
    public static int Score { get; private set; }
    public static int Wave { get; private set; } = 1;
    public static EnemyBuffKind CurrentBuff { get; private set; } = EnemyBuffKind.None;
    public static float Health { get; private set; } = 100f;
    public static float MaxHealth { get; private set; } = 100f;
    public static float Health01 => MaxHealth > 0f ? Mathf.Clamp01(Health / MaxHealth) : 0f;
    public static bool IsGameOver { get; private set; }

    // --- Typed Event Hooks ---
    public static event Action<Entity, Vector3> OnFired;
    public static event Action<Entity, Entity, Vector3, float, Faction> OnProjectileHit;
    public static event Action<Entity, Vector3, int> OnEnemyDied;
    public static event Action<Entity, Vector3> OnTowerDied;
    public static event Action<Entity, Vector3> OnEnemyReachedEnd;
    public static event Action<float, int, float> OnPlayerHit; // damage, remainingHP, normalizedHP
    public static event Action<float, int, float> OnBunkerHealed;
    public static event Action<int, EnemyBuffKind> OnWaveChanged;
    public static event Action<int, int> OnScoreChanged; // currentScore, delta
    public static event Action OnGameOver;
    public static event Action<Vector3, float> OnShellImpact; // position, splashRadius

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetStatics()
    {
        Score = 0;
        Wave = 1;
        CurrentBuff = EnemyBuffKind.None;
        Health = 100f;
        MaxHealth = 100f;
        IsGameOver = false;

        OnFired = null;
        OnProjectileHit = null;
        OnEnemyDied = null;
        OnTowerDied = null;
        OnEnemyReachedEnd = null;
        OnPlayerHit = null;
        OnBunkerHealed = null;
        OnWaveChanged = null;
        OnScoreChanged = null;
        OnGameOver = null;
        OnShellImpact = null;
    }

    public static void Seed(int initialScore, int initialWave, EnemyBuffKind initialBuff, float currentHealth, float maxHealth)
    {
        Score = initialScore;
        Wave = initialWave;
        CurrentBuff = initialBuff;
        Health = currentHealth;
        MaxHealth = maxHealth > 0f ? maxHealth : 100f;
        IsGameOver = false;
    }

    public static void RecordHeal(float amount)
    {
        Health = Mathf.Min(MaxHealth, Health + amount);
        OnBunkerHealed?.Invoke(amount, Mathf.RoundToInt(Health), Health01);
    }

    public static void Dispatch(SimEvent ev)
    {
        switch (ev.Kind)
        {
            case SimEventKind.Fired:
                OnFired?.Invoke(ev.Source, (Vector3)ev.Position);
                break;

            case SimEventKind.ProjectileHit:
                Faction faction = (Faction)ev.IntValue;
                OnProjectileHit?.Invoke(ev.Source, ev.Target, (Vector3)ev.Position, ev.Amount, faction);
                break;

            case SimEventKind.EnemyDied:
                OnEnemyDied?.Invoke(ev.Source, (Vector3)ev.Position, ev.IntValue);
                break;

            case SimEventKind.TowerDied:
                OnTowerDied?.Invoke(ev.Source, (Vector3)ev.Position);
                break;

            case SimEventKind.EnemyReachedEnd:
                OnEnemyReachedEnd?.Invoke(ev.Source, (Vector3)ev.Position);
                break;

            case SimEventKind.PlayerHit:
                Health = ev.IntValue;
                OnPlayerHit?.Invoke(ev.Amount, ev.IntValue, Health01);
                break;

            case SimEventKind.WaveChanged:
                Wave = ev.IntValue;
                CurrentBuff = (EnemyBuffKind)(int)ev.Amount;
                OnWaveChanged?.Invoke(Wave, CurrentBuff);
                break;

            case SimEventKind.ScoreChanged:
                int prevScore = Score;
                Score = ev.IntValue;
                OnScoreChanged?.Invoke(Score, Score - prevScore);
                break;

            case SimEventKind.GameOver:
                IsGameOver = true;
                OnGameOver?.Invoke();
                break;

            case SimEventKind.ShellImpact:
                OnShellImpact?.Invoke((Vector3)ev.Position, ev.Amount);
                break;
        }
    }
}
