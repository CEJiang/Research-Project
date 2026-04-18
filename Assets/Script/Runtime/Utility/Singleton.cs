using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component {

    private static T instance;

    public static T Instance
    {
        get
        {
            // Not initialized yet
            if (instance == null)
            {
                // Try to find the instance
                var objects = FindObjectsOfType<T>();

                if (objects.Length > 0)
                {
                    // Return the instance found in the scene
                    Logger.Developer($"{typeof(T)} Singleton found.");
                    
                    if (objects.Length > 1)
                    {
                        Logger.Error($"Found more than one {typeof(T)} in the scene.");
                    }
                    else
                    {
                        return objects[0];
                    }
                }
                else
                {
                    // Create a new instance if not found in the scene
                    GameObject object_ = new();
                    instance = object_.AddComponent<T>();
                    object_.name = typeof(T).ToString();
                    Logger.Warning($"{typeof(T)} Singleton created.");
                }
            }

            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            Logger.Developer($"{typeof(T)} Singleton awake.");
            instance = this as T;
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

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            Logger.Developer($"{typeof(T)} Singleton destroyed.");
            instance = null;
        }
        else
        {
            Logger.Developer($"Destroy duplicated {typeof(T)} object.");
        }
    }
}
