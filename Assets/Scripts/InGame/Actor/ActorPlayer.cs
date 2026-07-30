using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Collections;
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
    private const int ARCHER_RECORD_ID = 1;
    private const int MAGE_RECORD_ID = 2;
    private const int TOWER_RECORD_ID = 3;
    private const int CHAIN_COIL_RECORD_ID = 4;
    private const int HOMING_POD_RECORD_ID = 5;
    private const int LASER_RECORD_ID = 6;
    // 2026-07-30 신규 무기 — 메타 트리로만 해금(카드 드래프트 대상 아님, CardManager에 대응 카드 없음).
    private const int ORBITAL_SLOW_RECORD_ID = 7;
    private const int MORTAR_RECORD_ID = 8;

    // 2026-07-27 무기 다양화 — 무기마다 독립 쿨다운/타겟팅을 갖는 슬롯. m_WeaponList[0]이 항상 기본 무기(CentralTower, m_Record와 동일 레코드)이고
    // 카드로 해금되는 추가 무기(AddWeapon)는 뒤에 이어붙는다. 데미지%/공속% 등 전역 카드 효과는 전 무기 공통 적용, 기본 무기의 타겟팅 카드(SetTargetingStrategy)는 m_WeaponList[0]만 갈아끼운다.
    // 2026-07-29 — 치명타(%/배율)는 더 이상 전역이 아니라 CentralTower 전용(원래 크리 스탯을 가진 유일한 무기, Fire() 참고). Archer/HomingPod도 각자 전용 강화 필드(m_ArcherAttackSpeedBonus/m_HomingTurnRateBonus)를 갖는다.
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

        // Orbital Slow(#7) 전용 상태 — 발사/타겟팅 없이 매 프레임 타워 주위를 공전하며 범위 슬로우만 갱신(UpdateOrbitalSlowWeapon 참고).
        public float OrbitalAngle;
        public ActorProjectile OrbitalSlowVisual;
        // 2026-07-30 — 사용자 요청("데미지 약하게 천천히 들어가게, 대신 더 느리게")으로 추가된 약한 틱 데미지 타이머.
        public float OrbitalDamageTickTimer;

        // 2026-07-30 — 무기별 개별 공격력 메타 트리(WeaponDamagePercent) 해금분. Init()/AddWeapon() 시점에 1회 계산해 고정.
        public float MetaDamageMultiplier = 1f;
    }

    private TowerRecord m_Record;
    private List<TowerWeapon> m_WeaponList = new List<TowerWeapon>();

    private EntityManager m_EntityManager;
    private EntityQuery m_AliveMonsterQuery;

    // 2026-07-30 — 여러 무기(또는 한 무기의 다중 발사체)가 항상 같은 몬스터만 쏘던 문제 대응. 매 프레임 UpdateFire()
    // 시작 시 비우고, 발사가 확정될 때마다 그 대상을 추가 — 다음 SelectTarget 호출들이 이미 찍힌 대상을 피하게 한다.
    private HashSet<Entity> m_ClaimedTargetsThisFrame = new HashSet<Entity>();
    private List<Entity> m_MultiShotTargets = new List<Entity>();

    // 05_meta.html "STARTING POWER" 줄기(DamagePercent/RangePercent/AttackSpeedPercent) 해금분 — Init()에서 1회 계산해 고정
    private float m_MetaDamageMultiplier = 1f;
    private float m_MetaRangeMultiplier = 1f;
    private float m_MetaAttackSpeedMultiplier = 1f;

    // 2026-07-30 — 메타 트리(M-405, WeaponSlotCount)로 확장 가능한 무기 슬롯 최대치. GameConfigTable.MAX_WEAPON_COUNT(기본)
    // + 해금된 WeaponSlotCount 합산, Init()에서 1회 계산해 고정(다른 메타 배율들과 동일 패턴).
    public int maxWeaponSlots { get; private set; }

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
    private bool m_hasLaserDurationBonus;
    private float m_LaserDurationBonus;
    private float m_HomingTurnRateBonus;
    private eEnemySpecies? m_BonusSpeciesTarget;
    private float m_BonusSpeciesDamageFlat;
    private float m_BerserkerMaxBonusPercent;

    // 04_card.html 무기 전용 강화 카드(2026-07-29) — Archer는 자기 무기에만 붙는 추가 공속(GetWeaponAttackSpeedMultiplier에서 적용)
    private float m_ArcherAttackSpeedBonus;

    // 04_card.html 몬스터 변종(Normal/Elite/Boss)별 추가 데미지 — 3장 모두 독립적으로 누적(단일 슬롯 덮어쓰기가 아님, SpeciesBonusDamage와 다른 방식)
    // 2026-07-30 — 사용자 요청("트라이앵글 퍼센트 데미지도 좀 다 수치로 가야해... 보스 엘리트 이런거 다 수치로")로 %에서 고정 데미지로 전환.
    private float m_EliteDamageBonusFlat;
    private float m_BossDamageBonusFlat;
    private float m_NormalVariantDamageBonusFlat;

    private float m_DamageMultiplier = 1f;
    private float m_AttackSpeedMultiplier = 1f;

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

        // 2026-07-30 — 사용자 보고("카드 선택 시 더블샷 찍은것처럼 됨") 조사 중 발견: Init()이 무기 목록/메타 배율만
        // 리셋하고 카드로 누적되는 런 스코프 상태(발사체 수, 관통/스플래시/체인 등)는 전혀 초기화하지 않고 있었다.
        // ActorPlayer 인스턴스가 씬 재로드 없이 재사용되는 경로가 있으면 이전 런의 카드 효과가 그대로 남아
        // 새 런 시작부터 이미 적용된 것처럼 보인다(CLAUDE.md "초기화 로직 중복 호출" 버그 유형과 동일 패턴).
        m_CardDamagePercent = 0f;
        m_CardRangePercent = 0f;
        m_CardAttackSpeedPercent = 0f;
        m_CardProjectileSpeedPercent = 0f;
        m_CardCritChance = 0f;
        m_CardCritMultiplier = 0f;
        m_ProjectileCount = 1;
        m_PierceStacks = 0;
        m_hasSplash = false;
        m_SplashRadius = 0f;
        m_hasChain = false;
        m_ChainJumps = 0;
        m_ChainRadius = 0f;
        m_hasLaserDurationBonus = false;
        m_LaserDurationBonus = 0f;
        m_HomingTurnRateBonus = 0f;
        m_BonusSpeciesTarget = null;
        m_BonusSpeciesDamageFlat = 0f;
        m_BerserkerMaxBonusPercent = 0f;
        m_ArcherAttackSpeedBonus = 0f;
        m_EliteDamageBonusFlat = 0f;
        m_BossDamageBonusFlat = 0f;
        m_NormalVariantDamageBonusFlat = 0f;
        m_DamageTakenReductionPercent = 0f;
        m_HealPerSecond = 0f;
        m_HealAccumulator = 0f;
        m_ShieldBurstThresholdPercent = 0f;
        m_isShieldBurstArmed = true;
        m_hasRevive = false;
        m_ReviveHpPercent = 0f;

        m_WeaponList.Clear();
        m_WeaponList.Add(new TowerWeapon
        {
            Record = m_Record,
            TargetingStrategy = CreateTargetingStrategy(m_Record.DefaultTargeting),
            CooldownTimer = 0f,
            MetaDamageMultiplier = GetWeaponMetaDamageMultiplier(TOWER_RECORD_ID),
        });

        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        int damagePercent = (metaTreeTable != null) ? metaTreeTable.GetTotalEffectValue(eMetaEffectType.DamagePercent, PlayerManager.instance.playerData.UnlockedMetaNodes) : 0;
        int rangePercent = (metaTreeTable != null) ? metaTreeTable.GetTotalEffectValue(eMetaEffectType.RangePercent, PlayerManager.instance.playerData.UnlockedMetaNodes) : 0;
        int attackSpeedPercent = (metaTreeTable != null) ? metaTreeTable.GetTotalEffectValue(eMetaEffectType.AttackSpeedPercent, PlayerManager.instance.playerData.UnlockedMetaNodes) : 0;
        int weaponSlotBonus = (metaTreeTable != null) ? metaTreeTable.GetTotalEffectValue(eMetaEffectType.WeaponSlotCount, PlayerManager.instance.playerData.UnlockedMetaNodes) : 0;

        m_MetaDamageMultiplier = 1f + (damagePercent / 100f);
        m_MetaRangeMultiplier = 1f + (rangePercent / 100f);
        m_MetaAttackSpeedMultiplier = 1f + (attackSpeedPercent / 100f);
        maxWeaponSlots = GameConfigTable.MAX_WEAPON_COUNT + weaponSlotBonus;

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

    // 2026-07-30 — 무기별 개별 공격력 메타 트리(WeaponDamagePercent, EffectParam=TowerRecord.Id 문자열) 해금분.
    private float GetWeaponMetaDamageMultiplier(int _towerRecordId)
    {
        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        if (metaTreeTable == null)
            return 1f;

        int percent = metaTreeTable.GetTotalEffectValueForParam(eMetaEffectType.WeaponDamagePercent, _towerRecordId.ToString(), PlayerManager.instance.playerData.UnlockedMetaNodes);
        return 1f + (percent / 100f);
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

            case eTargetingType.Farthest:
                return new FarthestTargetingStrategy();

            default:
                return new ClosestTargetingStrategy();
        }
    }

    // 04_card.html 무기 해금 카드(WeaponUnlock) — _towerRecordId(TowerTable)로 정의된 무기를 독립 쿨다운/타겟팅 슬롯으로 추가
    public void AddWeapon(int _towerRecordId)
    {
        // 2026-07-30 — 사용자 요청("무기는 한꺼번에 4개만 갖을 수 있도록" + "무기 장착슬롯 추가도 메타트리에 넣으면 좋을듯").
        // CardManager가 weaponCount로 드래프트 풀을 미리 걸러주는 게 1차 방어선이지만, 치트 창 등 드래프트를 거치지
        // 않는 경로도 있어 여기서 최종적으로 막는다. maxWeaponSlots는 기본치+메타 트리(M-405) 해금분 합산(Init() 참고).
        if (m_WeaponList.Count >= maxWeaponSlots)
        {
            Logger.Log($"[ActorPlayer] AddWeapon Skipped - 이미 최대 무기 수({maxWeaponSlots}) 보유 중");
            return;
        }

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
            MetaDamageMultiplier = GetWeaponMetaDamageMultiplier(_towerRecordId),
        };

        // 무기 Id별로 필요한 시각 오브젝트 타입이 다르다(Laser=LaserBeamVisual, Orbital Slow=ActorProjectile 재사용) —
        // PrefabPath 유무만으로 분기하면 안 되고(예전엔 Laser 전용이라 그걸로 충분했음), Id로 명확히 나눠야 한다.
        if (weaponRecord.Id == LASER_RECORD_ID)
        {
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
        }
        else if (weaponRecord.Id == ORBITAL_SLOW_RECORD_ID)
        {
            // Prefabs/Projectile/Basic을 그대로 재사용(ActorProjectile은 SpriteRenderer+SetColor만 있으면 되는
            // 단순 원형 시각 오브젝트라 새 프리팹을 만들 필요가 없었음) — ECS 엔티티가 아니라 타워 자식 Transform으로
            // 직접 움직이는 순수 시각 오브젝트(Laser 비주얼과 동일한 방식).
            if (string.IsNullOrEmpty(weaponRecord.PrefabPath) == false)
            {
                newWeapon.OrbitalSlowVisual = ResUtil.Create<ActorProjectile>(weaponRecord.PrefabPath, transform);
                if (newWeapon.OrbitalSlowVisual != null)
                {
                    // 사용자 요청("기본 크기 더 크게") — 다른 투사체와 같은 프리팹을 쓰므로 스케일만 별도로 키움.
                    newWeapon.OrbitalSlowVisual.transform.localScale = Vector3.one * GameConfigTable.ORBITAL_SLOW_VISUAL_SCALE;

                    if (ColorUtility.TryParseHtmlString(weaponRecord.ColorHex, out Color orbColor) == true)
                    {
                        orbColor.a = weaponRecord.Alpha;

                        // 사용자 요청("타워처럼 Glow효과 추가" + "깜빡깜빡 거리게" + "하얀색→지금 색 천천히 트윈" →
                        // "근데 티가 안남" — 색 Tween과 글로우 Tween이 서로 다른 주기(Yoyo 2개, 언싱크)라 흰색 절정과
                        // 글로우 절정이 거의 안 겹쳐서 "흰색 Glow"가 뚜렷하게 안 보였음). 하나의 Sequence로 묶어
                        // 흰색+최대글로우/지정색+최소글로우가 항상 동시에 절정을 찍도록 동기화.
                        Material material = newWeapon.OrbitalSlowVisual.material;
                        Color whiteWithSameAlpha = new Color(1f, 1f, 1f, orbColor.a);

                        newWeapon.OrbitalSlowVisual.SetColor(whiteWithSameAlpha);
                        material.SetFloat("_GlowAmount", GameConfigTable.ORBITAL_SLOW_GLOW_MAX);

                        // 시작 상태(흰색+최대글로우)에서 지정색+최소글로우로 갔다가, Yoyo가 자동으로 역재생해서
                        // 다시 흰색+최대글로우로 돌아온다 — 왕복 구간을 직접 두 번 안 써도 됨.
                        float halfDuration = GameConfigTable.ORBITAL_SLOW_COLOR_TWEEN_DURATION * 0.5f;
                        TweenSequenceBuilder.Create()
                            .Append(TweenUtil.Color(material, orbColor.linear, halfDuration))
                            .Join(TweenUtil.Float(material, "_GlowAmount", GameConfigTable.ORBITAL_SLOW_GLOW_MIN, halfDuration))
                            .Loops(-1, LoopType.Yoyo)
                            .Play();
                    }
                }
            }

            newWeapon.OrbitalAngle = UnityEngine.Random.Range(0f, 360f);
            newWeapon.OrbitalDamageTickTimer = 0f;

            // 사용자 요청("사운드랑 로칼라이징 키 이런거 다 셋팅해야하는거 알지?") — 획득 시 1회 활성화음.
            InGameScene.Current.damageTextManager?.PlayWeaponFireSound("OrbitalSlowActivate");
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
        float attackInterval = weapon.Record.AttackInterval / GetWeaponAttackSpeedMultiplier(weapon);
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

    // 사용자 요청("반짝거리는 거 없는애들은 그냥 color값 적용, 냉기오브 같은 반짝거리는 효과 있는애들은 tween") —
    // UIInGameHUD가 무기 쿨다운 게이지 색을 정적으로 칠할지, 인게임 오브젝트와 톤을 맞춰 펄스 Tween을 걸지 판단하는 데 사용.
    public bool GetWeaponHasGlowPulse(int _index)
    {
        if (_index < 0 || _index >= m_WeaponList.Count)
            return false;

        return m_WeaponList[_index].Record.Id == ORBITAL_SLOW_RECORD_ID;
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
    public void AddCardAttackSpeedPercent(float _percent) { m_CardAttackSpeedPercent += _percent; RecalculateDerivedStats(); }
    public void AddCardProjectileSpeedPercent(float _percent) { m_CardProjectileSpeedPercent += _percent; }
    public void AddCardCritChance(float _percent) { m_CardCritChance += _percent / 100f; }
    public void AddCardCritMultiplier(float _value) { m_CardCritMultiplier += _value; }
    public void AddProjectileCount(int _amount) { m_ProjectileCount += _amount; }
    public void AddPierce(int _amount) { m_PierceStacks += _amount; }
    public void SetSplash(float _radius) { m_hasSplash = true; m_SplashRadius = _radius; }
    public void SetChain(int _jumps, float _radius) { m_hasChain = true; m_ChainJumps = _jumps; m_ChainRadius = _radius; }
    public void SetLaserDuration(float _duration) { m_hasLaserDurationBonus = true; m_LaserDurationBonus = _duration; }
    public void AddHomingTurnRate(float _value) { m_HomingTurnRateBonus += _value; }
    public void SetSpeciesBonusDamage(eEnemySpecies _species, float _flatDamage) { m_BonusSpeciesTarget = _species; m_BonusSpeciesDamageFlat = _flatDamage; }
    public void SetBerserker(float _maxBonusPercent) { m_BerserkerMaxBonusPercent = _maxBonusPercent; }

    // Archer 전용 강화 카드 — 전역 AddCardAttackSpeedPercent와 별개로 Archer 무기에만 곱해짐(GetWeaponAttackSpeedMultiplier 참고)
    public void AddArcherAttackSpeedPercent(float _percent) { m_ArcherAttackSpeedBonus += _percent; }

    // 몬스터 변종(Elite/Boss/Normal) 대상 추가 데미지 — 3장이 서로 다른 변종을 노리므로 독립 누적(덮어쓰기 아님)
    public void AddVariantBonusDamage(eEnemyVariant _variant, float _flatDamage)
    {
        switch (_variant)
        {
            case eEnemyVariant.Elite:
                m_EliteDamageBonusFlat += _flatDamage;
                break;

            case eEnemyVariant.Boss:
                m_BossDamageBonusFlat += _flatDamage;
                break;

            default:
                m_NormalVariantDamageBonusFlat += _flatDamage;
                break;
        }
    }

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
        m_AttackSpeedMultiplier = m_MetaAttackSpeedMultiplier * (1f + m_CardAttackSpeedPercent / 100f);
    }

    // 무기마다 기본 사거리(Record.Range)가 다르므로 공통 배율(메타+카드)만 곱해 매 발사 시점에 계산
    private float GetWeaponRange(TowerWeapon _weapon)
    {
        return _weapon.Record.Range * m_MetaRangeMultiplier * (1f + m_CardRangePercent / 100f);
    }

    // 전 무기 공통 배율(m_AttackSpeedMultiplier)에 Archer 전용 강화(m_ArcherAttackSpeedBonus)를 추가로 곱한다
    private float GetWeaponAttackSpeedMultiplier(TowerWeapon _weapon)
    {
        float multiplier = m_AttackSpeedMultiplier;

        if (_weapon.Record.Id == ARCHER_RECORD_ID)
            multiplier *= (1f + m_ArcherAttackSpeedBonus / 100f);

        return multiplier;
    }

    public override void UpdateLogic()
    {
        if (m_isInitialized == false)
            return;

        UpdateFire();
        UpdateRegeneration();
        UpdateCameraZoom();
    }

    // 사용자 요청("게임화면 조금더 넓게 볼 수 있게 카메라 조정기능... 오토 줌아웃기능인거지 디폴트 화면 안에 계속 몹들이
    // 있으면 안늘어나고 몹이 화면 밖의 범위에 숫자가 많다 하면 좀 늘어나는거") — 화면 밖 몬스터 수에 비례해 자동으로
    // 줌아웃. 매 프레임이 아니라 CAMERA_ZOOM_CHECK_INTERVAL마다만 재계산(줌 자체도 1.5초 트윈이라 프레임 단위로
    // 정확할 필요가 없음). "단, 몹 젠 되는 거리가 넘어가면 더이상 안늘어나게" — WayPoint.Radius(스폰 링, 씬 시작
    // 시점 카메라 기준으로 고정된 값)에 대응하는 orthographicSize를 상한으로 클램프해 스폰 지점이 노출되지 않게 한다.
    private float m_CameraZoomCheckTimer;
    private float m_LastCameraTargetOrthoSize = -1f;

    private void UpdateCameraZoom()
    {
        m_CameraZoomCheckTimer -= Time.deltaTime;
        if (m_CameraZoomCheckTimer > 0f)
            return;

        m_CameraZoomCheckTimer = GameConfigTable.CAMERA_ZOOM_CHECK_INTERVAL;

        Camera mainCamera = Camera.main;
        if (mainCamera == null || mainCamera.orthographic == false)
            return;

        int offScreenCount = CountMonstersOutsideView(mainCamera);
        float offScreenRatio = Mathf.Clamp01(offScreenCount / (float)GameConfigTable.CAMERA_ZOOM_FULL_MONSTER_COUNT);
        float targetOrthoSize = GameConfigTable.CAMERA_BASE_ORTHO_SIZE + offScreenRatio * GameConfigTable.CAMERA_MAX_ZOOM_OUT_AMOUNT;

        if (WayPoint.instance != null)
        {
            float maxOrthoSize = WayPoint.instance.Radius / Mathf.Sqrt(mainCamera.aspect * mainCamera.aspect + 1f);
            targetOrthoSize = Mathf.Min(targetOrthoSize, maxOrthoSize);
        }

        if (Mathf.Approximately(targetOrthoSize, m_LastCameraTargetOrthoSize) == true)
            return;

        m_LastCameraTargetOrthoSize = targetOrthoSize;

        // 이 프로젝트 DOTween 설치본엔 Camera 모듈(DOOrthoSize)이 빠져있어(Modules 폴더에 없음), 범용 DOTween.To로 직접 트윈.
        DOTween.Kill(mainCamera);
        DOTween.To(() => mainCamera.orthographicSize, value => mainCamera.orthographicSize = value, targetOrthoSize, GameConfigTable.CAMERA_ZOOM_TWEEN_DURATION)
            .SetTarget(mainCamera);
    }

    private int CountMonstersOutsideView(Camera _camera)
    {
        Vector3 cameraPosition = _camera.transform.position;
        float halfHeight = _camera.orthographicSize;
        float halfWidth = halfHeight * _camera.aspect;

        NativeArray<Entity> entities = m_AliveMonsterQuery.ToEntityArray(Allocator.Temp);
        int outsideCount = 0;

        for (int i = 0; i < entities.Length; ++i)
        {
            LocalTransform localTransform = m_EntityManager.GetComponentData<LocalTransform>(entities[i]);
            float deltaX = Mathf.Abs(localTransform.Position.x - cameraPosition.x);
            float deltaY = Mathf.Abs(localTransform.Position.y - cameraPosition.y);

            if (deltaX > halfWidth || deltaY > halfHeight)
                ++outsideCount;
        }

        entities.Dispose();
        return outsideCount;
    }

    // 무기마다 독립 쿨다운/타겟팅으로 발사 — 카드로 무기가 늘어나면 이 리스트도 함께 늘어남(AddWeapon)
    private void UpdateFire()
    {
        float3 towerPosition = new float3(transform.position.x, transform.position.y, 0f);

        // 이번 프레임에 어느 무기가 어느 대상을 쐈는지 초기화 — 무기별로 순서대로 채워나가며 서로 겹치지 않게 유도.
        m_ClaimedTargetsThisFrame.Clear();

        for (int i = 0; i < m_WeaponList.Count; ++i)
        {
            TowerWeapon weapon = m_WeaponList[i];

            if (weapon.Record.Id == LASER_RECORD_ID)
            {
                UpdateLaserWeapon(weapon);
                continue;
            }

            if (weapon.Record.Id == ORBITAL_SLOW_RECORD_ID)
            {
                UpdateOrbitalSlowWeapon(weapon);
                continue;
            }

            weapon.CooldownTimer -= Time.deltaTime;
            if (weapon.CooldownTimer > 0f)
                continue;

            Entity target = weapon.TargetingStrategy.SelectTarget(m_EntityManager, m_AliveMonsterQuery, towerPosition, GetWeaponRange(weapon), m_ClaimedTargetsThisFrame);

            if (target == Entity.Null)
                continue;

            m_ClaimedTargetsThisFrame.Add(target);

            Fire(weapon, target);

            weapon.CooldownTimer = weapon.Record.AttackInterval / GetWeaponAttackSpeedMultiplier(weapon);
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
            // 2026-07-29 — 매번 0도(고정 방향)에서 시작하지 않고 무작위 각도에서 시작(사용자 피드백
            // "항상 같은곳에서 똑같은 곳만 쏘고 가니까 범위가 너무 일정하다" — 기존엔 항상 같은 0~회전각 구간만
            // 쓸어서 타워 뒤쪽 절반은 영원히 레이저가 안 닿는 사각지대이기도 했음).
            _weapon.LaserRotationAngle = UnityEngine.Random.Range(0f, 360f);
            _weapon.LaserTickTimer = 0f;
            _weapon.LaserVisual?.SetBeamActive(true);

            // 사용자 요청("레이저는 불에 지지는 소리 같은걸로") — 활성화 시작 시 1회 재생(지속시간 3.2초, 기본 활성시간을 커버)
            InGameScene.Current.damageTextManager?.PlayWeaponFireSound("LaserSizzle");
            return;
        }

        _weapon.LaserActiveTimer -= Time.deltaTime;
        _weapon.LaserRotationAngle = (_weapon.LaserRotationAngle + GameConfigTable.LASER_ROTATION_SPEED * Time.deltaTime) % 360f;

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
        _weapon.CooldownTimer = _weapon.Record.AttackInterval / GetWeaponAttackSpeedMultiplier(_weapon);
    }

    // Orbital Slow(#7) — 발사/타겟팅 없이 타워 주위를 천천히 공전하며, 매 프레임 자기 위치 기준 범위 안의 적을
    // 슬로우(MonsterManager.ApplySlowAura가 범위 밖은 자동으로 원래 속도로 되돌림, 이 메서드 쪽에 별도 정리 로직 불필요).
    // Record.Range를 "공전 반지름"으로, Record.SplashRadius를 "슬로우 판정 반경"으로 재사용(둘 다 다른 무기에서 각각의
    // 원래 의미로 쓰이는 컬럼이라 새 컬럼을 늘리지 않고 재사용 — SlowPercent만 이 무기 전용으로 신설).
    private void UpdateOrbitalSlowWeapon(TowerWeapon _weapon)
    {
        _weapon.OrbitalAngle = (_weapon.OrbitalAngle + GameConfigTable.ORBITAL_SLOW_ROTATION_SPEED * Time.deltaTime) % 360f;

        float angleRadian = _weapon.OrbitalAngle * Mathf.Deg2Rad;
        Vector3 towerPosition = transform.position;
        Vector3 orbitOffset = new Vector3(Mathf.Cos(angleRadian), Mathf.Sin(angleRadian), 0f) * _weapon.Record.Range;
        Vector3 orbitPosition = towerPosition + orbitOffset;

        if (_weapon.OrbitalSlowVisual != null)
            _weapon.OrbitalSlowVisual.transform.position = orbitPosition;

        float slowMultiplier = 1f - Mathf.Clamp01(_weapon.Record.SlowPercent / 100f);
        InGameScene.Current.monsterManager.ApplySlowAura(orbitPosition, _weapon.Record.SplashRadius, slowMultiplier);

        // 사용자 요청("데미지 약하게 천천히 들어가게, 대신 더 느리게") — Orbital Ring 카드와 동일한 틱 간격 재사용,
        // 슬로우 판정 반경(SplashRadius) 그대로 데미지 판정에도 사용.
        _weapon.OrbitalDamageTickTimer -= Time.deltaTime;
        if (_weapon.OrbitalDamageTickTimer <= 0f && _weapon.Record.Damage > 0)
        {
            _weapon.OrbitalDamageTickTimer = GameConfigTable.ORBITAL_DAMAGE_TICK_INTERVAL;

            int tickDamage = Mathf.RoundToInt(_weapon.Record.Damage * m_DamageMultiplier * _weapon.MetaDamageMultiplier);
            if (tickDamage > 0)
                InGameScene.Current.monsterManager.DamageEntitiesInRadius(orbitPosition, _weapon.Record.SplashRadius, tickDamage);
        }
    }

    // Double Shot(#107) — 기본 무기(CentralTower)에만 적용(사용자 지적: "더블샷 스킬 같은경우는 기본무기에만 적용되어야해").
    // 추가 무기(Archer/Mage/ChainCoil/HomingPod)는 항상 1발만 발사. 2026-07-30부터 2발 이상일 때 같은 대상에 전부
    // 몰리지 않도록, 첫 발 이후의 각 발은 이번 프레임에 이미 찍힌 대상(m_ClaimedTargetsThisFrame)을 피해 같은
    // 무기의 타겟팅 전략으로 "다음 순위" 대상을 새로 고른다(사용자 요청: "다른 미사일이 잡고있는 타겟을 잡지말라").
    // 대상마다 실제 위치를 직접 조준하므로 기존의 부채꼴 각도 분산(GetSpreadTargetPosition)은 더 이상 필요 없다.
    private void Fire(TowerWeapon _weapon, Entity _primaryTarget)
    {
        int projectileCount = (_weapon.Record.Id == TOWER_RECORD_ID) ? m_ProjectileCount : 1;
        float weaponRange = GetWeaponRange(_weapon);

        m_MultiShotTargets.Clear();
        m_MultiShotTargets.Add(_primaryTarget);

        if (projectileCount > 1)
        {
            float3 towerPosition = new float3(transform.position.x, transform.position.y, 0f);

            for (int i = 1; i < projectileCount; ++i)
            {
                Entity nextTarget = _weapon.TargetingStrategy.SelectTarget(m_EntityManager, m_AliveMonsterQuery, towerPosition, weaponRange, m_ClaimedTargetsThisFrame);
                if (nextTarget == Entity.Null)
                    nextTarget = _primaryTarget;

                m_MultiShotTargets.Add(nextTarget);
                m_ClaimedTargetsThisFrame.Add(nextTarget);
            }
        }

        InGameScene.Current.damageTextManager?.PlayWeaponFireSound(GetWeaponFireSoundKey(_weapon));

        Vector2 firePosition = transform.position;
        float finalProjectileSpeed = _weapon.Record.ProjectileSpeed * (1f + m_CardProjectileSpeedPercent / 100f);

        for (int i = 0; i < m_MultiShotTargets.Count; ++i)
        {
            FireSingleShot(_weapon, m_MultiShotTargets[i], firePosition, finalProjectileSpeed, weaponRange);
        }
    }

    private void FireSingleShot(TowerWeapon _weapon, Entity _target, Vector2 _firePosition, float _projectileSpeed, float _range)
    {
        LocalTransform targetTransform = m_EntityManager.GetComponentData<LocalTransform>(_target);
        Vector2 targetPosition = new Vector2(targetTransform.Position.x, targetTransform.Position.y);

        // 최종 데미지 = (BaseDamage × DamageMul) × CritMul × (1 + ElementBonus) + FlatBonus — 02_combat.html "데미지 모델"
        // DamageMul은 메타 트리 해금분 + 카드 누적분(RecalculateDerivedStats). ElementBonus는 Berserker처럼 배율로 작동하는 카드,
        // FlatBonus는 종/변종 특효 카드(Triangle Hunter, Elite/Boss/Normal)처럼 2026-07-30부터 고정 데미지로 작동하는 카드
        // (사용자 요청: "트라이앵글 퍼센트 데미지도 좀 다 수치로... 보스 엘리트 이런거 다 수치로" — 배율 누적 폭주 방지 겸 체감 단순화).
        float elementBonus = 0f;
        float flatBonusDamage = 0f;

        if (m_BonusSpeciesTarget != null && m_EntityManager.HasComponent<EnemySpeciesData>(_target) == true)
        {
            eEnemySpecies targetSpecies = m_EntityManager.GetComponentData<EnemySpeciesData>(_target).Species;
            if (targetSpecies == m_BonusSpeciesTarget.Value)
                flatBonusDamage += m_BonusSpeciesDamageFlat;
        }

        // 04_card.html 몬스터 변종(Elite/Boss/Normal) 대상 추가 데미지 — 3장 독립 누적
        if (m_EntityManager.HasComponent<EnemyVariantData>(_target) == true)
        {
            eEnemyVariant targetVariant = m_EntityManager.GetComponentData<EnemyVariantData>(_target).Variant;
            switch (targetVariant)
            {
                case eEnemyVariant.Elite:
                    flatBonusDamage += m_EliteDamageBonusFlat;
                    break;

                case eEnemyVariant.Boss:
                    flatBonusDamage += m_BossDamageBonusFlat;
                    break;

                default:
                    flatBonusDamage += m_NormalVariantDamageBonusFlat;
                    break;
            }
        }

        // Berserker(#502) — 타워 HP가 낮을수록 데미지 증가(선형 보간, 최대 보너스는 카드 수치) — 이건 그대로 배율 유지
        if (m_BerserkerMaxBonusPercent > 0f && m_MaxHp > 0)
        {
            float missingHpRatio = 1f - ((float)currentHp.Value / m_MaxHp);
            elementBonus += (m_BerserkerMaxBonusPercent / 100f) * missingHpRatio;
        }

        // 2026-07-29 — 치명타는 이제 전역이 아니라 CentralTower 전용 정체성(원래 크리 스탯을 가진 유일한 무기).
        // m_CardCritChance/m_CardCritMultiplier는 CentralTower 전용 강화 카드 + Offense 시너지 티어7(AddCardCritChance)만 채운다.
        float weaponCardCritChance = (_weapon.Record.Id == TOWER_RECORD_ID) ? m_CardCritChance : 0f;
        float weaponCardCritMultiplier = (_weapon.Record.Id == TOWER_RECORD_ID) ? m_CardCritMultiplier : 0f;

        bool isCrit = UnityEngine.Random.value < (_weapon.Record.CritChance + weaponCardCritChance);
        // 크리티컬 배율 없는 무기(CritMultiplier=0)는 자기 CritChance도 0이라 원래 크리가 안 뜨지만, 위에서 CentralTower가
        // 아니면 weaponCardCritChance가 항상 0이라 이제 이 경로 자체가 CentralTower 한정이다 — 그래도 최소 1배 보장은 유지.
        float critMul = (isCrit == true) ? Mathf.Max(1f, _weapon.Record.CritMultiplier + weaponCardCritMultiplier) : 1f;
        float finalDamage = (_weapon.Record.Damage * m_DamageMultiplier * _weapon.MetaDamageMultiplier) * critMul * (1f + elementBonus) + flatBonusDamage;
        int roundedDamage = Mathf.RoundToInt(finalDamage);

        // Splash/Chain/Homing은 더 이상 전역 적용이 아니라 무기 고유 특성 — ApplyInnateWeaponAbility()가
        // 무기 Id로 분기해서 채운다(사용자 지적: "전부 호밍이 되더라, 각각 미사일에 맞게 해줘").
        // Pierce(#105/#106)도 2026-07-30부터 CentralTower 전용(사용자 요청: "기본무기만 관통이 통해야하고 나머지는 통하면 안됨")
        // — 치명타와 동일하게 CentralTower가 아니면 0으로 무시.
        ProjectileEffects cardEffects = new ProjectileEffects
        {
            Pierce = (_weapon.Record.Id == TOWER_RECORD_ID) ? m_PierceStacks : 0,
            SplashRadius = 0f,
            ChainJumps = 0,
            ChainRadius = 0f,
            IsHoming = false,
            HomingTarget = _target,
        };

        ApplyInnateWeaponAbility(_weapon, ref cardEffects);

        InGameScene.Current.projectileManager.Fire(
            _firePosition,
            targetPosition,
            roundedDamage,
            _projectileSpeed,
            _range,
            _weapon.Record.ProjectileId,
            cardEffects,
            isCrit);
    }

    // 사용자 요청("호밍은 날아가니까 삐슈우웅 2~3초음, 래피드는 두두두두 연속적으로") — 무기 정체성에 맞는 발사음 Key 매핑.
    // 나머지 무기(CentralTower/Mage/ChainCoil)는 DamageTextManager.PlayWeaponFireSound()의 기본값("WeaponFire") 사용.
    private string GetWeaponFireSoundKey(TowerWeapon _weapon)
    {
        switch (_weapon.Record.Id)
        {
            case ARCHER_RECORD_ID:
                return "RapidFire";

            case HOMING_POD_RECORD_ID:
                return "HomingFire";

            case MORTAR_RECORD_ID:
                return "MortarFire";

            default:
                return "WeaponFire";
        }
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

            case MORTAR_RECORD_ID:
                // 카드로 강화되지 않는 자체 스플래시(현재 대응 강화 카드 없음, Mage와 달리 m_hasSplash를 안 봄) — 필요해지면 그때 추가.
                _effects.SplashRadius = _weapon.Record.SplashRadius;
                break;

            case HOMING_POD_RECORD_ID:
                _effects.IsHoming = true;
                _effects.HomingTurnRateBonus = m_HomingTurnRateBonus;
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
