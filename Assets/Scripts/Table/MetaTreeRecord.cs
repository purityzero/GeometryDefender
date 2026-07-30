using System.Collections.Generic;

public enum eMetaBranch
{
    StartingPower,
    CardPool,
    Economy,
    Utility
}

public enum eMetaEffectType
{
    MaxHp,
    DamagePercent,
    RangePercent,
    AttackSpeedPercent,
    UnlockCard,
    XpPercent,
    ShardPercent,
    RerollCount,
    WeaponSlotCount,
    WeaponDamagePercent
}

public class MetaTreeRecord : Record
{
    public string DisplayName;
    public eMetaBranch Branch;
    public eMetaEffectType EffectType;
    public int EffectValue;
    public string EffectParam;
    public int Cost;
    public int PrereqId;
}

public class MetaTreeTable : Table<MetaTreeRecord>
{
    public Dictionary<eMetaBranch, List<MetaTreeRecord>> dicBranch { get; private set; }

    public MetaTreeTable(List<MetaTreeRecord> _listRecord) : base(_listRecord)
    {
        BuildDictionaries();
    }

    private void BuildDictionaries()
    {
        dicBranch = new Dictionary<eMetaBranch, List<MetaTreeRecord>>();

        for (int i = 0; i < list.Count; ++i)
        {
            MetaTreeRecord record = list[i];

            if (dicBranch.ContainsKey(record.Branch) == false)
                dicBranch[record.Branch] = new List<MetaTreeRecord>();
            dicBranch[record.Branch].Add(record);
        }
    }

    public MetaTreeRecord GetRecordById(int _id)
    {
        return list.Find(record => record.Id == _id);
    }

    public bool IsUnlockable(int _id, List<int> _unlockedIds)
    {
        MetaTreeRecord record = GetRecordById(_id);
        if (record == null)
            return false;

        if (_unlockedIds.Contains(_id) == true)
            return false;

        if (record.PrereqId == 0)
            return true;

        return _unlockedIds.Contains(record.PrereqId);
    }

    public bool IsUnlocked(int _id, List<int> _unlockedIds)
    {
        return _unlockedIds.Contains(_id);
    }

    // 해금된 노드들 중 지정한 효과 타입의 EffectValue를 전부 합산 — 영구 업그레이드 스탯 적용에 사용
    public int GetTotalEffectValue(eMetaEffectType _effectType, List<int> _unlockedIds)
    {
        int total = 0;

        for (int i = 0; i < _unlockedIds.Count; ++i)
        {
            MetaTreeRecord record = GetRecordById(_unlockedIds[i]);
            if (record == null)
                continue;

            if (record.EffectType == _effectType)
                total += record.EffectValue;
        }

        return total;
    }

    // GetTotalEffectValue와 동일하지만 EffectParam까지 일치해야 합산 — 무기별 개별 강화(WeaponDamagePercent, EffectParam=TowerRecord.Id 문자열)처럼
    // 같은 EffectType을 여러 무기가 각자 다른 EffectParam으로 나눠 쓰는 경우에 사용(2026-07-30, [[ActorPlayer]] 참고).
    public int GetTotalEffectValueForParam(eMetaEffectType _effectType, string _effectParam, List<int> _unlockedIds)
    {
        int total = 0;

        for (int i = 0; i < _unlockedIds.Count; ++i)
        {
            MetaTreeRecord record = GetRecordById(_unlockedIds[i]);
            if (record == null)
                continue;

            if (record.EffectType == _effectType && record.EffectParam == _effectParam)
                total += record.EffectValue;
        }

        return total;
    }
}
