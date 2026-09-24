using UnityEngine;

// Applies the selected LevelDefinition to the game scene: plants its flags and exposes it to the
// bridge (waves, enemy base, allies) and GameManager (starting hand). Runs before everything else.
[DefaultExecutionOrder(-500)]
public class LevelSetup : MonoBehaviour
{
    public static LevelDefinition Current { get; private set; }

    [Tooltip("Played when the scene is started directly from the editor, without the level selector")]
    [SerializeField] LevelDefinition fallbackLevel;
    [SerializeField] Flag flagPrefab;

    void Awake()
    {
        Time.timeScale = 1f;
        Current = LevelProgress.Selected != null ? LevelProgress.Selected : fallbackLevel;
        if (Current == null || flagPrefab == null) return;

        foreach (var placement in Current.flags)
        {
            var flag = Instantiate(flagPrefab, placement.position, Quaternion.identity);
            flag.Radius = placement.radius;
        }
    }

    void OnDestroy() => Current = null;
}
