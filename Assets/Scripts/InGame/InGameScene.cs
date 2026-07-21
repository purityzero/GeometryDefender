using UnityEngine;

public class InGameScene : BaseScene
{
    [SerializeField] private MonsterManager m_MonsterManager;
    [SerializeField] private SpawnManager m_SpawnManager;
    [SerializeField] private TimerManager m_TimerManager;

    protected override void OnSetup()
    {
        m_MonsterManager.Init();
        m_SpawnManager.Init();
        m_TimerManager.Init();
    }
}
