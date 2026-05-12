using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T m_Instance;

    public static T instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = Object.FindFirstObjectByType<T>();

                if (m_Instance == null)
                {
                    Debug.LogError($"No instance of {typeof(T).Name} found in scene.");
                    return null;
                }

                m_Instance.Init();
            }

            return m_Instance;
        }
    }

    protected virtual void Awake()
    {
        if (m_Instance != null && m_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        m_Instance = this as T;
        m_Instance.Init();
    }

    public virtual void Init() { }

    protected virtual void OnApplicationQuit()
    {
        m_Instance = null;
    }
}
