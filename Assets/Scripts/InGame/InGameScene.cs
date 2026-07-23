using Unity.Entities;
using UnityEngine;

// BaseScene.Awake()가 씬 내 다른 모든 스크립트의 Awake/OnEnable보다 먼저 실행되도록 강제
// — Unity는 "모든 오브젝트의 Awake가 끝난 뒤에야 Start가 불린다"는 보장은 하지만 OnEnable에는 이 보장이 없어서,
// 로드 순서에 따라 다른 스크립트의 OnEnable(예: UpdatableBehaviour/SceneSingleton/UIBase)이 BaseScene.Awake()보다 먼저 돌며
// BaseScene.Current가 아직 null인 상태로 BaseScene.Current.Register(this)를 호출해 NRE가 나는 사고가 실제로 있었음
[DefaultExecutionOrder(-1000)]
public class InGameScene : BaseScene
{
    // 개별 매니저가 각자 SceneSingleton<T>를 상속하던 방식(2026-07-23 이전) 대신, 이 씬 진입점 하나만 싱글톤 역할을 하고
    // 나머지 매니저는 전부 UpdatableBehaviour(등록/해제만 담당, Current 없음)로 통일 — 싱글톤 난립 방지(사용자 요청).
    // 주의: `BaseScene.Current`를 그대로 재사용(as InGameScene 캐스팅)하지 않고 별도 static을 직접 관리한다 —
    // BaseScene.Current는 TitleScene도 같이 쓰는 공유 슬롯이라, 씬 전환 중(페이드아웃→TitleScene additive 로드→InGameScene 언로드)
    // TitleScene의 Awake()가 먼저 돌면 아직 안 죽은 InGameScene 쪽 오브젝트가 하던 작업 중에 Current가 널로 바뀌는 레이스가 실제로 있었음.
    // InGameScene 자신의 Awake~OnDestroy 생명주기에만 묶인 전용 static이면 이 레이스가 없다(2026-07-23 확인).
    public new static InGameScene Current { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Current = this;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (Current == this)
            Current = null;

        // ECS World는 씬 언로드와 별개로 유지되므로(2026-07-23 확인, MonsterManager/ProjectileManager와 동일 이유),
        // 게임오버/일시정지로 SimulationSystemGroup을 꺼둔 채 씬을 나가면 다음 InGameScene 세션까지 몬스터가 영원히 멈춰있게 된다.
        // 씬을 나갈 때는 항상 무조건 다시 켜서 원복한다.
        World world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated == true)
        {
            SimulationSystemGroup simulationSystemGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
            if (simulationSystemGroup != null)
                simulationSystemGroup.Enabled = true;
        }
    }

    [SerializeField] private MonsterManager m_MonsterManager;
    [SerializeField] private SpawnManager m_SpawnManager;
    [SerializeField] private TimerManager m_TimerManager;
    [SerializeField] private ProjectileManager m_ProjectileManager;
    [SerializeField] private TowerController m_TowerController;
    [SerializeField] private DifficultyManager m_DifficultyManager;
    [SerializeField] private XpManager m_XpManager;
    [SerializeField] private CardManager m_CardManager;
    [SerializeField] private DamageTextManager m_DamageTextManager;

    public MonsterManager monsterManager => m_MonsterManager;
    public SpawnManager spawnManager => m_SpawnManager;
    public TimerManager timerManager => m_TimerManager;
    public ProjectileManager projectileManager => m_ProjectileManager;
    public TowerController towerController => m_TowerController;
    public DifficultyManager difficultyManager => m_DifficultyManager;
    public XpManager xpManager => m_XpManager;
    public CardManager cardManager => m_CardManager;
    public DamageTextManager damageTextManager => m_DamageTextManager;

    // Time.timeScale 대신 이 상태로 정지를 표현(2026-07-24, "TimeScale 건드는건 좀 위험해" 사용자 지시).
    // m_isPaused(팝업이 여닫는 일시정지)와 m_isGameOver(타워 사망, 영구)는 독립적이며 둘 중 하나라도 true면 정지.
    private bool m_isPaused;
    private bool m_isGameOver;

    public void SetPaused(bool _isPaused)
    {
        m_isPaused = _isPaused;
        ApplyFreezeState();
    }

    private void ApplyFreezeState()
    {
        bool shouldFreeze = (m_isPaused == true || m_isGameOver == true);

        isPaused = shouldFreeze;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || world.IsCreated == false)
            return;

        SimulationSystemGroup simulationSystemGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
        if (simulationSystemGroup != null)
            simulationSystemGroup.Enabled = (shouldFreeze == false);
    }

    protected override void OnSetup()
    {
        m_DifficultyManager.Init();
        m_MonsterManager.Init();
        m_SpawnManager.Init();
        m_TimerManager.Init();
        m_DamageTextManager.Init();

        GameConfigTable gameConfigTable = TableManager.instance.GetTable<GameConfigTable>();
        if (gameConfigTable == null)
        {
            Logger.Error($"[InGameScene] OnSetup Failed to init TowerController! GameConfigTable not loaded - TableManager.init() 선행 필요");
            return;
        }

        int towerMaxHp = (int)gameConfigTable.GetValue("TowerMaxHp", 100f);

        // 05_meta.html "STARTING POWER" 줄기 — 해금된 영구 업그레이드를 시작 스탯에 반영
        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        if (metaTreeTable != null)
            towerMaxHp += metaTreeTable.GetTotalEffectValue(eMetaEffectType.MaxHp, PlayerManager.instance.playerData.UnlockedMetaNodes);

        m_TowerController.Init(towerMaxHp);
        m_MonsterManager.OnMonsterReachEnd += m_TowerController.OnEnemyReachTower;
        m_TowerController.OnDie += OnTowerDie;

        m_ProjectileManager.Init();

        // Assets/Design/04_card.html — MonsterManager.Init() 이후여야 함(둘 다 InGameScene.Current.monsterManager.OnMonsterDie를 구독)
        m_XpManager.Init();
        m_CardManager.Init();
    }

    private void OnTowerDie()
    {
        m_isGameOver = true;
        ApplyFreezeState();

        UIManager.instance.Get<UIRunOver>();
    }
}
