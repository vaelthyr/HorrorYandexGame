using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class CoreStartPoint : MonoSingleton<CoreStartPoint>
{
    private const string BootstrapSceneName = "EntryPointBootstrap";
    private static CoreStartPoint activeCore;

    public LevelLoader LevelLoader { get; private set; }
    public static bool HasActiveCore => activeCore != null;

    private void Awake()
    {
        if (activeCore != null && activeCore != this)
        {
            Destroy(gameObject);
            return;
        }

        activeCore = this;
        DontDestroyOnLoad(gameObject);
        LevelLoader = GetComponentInChildren<LevelLoader>();

        if (LevelLoader == null)
        {
            Debug.LogError("CoreStartPoint could not find LevelLoader in children.");
        }
    }

    private async void Start()
    {
        if (SceneManager.GetActiveScene().name != BootstrapSceneName)
        {
            return;
        }

        await LevelLoader.LoadNewSceneAsync();
    }

    private void OnDestroy()
    {
        if (activeCore == this)
        {
            activeCore = null;
        }
    }
}
