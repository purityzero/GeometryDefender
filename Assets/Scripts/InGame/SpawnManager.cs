using UnityEngine;

public class SpawnManager : MonoBehaviour, IUpdatable
{
    [SerializeField] private MonsterManager m_MonsterManager;
    [SerializeField] private float m_SpawnInterval = 1f;

    private EnemyTable m_EnemyTable;
    private WaveTable m_WaveTable;
    private WaveSpawnTable m_WaveSpawnTable;

    private float m_ElapsedTime;
    private float m_SpawnTimer;
    private int m_LastBossSecond = -1;

    // 핫 리로드 시 테이블 참조는 null로 리셋되므로 플래그도 함께 리셋되도록 보존 대상에서 제외 (MonsterManager와 동일 이유)
    [System.NonSerialized] private bool m_isInitialized;

    public void Init()
    {
        m_EnemyTable = TableManager.instance.GetTable<EnemyTable>();
        m_WaveTable = TableManager.instance.GetTable<WaveTable>();
        m_WaveSpawnTable = TableManager.instance.GetTable<WaveSpawnTable>();

        if (m_EnemyTable == null || m_WaveTable == null || m_WaveSpawnTable == null)
        {
            Debug.LogError($"[SpawnManager] Init Failed! table not loaded - TableManager.init() 선행 필요");
            return;
        }

        m_ElapsedTime = 0f;
        m_SpawnTimer = 0f;
        m_LastBossSecond = -1;

        m_isInitialized = true;
    }

    private void Start()
    {
        BaseScene.Current.Register(this);
    }

    private void OnDestroy()
    {
        BaseScene.Current?.Unregister(this);
    }

    public void UpdateLogic()
    {
        if (m_isInitialized == false)
            return;

        m_ElapsedTime += Time.deltaTime;

        UpdatePhaseSpawn();
        UpdateBossSpawn();
    }

    private void UpdatePhaseSpawn()
    {
        m_SpawnTimer += Time.deltaTime;
        if (m_SpawnTimer < m_SpawnInterval)
            return;

        m_SpawnTimer -= m_SpawnInterval;

        WaveRecord phase = m_WaveTable.GetActivePhase((int)m_ElapsedTime);
        if (phase == null)
            return;

        eEnemySpecies species = PickSpecies(phase);
        eEnemyVariant variant = (Random.value < phase.EliteChance) ? eEnemyVariant.Elite : eEnemyVariant.Normal;

        EnemyRecord record = m_EnemyTable.GetRecordBySpeciesAndVariant(species, variant);
        if (record == null)
        {
            Debug.LogError($"[SpawnManager] EnemyRecord not found! species: {species}, variant: {variant}");
            return;
        }

        m_MonsterManager.Spawn(record);
    }

    private void UpdateBossSpawn()
    {
        int currentSecond = (int)m_ElapsedTime;
        if (currentSecond == m_LastBossSecond)
            return;

        // 프레임 드랍으로 1초 이상 건너뛰어도 보스 이벤트를 놓치지 않도록 지난 초를 전부 검사
        for (int second = m_LastBossSecond + 1; second <= currentSecond; ++second)
        {
            WaveSpawnRecord bossEvent = m_WaveSpawnTable.GetBossEventAtTime(second);
            if (bossEvent == null)
                continue;

            EnemyRecord bossRecord = m_EnemyTable.GetRecordById(bossEvent.EnemyId);
            if (bossRecord == null)
            {
                Debug.LogError($"[SpawnManager] Boss EnemyRecord not found! enemyId: {bossEvent.EnemyId}");
                continue;
            }

            m_MonsterManager.Spawn(bossRecord);
        }

        m_LastBossSecond = currentSecond;
    }

    private eEnemySpecies PickSpecies(WaveRecord _phase)
    {
        int totalWeight = _phase.NormalWeight + _phase.SwiftWeight + _phase.HeavyWeight + _phase.SplitterWeight + _phase.RangedWeight;
        if (totalWeight <= 0)
            return eEnemySpecies.Normal;

        int pick = Random.Range(0, totalWeight);

        if (pick < _phase.NormalWeight)
            return eEnemySpecies.Normal;
        pick -= _phase.NormalWeight;

        if (pick < _phase.SwiftWeight)
            return eEnemySpecies.Swift;
        pick -= _phase.SwiftWeight;

        if (pick < _phase.HeavyWeight)
            return eEnemySpecies.Heavy;
        pick -= _phase.HeavyWeight;

        if (pick < _phase.SplitterWeight)
            return eEnemySpecies.Splitter;

        return eEnemySpecies.Ranged;
    }
}
