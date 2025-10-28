using UnityEngine;

public class SingletonObject<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<T>();
                if (_instance == null) Debug.LogError($"Singleton<{typeof(T)}> instance not found in scene.");
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null) _instance = this as T;
        else if (_instance != this)
        {
            Debug.LogWarning($"Duplicate Singleton<{typeof(T)}> found. Destroying {gameObject.name}");
            Destroy(gameObject);
        }
    }
}
