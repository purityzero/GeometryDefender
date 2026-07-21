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
    UnlockCard,
    XpMagnetPercent,
    XpPercent,
    ShardPercent,
    RerollCount,
    SkipEnable
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
}
