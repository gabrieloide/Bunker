using UnityEngine;

// Lives on the audio host: synthesizes one SoundBank sound per frame until all are ready, then removes itself
public class SoundBankWarmup : MonoBehaviour
{
    void Update()
    {
        if (!SoundBank.WarmNext())
            Destroy(this);
    }
}
