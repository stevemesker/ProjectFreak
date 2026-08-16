using UnityEngine;

public static class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        RuntimeAssetSO settings = Resources.Load<RuntimeAssetSO>("Runtime/RuntimeSettings");

        if (settings == null)
        {
            Debug.LogError("RuntimeSettings could not be found in Resources/Runtime/");
            return;
        }

        foreach (GameObject prefab in settings.PersistentPrefabs)
        {
            if (prefab != null)
            {
                Object.Instantiate(prefab);
            }
        }
    }
}
