using UnityEngine;

// Title screen (UI/Title.uxml): any key moves on to the main menu once the CRT power-on finishes
public class TitleScreen : MonoBehaviour
{
    [SerializeField] string SceneName = "MainMenu 2";
    [SerializeField] CRTTurnOnEffect turnOnEffect;

    bool isTransitioning;

    void Update()
    {
        if (isTransitioning) return;

        // Wait until the CRT TV turning on animation completes
        if (turnOnEffect != null && turnOnEffect.IsPlaying) return;

        if (Input.anyKeyDown)
        {
            isTransitioning = true;
            CRTTransition.LoadScene(SceneName);
        }
    }
}
