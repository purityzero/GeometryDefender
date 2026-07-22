using UnityEngine;

public class InGameScene : BaseScene
{
    [SerializeField] private MonsterManager m_MonsterManager;
    [SerializeField] private SpawnManager m_SpawnManager;
    [SerializeField] private TimerManager m_TimerManager;
    [SerializeField] private TowerHealth m_TowerHealth;
    [SerializeField] private ProjectileManager m_ProjectileManager;
    [SerializeField] private TowerController m_TowerController;
    [SerializeField] private DifficultyManager m_DifficultyManager;

    protected override void OnSetup()
    {
        m_DifficultyManager.Init();
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

        // 05_meta.html "STARTING POWER" 줄기 — 해금된 영구 업그레이드를 시작 스탯에 반영
        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        if (metaTreeTable != null)
            towerMaxHp += metaTreeTable.GetTotalEffectValue(eMetaEffectType.MaxHp, PlayerManager.instance.playerData.UnlockedMetaNodes);

        m_TowerHealth.Init(towerMaxHp);
        m_MonsterManager.OnMonsterReachEnd += m_TowerHealth.OnEnemyReachTower;
        m_TowerHealth.OnDie += OnTowerDie;

        m_ProjectileManager.Init();
        m_TowerController.Init();
    }

    private void OnTowerDie()
    {
        Time.timeScale = 0f;
        UIManager.instance.Get<UIRunOver>();
    }
}
