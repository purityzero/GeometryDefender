using UnityEngine;

public class SpawnManager : UpdatableBehaviour
{
    [SerializeField] private MonsterManager m_MonsterManager;

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
            Logger.Error($"[SpawnManager] Init Failed! table not loaded - TableManager.init() 선행 필요");
            return;
        }

        m_ElapsedTime = 0f;
        m_SpawnTimer = 0f;
        m_LastBossSecond = -1;

        m_isInitialized = true;
    }

    // QA용 — CombatDebugWindow의 Wave 스킵 기능이 TimerManager.AddElapsedTime()과 함께 호출(2026-07-24).
    // Wave/스폰 배율 판정은 이 m_ElapsedTime 기준이라(TimerManager.elapsedTime과는 별개 필드), 둘 다 같이 옮겨야 UI 표시 시간과 실제 웨이브가 어긋나지 않는다.
    public void AddElapsedTime(float _seconds)
    {
        m_ElapsedTime += _seconds;
    }

    public override void UpdateLogic()
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

        // Assets/Design/08_balance.html "적 스폰 곡선": spawnRate(t) = baseRate × (1 + t/60)^exponent, 난이도 배율은 추가로 곱해짐
        float difficultyMultiplier = (InGameScene.Current.difficultyManager != null) ? InGameScene.Current.difficultyManager.GetDifficultyMultiplier() : 1f;
        float spawnRate = GameConfigTable.SPAWN_BASE_RATE * Mathf.Pow(1f + m_ElapsedTime / 60f, GameConfigTable.SPAWN_RATE_EXPONENT) * difficultyMultiplier;
        float spawnInterval = 1f / spawnRate;

        if (m_SpawnTimer < spawnInterval)
            return;

        m_SpawnTimer -= spawnInterval;

        WaveRecord phase = m_WaveTable.GetActivePhase((int)m_ElapsedTime);
        if (phase == null)
            return;

        eEnemySpecies species = PickSpecies(phase);
        eEnemyVariant variant = (Random.value < phase.EliteChance) ? eEnemyVariant.Elite : eEnemyVariant.Normal;

        EnemyRecord record = m_EnemyTable.GetRecordBySpeciesAndVariant(species, variant);
        if (record == null)
        {
            Logger.Error($"[SpawnManager] EnemyRecord not found! species: {species}, variant: {variant}");
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
                Logger.Error($"[SpawnManager] Boss EnemyRecord not found! enemyId: {bossEvent.EnemyId}");
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
