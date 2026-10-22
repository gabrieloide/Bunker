using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Simulates the physical power-on of a vintage CRT cathode ray tube television:
// 1. Black screen -> 2. Central electron beam ignition -> 3. Vertical slit opening -> 4. Phosphor settle.
[RequireComponent(typeof(Image))]
public class CRTTurnOnEffect : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Total duration of the turn-on animation in seconds")]
    [SerializeField] float duration = 0.95f;
    [SerializeField] bool playOnStart = true;
    [SerializeField] bool unscaledTime = true;

    [Header("Audio (Optional)")]
    [Tooltip("Play retro CRT degauss and coil hum sound on power on")]
    [SerializeField] bool playSynthSound = true;
    [SerializeField] AudioClip customSound;

    [Header("References")]
    [SerializeField] Material crtTurnOnMaterial;

    Image image;
    Material matInstance;
    AudioSource audioSource;

    public event Action OnTurnOnComplete;
    public bool IsPlaying { get; private set; }

    void Awake()
    {
        image = GetComponent<Image>();

        if (crtTurnOnMaterial == null && image != null && image.material != null)
        {
            crtTurnOnMaterial = image.material;
        }

        if (crtTurnOnMaterial != null)
        {
            matInstance = new Material(crtTurnOnMaterial);
            image.material = matInstance;
            matInstance.SetFloat("_Progress", 0f);
        }

        if (playSynthSound)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D Sound
        }
    }

    void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        if (IsPlaying) return;
        gameObject.SetActive(true);
        StartCoroutine(TurnOnRoutine());
    }

    IEnumerator TurnOnRoutine()
    {
        IsPlaying = true;

        if (matInstance != null)
        {
            matInstance.SetFloat("_Progress", 0f);
        }

        // Play authentic CRT power-on sound
        PlayPowerOnAudio();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / duration);

            if (matInstance != null)
            {
                matInstance.SetFloat("_Progress", t);
            }

            yield return null;
        }

        if (matInstance != null)
        {
            matInstance.SetFloat("_Progress", 1f);
        }

        IsPlaying = false;
        OnTurnOnComplete?.Invoke();

        // Deactivate overlay so it doesn't block UI input or take drawcalls
        gameObject.SetActive(false);
    }

    void PlayPowerOnAudio()
    {
        if (!playSynthSound || audioSource == null) return;

        try
        {
            if (customSound != null)
            {
                audioSource.PlayOneShot(customSound, 0.8f);
                return;
            }

            // Procedural retro CRT Degauss pop + 60Hz flyback coil whistle
            AudioClip clip = GenerateCrtPowerOnSound();
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, 0.7f);
            }
        }
        catch
        {
            // Silently ignore if Wwise has disabled Unity Audio engine
        }
    }

    AudioClip GenerateCrtPowerOnSound()
    {
        try
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.75f);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float time = (float)i / sampleRate;

                // 1. Initial power surge pop
                float popDecay = Mathf.Exp(-time * 25f);
                float pop = Mathf.Sin(2f * Mathf.PI * 65f * time) * popDecay * 0.8f;

                // 2. Rising coil sweep (1kHz to 12kHz charging tone)
                float sweepDecay = Mathf.Clamp01(time / 0.35f) * Mathf.Exp(-time * 4f);
                float freq = Mathf.Lerp(1200f, 9500f, Mathf.Clamp01(time * 3f));
                float whine = Mathf.Sin(2f * Mathf.PI * freq * time) * sweepDecay * 0.25f;

                // 3. Subtle AC hum (60Hz)
                float hum = Mathf.Sin(2f * Mathf.PI * 60f * time) * 0.08f * Mathf.Clamp01((0.7f - time) * 2f);

                samples[i] = Mathf.Clamp(pop + whine + hum, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("CRTPowerOnSynth", length, 1, sampleRate, false);
            if (clip != null)
            {
                clip.SetData(samples, 0);
            }
            return clip;
        }
        catch
        {
            return null;
        }
    }

    void OnDestroy()
    {
        if (matInstance != null)
        {
            Destroy(matInstance);
        }
    }
}
