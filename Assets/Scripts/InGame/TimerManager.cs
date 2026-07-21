using UnityEngine;

public class TimerManager : MonoBehaviour, IUpdatable
{
    public static TimerManager Current { get; private set; }

    public float elapsedTime { get; private set; }

    public void Init()
    {
        elapsedTime = 0f;
    }

    private void Start()
    {
        Current = this;
        BaseScene.Current.Register(this);
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;

        BaseScene.Current?.Unregister(this);

#if UNITY_EDITOR
        Time.timeScale = 1f;
#endif
    }

    public void UpdateLogic()
    {
        elapsedTime += Time.deltaTime;
    }
}
