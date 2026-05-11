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

        Object.Instantiate(corePrefab);
    }
}
