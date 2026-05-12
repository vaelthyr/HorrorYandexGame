using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> pool = new Queue<T>();
    private readonly T prefab;
    private readonly Transform parent;

    public ObjectPool(T prefab, int initialSize = 10, Transform parent = null)
    {
        if (prefab == null)
        {
            Debug.LogError($"ObjectPool<{typeof(T).Name}> cannot be created with a null prefab.");
            return;
        }

        this.prefab = prefab;
        this.parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            AddObjectToPool();
        }
    }

    private void AddObjectToPool()
    {
        if (prefab == null)
        {
            return;
        }

        T obj = Object.Instantiate(prefab, parent);
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }

    public T GetObject()
    {
        if (prefab == null)
        {
            return null;
        }

        if (pool.Count == 0)
        {
            AddObjectToPool();
        }

        T obj = pool.Dequeue();
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void ReturnObject(T obj)
    {
        if (obj == null)
        {
            return;
        }

        obj.gameObject.SetActive(false);
        obj.transform.SetParent(parent, false);
        pool.Enqueue(obj);
    }
}
