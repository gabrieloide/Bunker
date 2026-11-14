using System;
using System.Collections;
using UnityEngine;

// The few seconds between the killing blow and the result panel: the game freezes on the hit, drops into slow motion, the
// camera travels to the fallen bunker, a chain of explosions shakes it, it goes dark and smokes, and on a victory the enemies
// still standing blow up one by one. A click skips straight to the panel.
public class MatchEndSequence : MonoBehaviour
{
    [Header("Timing (real seconds)")]
    [SerializeField] float hitStop = 0.12f;
    [Range(0.05f, 1f)] [SerializeField] float slowMotion = 0.3f;
    [SerializeField] float cameraTravel = 0.6f;
    [SerializeField] int explosions = 6;
    [SerializeField] float explosionGap = 0.15f;
    [SerializeField] float enemyGap = 0.08f;
    [SerializeField] float holdBeforePanel = 0.5f;

    [Header("Defeat")]
    [Tooltip("Every wait of the sequence is this many times longer when the player's bunker falls")]
    [Min(1f)] [SerializeField] float defeatStretch = 1.8f;
    [SerializeField] int defeatExplosions = 9;
    [Tooltip("Extra real seconds watching the ruined bunker smoke before the panel")]
    [SerializeField] float defeatLinger = 1.2f;

    [Header("Look")]
    [SerializeField] GameObject explosionPrefab;
    [Tooltip("Tint of the fallen bunker until it has ruins art")]
    [SerializeField] Color ruinTint = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] float shakeDuration = 0.25f;
    [SerializeField] float shakeIntensity = 0.12f;

    Action onFinished;
    bool skipped;
    float stretch = 1f;

    public bool Running { get; private set; }

    public void Play(bool victory, Action finished)
    {
        if (Running) return;
        onFinished = finished;
        Running = true;
        skipped = false;
        stretch = victory ? 1f : defeatStretch;
        StartCoroutine(Run(victory));
    }

    void Update()
    {
        if (Running && !skipped && BunkerInput.SkipWasPressedThisFrame)
            skipped = true;
    }

    IEnumerator Run(bool victory)
    {
        // Escape would unfreeze the game underneath
        var pause = FindAnyObjectByType<PauseMenu>();
        if (pause != null) pause.enabled = false;

        Transform target = victory ? FindDestroyedEnemyBase() : (TowerPlayer.instance != null ? TowerPlayer.instance.transform : null);
        SpriteRenderer body = target != null ? Body(target) : null;

        // 1. Freeze on the blow
        RetroCRTFilter.Hit();
        Time.timeScale = 0f;
        yield return Wait(hitStop);

        // 2. Slow motion, the camera travels to the fallen bunker
        Time.timeScale = slowMotion;
        if (target != null && CameraMovement.instance != null)
            CameraMovement.instance.Focus(target.position, cameraTravel * stretch);
        yield return Wait(cameraTravel * 0.5f);

        // 3. Chain of explosions across the bunker
        Bounds bounds = body != null ? body.bounds : new Bounds(target != null ? target.position : Vector3.zero, Vector3.one * 2f);
        int count = victory ? explosions : defeatExplosions;
        for (int i = 0; i < count && !skipped; i++)
        {
            Vector3 point = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y), 0f);
            if (explosionPrefab != null)
                OneShotEffect.EnsureFrontSorting(Instantiate(explosionPrefab, point, Quaternion.identity));
            SoundBank.Play(i % 2 == 0 ? Sfx.MineExplosion : Sfx.VehicleDeath);
            CameraShake.Shake(shakeDuration, shakeIntensity);
            RetroCRTFilter.Hit();
            yield return Wait(explosionGap);
        }

        // 4. Switch enemy bunker to ruins art when available; darken anything without ruins art
        bool swappedArt = victory && target != null && target.TryGetComponent<EnemyBaseView>(out var baseView) && baseView.ApplyDestroyedVisual();
        if (body != null && !swappedArt) body.color = ruinTint;

        // 5. Victory: the enemies still standing blow up one by one
        if (victory)
        {
            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                if (skipped) break;
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                if (enemy.ExplosionParticle != null)
                    OneShotEffect.EnsureFrontSorting(Instantiate(enemy.ExplosionParticle, enemy.transform.position, Quaternion.identity));
                SoundBank.Play(Sfx.EnemyDeath);
                enemy.gameObject.SetActive(false);
                yield return Wait(enemyGap);
            }
        }

        // 6. Silence, then the closing sting
        SoundBank.StopMusic();
        SoundBank.Play(victory ? Sfx.WaveCleared : Sfx.Defeat);
        if (!victory) yield return Wait(defeatLinger);
        yield return Wait(holdBeforePanel);

        Time.timeScale = 0f;
        Running = false;
        onFinished?.Invoke();
    }

    // Real time, cut short by a skip
    IEnumerator Wait(float seconds)
    {
        float end = Time.unscaledTime + seconds * stretch;
        while (Time.unscaledTime < end && !skipped)
            yield return null;
    }

    static Transform FindDestroyedEnemyBase()
    {
        var bases = FindObjectsByType<EnemyBaseView>(FindObjectsSortMode.None);
        foreach (var b in bases)
            if (b != null && b.IsDestroyed) return b.transform;
        return bases.Length > 0 && bases[0] != null ? bases[0].transform : null;
    }

    static Transform FindTarget<T>() where T : Component
    {
        var found = FindFirstObjectByType<T>();
        return found != null ? found.transform : null;
    }

    // The building is its biggest sprite (not its life bar, smoke or hit effects)
    static SpriteRenderer Body(Transform target)
    {
        if (target != null && target.TryGetComponent<EnemyBaseView>(out var baseView) && baseView.BodyRenderer != null)
            return baseView.BodyRenderer;

        SpriteRenderer best = null;
        float bestArea = 0f;
        foreach (var r in target.GetComponentsInChildren<SpriteRenderer>())
        {
            if (r.sprite == null || r.GetComponentInParent<PixelSmoke>() != null) continue;
            float area = r.bounds.size.x * r.bounds.size.y;
            if (area > bestArea) { bestArea = area; best = r; }
        }
        return best;
    }
}
