using UnityEngine;

public class InGameScene : BaseScene
{
    [SerializeField] private MonsterManager m_MonsterManager;
    [SerializeField] private SpawnManager m_SpawnManager;
    [SerializeField] private TimerManager m_TimerManager;
    [SerializeField] private TowerHealth m_TowerHealth;

    protected override void OnSetup()
    {
        m_MonsterManager.Init();
        m_SpawnManager.Init();
        m_TimerManager.Init();

        GameConfigTable gameConfigTable = TableManager.instance.GetTable<GameConfigTable>();
        if (gameConfigTable == null)
        {
            Logger.Error($"[InGameScene] OnSetup Failed to init TowerHealth! GameConfigTable not loaded - TableManager.init() 선행 필요");
            return;
        }

        int towerMaxHp = (int)gameConfigTable.GetValue("TowerMaxHp", 100f);
        m_TowerHealth.Init(towerMaxHp);
        m_MonsterManager.OnMonsterReachEnd += m_TowerHealth.OnEnemyReachTower;
        m_TowerHealth.OnDie += OnTowerDie;
    }

    private void OnTowerDie()
    {
        Time.timeScale = 0f;
        UIManager.instance.Get<UIRunOver>();
    }
}
