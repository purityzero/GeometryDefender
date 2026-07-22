using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// 02_combat.html "중앙 타워" — MonoBehaviour(인스턴스 1개, ECS 이점 없음). 사격/타겟팅/치명타/데미지 계산 담당.
public class TowerController : MonoBehaviour, IUpdatable
{
    private const int TOWER_RECORD_ID = 3;

    private TowerRecord m_Record;
    private ITargetingStrategy m_TargetingStrategy;

    private EntityManager m_EntityManager;
    private EntityQuery m_AliveMonsterQuery;

    private float m_CooldownTimer;

    // 05_meta.html "STARTING POWER" 줄기(DamagePercent/RangePercent) 해금분을 Init()에서 1회 계산해 고정
    private float m_DamageMultiplier;
    private float m_EffectiveRange;

    // 핫 리로드 시 EntityQuery는 default로 리셋되지만 이 bool은 값이 보존됨 — MonsterManager/SpawnManager와 동일 이유로 보존 대상에서 제외
    [System.NonSerialized] private bool m_isInitialized;

    public void Init()
    {
        TowerTable towerTable = TableManager.instance.GetTable<TowerTable>();
        if (towerTable == null)
        {
            Logger.Error($"[TowerController] Init Failed! TowerTable not loaded - TableManager.init() 선행 필요");
            return;
        }

        m_Record = towerTable.GetRecordById(TOWER_RECORD_ID);
        if (m_Record == null)
        {
            Logger.Error($"[TowerController] Init Failed! TowerRecord(Id={TOWER_RECORD_ID}) not found");
            return;
        }

        SetTargetingStrategy(m_Record.DefaultTargeting);

        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        int damagePercent = (metaTreeTable != null) ? metaTreeTable.GetTotalEffectValue(eMetaEffectType.DamagePercent, PlayerManager.instance.playerData.UnlockedMetaNodes) : 0;
        int rangePercent = (metaTreeTable != null) ? metaTreeTable.GetTotalEffectValue(eMetaEffectType.RangePercent, PlayerManager.instance.playerData.UnlockedMetaNodes) : 0;

        m_DamageMultiplier = 1f + (damagePercent / 100f);
        m_EffectiveRange = m_Record.Range * (1f + (rangePercent / 100f));

        m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        m_AliveMonsterQuery = m_EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<MonsterTag>(),
            ComponentType.Exclude<DeadTag>(),
            ComponentType.Exclude<ReachedEndTag>());

        m_CooldownTimer = 0f;
        m_isInitialized = true;
    }

    // 향후 카드 시스템이 런타임에 전략을 갈아끼울 확장 지점
    public void SetTargetingStrategy(ITargetingStrategy _strategy)
    {
        m_TargetingStrategy = _strategy;
    }

    public void SetTargetingStrategy(eTargetingType _type)
    {
        switch (_type)
        {
            case eTargetingType.Strongest:
                SetTargetingStrategy(new StrongestTargetingStrategy());
                break;

            case eTargetingType.Weakest:
                SetTargetingStrategy(new WeakestTargetingStrategy());
                break;

            case eTargetingType.Fastest:
                SetTargetingStrategy(new FastestTargetingStrategy());
                break;

            case eTargetingType.Random:
                SetTargetingStrategy(new RandomTargetingStrategy());
                break;

            default:
                SetTargetingStrategy(new ClosestTargetingStrategy());
                break;
        }
    }

    private void Start()
    {
        BaseScene.Current.Register(this);
    }

    public void UpdateLogic()
    {
        if (m_isInitialized == false)
            return;

        m_CooldownTimer -= Time.deltaTime;
        if (m_CooldownTimer > 0f)
            return;

        float3 towerPosition = new float3(transform.position.x, transform.position.y, 0f);
        Entity target = m_TargetingStrategy.SelectTarget(m_EntityManager, m_AliveMonsterQuery, towerPosition, m_EffectiveRange);

        if (target == Entity.Null)
            return;

        Fire(target);
        m_CooldownTimer = m_Record.AttackInterval;
    }

    private void Fire(Entity _target)
    {
        LocalTransform targetTransform = m_EntityManager.GetComponentData<LocalTransform>(_target);
        Vector2 targetPosition = new Vector2(targetTransform.Position.x, targetTransform.Position.y);

        // 최종 데미지 = (BaseDamage × DamageMul) × CritMul × (1 + ElementBonus) — 02_combat.html "데미지 모델"
        // DamageMul은 Init()에서 계산한 메타 트리 해금분(m_DamageMultiplier) 반영. ElementBonus는 카드 시스템 도입 전까지 중립값 고정 — 카드가 이 값을 갈아끼울 확장 지점
        const float ELEMENT_BONUS = 0f;

        bool isCrit = UnityEngine.Random.value < m_Record.CritChance;
        float critMul = (isCrit == true) ? m_Record.CritMultiplier : 1f;
        float finalDamage = (m_Record.Damage * m_DamageMultiplier) * critMul * (1f + ELEMENT_BONUS);

        ProjectileManager.Current.Fire(
            transform.position,
            targetPosition,
            Mathf.RoundToInt(finalDamage),
            m_Record.ProjectileSpeed,
            m_EffectiveRange,
            m_Record.ProjectileId);
    }

    private void OnDestroy()
    {
        BaseScene.Current?.Unregister(this);

        if (m_isInitialized == false)
            return;

        if (World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.IsCreated == true)
            m_AliveMonsterQuery.Dispose();
    }
}
