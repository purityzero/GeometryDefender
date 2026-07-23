using UnityEngine;

// SceneSingleton<T>/UIBase처럼 공용 베이스가 따로 없는 일반 MonoBehaviour가 BaseScene의 갱신 루프를 타야 할 때 상속
public abstract class UpdatableBehaviour : MonoBehaviour, IUpdatable
{
    protected virtual void OnEnable()
    {
        BaseScene.Current?.Register(this);
    }

    protected virtual void OnDisable()
    {
        BaseScene.Current?.Unregister(this);
    }

    public virtual void UpdateLogic() { }
}
