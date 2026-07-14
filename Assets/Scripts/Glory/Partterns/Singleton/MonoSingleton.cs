using UnityEngine;
using System;

public class MonoSingleton<T> : MonoBehaviour where T : Component
{
    private static readonly Lazy<T> _instance = new Lazy<T>(CreateSingleton);
    public static T instance => _instance.Value;

    private static T CreateSingleton()
    {
        T instance = FindFirstObjectByType<T>();

        if (instance == null)
        {
            T[] instances = Resources.FindObjectsOfTypeAll<T>();
            if (instances.Length > 0)
            {
                instance = instances[0];
            }
        }

        if (instance == null)
        {
            GameObject singletonObj = new GameObject(typeof(T).Name);
            instance = singletonObj.AddComponent<T>();
        }

        return instance;
    }

    protected virtual void Awake()
    {
        if (_instance.Value != this)
        {
            Destroy(gameObject); // 중복된 싱글톤 제거
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

}