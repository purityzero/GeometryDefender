using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] private Transform m_PoolParent;

    private EntityManager m_EntityManager;
    private EntityQuery m_DeadQuery;
    private EntityQuery m_ReachedEndQuery;

    private MemoryPoolFactory<ActorMonster, eEnemyShape> m_MonsterFactory;

    // Entity → (Shape, ActorMonster) 역참조용 — Recycle 시 Shape이 필요
    private Dictionary<Entity, (eEnemyShape shape, ActorMonster actor)> m_VisualMap = new Dictionary<Entity, (eEnemyShape, ActorMonster)>();

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
            Debug.LogError($"[MonsterManager] Init Failed! EnemyTable not loaded - TableManager.init() 선행 필요");
            return;
        }

        Dictionary<eEnemyShape, string> pathMap = new Dictionary<eEnemyShape, string>();
        foreach (KeyValuePair<eEnemyShape, List<EnemyRecord>> pair in enemyTable.shapeMap)
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

        m_EntityManager.AddComponentData(entity, new HealthData
        {
            MaxHp = _record.MaxHp,
            CurrentHp = _record.MaxHp,
        });
        m_EntityManager.AddComponentData(entity, new MoveData
        {
            MoveSpeed = _record.MoveSpeed,
            CurrentWaypointIndex = 0,
        });
        m_EntityManager.AddComponentData(entity, new RewardData
        {
            GoldReward = _record.GoldReward,
            DamageToBase = _record.DamageToBase,
        });
        m_EntityManager.AddComponentData(entity, new MonsterTag());
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

    private void Update()
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

        m_VisualMap[_entity] = (_record.Shape, actorMonster);

        m_EntityManager.AddComponentObject(_entity, new VisualObject
        {
            transform = actorMonster.transform,
        });
    }

    private void RecycleVisual(Entity _entity)
    {
        if (m_VisualMap.TryGetValue(_entity, out var visual) == false)
            return;

        m_MonsterFactory.Recycle(visual.shape, visual.actor);
        m_VisualMap.Remove(_entity);
    }

    private void OnDestroy()
    {
        if (m_isInitialized == false)
            return;

        m_DeadQuery.Dispose();
        m_ReachedEndQuery.Dispose();
        m_MonsterFactory.Clear();
    }
}
