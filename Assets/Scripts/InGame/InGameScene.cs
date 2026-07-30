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
    [SerializeField] private ActorPlayer m_TowerController;
    [SerializeField] private DifficultyManager m_DifficultyManager;
    [SerializeField] private XpManager m_XpManager;
    [SerializeField] private CardManager m_CardManager;
    [SerializeField] private DamageTextManager m_DamageTextManager;

    public MonsterManager monsterManager => m_MonsterManager;
    public SpawnManager spawnManager => m_SpawnManager;
    public TimerManager timerManager => m_TimerManager;
    public ProjectileManager projectileManager => m_ProjectileManager;
    public ActorPlayer towerController => m_TowerController;
    public DifficultyManager difficultyManager => m_DifficultyManager;
    public XpManager xpManager => m_XpManager;
    public CardManager cardManager => m_CardManager;
    public DamageTextManager damageTextManager => m_DamageTextManager;

    // Time.timeScale 대신 이 상태로 정지를 표현(2026-07-24, "TimeScale 건드는건 좀 위험해" 사용자 지시).
    // m_PauseRequestCount(팝업이 여닫는 일시정지)와 m_isGameOver(타워 사망, 영구)는 독립적이며 둘 중 하나라도 true면 정지.
    //
    // 단일 bool이 아니라 참조 카운터인 이유(2026-07-29 수정, .claude/qa/client-issues.md 2026-07-29-0):
    // UICardDraft/UICheatWindow/UIPause 3개 팝업이 서로의 존재를 모른 채 각자 Show()/Close()에서 SetPaused(true/false)를 호출한다.
    // 단일 bool로 "마지막 호출값"만 저장하면, 두 팝업이 겹쳐 열린 상태에서 하나만 먼저 닫혀도
    // 아직 열려있는 다른 팝업의 의도(계속 정지)와 무관하게 게임이 조용히 재개돼버린다(실제 재현됨).
    private int m_PauseRequestCount;
    private bool m_isGameOver;

    public void SetPaused(bool _isPaused)
    {
        if (_isPaused == true)
        {
            m_PauseRequestCount++;
        }
        else
        {
            // 이미 0인데 SetPaused(false)가 또 호출되는 경우(팝업이 겹쳐 열렸다 닫히는 조합에서 실제로 일어날 수 있음) 음수로 안 내려가게 방어
            if (m_PauseRequestCount > 0)
                m_PauseRequestCount--;
        }

        ApplyFreezeState();
    }

    private void ApplyFreezeState()
    {
        bool shouldFreeze = (m_PauseRequestCount > 0 || m_isGameOver == true);

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

        int towerMaxHp = (int)gameConfigTable.GetValue("TowerMaxHp", 150f);

        // 05_meta.html "STARTING POWER" 줄기 — 해금된 영구 업그레이드를 시작 스탯에 반영
        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        if (metaTreeTable != null)
            towerMaxHp += metaTreeTable.GetTotalEffectValue(eMetaEffectType.MaxHp, PlayerManager.instance.playerData.UnlockedMetaNodes);

        m_TowerController.Init(towerMaxHp);
        m_MonsterManager.OnMonsterReachEnd += m_TowerController.OnEnemyReachTower;
        m_TowerController.OnDie += OnRunEnd;
        m_DifficultyManager.OnCleared += OnRunEnd;

        m_ProjectileManager.Init();

        // Assets/Design/04_card.html — MonsterManager.Init() 이후여야 함(둘 다 InGameScene.Current.monsterManager.OnMonsterDie를 구독)
        m_XpManager.Init();
        m_CardManager.Init();

        PlayBgm();
    }

    private void PlayBgm()
    {
        SoundTable soundTable = TableManager.instance.GetTable<SoundTable>();
        SoundRecord record = soundTable?.GetRecordByKey("BattleTheme");
        if (record == null)
        {
            Logger.Error($"[InGameScene] PlayBgm Failed! SoundRecord not found - BattleTheme");
            return;
        }

        AudioClip clip = ResUtil.Load<AudioClip>(record.ClipPath);
        if (clip == null)
            return;

        SoundManager.instance.PlayBgm(clip);
    }

    // 타워 사망 또는 난이도 클리어(Infinite 제외) 둘 다 "런 종료"로 취급 — 동일하게 정지 + 결과 팝업.
    private void OnRunEnd()
    {
        m_isGameOver = true;
        ApplyFreezeState();

        UIManager.instance.Get<UIRunOver>();
    }
}
