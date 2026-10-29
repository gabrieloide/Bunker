using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Global CRT CCTV Channel Switch & Beam Snap Transition:
// Operates across all scene changes with zero setup required via DontDestroyOnLoad.
[DefaultExecutionOrder(-2000)]
public class CRTTransition : MonoBehaviour
{
    public static CRTTransition Instance { get; private set; }

    [Header("Transition Timings")]
    [SerializeField] float outDuration = 0.18f;
    [SerializeField] float inDuration = 0.35f;

    [Header("Sound Effect")]
    [SerializeField] bool playSynthClick = true;
    [SerializeField] AudioClip customSwitchSound;

    [Header("Material")]
    [SerializeField] Material transitionMaterial;

    Material matInstance;
    bool overlayVisible;
    AudioSource audioSource;
    bool isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            var go = new GameObject("[CRTTransition]");
            go.AddComponent<CRTTransition>();
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupOverlay();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void SetupOverlay()
    {
        // 1. Load shader material if missing
        if (transitionMaterial == null)
        {
            transitionMaterial = Resources.Load<Material>("CRTChannelSwitch");
            if (transitionMaterial == null)
            {
                var s = Shader.Find("Bunker/CRTChannelSwitch");
                if (s != null) transitionMaterial = new Material(s);
            }
        }

        if (transitionMaterial != null)
            matInstance = new Material(transitionMaterial);

        // 2. Audio Source for click
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    // Drawn last over the game and every UI Toolkit panel
    void OnGUI()
    {
        if (!overlayVisible || matInstance == null || Event.current.type != EventType.Repaint) return;
        GUI.depth = -1000;
        Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture,
            new Rect(0, 0, 1, 1), 0, 0, 0, 0, Color.white, matInstance);
    }

    public static void LoadScene(string sceneName)
    {
        if (Instance != null)
        {
            Instance.StartTransition(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    public static void LoadScene(int sceneBuildIndex)
    {
        if (Instance != null)
        {
            Instance.StartTransitionIndex(sceneBuildIndex);
        }
        else
        {
            SceneManager.LoadScene(sceneBuildIndex);
        }
    }

    public void StartTransition(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName, -1));
    }

    public void StartTransitionIndex(int sceneIndex)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine("", sceneIndex));
    }

    IEnumerator TransitionRoutine(string sceneName, int sceneIndex)
    {
        isTransitioning = true;
        Time.timeScale = 1f;

        overlayVisible = true;

        PlaySwitchAudio();

        // --- 1. PHASE 0: OUT (CCTV Desync & Cathode Beam Collapse) ---
        if (matInstance != null)
        {
            matInstance.SetFloat("_Phase", 0f);
            matInstance.SetFloat("_Progress", 0f);
        }

        float elapsed = 0f;
        while (elapsed < outDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / outDuration);
            if (matInstance != null) matInstance.SetFloat("_Progress", t);
            yield return null;
        }

        if (matInstance != null) matInstance.SetFloat("_Progress", 1f);

        // --- 2. LOAD SCENE ASYNC ---
        AsyncOperation op = null;
        if (!string.IsNullOrEmpty(sceneName))
            op = SceneManager.LoadSceneAsync(sceneName);
        else if (sceneIndex >= 0)
            op = SceneManager.LoadSceneAsync(sceneIndex);

        if (op != null)
        {
            while (!op.isDone) yield return null;
        }

        // Wait 1 frame for new scene to awake and camera to initialize
        yield return null;

        // --- 3. PHASE 1: IN (Cathode Beam Snap Expansion & Settle) ---
        if (matInstance != null)
        {
            matInstance.SetFloat("_Phase", 1f);
            matInstance.SetFloat("_Progress", 0f);
        }

        elapsed = 0f;
        while (elapsed < inDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / inDuration);
            if (matInstance != null) matInstance.SetFloat("_Progress", t);
            yield return null;
        }

        if (matInstance != null) matInstance.SetFloat("_Progress", 1f);

        overlayVisible = false;

        isTransitioning = false;
    }

    void PlaySwitchAudio()
    {
        if (!playSynthClick) return;

        try
        {
            if (customSwitchSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(customSwitchSound, 0.75f);
            }
        }
        catch { }
    }

    void OnDestroy()
    {
        if (matInstance != null)
        {
            Destroy(matInstance);
        }
    }
}
