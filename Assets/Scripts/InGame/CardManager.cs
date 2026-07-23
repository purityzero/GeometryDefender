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

    private List<CardRecord> m_AllCards;
    private List<int> m_ObtainedCardIds = new List<int>();
    private HashSet<int> m_ObtainedUniqueIds = new HashSet<int>();
    private Dictionary<eCardCategory, int> m_CategoryCounts = new Dictionary<eCardCategory, int>();
    private HashSet<(eCardCategory Category, int Tier)> m_GrantedSynergyTiers = new HashSet<(eCardCategory, int)>();

    private int m_PitySinceEpic;
    private int m_RerollsUsed;

    private bool m_hasVampire;
    private float m_VampireChancePercent;

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

    private static readonly (eCardRarity Rarity, float Weight)[] RARITY_WEIGHTS = new[]
    {
        (eCardRarity.Common, 60f),
        (eCardRarity.Rare, 25f),
        (eCardRarity.Epic, 12f),
        (eCardRarity.Legendary, 3f),
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

            bool isUnique = (record.Rarity == eCardRarity.Epic || record.Rarity == eCardRarity.Legendary);
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

        bool isUnique = (_record.Rarity == eCardRarity.Epic || _record.Rarity == eCardRarity.Legendary);
        if (isUnique == true)
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

            if (m_GrantedSynergyTiers.Contains((_category, tier)) == true)
                continue;

            m_GrantedSynergyTiers.Add((_category, tier));
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
            Logger.Error($"[CardManager] ApplyCardEffect Failed! TowerController/TowerHealth not ready - Id:{_record.Id}");
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

            case eCardEffectType.TargetingOverride:
                if (Enum.TryParse(_record.EffectParam, out eTargetingType targetingType) == true)
                    InGameScene.Current.towerController.SetTargetingStrategy(targetingType);
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
        }
    }

    private void OnMonsterKilledForVampire(RewardData _reward)
    {
        if (m_hasVampire == false)
            return;

        if (UnityEngine.Random.value < m_VampireChancePercent / 100f)
            InGameScene.Current.towerController?.Heal(1);
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
