using UnityEngine;

public abstract class SceneSingleton<T> : MonoBehaviour, IUpdatable where T : SceneSingleton<T>
{
    public static T Current { get; protected set; }

    protected virtual void Awake()
    {
        Current = this as T;
    }

    // Play 중 스크립트 재컴파일(도메인 리로드) 시 static Current는 초기화되지만 Awake()는 재호출되지 않고
    // OnEnable()만 재호출된다 — 여기서도 갱신해야 재컴파일 이후 Current가 영구 null로 남는 것을 막을 수 있다.
    protected virtual void OnEnable()
    {
        Current = this as T;
        BaseScene.Current?.Register(this);
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
