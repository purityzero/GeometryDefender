using UnityEngine;

public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
{
    public static T Current { get; private set; }

    protected virtual void Awake()
    {
        Current = this as T;
    }

    protected virtual void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }
}
