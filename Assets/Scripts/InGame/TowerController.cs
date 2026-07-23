using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// 02_combat.html "중앙 타워" — MonoBehaviour(인스턴스 1개, ECS 이점 없음). 사격/타겟팅/치명타/데미지 계산 + 체력 관리 담당.
// 2026-07-23 TowerHealth를 이 클래스로 병합 — 둘 다 같은 오브젝트(ActorPlayer)를 다루는 "타워" 하나의 개념이라 분리 실익이 없었음.
// InGameScene.Current.towerController로 접근(개별 SceneSingleton 대신 InGameScene이 매니저들을 한데 모아 노출).
public class TowerController : UpdatableBehaviour
{
    private const int TOWER_RECORD_ID = 3;

    private TowerRecord m_Record;
    private ITargetingStrategy m_TargetingStrategy;

    private EntityManager m_EntityManager;
    private EntityQuery m_AliveMonsterQuery;

    private float m_CooldownTimer;

    // 05_meta.html "STARTING POWER" 줄기(DamagePercent/RangePercent) 해금분 — Init()에서 1회 계산해 고정
    private float m_MetaDamageMultiplier = 1f;
    private float m_MetaRangeMultiplier = 1f;

    // 04_card.html 카드 효과(공격) — 런 중 CardManager가 계속 누적/설정(사거리/데미지는 매번 재계산, 나머지는 즉시 반영)
    private float m_CardDamagePercent;
    private float m_CardRangePercent;
    private float m_CardAttackSpeedPercent;
    private float m_CardProjectileSpeedPercent;
    private float m_CardCritChance;
    private float m_CardCritMultiplier;
    private int m_ProjectileCount = 1;
    private int m_PierceStacks;
    private bool m_hasSplash;
    private float m_SplashRadius;
    private bool m_hasChain;
    private int m_ChainJumps;
    private float m_ChainRadius;
    private bool m_hasHoming;
    private eEnemySpecies? m_BonusSpeciesTarget;
    private float m_BonusSpeciesDamagePercent;
    private float m_BerserkerMaxBonusPercent;

    private float m_DamageMultiplier = 1f;
    private float m_EffectiveRange;

    // 04_card.html 카드 효과(체력) — TowerHealth 병합분
    public event Action OnDie;

    private int m_BaseMaxHp;
    private float m_MaxHpPercentBonus;
    private int m_MaxHp;

    private float m_DamageTakenReductionPercent;
    private float m_HealPerSecond;
    private float m_HealAccumulator;
    private float m_ShieldBurstThresholdPercent;
    private bool m_isShieldBurstArmed = true;
    private bool m_hasRevive;
    private float m_ReviveHpPercent;

    public int maxHp { get { return m_MaxHp; } }
    public ObservableVariable<int> currentHp { get; } = new ObservableVariable<int>(0);

    // 핫 리로드 시 EntityQuery는 default로 리셋되지만 이 bool은 값이 보존됨 — MonsterManager/SpawnManager와 동일 이유로 보존 대상에서 제외
    [System.NonSerialized] private bool m_isInitialized;

    public void Init(int _maxHp)
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

        m_MetaDamageMultiplier = 1f + (damagePercent / 100f);
        m_MetaRangeMultiplier = 1f + (rangePercent / 100f);

        RecalculateDerivedStats();

        m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        m_AliveMonsterQuery = m_EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<MonsterTag>(),
            ComponentType.Exclude<DeadTag>(),
            ComponentType.Exclude<ReachedEndTag>());

        m_CooldownTimer = 0f;

        m_BaseMaxHp = _maxHp;
        m_MaxHpPercentBonus = 0f;
        m_MaxHp = _maxHp;
        currentHp.Value = _maxHp;

        m_isInitialized = true;
    }

    // 07_ui.html "CURRENT BUILD" — 일시정지 화면에 현재 타겟팅 우선순위를 보여주기 위한 값
    public eTargetingType currentTargetingType { get; private set; }

    // 향후 카드 시스템이 런타임에 전략을 갈아끼울 확장 지점
    public void SetTargetingStrategy(ITargetingStrategy _strategy)
    {
        m_TargetingStrategy = _strategy;
    }

    public void SetTargetingStrategy(eTargetingType _type)
    {
        currentTargetingType = _type;

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

    // 이하 카드 효과 적용 API — CardManager.ApplyCard()가 호출
    public void AddCardDamagePercent(float _percent) { m_CardDamagePercent += _percent; RecalculateDerivedStats(); }
    public void AddCardRangePercent(float _percent) { m_CardRangePercent += _percent; RecalculateDerivedStats(); }
    public void AddCardAttackSpeedPercent(float _percent) { m_CardAttackSpeedPercent += _percent; }
    public void AddCardProjectileSpeedPercent(float _percent) { m_CardProjectileSpeedPercent += _percent; }
    public void AddCardCritChance(float _percent) { m_CardCritChance += _percent / 100f; }
    public void AddCardCritMultiplier(float _value) { m_CardCritMultiplier += _value; }
    public void AddProjectileCount(int _amount) { m_ProjectileCount += _amount; }
    public void AddPierce(int _amount) { m_PierceStacks += _amount; }
    public void SetSplash(float _radius) { m_hasSplash = true; m_SplashRadius = _radius; }
    public void SetChain(int _jumps, float _radius) { m_hasChain = true; m_ChainJumps = _jumps; m_ChainRadius = _radius; }
    public void SetHoming() { m_hasHoming = true; }
    public void SetSpeciesBonusDamage(eEnemySpecies _species, float _percent) { m_BonusSpeciesTarget = _species; m_BonusSpeciesDamagePercent = _percent; }
    public void SetBerserker(float _maxBonusPercent) { m_BerserkerMaxBonusPercent = _maxBonusPercent; }

    // Shield Burst(#404)가 터질 때의 폭발 데미지 — 현재 타워 기본 데미지(배율 반영)를 그대로 사용
    public float GetShieldBurstDamage()
    {
        if (m_Record == null)
            return 0f;

        return m_Record.Damage * m_DamageMultiplier;
    }

    private void RecalculateDerivedStats()
    {
        if (m_Record == null)
            return;

        m_DamageMultiplier = m_MetaDamageMultiplier * (1f + m_CardDamagePercent / 100f);
        m_EffectiveRange = m_Record.Range * m_MetaRangeMultiplier * (1f + m_CardRangePercent / 100f);
    }

    public override void UpdateLogic()
    {
        if (m_isInitialized == false)
            return;

        UpdateFire();
        UpdateRegeneration();
    }

    private void UpdateFire()
    {
        m_CooldownTimer -= Time.deltaTime;
        if (m_CooldownTimer > 0f)
            return;

        float3 towerPosition = new float3(transform.position.x, transform.position.y, 0f);
        Entity target = m_TargetingStrategy.SelectTarget(m_EntityManager, m_AliveMonsterQuery, towerPosition, m_EffectiveRange);

        if (target == Entity.Null)
            return;

        Fire(target);

        float attackInterval = m_Record.AttackInterval / (1f + m_CardAttackSpeedPercent / 100f);
        m_CooldownTimer = attackInterval;
    }

    private void Fire(Entity _target)
    {
        LocalTransform targetTransform = m_EntityManager.GetComponentData<LocalTransform>(_target);
        Vector2 targetPosition = new Vector2(targetTransform.Position.x, targetTransform.Position.y);

        // 최종 데미지 = (BaseDamage × DamageMul) × CritMul × (1 + ElementBonus) — 02_combat.html "데미지 모델"
        // DamageMul은 메타 트리 해금분 + 카드 누적분(RecalculateDerivedStats). ElementBonus는 종 특효 카드(Triangle Hunter 등)가 채우는 확장 지점
        float elementBonus = 0f;
        if (m_BonusSpeciesTarget != null && m_EntityManager.HasComponent<EnemySpeciesData>(_target) == true)
        {
            eEnemySpecies targetSpecies = m_EntityManager.GetComponentData<EnemySpeciesData>(_target).Species;
            if (targetSpecies == m_BonusSpeciesTarget.Value)
                elementBonus += m_BonusSpeciesDamagePercent / 100f;
        }

        // Berserker(#502) — 타워 HP가 낮을수록 데미지 증가(선형 보간, 최대 보너스는 카드 수치)
        if (m_BerserkerMaxBonusPercent > 0f && m_MaxHp > 0)
        {
            float missingHpRatio = 1f - ((float)currentHp.Value / m_MaxHp);
            elementBonus += (m_BerserkerMaxBonusPercent / 100f) * missingHpRatio;
        }

        bool isCrit = UnityEngine.Random.value < (m_Record.CritChance + m_CardCritChance);
        float critMul = (isCrit == true) ? (m_Record.CritMultiplier + m_CardCritMultiplier) : 1f;
        float finalDamage = (m_Record.Damage * m_DamageMultiplier) * critMul * (1f + elementBonus);
        int roundedDamage = Mathf.RoundToInt(finalDamage);

        float finalProjectileSpeed = m_Record.ProjectileSpeed * (1f + m_CardProjectileSpeedPercent / 100f);

        ProjectileEffects cardEffects = new ProjectileEffects
        {
            Pierce = m_PierceStacks,
            SplashRadius = (m_hasSplash == true) ? m_SplashRadius : 0f,
            ChainJumps = (m_hasChain == true) ? m_ChainJumps : 0,
            ChainRadius = (m_hasChain == true) ? m_ChainRadius : 0f,
            IsHoming = m_hasHoming,
            HomingTarget = _target,
        };

        // Double Shot(#107) — m_ProjectileCount만큼 동일 타겟에 동시 발사
        for (int i = 0; i < m_ProjectileCount; ++i)
        {
            InGameScene.Current.projectileManager.Fire(
                transform.position,
                targetPosition,
                roundedDamage,
                finalProjectileSpeed,
                m_EffectiveRange,
                m_Record.ProjectileId,
                cardEffects,
                isCrit);
        }
    }

    public void TakeDamage(int _amount)
    {
        if (currentHp.Value <= 0)
            return;

        int reducedAmount = Mathf.RoundToInt(_amount * (1f - m_DamageTakenReductionPercent / 100f));
        int newHp = currentHp.Value - reducedAmount;

        InGameScene.Current.damageTextManager.ShowAllyDamage(transform.position, reducedAmount);

        if (newHp <= 0)
        {
            // Phoenix(#406) — 사망 대신 1회 부활(카드는 유니크라 재사용 없음, 소비 후 다시 트리거되지 않음)
            if (m_hasRevive == true)
            {
                m_hasRevive = false;
                newHp = Mathf.Max(1, Mathf.RoundToInt(m_MaxHp * (m_ReviveHpPercent / 100f)));
                currentHp.Value = newHp;

                Logger.Log($"[TowerController] Phoenix 발동 - HP {newHp}/{m_MaxHp}로 부활");
                return;
            }

            newHp = 0;
        }

        currentHp.Value = newHp;

        Logger.Log($"[TowerController] TakeDamage - amount:{_amount}, currentHp:{currentHp.Value}/{m_MaxHp}");

        CheckShieldBurst();

        if (currentHp.Value <= 0)
            OnDie?.Invoke();
    }

    public void OnEnemyReachTower(RewardData _reward)
    {
        TakeDamage(_reward.DamageToBase);
    }

    // Regeneration(#403) — 초당 회복량을 누적하다 1 이상이 되면 정수만큼 소모해서 회복
    private void UpdateRegeneration()
    {
        if (m_HealPerSecond <= 0f || currentHp.Value <= 0)
            return;

        m_HealAccumulator += m_HealPerSecond * Time.deltaTime;

        int wholeHeal = Mathf.FloorToInt(m_HealAccumulator);
        if (wholeHeal <= 0)
            return;

        m_HealAccumulator -= wholeHeal;
        Heal(wholeHeal);
    }

    public void Heal(int _amount)
    {
        if (currentHp.Value <= 0)
            return;

        currentHp.Value = Mathf.Min(m_MaxHp, currentHp.Value + _amount);
    }

    // 카드 효과 적용 API — CardManager.ApplyCard()가 호출
    public void AddMaxHp(int _amount)
    {
        m_BaseMaxHp += _amount;
        RecalculateMaxHp(true);
    }

    public void AddMaxHpPercent(float _percent)
    {
        m_MaxHpPercentBonus += _percent;
        RecalculateMaxHp(false);
    }

    public void AddDamageTakenReductionPercent(float _percent)
    {
        m_DamageTakenReductionPercent += _percent;
    }

    public void AddHealPerSecond(float _amount)
    {
        m_HealPerSecond += _amount;
    }

    public void SetShieldBurstThreshold(float _thresholdPercent)
    {
        m_ShieldBurstThresholdPercent = _thresholdPercent;
    }

    public void SetReviveOnce(float _reviveHpPercent)
    {
        m_hasRevive = true;
        m_ReviveHpPercent = _reviveHpPercent;
    }

    private void RecalculateMaxHp(bool _healByDelta)
    {
        int previousMaxHp = m_MaxHp;
        m_MaxHp = Mathf.Max(1, Mathf.RoundToInt(m_BaseMaxHp * (1f + m_MaxHpPercentBonus / 100f)));

        int maxHpDelta = m_MaxHp - previousMaxHp;
        if (_healByDelta == true && maxHpDelta > 0)
            currentHp.Value = Mathf.Min(m_MaxHp, currentHp.Value + maxHpDelta);
        else
            currentHp.Value = Mathf.Min(m_MaxHp, currentHp.Value);
    }

    // HP 30%(기본값 카드 수치) 이하로 처음 내려가는 순간 반경 3 폭발 — HP가 다시 문턱 위로 회복되면 재무장
    private void CheckShieldBurst()
    {
        if (m_ShieldBurstThresholdPercent <= 0f || m_MaxHp <= 0)
            return;

        float hpRatio = (float)currentHp.Value / m_MaxHp;
        float thresholdRatio = m_ShieldBurstThresholdPercent / 100f;

        if (m_isShieldBurstArmed == true && hpRatio <= thresholdRatio)
        {
            m_isShieldBurstArmed = false;

            int burstDamage = Mathf.RoundToInt(GetShieldBurstDamage());

            if (burstDamage > 0)
                InGameScene.Current.monsterManager.DamageEntitiesInRadius(transform.position, GameConfigTable.SHIELD_BURST_RADIUS, burstDamage);
        }
        else if (hpRatio > thresholdRatio)
        {
            m_isShieldBurstArmed = true;
        }
    }

    private void OnDestroy()
    {
        if (m_isInitialized == false)
            return;

        if (World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.IsCreated == true)
            m_AliveMonsterQuery.Dispose();
    }
}
