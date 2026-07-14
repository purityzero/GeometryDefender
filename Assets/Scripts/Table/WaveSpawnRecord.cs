using System.Collections.Generic;

public class WaveSpawnRecord : Record
{
    public int SpawnTime;
    public int EnemyId;
}

public class WaveSpawnTable : Table<WaveSpawnRecord>
{
    public WaveSpawnTable(List<WaveSpawnRecord> _listRecord) : base(_listRecord) { }

    public WaveSpawnRecord GetBossEventAtTime(int _elapsedSeconds)
    {
        return list.Find(record => record.SpawnTime == _elapsedSeconds);
    }
}
