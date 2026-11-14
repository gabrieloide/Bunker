using UnityEngine;
using UnityEngine.SceneManagement;

// Which level is being played (survives scene loads) and how many are unlocked (PlayerPrefs).
public static class LevelProgress
{
    const string UnlockedKey = "Bunker.LevelsUnlocked";
    const string CompletedKeyPrefix = "Bunker.LevelCompleted.";
    const string PendingUnlockKey = "Bunker.PendingUnlockNode";
    public const string SelectorScene = "LevelSelector";

    public static LevelCatalog Catalog { get; private set; }
    public static LevelDefinition Selected { get; private set; }
    public static int SelectedIndex => Catalog != null && Selected != null ? Catalog.IndexOf(Selected) : -1;

    public static int UnlockedCount => Mathf.Max(1, PlayerPrefs.GetInt(UnlockedKey, 1));
    public static int PendingUnlockIndex => PlayerPrefs.GetInt(PendingUnlockKey, -1);
    public static bool IsUnlocked(int index) => index < UnlockedCount;
    public static bool IsCompleted(int index) => PlayerPrefs.GetInt(CompletedKeyPrefix + index, 0) == 1 || index < UnlockedCount - 1;

    public static LevelDefinition Next => Catalog != null ? Catalog.At(SelectedIndex + 1) : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Catalog = null;
        Selected = null;
    }

    public static void Play(LevelCatalog catalog, LevelDefinition level)
    {
        if (level == null) return;
        Catalog = catalog;
        Selected = level;
        Time.timeScale = 1f;
        CRTTransition.LoadScene(level.sceneName);
    }

    public static void MarkSelectedCompleted()
    {
        int index = SelectedIndex;
        if (index >= 0)
        {
            PlayerPrefs.SetInt(CompletedKeyPrefix + index, 1);
        }
        if (index < 0 || index + 2 <= UnlockedCount)
        {
            PlayerPrefs.Save();
            return;
        }
        PlayerPrefs.SetInt(PendingUnlockKey, index + 1);
        PlayerPrefs.SetInt(UnlockedKey, index + 2);
        PlayerPrefs.Save();
    }

    public static int ConsumePendingUnlock()
    {
        int pending = PendingUnlockIndex;
        if (pending >= 0)
        {
            PlayerPrefs.SetInt(PendingUnlockKey, -1);
            PlayerPrefs.Save();
        }
        return pending;
    }

    public static void BackToSelector()
    {
        Time.timeScale = 1f;
        CRTTransition.LoadScene(SelectorScene);
    }

    public static void Retry()
    {
        Time.timeScale = 1f;
        CRTTransition.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
