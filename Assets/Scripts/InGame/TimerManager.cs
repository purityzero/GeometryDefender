using UnityEngine;

public class TimerManager : SceneSingleton<TimerManager>, IUpdatable
{
    public float elapsedTime { get; private set; }

    public void Init()
    {
        elapsedTime = 0f;
    }

    private void Start()
    {
        BaseScene.Current.Register(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

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
