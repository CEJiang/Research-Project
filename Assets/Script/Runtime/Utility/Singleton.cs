using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T instance;
    private static bool isQuitting;

    public static bool HasInstance => instance != null;

    public static T Instance
    {
        get
        {
            if (isQuitting)
                return null;

            if (instance == null)
            {
                T[] objects = FindObjectsOfType<T>();

                if (objects.Length > 0)
                {
                    instance = objects[0];

                    if (objects.Length > 1)
                    {
                        Logger.Error($"Found more than one {typeof(T)} in the scene.");
                    }

                    Logger.Developer($"{typeof(T)} Singleton found.");
                    return instance;
                }

                Logger.Warning($"{typeof(T)} Singleton not found in scene.");
                return null;
            }

            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            Logger.Developer($"{typeof(T)} Singleton awake.");
        }
        else if (instance == this)
        {
            Logger.Developer($"{typeof(T)} Singleton awake.");
        }
        else
        {
            Logger.Warning($"Destroy duplicated {typeof(T)} object found when awake.");
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        isQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            Logger.Developer($"{typeof(T)} Singleton destroyed.");
            instance = null;
        }
    }
}