using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this component to any Level scene.
/// It guarantees that the master CoreManagers scene (Canvas UI, GameManager,
/// SimulationBridge, Pooling, Main Camera) is loaded additively so any
/// level scene can be played directly from the editor or via menus without duplicating managers.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class LevelCoreBootstrapper : MonoBehaviour
{
    public const string CoreManagersSceneName = "CoreManagers";

    static int lastRequestFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        lastRequestFrame = -1;
    }

    void Awake()
    {
        EnsureCoreManagersLoaded();
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    void OnSceneUnloaded(Scene s)
    {
        if (s.name == CoreManagersSceneName
            && gameObject.scene == SceneManager.GetActiveScene()
            && !CRTTransition.IsTransitioning)
        {
            EnsureCoreManagersLoaded();
        }
    }

    public static void EnsureCoreManagersLoaded()
    {
        var coreScene = SceneManager.GetSceneByName(CoreManagersSceneName);
        if (!coreScene.isLoaded && lastRequestFrame != Time.frameCount)
        {
            lastRequestFrame = Time.frameCount;
            SceneManager.LoadScene(CoreManagersSceneName, LoadSceneMode.Additive);
        }
    }
}
