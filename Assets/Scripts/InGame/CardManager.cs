using System;
using System.Collections.Generic;
using UnityEngine;

// Assets/Design/04_card.html — 카드 풀 관리(가중치 뽑기/Pity/유니크 중복 방지), 리롤/스킵, 카드 효과 적용을 담당.
// UICardDraft(UI)는 이 클래스에 롤/적용을 위임하고 자기 자신은 표시/입력만 다룬다.
public class CardManager : UpdatableBehaviour
{
    // 05_meta.html CARD POOL 줄기 — UnlockCard 노드의 EffectParam ↔ 잠긴 CardRecord.Id 매핑
    private static readonly Dictionary<string, int> LOCKED_CARD_IDS = new Dictionary<string, int>
    {
        { "Pierce1", 105 },
        { "Splash1", 303 },
        { "GlassCannon", 501 },
        { "OrbitalRing", 503 },
    };

    // Splash(#303)/Chain(#304)/Homing(#305)은 이제 무기 전용 강화 카드(ActorPlayer.ApplyInnateWeaponAbility 참고) —
    // 이 런에서 해당 무기(Mage/ChainCoil/HomingPod)를 아직 안 뽑았으면 드래프트 풀에서 제외(사용자 요청: "무기가 없으면 다른 걸 뽑아와야지").
    private static readonly Dictionary<int, int> WEAPON_REQUIRED_CARD_IDS = new Dictionary<int, int>
    {
        { 303, 2 }, // Splash I → Mage
        { 304, 4 }, // Chain Lightning → ChainCoil
        { 305, 5 }, // Homing Missile → HomingPod
        { 308, 6 }, // Laser Overdrive → LaserSpinner
    };

    // Double Shot(#107)은 Legendary라 등급상 유니크 취급되지만, 사용자 요청("더블샷 계속 먹으면 탄수 한번에 늘어나게")대로
    // 반복 드래프트가 가능해야 하는 예외 카드 — 계속 뽑을 때마다 AddProjectileCount(1)이 그대로 누적된다.
    private static readonly HashSet<int> REPEATABLE_CARD_IDS = new HashSet<int> { 107 };

    // 카테고리 시너지(3/5/7장) 중복 부여 방지용 키 — 튜플 대신 struct 사용(CODE.MD "튜플 구조 분해/저장 금지")
    private struct SynergyTierKey
    {
        public eCardCategory Category;
        public int Tier;
    }

    private List<CardRecord> m_AllCards;
    private List<int> m_ObtainedCardIds = new List<int>();
    private HashSet<int> m_ObtainedUniqueIds = new HashSet<int>();
    private Dictionary<eCardCategory, int> m_CategoryCounts = new Dictionary<eCardCategory, int>();
    private HashSet<SynergyTierKey> m_GrantedSynergyTiers = new HashSet<SynergyTierKey>();

    private int m_PitySinceEpic;
    private int m_RerollsUsed;

    private bool m_hasVampire;
    private float m_VampireChancePercent;
    private float m_VampireHealAmount;

    [System.NonSerialized] private bool m_isInitialized;

    public IReadOnlyList<int> obtainedCardIds => m_ObtainedCardIds;
    public int obtainedCardCount => m_ObtainedCardIds.Count;
    public IReadOnlyDictionary<eCardCategory, int> categoryCounts => m_CategoryCounts;

    public void Init()
    {
        CardTable cardTable = TableManager.instance.GetTable<CardTable>();
        if (cardTable == null)
        {
            Logger.Error($"[CardManager] Init Failed! CardTable not loaded - TableManager.init() 선행 필요");
            return;
        }

        m_AllCards = cardTable.list;
        m_PitySinceEpic = 0;
        m_RerollsUsed = 0;
        m_ObtainedCardIds.Clear();
        m_ObtainedUniqueIds.Clear();
        m_CategoryCounts.Clear();
        m_GrantedSynergyTiers.Clear();
        m_hasVampire = false;

        CardEffectState.Reset();

        if (InGameScene.Current.monsterManager == null)
        {
            Logger.Error($"[CardManager] Init Failed! InGameScene.Current.monsterManager is null - MonsterManager.Init() 선행 필요");
            return;
        }

        InGameScene.Current.monsterManager.OnMonsterDie += OnMonsterKilledForVampire;

        m_isInitialized = true;
    }

    public List<CardRecord> RollCards()
    {
        Dictionary<eCardRarity, List<CardRecord>> pool = BuildAvailablePool();
        List<CardRecord> result = new List<CardRecord>();
        HashSet<int> pickedIds = new HashSet<int>();

        bool isPityActive = (m_PitySinceEpic >= GameConfigTable.PITY_THRESHOLD)
            && (HasAnyCard(pool, eCardRarity.Epic) == true || HasAnyCard(pool, eCardRarity.Legendary) == true);

        for (int slot = 0; slot < GameConfigTable.DRAFT_SIZE; ++slot)
        {
            eCardRarity rarity = (isPityActive == true && slot == 0) ? RollPityRarity(pool) : RollRarity(pool);

            CardRecord picked = PickCardExcluding(pool[rarity], pickedIds);
            if (picked == null)
                continue;

            result.Add(picked);
            pickedIds.Add(picked.Id);
        }

        bool hasEpicOrAbove = result.Exists(record => record.Rarity == eCardRarity.Epic || record.Rarity == eCardRarity.Legendary);
        m_PitySinceEpic = (hasEpicOrAbove == true) ? 0 : m_PitySinceEpic + 1;

        return result;
    }

    private eCardRarity RollPityRarity(Dictionary<eCardRarity, List<CardRecord>> _pool)
    {
        if (HasAnyCard(_pool, eCardRarity.Epic) == true)
            return eCardRarity.Epic;

        if (HasAnyCard(_pool, eCardRarity.Legendary) == true)
            return eCardRarity.Legendary;

        return RollRarity(_pool);
    }

    // 등급별 가중치 — 튜플 대신 struct 사용(CODE.MD "튜플 구조 분해/저장 금지")
    private struct RarityWeight
    {
        public eCardRarity Rarity;
        public float Weight;
    }

    private static readonly RarityWeight[] RARITY_WEIGHTS = new[]
    {
        new RarityWeight { Rarity = eCardRarity.Common, Weight = 60f },
        new RarityWeight { Rarity = eCardRarity.Rare, Weight = 25f },
        new RarityWeight { Rarity = eCardRarity.Epic, Weight = 12f },
        new RarityWeight { Rarity = eCardRarity.Legendary, Weight = 3f },
    };

    private eCardRarity RollRarity(Dictionary<eCardRarity, List<CardRecord>> _pool)
    {
        float totalWeight = 0f;
        foreach (var entry in RARITY_WEIGHTS)
        {
            if (HasAnyCard(_pool, entry.Rarity) == true)
                totalWeight += entry.Weight;
        }

        if (totalWeight <= 0f)
            return eCardRarity.Common;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in RARITY_WEIGHTS)
        {
            if (HasAnyCard(_pool, entry.Rarity) == false)
                continue;

            cumulative += entry.Weight;
            if (roll <= cumulative)
                return entry.Rarity;
        }

        return eCardRarity.Common;
    }

    private bool HasAnyCard(Dictionary<eCardRarity, List<CardRecord>> _pool, eCardRarity _rarity)
    {
        return _pool.TryGetValue(_rarity, out List<CardRecord> list) == true && list.Count > 0;
    }

    private CardRecord PickCardExcluding(List<CardRecord> _candidates, HashSet<int> _excludeIds)
    {
        List<CardRecord> filtered = _candidates.FindAll(record => _excludeIds.Contains(record.Id) == false);
        if (filtered.Count == 0)
            filtered = _candidates;

        if (filtered.Count == 0)
            return null;

        return filtered[UnityEngine.Random.Range(0, filtered.Count)];
    }

    private Dictionary<eCardRarity, List<CardRecord>> BuildAvailablePool()
    {
        Dictionary<eCardRarity, List<CardRecord>> pool = new Dictionary<eCardRarity, List<CardRecord>>
        {
            { eCardRarity.Common, new List<CardRecord>() },
            { eCardRarity.Rare, new List<CardRecord>() },
            { eCardRarity.Epic, new List<CardRecord>() },
            { eCardRarity.Legendary, new List<CardRecord>() },
        };

        for (int i = 0; i < m_AllCards.Count; ++i)
        {
            CardRecord record = m_AllCards[i];

            if (IsCardUnlocked(record) == false)
                continue;

            if (HasRequiredWeapon(record) == false)
                continue;

            bool isUnique = IsUniqueCard(record);
            if (isUnique == true && m_ObtainedUniqueIds.Contains(record.Id) == true)
                continue;

            pool[record.Rarity].Add(record);
        }

        return pool;
    }

    private bool IsCardUnlocked(CardRecord _record)
    {
        bool isLockedByDefault = false;
        string requiredParam = null;

        foreach (var pair in LOCKED_CARD_IDS)
        {
            if (pair.Value != _record.Id)
                continue;

            isLockedByDefault = true;
            requiredParam = pair.Key;
            break;
        }

        if (isLockedByDefault == false)
            return true;

        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        if (metaTreeTable == null)
            return false;

        for (int i = 0; i < metaTreeTable.list.Count; ++i)
        {
            MetaTreeRecord node = metaTreeTable.list[i];
            if (node.EffectType != eMetaEffectType.UnlockCard || node.EffectParam != requiredParam)
                continue;

            return PlayerManager.instance.playerData.UnlockedMetaNodes.Contains(node.Id);
        }

        return false;
    }

    private bool HasRequiredWeapon(CardRecord _record)
    {
        if (WEAPON_REQUIRED_CARD_IDS.TryGetValue(_record.Id, out int requiredTowerRecordId) == false)
            return true;

        return InGameScene.Current.towerController.HasWeapon(requiredTowerRecordId);
    }

    // Epic/Legendary는 원래 한 런에 한 장만(유니크) — REPEATABLE_CARD_IDS에 등록된 카드(Double Shot #107)만 예외로 반복 드래프트 허용.
    private bool IsUniqueCard(CardRecord _record)
    {
        if (REPEATABLE_CARD_IDS.Contains(_record.Id) == true)
            return false;

        return (_record.Rarity == eCardRarity.Epic || _record.Rarity == eCardRarity.Legendary);
    }

    public int GetMaxRerolls()
    {
        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        return (metaTreeTable != null) ? metaTreeTable.GetTotalEffectValue(eMetaEffectType.RerollCount, PlayerManager.instance.playerData.UnlockedMetaNodes) : 0;
    }

    public int GetRemainingRerolls()
    {
        return Mathf.Max(0, GetMaxRerolls() - m_RerollsUsed);
    }

    public bool CanReroll()
    {
        return GetRemainingRerolls() > 0;
    }

    public void UseReroll()
    {
        m_RerollsUsed++;
    }

    public bool CanSkip()
    {
        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        return metaTreeTable != null && metaTreeTable.GetTotalEffectValue(eMetaEffectType.SkipEnable, PlayerManager.instance.playerData.UnlockedMetaNodes) > 0;
    }

    public void Skip()
    {
        PlayerManager.instance.AddCurrency(eCurrencyType.Shard, GameConfigTable.SKIP_SHARD_REWARD);
    }

    public void ApplyCard(CardRecord _record)
    {
        m_ObtainedCardIds.Add(_record.Id);

        if (IsUniqueCard(_record) == true)
            m_ObtainedUniqueIds.Add(_record.Id);

        if (m_CategoryCounts.ContainsKey(_record.Category) == false)
            m_CategoryCounts[_record.Category] = 0;
        m_CategoryCounts[_record.Category]++;

        ApplyCategorySynergy(_record.Category);
        ApplyCardEffect(_record);
    }

    private void ApplyCategorySynergy(eCardCategory _category)
    {
        int count = m_CategoryCounts[_category];

        int[] tiers = { 3, 5, 7 };
        for (int i = 0; i < tiers.Length; ++i)
        {
            int tier = tiers[i];
            if (count < tier)
                continue;

            SynergyTierKey tierKey = new SynergyTierKey { Category = _category, Tier = tier };
            if (m_GrantedSynergyTiers.Contains(tierKey) == true)
                continue;

            m_GrantedSynergyTiers.Add(tierKey);
            GrantSynergyBonus(_category, tier);
        }
    }

    // Assets/Design/04_card.html "카테고리 시너지" 표 — 임계값을 넘을 때마다 추가로 누적되는 보너스
    private void GrantSynergyBonus(eCardCategory _category, int _tier)
    {
        if (InGameScene.Current.towerController == null)
            return;

        switch (_category)
        {
            case eCardCategory.Offense:
                if (_tier == 3)
                    InGameScene.Current.towerController.AddCardDamagePercent(10f);
                else if (_tier == 5)
                    InGameScene.Current.towerController.AddCardDamagePercent(25f);
                else if (_tier == 7)
                    InGameScene.Current.towerController.AddCardCritChance(25f);
                break;

            case eCardCategory.Speed:
                if (_tier == 3)
                    InGameScene.Current.towerController.AddCardAttackSpeedPercent(10f);
                else if (_tier == 5)
                    InGameScene.Current.towerController.AddCardAttackSpeedPercent(25f);
                else if (_tier == 7)
                    InGameScene.Current.towerController.AddProjectileCount(1);
                break;

            case eCardCategory.Utility:
                if (_tier == 3)
                    InGameScene.Current.towerController.AddCardRangePercent(10f);
                else if (_tier == 5)
                    InGameScene.Current.towerController.AddPierce(1);
                else if (_tier == 7)
                    InGameScene.Current.towerController.SetSplash(1.0f);
                break;

            case eCardCategory.Defense:
                if (InGameScene.Current.towerController == null)
                    break;

                if (_tier == 3)
                    InGameScene.Current.towerController.AddMaxHp(20);
                else if (_tier == 5)
                    InGameScene.Current.towerController.AddHealPerSecond(0.5f);
                else if (_tier == 7)
                    InGameScene.Current.towerController.AddDamageTakenReductionPercent(20f);
                break;
        }
    }

    private void ApplyCardEffect(CardRecord _record)
    {
        if (InGameScene.Current.towerController == null || InGameScene.Current.towerController == null)
        {
            Logger.Error($"[CardManager] ApplyCardEffect Failed! ActorPlayer/TowerHealth not ready - Id:{_record.Id}");
            return;
        }

        switch (_record.EffectType)
        {
            case eCardEffectType.DamagePercent:
                InGameScene.Current.towerController.AddCardDamagePercent(_record.EffectValue);
                break;

            case eCardEffectType.CritChance:
                InGameScene.Current.towerController.AddCardCritChance(_record.EffectValue);
                break;

            case eCardEffectType.CritMultiplier:
                InGameScene.Current.towerController.AddCardCritMultiplier(_record.EffectValue);
                break;

            case eCardEffectType.PierceAdd:
                InGameScene.Current.towerController.AddPierce(Mathf.RoundToInt(_record.EffectValue));
                break;

            case eCardEffectType.DoubleShot:
                InGameScene.Current.towerController.AddProjectileCount(1);
                break;

            case eCardEffectType.SpeciesBonusDamage:
                if (Enum.TryParse(_record.EffectParam, out eEnemySpecies species) == true)
                    InGameScene.Current.towerController.SetSpeciesBonusDamage(species, _record.EffectValue);
                break;

            // Overdrive(#205) 전용 — AS +100%(EffectValue) / DMG -30%(EffectParam, 음수 문자열)
            case eCardEffectType.AttackSpeedPercent:
                InGameScene.Current.towerController.AddCardAttackSpeedPercent(_record.EffectValue);
                if (string.IsNullOrEmpty(_record.EffectParam) == false && float.TryParse(_record.EffectParam, out float overdriveDamagePercent) == true)
                    InGameScene.Current.towerController.AddCardDamagePercent(overdriveDamagePercent);
                break;

            // Hypersonic(#204) 전용 — Proj Speed +60%(EffectValue) / Range +20%(EffectParam)
            case eCardEffectType.ProjectileSpeedPercent:
                InGameScene.Current.towerController.AddCardProjectileSpeedPercent(_record.EffectValue);
                if (string.IsNullOrEmpty(_record.EffectParam) == false && float.TryParse(_record.EffectParam, out float hypersonicRangePercent) == true)
                    InGameScene.Current.towerController.AddCardRangePercent(hypersonicRangePercent);
                break;

            case eCardEffectType.RangePercent:
                InGameScene.Current.towerController.AddCardRangePercent(_record.EffectValue);
                break;

            case eCardEffectType.SplashEnable:
                InGameScene.Current.towerController.SetSplash(_record.EffectValue);
                break;

            case eCardEffectType.ChainEnable:
                float chainRadius = (string.IsNullOrEmpty(_record.EffectParam) == false && float.TryParse(_record.EffectParam, out float parsedChainRadius) == true) ? parsedChainRadius : 2f;
                InGameScene.Current.towerController.SetChain(Mathf.RoundToInt(_record.EffectValue), chainRadius);
                break;

            case eCardEffectType.HomingEnable:
                InGameScene.Current.towerController.SetHoming();
                break;

            case eCardEffectType.LaserDurationAdd:
                InGameScene.Current.towerController.SetLaserDuration(_record.EffectValue);
                break;

            // Fortify(#402) 전용 — MaxHp +50(EffectValue)에 더해 그만큼 즉시 회복(AddMaxHp가 이미 델타만큼 자동 회복하므로 별도 처리 불필요)
            case eCardEffectType.MaxHpAdd:
                InGameScene.Current.towerController.AddMaxHp(Mathf.RoundToInt(_record.EffectValue));
                break;

            // Glass Cannon(#501) 전용 — MaxHp -50%(EffectValue) / DMG +150%(EffectParam, = ×2.5)
            case eCardEffectType.MaxHpPercent:
                InGameScene.Current.towerController.AddMaxHpPercent(_record.EffectValue);
                if (string.IsNullOrEmpty(_record.EffectParam) == false && float.TryParse(_record.EffectParam, out float glassCannonDamagePercent) == true)
                    InGameScene.Current.towerController.AddCardDamagePercent(glassCannonDamagePercent);
                break;

            case eCardEffectType.HealPerSecond:
                InGameScene.Current.towerController.AddHealPerSecond(_record.EffectValue);
                break;

            case eCardEffectType.ShieldBurstThreshold:
                InGameScene.Current.towerController.SetShieldBurstThreshold(_record.EffectValue);
                break;

            case eCardEffectType.LifestealOnKill:
                m_hasVampire = true;
                m_VampireChancePercent = _record.EffectValue;
                m_VampireHealAmount = (string.IsNullOrEmpty(_record.EffectParam) == false && float.TryParse(_record.EffectParam, out float parsedHealAmount) == true) ? parsedHealAmount : 1f;
                break;

            case eCardEffectType.ReviveOnce:
                InGameScene.Current.towerController.SetReviveOnce(_record.EffectValue);
                break;

            case eCardEffectType.BerserkerCurve:
                InGameScene.Current.towerController.SetBerserker(_record.EffectValue);
                break;

            case eCardEffectType.OrbitalRing:
                if (InGameScene.Current.projectileManager != null)
                {
                    int orbitalCount = Mathf.RoundToInt(_record.EffectValue);
                    int orbitalDamage = Mathf.RoundToInt(InGameScene.Current.towerController.GetShieldBurstDamage());
                    InGameScene.Current.projectileManager.SpawnOrbitals(InGameScene.Current.towerController.transform.position, orbitalCount, orbitalDamage, 0.3f, 1.5f);
                }
                break;

            case eCardEffectType.TimeSlowAura:
                CardEffectState.TimeSlowMultiplier = 1f - (_record.EffectValue / 100f);
                break;

            // 무기 해금 카드 — EffectValue = TowerTable(무기 정의)의 Id, 독립 쿨다운/타겟팅을 갖는 무기 슬롯을 추가
            case eCardEffectType.WeaponUnlock:
                InGameScene.Current.towerController.AddWeapon(Mathf.RoundToInt(_record.EffectValue));
                break;
        }
    }

    private void OnMonsterKilledForVampire(RewardData _reward)
    {
        if (m_hasVampire == false)
            return;

        if (UnityEngine.Random.value < m_VampireChancePercent / 100f)
            InGameScene.Current.towerController?.Heal(Mathf.RoundToInt(m_VampireHealAmount));
    }

    private void OnDestroy()
    {
        if (m_isInitialized == false)
            return;

        if (InGameScene.Current != null && InGameScene.Current.monsterManager != null)
            InGameScene.Current.monsterManager.OnMonsterDie -= OnMonsterKilledForVampire;

        CardEffectState.Reset();
    }
}
