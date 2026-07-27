using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ProjectileManager : UpdatableBehaviour
{
    [SerializeField] private Transform m_PoolParent;

    private EntityManager m_EntityManager;
    private EntityQuery m_ExpiredQuery;

    private MemoryPoolFactory<ActorProjectile, eProjectileType> m_ProjectileFactory;
    private ProjectileTable m_ProjectileTable;

    // 핫 리로드 시 EntityQuery는 default로 리셋되지만 이 bool은 값이 보존됨 — MonsterManager/SpawnManager와 동일 이유로 보존 대상에서 제외
    [System.NonSerialized] private bool m_isInitialized;

    public void Init()
    {
        m_ProjectileTable = TableManager.instance.GetTable<ProjectileTable>();
        if (m_ProjectileTable == null)
        {
            Logger.Error($"[ProjectileManager] Init Failed! ProjectileTable not loaded - TableManager.init() 선행 필요");
            return;
        }

        m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Dictionary<eProjectileType, string> pathMap = new Dictionary<eProjectileType, string>();
        foreach (ProjectileRecord record in m_ProjectileTable.list)
        {
            pathMap[record.Type] = record.PrefabPath;
        }

        m_ProjectileFactory = new MemoryPoolFactory<ActorProjectile, eProjectileType>(pathMap, GameConfigTable.PROJECTILE_POOL_SIZE, m_PoolParent);
        m_ProjectileFactory.Prewarm();

        m_ExpiredQuery = m_EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<ProjectileExpiredTag>(),
            ComponentType.ReadOnly<ProjectileTag>());

        m_isInitialized = true;
    }

    /// <summary>타워가 호출 — _projectileId는 ProjectileTable의 Id(TowerRecord.ProjectileId로 지정). 크기/색/프리팹/관통 등은 전부 테이블에서 읽는다.
    /// _cardEffects: 카드로 해금된 관통/스플래시/체인/호밍 — 기본값(default)이면 전부 무효과.
    /// _isCrit: ActorPlayer가 발사 시점에 미리 판정한 치명타 여부 — 명중 시 데미지 텍스트 스타일에 사용.</summary>
    public void Fire(Vector2 _from, Vector2 _to, int _baseDamage, float _speed, float _range, int _projectileId, ProjectileEffects _cardEffects = default, bool _isCrit = false)
    {
        if (m_isInitialized == false)
            return;

        ProjectileRecord record = m_ProjectileTable.GetRecordById(_projectileId);
        if (record == null)
        {
            Logger.Error($"[ProjectileManager] Fire Failed! ProjectileRecord(Id={_projectileId}) not found");
            return;
        }

        Vector2 direction = (_to - _from).normalized;
        float3 spawnPosition = new float3(_from.x, _from.y, 0f);
        int finalDamage = Mathf.RoundToInt(_baseDamage * record.DamageMultiplier);

        Entity entity = m_EntityManager.CreateEntity();

        m_EntityManager.AddComponentData(entity, new ProjectileTag());
        m_EntityManager.AddComponentData(entity, new ProjectileStats
        {
            Damage = finalDamage,
            Radius = record.Size,
            Pierce = record.Pierce + _cardEffects.Pierce,
            IsCrit = _isCrit,
        });
        m_EntityManager.AddComponentData(entity, new ProjectileMotion
        {
            Direction = new float3(direction.x, direction.y, 0f),
            SpawnPosition = spawnPosition,
            MaxDistance = _range,
            Speed = _speed,
        });
        m_EntityManager.AddComponentData(entity, _cardEffects);
        m_EntityManager.AddComponentData(entity, LocalTransform.FromPosition(spawnPosition));

        SpawnVisual(entity, record);
    }

    /// <summary>Orbital Ring(#503) 카드 — 타워 주위를 계속 회전하는 상시 투사체 _count개를 생성(만료 없음, 카드는 유니크라 중복 호출 안 됨 전제).</summary>
    public void SpawnOrbitals(Vector2 _center, int _count, int _damage, float _radius, float _orbitDistance)
    {
        if (m_isInitialized == false)
            return;

        float3 center = new float3(_center.x, _center.y, 0f);

        for (int i = 0; i < _count; ++i)
        {
            Entity entity = m_EntityManager.CreateEntity();

            m_EntityManager.AddComponentData(entity, new OrbitalTag());
            m_EntityManager.AddComponentData(entity, new ProjectileStats
            {
                Damage = _damage,
                Radius = _radius,
                Pierce = 0,
            });
            m_EntityManager.AddComponentData(entity, new OrbitalData
            {
                Center = center,
                Radius = _orbitDistance,
                AngularSpeed = 2f,
                AngleOffset = (math.PI * 2f / _count) * i,
                DamageCooldownTimer = 0f,
            });
            m_EntityManager.AddComponentData(entity, LocalTransform.FromPosition(center));

            ProjectileRecord record = m_ProjectileTable.GetRecordByType(eProjectileType.Basic);
            if (record != null)
                SpawnVisual(entity, record);
        }
    }

    public override void UpdateLogic()
    {
        if (m_isInitialized == false)
            return;

        ProcessExpiredProjectiles();
    }

    private void ProcessExpiredProjectiles()
    {
        if (m_ExpiredQuery.IsEmpty == true)
            return;

        NativeArray<Entity> expiredEntities = m_ExpiredQuery.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < expiredEntities.Length; ++i)
        {
            RecycleVisual(expiredEntities[i]);
            m_EntityManager.DestroyEntity(expiredEntities[i]);
        }

        expiredEntities.Dispose();
    }

    private void SpawnVisual(Entity _entity, ProjectileRecord _record)
    {
        ActorProjectile actorProjectile = m_ProjectileFactory.Create(_record.Type);
        if (actorProjectile == null)
            return;

        // ResUtil.Attach()가 풀에서 꺼낸 오브젝트의 localScale을 항상 Vector3.one으로 리셋하므로
        // (MonsterManager.SpawnVisual()이 코드로 직접 스케일을 세팅하는 것과 동일한 이유), 스폰 시점에 코드로 직접 세팅.
        // PROJECTILE_PREFAB_NATIVE_DIAMETER: 프리팹 스프라이트(shape_circle)의 스케일 1일 때 지름 — ProjectileRecord.Size(반지름)로부터 시각 스케일을 역산하는 데 사용
        float visualScale = (_record.Size * 2f) / GameConfigTable.PROJECTILE_PREFAB_NATIVE_DIAMETER;
        actorProjectile.transform.localScale = Vector3.one * visualScale;

        if (ColorUtility.TryParseHtmlString(_record.ColorHex, out Color color) == true)
            actorProjectile.SetColor(color);

        m_EntityManager.AddComponentObject(_entity, new VisualObject
        {
            transform = actorProjectile.transform,
        });
    }

    // 타입은 MemoryPoolFactory가 Create() 시점에 스스로 기억하므로, 여기선 VisualObject(entity에 이미 붙어있는 참조)에서
    // ActorProjectile만 꺼내면 된다 — 별도의 Entity→(타입,오브젝트) 역참조 테이블이 필요 없다.
    private void RecycleVisual(Entity _entity)
    {
        if (m_EntityManager.HasComponent<VisualObject>(_entity) == false)
            return;

        VisualObject visualObject = m_EntityManager.GetComponentObject<VisualObject>(_entity);
        ActorProjectile actorProjectile = visualObject.transform.GetComponent<ActorProjectile>();
        m_ProjectileFactory.Recycle(actorProjectile);
    }

    private void OnDestroy()
    {
        if (m_isInitialized == false)
            return;

        // Play Mode 종료 시 World가 ProjectileManager보다 먼저 정리되는 타이밍이 있음(MonsterManager와 동일 이유)
        if (World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.IsCreated == true)
        {
            // ECS World는 씬 언로드와 별개라 여기서 정리하지 않으면, 아직 만료되지 않은 투사체/오비탈 엔티티가
            // 다음 플레이 세션까지 그대로 남아 이미 파괴된 시각 오브젝트를 참조하게 된다(MonsterManager와 동일 이유, 2026-07-23 확인)
            EntityQuery allProjectileQuery = m_EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ProjectileTag>());
            m_EntityManager.DestroyEntity(allProjectileQuery);
            allProjectileQuery.Dispose();

            EntityQuery allOrbitalQuery = m_EntityManager.CreateEntityQuery(ComponentType.ReadOnly<OrbitalTag>());
            m_EntityManager.DestroyEntity(allOrbitalQuery);
            allOrbitalQuery.Dispose();

            m_ExpiredQuery.Dispose();
        }

        m_ProjectileFactory.Clear();
    }
}
