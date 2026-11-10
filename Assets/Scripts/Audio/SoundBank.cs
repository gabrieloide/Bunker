using System;
using System.Collections.Generic;
using RetroSoundSynthesizer.Runtime;
using UnityEngine;

// Every sound in the game. Values are stored in prefabs and scenes: only append, never renumber.
public enum Sfx
{
    None = 0,
    TowerShot = 1,
    SniperShot = 2,
    MortarLaunch = 3,
    ShellImpact = 4,
    BulletHit = 5,
    EnemyShot = 6,
    EnemyDeath = 7,
    VehicleDeath = 8,
    AllyAttack = 9,
    AllyDeath = 10,
    BunkerHit = 11,
    MineExplosion = 12,
    PlaneFlyby = 13,
    PlaneShot = 14,
    Buff = 15,
    CardDraw = 16,
    CardPlace = 17,
    InfoBox = 18,
    TrashOpen = 19,
    TrashClose = 20,
    WaveStart = 21,
    EnemyBaseHit = 22,
    EnemyBaseDestroyed = 23,
    Defeat = 24
}

// All the game's sound effects, synthesized at runtime with the Retro Sound Synthesizer (Tools > Procedural Audio Synthesizer:
// design a sound there, Copy JSON, then Paste JSON on its entry here). Lives in Resources so SoundBank.Play works from anywhere.
[CreateAssetMenu(menuName = "Bunker/Sound Bank", fileName = "SoundBank")]
public class SoundBank : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public Sfx id;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Pitch spread between the takes it alternates, so repeated sounds don't feel copy-pasted")]
        [Range(0f, 0.5f)] public float variation = 0.08f;
        [Tooltip("Seconds before it can sound again: twenty towers firing in the same frame play it once")]
        [Min(0f)] public float minInterval = 0.05f;
        public CompositeSound sound = new CompositeSound();

        [NonSerialized] public AudioClip[] takes;
        [NonSerialized] public float lastPlayed = float.NegativeInfinity;
        [NonSerialized] public int nextTake;
    }

    const string ResourcePath = "SoundBank";
    const int TakeCount = 3;

    public List<Entry> entries = new List<Entry>();
    [Header("Music")]
    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    static SoundBank instance;
    static Dictionary<Sfx, Entry> lookup;
    static AudioSource sfxSource, musicSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        lookup = null;
        sfxSource = null;
        musicSource = null;
    }

    public static void Play(Sfx id)
    {
        if (id == Sfx.None || !TryGetEntry(id, out var entry)) return;
        float now = Time.unscaledTime;
        if (now - entry.lastPlayed < entry.minInterval) return;

        var clip = NextTake(entry);
        if (clip == null) return;
        entry.lastPlayed = now;
        Source(ref sfxSource, "Sfx").PlayOneShot(clip, entry.volume);
    }

    public static void PlayMusic()
    {
        if (!TryLoad() || instance.music == null) return;
        var source = Source(ref musicSource, "Music");
        if (source.isPlaying && source.clip == instance.music) return;
        source.clip = instance.music;
        source.loop = true;
        source.volume = instance.musicVolume;
        source.Play();
    }

    public static void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    // Synthesizes a take; the first is the sound as designed, the others are pitched a little up and down
    public static AudioClip Synthesize(Entry entry, int take)
    {
        if (entry.sound == null || entry.sound.baseSound == null) return null;
        var sound = entry.sound.Clone();
        float pitch = 1f + entry.variation * (take == 0 ? 0f : take % 2 == 1 ? -0.5f : 0.5f);
        Repitch(sound.baseSound, pitch);
        if (sound.layers != null)
            foreach (var layer in sound.layers) Repitch(layer, pitch);
        return ProceduralAudioPlayer.CreateAudioClip(sound, $"{entry.id}_{take}");
    }

    static void Repitch(SoundParameters parameters, float pitch)
    {
        if (parameters != null)
            parameters.startFrequency = Mathf.Clamp(parameters.startFrequency * pitch, 0.01f, 1f);
    }

    static AudioClip NextTake(Entry entry)
    {
        if (entry.takes == null)
        {
            entry.takes = new AudioClip[TakeCount];
            for (int i = 0; i < TakeCount; i++)
                entry.takes[i] = Synthesize(entry, i);
        }
        var clip = entry.takes[entry.nextTake];
        entry.nextTake = (entry.nextTake + 1) % TakeCount;
        return clip;
    }

    static bool TryGetEntry(Sfx id, out Entry entry)
    {
        entry = null;
        return TryLoad() && lookup.TryGetValue(id, out entry);
    }

    static bool TryLoad()
    {
        if (instance != null) return true;
        instance = Resources.Load<SoundBank>(ResourcePath);
        if (instance == null)
        {
            Debug.LogError($"SoundBank: missing Resources/{ResourcePath} asset.");
            return false;
        }
        lookup = new Dictionary<Sfx, Entry>();
        foreach (var entry in instance.entries)
        {
            entry.takes = null;
            entry.lastPlayed = float.NegativeInfinity;
            lookup[entry.id] = entry;
        }
        return true;
    }

    // One 2D source per channel, kept across scene loads; PlayOneShot lets the effects overlap
    static AudioSource Source(ref AudioSource source, string channel)
    {
        if (source != null) return source;
        var host = new GameObject($"[Audio {channel}]");
        DontDestroyOnLoad(host);
        source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }
}
