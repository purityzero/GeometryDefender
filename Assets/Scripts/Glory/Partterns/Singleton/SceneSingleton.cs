using UnityEngine;

public abstract class SceneSingleton<T> : MonoBehaviour, IUpdatable where T : SceneSingleton<T>
{
    public static T Current { get; private set; }

    protected virtual void Awake()
    {
        Current = this as T;
    }

    protected virtual void OnEnable()
    {
        BaseScene.Current.Register(this);
    }

    protected virtual void OnDisable()
    {
        BaseScene.Current?.Unregister(this);
    }

    protected virtual void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    public virtual void UpdateLogic() { }
}
