using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this component to any Level scene.
/// It guarantees that the master CoreManagers scene (Canvas UI, GameManager,
/// SimulationBridge, Pooling, Wwise, Main Camera) is loaded additively so any
/// level scene can be played directly from the editor or via menus without duplicating managers.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class LevelCoreBootstrapper : MonoBehaviour
{
    public const string CoreManagersSceneName = "CoreManagers";

    void Awake()
    {
        EnsureCoreManagersLoaded();
    }

    public static void EnsureCoreManagersLoaded()
    {
        var coreScene = SceneManager.GetSceneByName(CoreManagersSceneName);
        if (!coreScene.isLoaded)
        {
            SceneManager.LoadScene(CoreManagersSceneName, LoadSceneMode.Additive);
        }
    }
}
