using UnityEngine;

public class TimerManager : UpdatableBehaviour
{
    public float elapsedTime { get; private set; }

    public void Init()
    {
        elapsedTime = 0f;
    }

    // QA용 — CombatDebugWindow의 Wave 스킵 기능이 SpawnManager.AddElapsedTime()과 함께 호출(2026-07-24)
    public void AddElapsedTime(float _seconds)
    {
        elapsedTime += _seconds;
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        Time.timeScale = 1f;
#endif
    }

    public override void UpdateLogic()
    {
        elapsedTime += Time.deltaTime;
    }
}
