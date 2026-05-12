using IngameDebugConsole;
using UnityEngine;

public static class CoreBootstrap
{
    private const string CoreResourcePath = "System/Core";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureCoreExists()
    {
        if (CoreStartPoint.HasActiveCore || Object.FindAnyObjectByType<CoreStartPoint>() != null)
        {
            return;
        }

        GameObject corePrefab = Resources.Load<GameObject>(CoreResourcePath);
        if (corePrefab == null)
        {
            Debug.LogError($"Core bootstrap failed. Could not load prefab at Resources/{CoreResourcePath}.");
            return;
        }

        bool prefabWasActive = corePrefab.activeSelf;
        corePrefab.SetActive(false);

        GameObject core = Object.Instantiate(corePrefab);
        corePrefab.SetActive(prefabWasActive);

        core.transform.SetParent(null);
        DetachPersistentChildren(core);
        core.SetActive(true);
    }

    private static void DetachPersistentChildren(GameObject core)
    {
        DebugLogManager[] debugLogManagers = core.GetComponentsInChildren<DebugLogManager>(true);
        foreach (DebugLogManager debugLogManager in debugLogManagers)
        {
            debugLogManager.transform.SetParent(null, false);
        }
    }
}
