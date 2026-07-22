using System.Collections.Generic;

public class DifficultyRecord : Record
{
    public string DisplayName;
    public eDifficultyLevel Level;
    public float DifficultyMultiplier;
    public float ShardMultiplier;
    public float InfiniteStepSeconds;
    public float InfiniteStepAmount;
    public int NextId;
}

public class DifficultyTable : Table<DifficultyRecord>
{
    public DifficultyTable(List<DifficultyRecord> _listRecord) : base(_listRecord) { }

    public DifficultyRecord GetRecordById(int _id)
    {
        return list.Find(record => record.Id == _id);
    }

    public DifficultyRecord GetRecordByLevel(eDifficultyLevel _level)
    {
        return list.Find(record => record.Level == _level);
    }

    public eDifficultyLevel? GetNextLevel(eDifficultyLevel _level)
    {
        DifficultyRecord record = GetRecordByLevel(_level);
        if (record == null || record.NextId <= 0)
            return null;

        DifficultyRecord nextRecord = GetRecordById(record.NextId);
        return (nextRecord != null) ? (eDifficultyLevel?)nextRecord.Level : null;
    }
}
