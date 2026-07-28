using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// 02_combat.html "중앙 타워" — MonoBehaviour(인스턴스 1개, ECS 이점 없음). 사격/타겟팅/치명타/데미지 계산 + 체력 관리 담당.
// 2026-07-23 TowerHealth를 이 클래스로 병합 — 둘 다 같은 오브젝트(ActorPlayer)를 다루는 "타워" 하나의 개념이라 분리 실익이 없었음.
// 2026-07-27 클래스명을 실제 오브젝트명(ActorPlayer)에 맞춰 리네임 + Actor 상속으로 전환(ActorMonster/ActorProjectile과 계열 통일).
// InGameScene.Current.towerController로 접근(개별 SceneSingleton 대신 InGameScene이 매니저들을 한데 모아 노출).
public class ActorPlayer : Actor
{
    // TowerTable 특정 행을 가리키는 FK성 참조 — 밸런스 튜닝값이 아니라 데이터 스키마 연결점이라 GameConfigTable
    // 이관 대상에서 제외(TOWER_RECORD_ID와 동일 기준, [[GameConfigRecord]] 2026-07-24-0 참고).
    private const int TOWER_RECORD_ID = 3;
    private const int MAGE_RECORD_ID = 2;
    private const int CHAIN_COIL_RECORD_ID = 4;
    private const int HOMING_POD_RECORD_ID = 5;
    private const int LASER_RECORD_ID = 6;

    // 2026-07-27 무기 다양화 — 무기마다 독립 쿨다운/타겟팅을 갖는 슬롯. m_WeaponList[0]이 항상 기본 무기(CentralTower, m_Record와 동일 레코드)이고
    // 카드로 해금되는 추가 무기(AddWeapon)는 뒤에 이어붙는다. 데미지%/치명타%/공속% 등 전역 카드 효과는 전 무기 공통 적용, 기본 무기의 타겟팅 카드(SetTargetingStrategy)는 m_WeaponList[0]만 갈아끼운다.
    private class TowerWeapon
    {
        public TowerRecord Record;
        public ITargetingStrategy TargetingStrategy;
        public float CooldownTimer;

        // Laser(#6) 전용 상태 — 다른 무기와 달리 "쿨다운→즉시 발사"가 아니라 "쿨다운→일정 시간 회전하며 지속 피해"라
        // 범용 Fire() 흐름을 안 타고 UpdateLaserWeapon()에서 별도로 관리(사용자 요청: "어느정도 돌다가 사라져야해").
        public bool IsLaserActive;
        public float LaserActiveTimer;
        public float LaserRotationAngle;
        public float LaserTickTimer;
        public LaserBeamVisual LaserVisual;
    }

    private TowerRecord m_Record;
    private List<TowerWeapon> m_WeaponList = new List<TowerWeapon>();

    private EntityManager m_EntityManager;
    private EntityQuery m_AliveMonsterQuery;

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
    private bool m_hasLaserDurationBonus;
    private float m_LaserDurationBonus;
    private eEnemySpecies? m_BonusSpeciesTarget;
    private float m_BonusSpeciesDamagePercent;
    private float m_BerserkerMaxBonusPercent;

    private float m_DamageMultiplier = 1f;

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
            Logger.Error($"[ActorPlayer] Init Failed! TowerTable not loaded - TableManager.init() 선행 필요");
            return;
        }

        m_Record = towerTable.GetRecordById(TOWER_RECORD_ID);
        if (m_Record == null)
        {
            Logger.Error($"[ActorPlayer] Init Failed! TowerRecord(Id={TOWER_RECORD_ID}) not found");
            return;
        }

        currentTargetingType = m_Record.DefaultTargeting;

        m_WeaponList.Clear();
        m_WeaponList.Add(new TowerWeapon
        {
            Record = m_Record,
            TargetingStrategy = CreateTargetingStrategy(m_Record.DefaultTargeting),
            CooldownTimer = 0f,
        });

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

        m_BaseMaxHp = _maxHp;
        m_MaxHpPercentBonus = 0f;
        m_MaxHp = _maxHp;
        currentHp.Value = _maxHp;

        m_isInitialized = true;

        Open();
    }

    // 07_ui.html "CURRENT BUILD" — 일시정지 화면에 현재 타겟팅 우선순위를 보여주기 위한 값
    public eTargetingType currentTargetingType { get; private set; }

    // 향후 카드 시스템이 런타임에 전략을 갈아끼울 확장 지점 — 기본 무기(m_WeaponList[0])만 대상으로 함
    public void SetTargetingStrategy(ITargetingStrategy _strategy)
    {
        if (m_WeaponList.Count == 0)
            return;

        m_WeaponList[0].TargetingStrategy = _strategy;
    }

    public void SetTargetingStrategy(eTargetingType _type)
    {
        currentTargetingType = _type;
        SetTargetingStrategy(CreateTargetingStrategy(_type));
    }

    private ITargetingStrategy CreateTargetingStrategy(eTargetingType _type)
    {
        switch (_type)
        {
            case eTargetingType.Strongest:
                return new StrongestTargetingStrategy();

            case eTargetingType.Weakest:
                return new WeakestTargetingStrategy();

            case eTargetingType.Fastest:
                return new FastestTargetingStrategy();

            case eTargetingType.Random:
                return new RandomTargetingStrategy();

            default:
                return new ClosestTargetingStrategy();
        }
    }

    // 04_card.html 무기 해금 카드(WeaponUnlock) — _towerRecordId(TowerTable)로 정의된 무기를 독립 쿨다운/타겟팅 슬롯으로 추가
    public void AddWeapon(int _towerRecordId)
    {
        TowerTable towerTable = TableManager.instance.GetTable<TowerTable>();
        if (towerTable == null)
        {
            Logger.Error($"[ActorPlayer] AddWeapon Failed! TowerTable not loaded");
            return;
        }

        TowerRecord weaponRecord = towerTable.GetRecordById(_towerRecordId);
        if (weaponRecord == null)
        {
            Logger.Error($"[ActorPlayer] AddWeapon Failed! TowerRecord(Id={_towerRecordId}) not found");
            return;
        }

        TowerWeapon newWeapon = new TowerWeapon
        {
            Record = weaponRecord,
            TargetingStrategy = CreateTargetingStrategy(weaponRecord.DefaultTargeting),
            CooldownTimer = 0f,
        };

        if (string.IsNullOrEmpty(weaponRecord.PrefabPath) == false)
        {
            newWeapon.LaserVisual = ResUtil.Create<LaserBeamVisual>(weaponRecord.PrefabPath, transform);
            if (newWeapon.LaserVisual != null)
            {
                if (ColorUtility.TryParseHtmlString(weaponRecord.ColorHex, out Color laserColor) == true)
                {
                    laserColor.a = weaponRecord.Alpha;
                    newWeapon.LaserVisual.SetColor(laserColor);
                }

                newWeapon.LaserVisual.SetBeamActive(false);
            }
        }

        m_WeaponList.Add(newWeapon);
    }

    // 이하 UIInGameHUD 하단 무기 쿨다운 게이지가 매 프레임 폴링(무기 목록은 씬 배치 오브젝트가 아니라 이 클래스 내부
    // private 상태라 UI가 직접 들여다볼 수 없음 — 최소한의 조회용 API만 노출).
    public int weaponCount => m_WeaponList.Count;

    public float GetWeaponCooldownRatio(int _index)
    {
        if (_index < 0 || _index >= m_WeaponList.Count)
            return 0f;

        TowerWeapon weapon = m_WeaponList[_index];
        float attackInterval = weapon.Record.AttackInterval / (1f + m_CardAttackSpeedPercent / 100f);
        if (attackInterval <= 0f)
            return 1f;

        return 1f - Mathf.Clamp01(weapon.CooldownTimer / attackInterval);
    }

    // StringTable 키를 반환 — 호출부(UIInGameHUD)가 로컬라이즈해서 표시한다(원문 그대로 쓰지 않음).
    public string GetWeaponNameKey(int _index)
    {
        if (_index < 0 || _index >= m_WeaponList.Count)
            return string.Empty;

        return m_WeaponList[_index].Record.NameKey;
    }

    public string GetWeaponColorHex(int _index)
    {
        if (_index < 0 || _index >= m_WeaponList.Count)
            return "#FFFFFF";

        return m_WeaponList[_index].Record.ColorHex;
    }

    public float GetWeaponAlpha(int _index)
    {
        if (_index < 0 || _index >= m_WeaponList.Count)
            return 1f;

        return m_WeaponList[_index].Record.Alpha;
    }

    // CardManager가 카드 드래프트 풀 필터링에 사용 — Splash/Chain/Homing 카드(#303/#304/#305)는
    // 해당 무기(Mage/ChainCoil/HomingPod)를 이미 보유했을 때만 드래프트에 나오게 하는 선행조건 체크.
    public bool HasWeapon(int _towerRecordId)
    {
        return m_WeaponList.Exists(weapon => weapon.Record.Id == _towerRecordId);
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
    public void SetLaserDuration(float _duration) { m_hasLaserDurationBonus = true; m_LaserDurationBonus = _duration; }
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
        m_DamageMultiplier = m_MetaDamageMultiplier * (1f + m_CardDamagePercent / 100f);
    }

    // 무기마다 기본 사거리(Record.Range)가 다르므로 공통 배율(메타+카드)만 곱해 매 발사 시점에 계산
    private float GetWeaponRange(TowerWeapon _weapon)
    {
        return _weapon.Record.Range * m_MetaRangeMultiplier * (1f + m_CardRangePercent / 100f);
    }

    public override void UpdateLogic()
    {
        if (m_isInitialized == false)
            return;

        UpdateFire();
        UpdateRegeneration();
    }

    // 무기마다 독립 쿨다운/타겟팅으로 발사 — 카드로 무기가 늘어나면 이 리스트도 함께 늘어남(AddWeapon)
    private void UpdateFire()
    {
        float3 towerPosition = new float3(transform.position.x, transform.position.y, 0f);

        for (int i = 0; i < m_WeaponList.Count; ++i)
        {
            TowerWeapon weapon = m_WeaponList[i];

            if (weapon.Record.Id == LASER_RECORD_ID)
            {
                UpdateLaserWeapon(weapon);
                continue;
            }

            weapon.CooldownTimer -= Time.deltaTime;
            if (weapon.CooldownTimer > 0f)
                continue;

            Entity target = weapon.TargetingStrategy.SelectTarget(m_EntityManager, m_AliveMonsterQuery, towerPosition, GetWeaponRange(weapon));

            if (target == Entity.Null)
                continue;

            Fire(weapon, target);

            weapon.CooldownTimer = weapon.Record.AttackInterval / (1f + m_CardAttackSpeedPercent / 100f);
        }
    }

    // Laser(#6) — 쿨다운이 끝나면 일정 시간(GameConfigTable.LASER_INNATE_ROTATE_DURATION, 카드로 연장)
    // 계속 회전하며 부채꼴 범위 안의 모든 적에게 주기적으로(LASER_TICK_INTERVAL) 피해를 준 뒤 사라지고 다시 쿨다운에 들어간다.
    private void UpdateLaserWeapon(TowerWeapon _weapon)
    {
        if (_weapon.IsLaserActive == false)
        {
            _weapon.CooldownTimer -= Time.deltaTime;
            if (_weapon.CooldownTimer > 0f)
                return;

            float duration = GameConfigTable.LASER_INNATE_ROTATE_DURATION;
            if (m_hasLaserDurationBonus == true)
                duration = Mathf.Max(duration, m_LaserDurationBonus);

            _weapon.IsLaserActive = true;
            _weapon.LaserActiveTimer = duration;
            _weapon.LaserRotationAngle = 0f;
            _weapon.LaserTickTimer = 0f;
            _weapon.LaserVisual?.SetBeamActive(true);
            return;
        }

        _weapon.LaserActiveTimer -= Time.deltaTime;
        _weapon.LaserRotationAngle += GameConfigTable.LASER_ROTATION_SPEED * Time.deltaTime;

        // 사용자 요청("사정거리는 무한이야") — 다른 무기와 달리 Record.Range를 안 쓰고 맵 전체를 항상 커버하는 고정값 사용.
        Vector3 towerPosition = transform.position;
        float laserRange = GameConfigTable.LASER_RANGE;
        _weapon.LaserVisual?.UpdateBeam(towerPosition, _weapon.LaserRotationAngle, laserRange);

        _weapon.LaserTickTimer -= Time.deltaTime;
        if (_weapon.LaserTickTimer <= 0f)
        {
            _weapon.LaserTickTimer = GameConfigTable.LASER_TICK_INTERVAL;

            float laserAngleRadian = _weapon.LaserRotationAngle * Mathf.Deg2Rad;
            Vector2 beamDirection = new Vector2(Mathf.Cos(laserAngleRadian), Mathf.Sin(laserAngleRadian));
            int tickDamage = Mathf.RoundToInt(_weapon.Record.Damage * m_DamageMultiplier);

            InGameScene.Current.monsterManager.DamageEntitiesInArc(towerPosition, beamDirection, laserRange, GameConfigTable.LASER_ARC_HALF_WIDTH_DEGREES, tickDamage);
        }

        if (_weapon.LaserActiveTimer > 0f)
            return;

        _weapon.IsLaserActive = false;
        _weapon.LaserVisual?.SetBeamActive(false);
        _weapon.CooldownTimer = _weapon.Record.AttackInterval / (1f + m_CardAttackSpeedPercent / 100f);
    }

    private void Fire(TowerWeapon _weapon, Entity _target)
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

        bool isCrit = UnityEngine.Random.value < (_weapon.Record.CritChance + m_CardCritChance);
        float critMul = (isCrit == true) ? (_weapon.Record.CritMultiplier + m_CardCritMultiplier) : 1f;
        float finalDamage = (_weapon.Record.Damage * m_DamageMultiplier) * critMul * (1f + elementBonus);
        int roundedDamage = Mathf.RoundToInt(finalDamage);

        float finalProjectileSpeed = _weapon.Record.ProjectileSpeed * (1f + m_CardProjectileSpeedPercent / 100f);

        // Splash/Chain/Homing은 더 이상 전역 적용이 아니라 무기 고유 특성 — ApplyInnateWeaponAbility()가
        // 무기 Id로 분기해서 채운다(사용자 지적: "전부 호밍이 되더라, 각각 미사일에 맞게 해줘").
        ProjectileEffects cardEffects = new ProjectileEffects
        {
            Pierce = m_PierceStacks,
            SplashRadius = 0f,
            ChainJumps = 0,
            ChainRadius = 0f,
            IsHoming = false,
            HomingTarget = _target,
        };

        ApplyInnateWeaponAbility(_weapon, ref cardEffects);

        // Double Shot(#107) — 기본 무기(CentralTower)에만 적용(사용자 지적: "더블샷 스킬 같은경우는 기본무기에만 적용되어야해").
        // 추가 무기(Archer/Mage/ChainCoil/HomingPod)는 항상 1발만 발사. 2발 이상일 때는 같은 궤적에 겹치지 않도록
        // 조준 방향을 중심으로 부채꼴로 벌려서 발사 — 사용자 요청("더블샷 같은애들 부채꼴로 발사").
        int projectileCount = (_weapon.Record.Id == TOWER_RECORD_ID) ? m_ProjectileCount : 1;

        Vector2 firePosition = transform.position;
        for (int i = 0; i < projectileCount; ++i)
        {
            Vector2 spreadTargetPosition = GetSpreadTargetPosition(firePosition, targetPosition, i, projectileCount);

            InGameScene.Current.projectileManager.Fire(
                firePosition,
                spreadTargetPosition,
                roundedDamage,
                finalProjectileSpeed,
                GetWeaponRange(_weapon),
                _weapon.Record.ProjectileId,
                cardEffects,
                isCrit);
        }
    }

    // 발사체 여러 발이 나갈 때 조준 방향을 중심으로 균등하게 벌린 가상의 조준점을 계산 — 실제 타겟 엔티티는 그대로 두고
    // (호밍은 ProjectileEffects.HomingTarget으로 별도 추적되므로 초기 발사 방향만 벌어짐, 유도 중엔 다시 타겟으로 모여든다)
    // 초기 방향 벡터만 회전시킨다.
    private Vector2 GetSpreadTargetPosition(Vector2 _firePosition, Vector2 _targetPosition, int _index, int _count)
    {
        if (_count <= 1)
            return _targetPosition;

        float centeredIndex = _index - (_count - 1) / 2f;
        float angleDegrees = centeredIndex * GameConfigTable.PROJECTILE_SPREAD_ANGLE_STEP;

        Vector2 toTarget = _targetPosition - _firePosition;
        Vector2 rotatedDirection = Quaternion.Euler(0f, 0f, angleDegrees) * toTarget;
        return _firePosition + rotatedDirection;
    }

    // 테마 무기(Mage=스플래쉬/ChainCoil=체인/HomingPod=유도)만 자기 고유 효과를 갖는다 — 다른 무기(CentralTower/Archer 등)에는
    // 절대 안 붙는다. 대응 카드(#303/#304/#305)는 이제 전역 인챈트가 아니라 "해당 무기를 이미 보유했을 때만" 그 무기의
    // 수치를 더 강하게 만드는 업그레이드로 재정의(Max 비교) — 카드 자체는 CardManager가 무기 미보유 시 드래프트 풀에서 제외한다.
    private void ApplyInnateWeaponAbility(TowerWeapon _weapon, ref ProjectileEffects _effects)
    {
        switch (_weapon.Record.Id)
        {
            case MAGE_RECORD_ID:
                float splashRadius = _weapon.Record.SplashRadius;
                if (m_hasSplash == true)
                    splashRadius = Mathf.Max(splashRadius, m_SplashRadius);
                _effects.SplashRadius = splashRadius;
                break;

            case CHAIN_COIL_RECORD_ID:
                int chainJumps = GameConfigTable.CHAIN_COIL_INNATE_CHAIN_JUMPS;
                float chainRadius = GameConfigTable.CHAIN_COIL_INNATE_CHAIN_RADIUS;
                if (m_hasChain == true)
                {
                    chainJumps = Mathf.Max(chainJumps, m_ChainJumps);
                    chainRadius = Mathf.Max(chainRadius, m_ChainRadius);
                }
                _effects.ChainJumps = chainJumps;
                _effects.ChainRadius = chainRadius;
                break;

            case HOMING_POD_RECORD_ID:
                _effects.IsHoming = true;
                break;
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

                Logger.Log($"[ActorPlayer] Phoenix 발동 - HP {newHp}/{m_MaxHp}로 부활");
                return;
            }

            newHp = 0;
        }

        currentHp.Value = newHp;

        Logger.Log($"[ActorPlayer] TakeDamage - amount:{_amount}, currentHp:{currentHp.Value}/{m_MaxHp}");

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

        Close();

        if (World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.IsCreated == true)
            m_AliveMonsterQuery.Dispose();
    }
}
