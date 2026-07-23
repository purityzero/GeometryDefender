using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class MonsterManager : UpdatableBehaviour
{
    [SerializeField] private Transform m_PoolParent;

    public ObservableVariable<int> killCount { get; } = new ObservableVariable<int>(0);
    public ObservableVariable<int> bossKillCount { get; } = new ObservableVariable<int>(0);

    private EntityManager m_EntityManager;
    private EntityQuery m_DeadQuery;
    private EntityQuery m_ReachedEndQuery;

    private MemoryPoolFactory<ActorMonster, eEnemyShape> m_MonsterFactory;

    public event Action<RewardData> OnMonsterDie;
    public event Action<RewardData> OnMonsterReachEnd;

    // 플레이 중 스크립트 핫 리로드 시 private bool도 보존되지만 EntityQuery는 default로 리셋됨
    // — 플래그만 살아남아 무효 쿼리에 접근하는 NRE를 막기 위해 보존 대상에서 제외
    [NonSerialized] private bool m_isInitialized;

    public void Init()
    {
        m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EnemyTable enemyTable = TableManager.instance.GetTable<EnemyTable>();
        if (enemyTable == null)
        {
            Logger.Error($"[MonsterManager] Init Failed! EnemyTable not loaded - TableManager.init() 선행 필요");
            return;
        }

        Dictionary<eEnemyShape, string> pathMap = new Dictionary<eEnemyShape, string>();
        foreach (KeyValuePair<eEnemyShape, List<EnemyRecord>> pair in enemyTable.dicShape)
        {
            pathMap[pair.Key] = pair.Value[0].PrefabPath;
        }

        m_MonsterFactory = new MemoryPoolFactory<ActorMonster, eEnemyShape>(pathMap, 10, m_PoolParent);
        m_MonsterFactory.Prewarm();

        m_DeadQuery = m_EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<DeadTag>(),
            ComponentType.ReadOnly<MonsterTag>(),
            ComponentType.ReadOnly<RewardData>());

        m_ReachedEndQuery = m_EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<ReachedEndTag>(),
            ComponentType.ReadOnly<MonsterTag>(),
            ComponentType.ReadOnly<RewardData>());

        m_isInitialized = true;
    }

    public Entity Spawn(EnemyRecord _record)
    {
        Entity entity = m_EntityManager.CreateEntity();
        Vector2 wayPoint = WayPoint.instance.GetRandomWayPoint();

        // Assets/Design/08_balance.html "적 스탯 시간 보정": 스폰 시점 경과 시간이 길수록 HP/DamageToBase가 커짐, 난이도 배율은 추가로 곱해짐
        float elapsedMinutes = (InGameScene.Current.timerManager != null) ? InGameScene.Current.timerManager.elapsedTime / 60f : 0f;
        float difficultyMultiplier = (InGameScene.Current.difficultyManager != null) ? InGameScene.Current.difficultyManager.GetDifficultyMultiplier() : 1f;
        float hpMultiplier = (1f + elapsedMinutes * GameConfigTable.HP_MULTIPLIER_GROWTH) * difficultyMultiplier;
        float damageMultiplier = (1f + elapsedMinutes * GameConfigTable.DAMAGE_MULTIPLIER_GROWTH) * difficultyMultiplier;

        int scaledMaxHp = Mathf.RoundToInt(_record.MaxHp * hpMultiplier);
        int scaledDamageToBase = Mathf.RoundToInt(_record.DamageToBase * damageMultiplier);

        m_EntityManager.AddComponentData(entity, new HealthData
        {
            MaxHp = scaledMaxHp,
            CurrentHp = scaledMaxHp,
        });
        m_EntityManager.AddComponentData(entity, new MoveData
        {
            MoveSpeed = _record.MoveSpeed,
            CurrentWaypointIndex = 0,
        });
        m_EntityManager.AddComponentData(entity, new RewardData
        {
            GoldReward = _record.GoldReward,
            DamageToBase = scaledDamageToBase,
            IsBoss = (_record.Variant == eEnemyVariant.Boss),
            XpReward = _record.XpReward,
        });
        m_EntityManager.AddComponentData(entity, new EnemySpeciesData
        {
            Species = _record.Species,
        });
        m_EntityManager.AddComponentData(entity, new MonsterTag());
        m_EntityManager.AddComponentData(entity, new CombatRadius
        {
            // VisualSize(스프라이트 스케일 배율)의 절반을 대략적인 충돌 반경으로 사용 — 투사체 충돌 판정용 근사치
            Value = _record.VisualSize * 0.5f,
        });
        m_EntityManager.AddComponentData(entity, LocalTransform.FromPosition(new float3(
            wayPoint.x,
            wayPoint.y,
            0f)));



        DynamicBuffer<WaypointElement> waypointBuffer = m_EntityManager.AddBuffer<WaypointElement>(entity);
        waypointBuffer.Add(new WaypointElement { Position = Vector2.zero });

        m_EntityManager.AddBuffer<DamageRequest>(entity);

        SpawnVisual(entity, _record);

        return entity;
    }

    public int GetAliveMonsterCount()
    {
        if (m_isInitialized == false)
            return 0;

        EntityQuery aliveQuery = m_EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<MonsterTag>(),
            ComponentType.Exclude<DeadTag>(),
            ComponentType.Exclude<ReachedEndTag>());

        int aliveCount = aliveQuery.CalculateEntityCount();
        aliveQuery.Dispose();

        return aliveCount;
    }

    // Shield Burst(#404) 카드 — 즉시(1프레임 내) 반경 내 전체 데미지. ECS 시스템이 아니라 MonoBehaviour에서 호출되는 일회성 트리거라 EntityQuery를 그때그때 생성/해제.
    public void DamageEntitiesInRadius(Vector2 _center, float _radius, int _damage)
    {
        if (m_isInitialized == false)
            return;

        float3 center = new float3(_center.x, _center.y, 0f);

        EntityQuery aliveQuery = m_EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<MonsterTag>(),
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.Exclude<DeadTag>(),
            ComponentType.Exclude<ReachedEndTag>());

        NativeArray<Entity> entities = aliveQuery.ToEntityArray(Allocator.Temp);
        NativeArray<LocalTransform> transforms = aliveQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        for (int i = 0; i < entities.Length; ++i)
        {
            if (math.distancesq(center, transforms[i].Position) > _radius * _radius)
                continue;

            m_EntityManager.GetBuffer<DamageRequest>(entities[i]).Add(new DamageRequest { Amount = _damage });
        }

        entities.Dispose();
        transforms.Dispose();
        aliveQuery.Dispose();
    }

    public void TakeDamage(Entity _entity, int _amount)
    {
        if (m_EntityManager.Exists(_entity) == false)
            return;

        if (m_EntityManager.HasComponent<DeadTag>(_entity) == true)
            return;

        if (m_EntityManager.HasComponent<ReachedEndTag>(_entity) == true)
            return;

        m_EntityManager.GetBuffer<DamageRequest>(_entity).Add(new DamageRequest { Amount = _amount });
    }

    public override void UpdateLogic()
    {
        if (m_isInitialized == false)
            return;

        ProcessDeadMonsters();
        ProcessReachedEndMonsters();
        m_MonsterFactory.UpdateLogic();
    }

    private void ProcessDeadMonsters()
    {
        if (m_DeadQuery.IsEmpty == true)
            return;

        NativeArray<Entity> deadEntities = m_DeadQuery.ToEntityArray(Allocator.Temp);
        NativeArray<RewardData> rewards = m_DeadQuery.ToComponentDataArray<RewardData>(Allocator.Temp);

        for (int i = 0; i < deadEntities.Length; ++i)
        {
            RecycleVisual(deadEntities[i]);
            killCount.Value++;
            if (rewards[i].IsBoss == true)
                bossKillCount.Value++;
            OnMonsterDie?.Invoke(rewards[i]);
            m_EntityManager.DestroyEntity(deadEntities[i]);
        }

        deadEntities.Dispose();
        rewards.Dispose();
    }

    private void ProcessReachedEndMonsters()
    {
        if (m_ReachedEndQuery.IsEmpty == true)
            return;

        NativeArray<Entity> reachedEntities = m_ReachedEndQuery.ToEntityArray(Allocator.Temp);
        NativeArray<RewardData> rewards = m_ReachedEndQuery.ToComponentDataArray<RewardData>(Allocator.Temp);

        for (int i = 0; i < reachedEntities.Length; ++i)
        {
            RecycleVisual(reachedEntities[i]);
            OnMonsterReachEnd?.Invoke(rewards[i]);
            m_EntityManager.DestroyEntity(reachedEntities[i]);
        }

        reachedEntities.Dispose();
        rewards.Dispose();
    }

    private void SpawnVisual(Entity _entity, EnemyRecord _record)
    {
        ActorMonster actorMonster = m_MonsterFactory.Create(_record.Shape);
        if (actorMonster == null)
            return;

        if (ColorUtility.TryParseHtmlString(_record.ColorHex, out Color color) == true)
            actorMonster.SetColor(color);

        actorMonster.transform.localScale = Vector3.one * _record.VisualSize;

        m_EntityManager.AddComponentObject(_entity, new VisualObject
        {
            transform = actorMonster.transform,
        });
    }

    // 타입(Shape)은 MemoryPoolFactory가 Create() 시점에 스스로 기억하므로, 여기선 VisualObject(entity에 이미 붙어있는 참조)에서
    // ActorMonster만 꺼내면 된다 — 별도의 Entity→(Shape,오브젝트) 역참조 테이블이 필요 없다.
    private void RecycleVisual(Entity _entity)
    {
        if (m_EntityManager.HasComponent<VisualObject>(_entity) == false)
            return;

        VisualObject visualObject = m_EntityManager.GetComponentObject<VisualObject>(_entity);
        if (visualObject.transform == null)
            return;

        ActorMonster actorMonster = visualObject.transform.GetComponent<ActorMonster>();
        m_MonsterFactory.Recycle(actorMonster);
    }

    private void OnDestroy()
    {
        if (m_isInitialized == false)
            return;

        // Play Mode 종료 시 World가 MonsterManager보다 먼저 정리되는 타이밍이 있음
        // — 이미 무효화된 EntityQuery를 Dispose하면 NRE가 나므로 World 생존 여부를 먼저 확인
        if (World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.IsCreated == true)
        {
            // ECS World는 씬 언로드와 별개라 여기서 정리하지 않으면, 아직 죽지도 도달하지도 않은 몬스터 엔티티가
            // 다음 플레이 세션까지 그대로 남아 이미 파괴된 시각 오브젝트를 참조하게 된다(2026-07-23 확인)
            EntityQuery allMonsterQuery = m_EntityManager.CreateEntityQuery(ComponentType.ReadOnly<MonsterTag>());
            m_EntityManager.DestroyEntity(allMonsterQuery);
            allMonsterQuery.Dispose();

            m_DeadQuery.Dispose();
            m_ReachedEndQuery.Dispose();
        }

        m_MonsterFactory.Clear();
    }
}
